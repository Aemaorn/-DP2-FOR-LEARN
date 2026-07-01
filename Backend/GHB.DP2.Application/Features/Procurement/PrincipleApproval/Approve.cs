namespace GHB.DP2.Application.Features.Procurement.PrincipleApproval;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.PrincipleApproval.Abstract;
using GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ApprovePrincipleApprovalRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid Id,
    string? Remark);

public class ApproveEndpoint : PrincipleApprovalEndpointBase<ApprovePrincipleApprovalRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly ILoggerFactory loggerFactory;
    private readonly IFileServiceClient fileServiceClient;

    public ApproveEndpoint(
        Dp2DbContext dbContext,
        ILogger<ApproveEndpoint> logger,
        ILoggerFactory loggerFactory,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
        this.loggerFactory = loggerFactory;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/PrincipleApproval")
             .WithName("ApprovePrincipleApproval")
             .Accepts<ApprovePrincipleApprovalRequest>("application/json"));
        this.Post("procurement/{procurementId:guid}/principle-approval/{id:guid}/approve");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApprovePrincipleApprovalRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PPrincipleApprovals
                               .Include(x => x.PrincipleApprovalAcceptors)
                               .Include(p => p.Procurement)
                               .Include(pPrincipleApproval => pPrincipleApproval.PrincipleApprovalAssignees)
                               .FirstOrDefaultAsync(x => x.Id == PPrincipleApprovalId.From(req.Id) && x.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        if (entity == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลอนุมัติหลักการ");
        }

        var type = entity.Status == PPrincipleApprovalStatus.WaitingUnitApproval
            ? AcceptorType.DepartmentDirectorAgree
            : AcceptorType.Approver;

        var acceptors = entity.PrincipleApprovalAcceptors
                              .Where(a => a.IsActive && a.Type == type)
                              .Select(DelegatorExtensions.DelegatorToAcceptor)
                              .OrderBy(a => a.Sequence)
                              .ToList();

        var current = acceptors.FirstOrDefault(a => (a.Delegatee?.SuUserId == null
                                                        ? a.UserId == req.UserId
                                                        : a.Delegatee?.SuUserId == UserId.From(req.UserId))
                                                     && a.Status == AcceptorStatus.Pending);

        if (current == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้อนุมัติในรายการนี้");
        }

        if (!IsGroupAllowedToApprove(entity.Status, current.Type))
        {
            return TypedResults.BadRequest(GetGroupNotAllowedMessage(entity.Status));
        }

        if (!IsPreviousApproved(acceptors, current))
        {
            return TypedResults.BadRequest("ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน");
        }

        switch (entity.Status)
        {
            case PPrincipleApprovalStatus.WaitingAcceptance:
                entity.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.Approved,
                        "ผู้มีอำนาจเห็นชอบ/อนุมัติ",
                        entity.Status.ToString(),
                        req.Remark));

                break;

            case PPrincipleApprovalStatus.WaitingUnitApproval:
                entity.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.UnitApproved,
                        "สายงานเห็นชอบ/อนุมัติ",
                        entity.Status.ToString(),
                        req.Remark));

                break;
        }

        var currentAcceptorUser = entity.PrincipleApprovalAcceptors.FirstOrDefault(a => a.Id == current.Id);

        if (currentAcceptorUser == null)
        {
            this.ThrowError(
                    "ไม่พบผู้อนุมัติที่ใช้งานได้",
                    StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(current.DelegateeId)
            .Approve(remark: req.Remark);

        UpdateSequentialCurrents(entity);

        if (ShouldUpdateStatus(current.Type, [.. entity.PrincipleApprovalAcceptors.Where(a => a.IsActive && a.Type == type)]))
        {
            UpdateStatusAndSendNotification(entity, current);

            await this.CreatePrincipleApprovalRentalFromApproval(entity, current, ct, UserId.From(req.UserId));
        }

        this.dbContext.PPrincipleApprovals.Update(entity);

        if (entity.Status is PPrincipleApprovalStatus.Approved or PPrincipleApprovalStatus.WaitingAcceptance)
        {
            await this.ReplaceDocumentsAsync(entity, true, true, ct);
        }

        await this.dbContext.SaveChangesAsync(ct);

        if (entity.Status == PPrincipleApprovalStatus.Approved)
        {
            await SendNotificationCommitteeAsync(entity);
        }

        return TypedResults.Ok();
    }

    private static void UpdateStatusAndSendNotification(PPrincipleApproval entity, PPrincipleApprovalAcceptor current)
    {
        if (current.Type != AcceptorType.DepartmentDirectorAgree)
        {
            return;
        }

        entity.PrincipleApprovalAssignees.Iter(r => r.Pending());
        entity.SetStatus(PPrincipleApprovalStatus.WaitingAssign);
        var directorAssignee = entity.PrincipleApprovalAssignees.FirstOrDefault(x => x.Type == AssigneeType.Director);

        if (directorAssignee is not null)
        {
            foreach (var targetUserId in directorAssignee.GetAssigneeNotificationTargets())
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.WaitForAssignment.Title,
                    string.Format(NotificationConstant.WaitForAssignment.Message, ProgramConstant.BranchSpaceRent.Name, entity.Procurement.ProcurementNumber));
            }
        }
    }

    private static bool IsPreviousApproved(List<PPrincipleApprovalAcceptor> acceptors, PPrincipleApprovalAcceptor current)
    {
        if (current.Sequence <= 1)
        {
            return true;
        }

        var prev = acceptors.LastOrDefault(a => a.Sequence < current.Sequence && a.IsActive);

        return prev == null || prev.Status == AcceptorStatus.Approved;
    }

    private static bool IsGroupAllowedToApprove(PPrincipleApprovalStatus status, AcceptorType group)
    {
        if (status == PPrincipleApprovalStatus.WaitingUnitApproval && group != AcceptorType.DepartmentDirectorAgree)
        {
            return false;
        }

        if (status == PPrincipleApprovalStatus.WaitingAcceptance && group != AcceptorType.Approver)
        {
            return false;
        }

        return true;
    }

    private static string GetGroupNotAllowedMessage(PPrincipleApprovalStatus status)
    {
        if (status == PPrincipleApprovalStatus.WaitingAcceptance)
        {
            return "อนุมัติได้เฉพาะผู้มีอำนาจเห็นชอบ/อนุมัติเท่านั้น";
        }

        return "ไม่สามารถอนุมัติในสถานะนี้ได้";
    }

    private static bool ShouldUpdateStatus(AcceptorType type, List<PPrincipleApprovalAcceptor> acceptors)
    {
        return acceptors
            .All(a => a.Type == type && a.Status == AcceptorStatus.Approved);
    }

    private async Task CreatePrincipleApprovalRentalFromApproval(PPrincipleApproval entity, PPrincipleApprovalAcceptor current, CancellationToken ct, UserId sendToAcceptorId)
    {
        if (current.Type != AcceptorType.Approver)
        {
            return;
        }

        entity.SetStatus(PPrincipleApprovalStatus.Approved);
        entity.Procurement.SetProcessType(ProcessType.PrincipleApprovalRental);

        var procurementId = entity.ProcurementId.Value;
        var useContract = UseContractType.CentralContract;
        var status = PPrincipleApprovalRentalStatus.Draft;

        var perftSupportData = entity?.PerfSupportData.FirstOrDefault();

        var jorporDirector = await this.dbContext.RawEmployeePositions
                                       .Include(p => p.Employee)
                                       .ThenInclude(e => e.View)
                                       .Where(p =>
                                           p.BusinessUnit.BusinessUnitCode == JorPor.DefaultDirector.BusinessUnitCode &&
                                           p.Position.InRefCode == JorPor.DefaultDirector.PositionInRefCode)
                                       .SelectMany(p => p.Employee.Users)
                                       .FirstOrDefaultAsync(ct);

        if (jorporDirector?.Employee?.View is null)
        {
            this.ThrowError("ไม่สามารถกำหนดข้อมูลเจ้าหน้าที่พัสดุให้ความเห็นได้เนื่องจาก ไม่พบข้อมูลผอ. จพ.", StatusCodes.Status404NotFound);
        }

        List<AssigneeRequest> assignee = new List<AssigneeRequest>
        {
            new(
                null,
                AssigneeGroup.JorPor,
                AssigneeType.Director,
                jorporDirector.Id.Value,
                1),
            new(
                null,
                AssigneeGroup.Contract,
                AssigneeType.Director,
                jorporDirector.Id.Value,
                1),
        };

        List<AcceptorRequest> acceptor = new List<AcceptorRequest>();

        if (entity?.PrincipleApprovalCommittees != null)
        {
            foreach (var committee in entity.PrincipleApprovalCommittees.Where(w => w.GroupType is CommitteeGroupType.RentCommittee))
            {
                acceptor.Add(new AcceptorRequest(
                    null,
                    AcceptorType.RentCommittee,
                    committee.User.Id.Value,
                    committee.Sequence,
                    committee.CommitteePositionsCode.IsNull() ? null : (string?)committee.CommitteePositionsCode.Value));
            }
        }

        var request = new GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.CreatePrincipleApprovalRentalRequest(
            sendToAcceptorId.Value,
            procurementId,
            useContract,
            status,
            entity!.BranchLocation,
            entity.RentTypeCode.Value,
            entity.RentalStartDate,
            entity.RentalEndDate,
            entity.RentalDurationYear,
            entity.RentalDurationMonth,
            entity.RentalDurationDay,
            entity.MaxMonthlyRent,
            entity.TotalRentalAmount,
            entity.ExpectedContractDate,
            entity.RentalLocationDetails,
            entity.SubDistrictCode,
            entity.SubDistrictName,
            entity.DistrictCode,
            entity.DistrictName,
            entity.ProvinceCode,
            entity.ProvinceName,
            entity.ReferencePriceAmount, // ReferencePriceAmount
            entity.AnalysisSummaryNpv, // AnalysisSummaryNpv
            entity.AnalysisSummaryPaybackYearPeriod, // AnalysisSummaryPaybackYearPeriod
            entity.AnalysisSummaryPaybackYearPeriod, // AnalysisSummaryDiscountedPaybackYearPeriod
            entity.PhoneNumber, // PhoneNumber
            acceptor, // Acceptors
            assignee, // Assignees
            new PerfSupportDataRequest(
                null,
                perftSupportData!.TransactionVolume,
                perftSupportData.ActivityDescription,
                perftSupportData.PeriodYear,
                perftSupportData.StartMonth,
                perftSupportData.EndMonth), // PerfSupportData
            entity.PerfSupportDataDetails
                  .Select(d => new PerfSupportDataDetailsRequest(
                      null,
                      d.Sequence,
                      d.ActivityDescription,
                      d.AccountCountYear1,
                      d.AmountYear1,
                      d.AccountCountYear2,
                      d.AmountYear2)), // PerfSupportDataDetails
            entity.RoiLoanAndDepositSummaries
                  .Select(r => new RoiLoanAndDepositSummaryRequest(
                      null,
                      r.Sequence,
                      r.ActivityDescription,
                      r.AmountYear1,
                      r.AmountYear2,
                      r.AmountYear3)), // RoiLoanAndDepositSummaries
            entity.RoiPerfResults
                  .Select(r => new RoiPerfResultRequest(
                      null,
                      r.Sequence,
                      r.PerformanceResultGroup,
                      r.Year,
                      r.AccountActual,
                      r.AccountGrowth,
                      r.AmountTarget,
                      r.AmountActual,
                      r.AmountRate,
                      r.AmountGrowth)), // RoiPerfResults
            entity.PrincipleApprovalBudgets
                  .Select(r => new BudgetRequest(
                      null,
                      r.Sequence,
                      r.Description,
                      r.BudgetAmount,
                      [.. r.PrincipleApprovalBudgetDetails
                       .Select(d => new BudgetDetail(
                           null,
                           d.Sequence,
                           d.Department,
                           d.BudgetType,
                           d.ProjectCode,
                           d.AccountNo,
                           d.Budget))])), // Budgets
            entity.PrincipleApprovalRentalAnalyses
                  .Select(r => new RentalAnalysisRequest(
                      null,
                      r.Sequence,
                      r.Type,
                      r.Description,
                      [.. r.PrincipleApprovalRentalAnalysisDetails
                       .Select(d => new RentalAnalysisDetail(
                           null,
                           d.Year,
                           d.Amount))])), // RentalAnalysis
            null, // Entrepreneurs
            null); // Attachments

        // Is this really a responsibility of the endpoint?
        var createEndpoint =
            new GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.CreatePrincipleApprovalRentalEndpoint(
                this.loggerFactory.CreateLogger<GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.CreatePrincipleApprovalRentalEndpoint>(),
                this.dbContext,
                this.fileServiceClient);

        await createEndpoint.CreateEntityAsync(request, ct);
    }

    private static void UpdateSequentialCurrents(PPrincipleApproval entity)
    {
        var approvers = entity.PrincipleApprovalAcceptors
                              .Where(a => a.IsActive)
                              .OrderBy(a => a.Sequence)
                              .ToList();

        if (approvers.Count == 0)
        {
            return;
        }

        foreach (var a in approvers)
        {
            a.SetCurrent(false);
        }

        var next = approvers.FirstOrDefault(a =>
            a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        next.SetCurrent(true);

        var pendingOfType = approvers.Where(a => a.Status == AcceptorStatus.Pending).ToList();
        var isLastPending = pendingOfType.Count == 1;

        if (next.Type == AcceptorType.DepartmentDirectorAgree ||
            (next.Type == AcceptorType.Approver && !isLastPending))
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.BranchSpaceRent.Name, entity.Procurement.ProcurementNumber));
            }
        }
        else if (next.Type == AcceptorType.Approver && isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.BranchSpaceRent.Name, entity.Procurement.ProcurementNumber));
            }
        }
    }

    private static async Task SendNotificationAsync(PPrincipleApproval entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.BranchSpaceRent.Url, entity.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationCommitteeAsync(PPrincipleApproval entity)
    {
        await entity.PrincipleApprovalCommittees.Where(w => w.GroupType == CommitteeGroupType.RentCommittee).Map(pa =>
                           Notification
                               .Crate(
                                   pa.SuUserId,
                                   NotificationConstant.PrincipleApprovalRentalCommittee.Title,
                                   string.Format(NotificationConstant.PrincipleApprovalRentalCommittee.Message, ProgramConstant.BranchSpaceRent.Name, entity.Procurement.ProcurementNumber),
                                   NotificationProgram.Procurement)
                               .SetReferenceId(entity.Id.Value)
                               .SetLinkUrl(
                                   string.Format(ProgramConstant.BranchSpaceRent.Url, entity.Procurement.Id),
                                   "ดูรายละเอียด"))
                       .Map(n => n.PublishAsync(CancellationToken.None).ToUnit())
                       .SequenceSerial();
    }
}