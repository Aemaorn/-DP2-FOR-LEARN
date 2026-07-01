namespace GHB.DP2.Application.Features.Procurement.MedianPrice;

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.AnnouncementInfo;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.MedianPrice.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class ApproveMedianPriceRequest
{
    public Guid ProcurementId { get; init; }

    public Guid MedianPriceId { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public string? Remark { get; init; }
}

public class ApproveMedianPriceEndpoint : MedianPriceEndpointBase<ApproveMedianPriceRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public ApproveMedianPriceEndpoint(
        ILogger<ApproveMedianPriceEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/median-price/{MedianPriceId:guid}/approve");
        this.Options(b =>
            b.WithTags(nameof(MedianPrice))
             .WithName("ApproveMedianPrice")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApproveMedianPriceRequest req, CancellationToken ct)
    {
        // Fetch median price data
        var medianPrice = await this.FetchMedianPriceAsync(req, ct);

        if (medianPrice == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลราคากลาง");
        }

        var previousStatus = medianPrice.Status;

        // Validate current status
        this.ValidateMedianPriceStatus(medianPrice);

        switch (medianPrice.Status)
        {
            case MedianPriceStatus.WaitingCommitteeApproval:
                this.CommitteeApproval(medianPrice, req);
                medianPrice.AddActivity(new ActivityInfo(
                    "บุคคล/คณะกรรมการจัดทำร่างขอบเขตของงานเห็นชอบ",
                    $"คณะกรรมการเห็นชอบ/อนุมัติ",
                    medianPrice.Status.ToString(),
                    req.Remark));

                break;

            case MedianPriceStatus.WaitingUnitApproval:
                this.DepartmentDirectorApproval(medianPrice, req);
                medianPrice.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.UnitApproved,
                    $"ลำดับเห็นชอบ/อนุมัติ",
                    medianPrice.Status.ToString(),
                    req.Remark));

                break;

            case MedianPriceStatus.WaitingApproval:
                this.ApproverApproval(medianPrice, req);
                medianPrice.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Approved,
                    $"ผู้มีอำนาจเห็นชอบ/อนุมัติ",
                    medianPrice.Status.ToString(),
                    req.Remark));

                break;

            default:
                this.ThrowError(
                    "กลุ่มการอนุมัติไม่รองรับ",
                    StatusCodes.Status400BadRequest);

                break;
        }

        medianPrice.EvaluateAcceptorApproval();

        var isReplace = previousStatus == MedianPriceStatus.WaitingApproval ? true : false;

        switch (medianPrice.Status)
        {
            case MedianPriceStatus.WaitingUnitApproval:
                UpdateSequentialCurrents(medianPrice, AcceptorType.DepartmentDirectorAgree);

                break;

            case MedianPriceStatus.WaitingApproval:
                UpdateSequentialCurrents(medianPrice, AcceptorType.Approver);

                break;

            case MedianPriceStatus.WaitingCommitteeApproval:
                UpdateCommitteeCurrents(medianPrice);
                isReplace = true;

                break;

            case MedianPriceStatus.WaitingAssign:
                if (medianPrice.Assignees.Any())
                {
                    var directorAssignee = medianPrice.Assignees.Select(DelegatorExtensions.DelegatorToAssignee)
                                                      .FirstOrDefault(x => x.Type == AssigneeType.Director);

                    if (directorAssignee is not null)
                    {
                        foreach (var targetUserId in directorAssignee.GetAssigneeNotificationTargets())
                        {
                            _ = SendNotificationAsync(
                                medianPrice,
                                targetUserId,
                                NotificationConstant.WaitForAssignment.Title,
                                string.Format(NotificationConstant.WaitForAssignment.Message, GetMdpName(medianPrice), medianPrice.ReferenceNumber));
                        }
                    }
                }

                break;

            case MedianPriceStatus.Approved:
                {
                    if (medianPrice.Procurement.HasMd && medianPrice.Assignees.Any())
                    {
                        _ = SendNotificationAssigneeAsync(medianPrice, CancellationToken.None);
                    }

                    medianPrice.BudgetAllocation.SetReferenceDate();

                    var planType = medianPrice.Procurement.SupplyMethod.Code == SupplyMethodConstant.Sixty ? Section60.MedianPrice : Section80.MedianPrice;

                    var file = await this.fileServiceClient.DownloadAsStreamAsync(medianPrice.LastedDocument!.FileId, cancellationToken: ct);

                    await AnnouncementData.Create(
                                              medianPrice.Procurement.Name,
                                              DateTimeOffset.UtcNow,
                                              medianPrice.Procurement.Budget ?? decimal.Zero,
                                              string.Empty,
                                              planType,
                                              file?.Stream)
                                          .PublishEvent(ct);

                    // Send notification to all relevant stakeholders about final approval
                    var allStakeholders = medianPrice.Acceptors
                                                     .Where(a => a.Type == AcceptorType.MedianPriceCommittee && a.IsActive)
                                                     .Select(a => a.UserId)
                                                     .Distinct()
                                                     .ToList();

                    foreach (var stakeholderId in allStakeholders)
                    {
                        _ = SendNotificationAsync(
                            medianPrice,
                            stakeholderId,
                            NotificationConstant.InformCommittee.Title,
                            string.Format(
                                NotificationConstant.InformCommittee.Message,
                                GetMdpName(medianPrice),
                                medianPrice.ReferenceNumber));
                    }

                    break;
                }
        }

        this.dbContext.PpMedianPrices.Update(medianPrice);

        await this.ReplaceDocumentsAsync(medianPrice, isReplace, ct, previousStatus);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<PpMedianPrice?> FetchMedianPriceAsync(ApproveMedianPriceRequest req, CancellationToken ct)
    {
        return await this.dbContext.PpMedianPrices
                         .Include(mp => mp.Acceptors)
                         .ThenInclude(a => a.User)
                         .ThenInclude(u => u.Employee)
                         .Include(mp => mp.Assignees)
                         .ThenInclude(a => a.User)
                         .ThenInclude(u => u.Employee)
                         .Include(mp => mp.Procurement)
                         .SingleOrDefaultAsync(
                             mp =>
                                 mp.Id == MedianPriceId.From(req.MedianPriceId) &&
                                 mp.ProcurementId == ProcurementId.From(req.ProcurementId),
                             ct);
    }

    private void CommitteeApproval(PpMedianPrice medianPrice, ApproveMedianPriceRequest req)
    {
        var committeeAcceptors =
            medianPrice.Acceptors
                       .Where(a =>
                           a is
                           {
                               Type: AcceptorType.MedianPriceCommittee,
                               Status: AcceptorStatus.Pending,
                               IsUnableToPerformDuties: false,
                               IsActive: true
                           })
                       .ToArray();

        var acceptor =
            committeeAcceptors
                .FirstOrDefault(a => a.UserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติคณะกรรมการที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        if (committeeAcceptors.Any(c => !c.IsBoardChairman()) &&
            acceptor.IsBoardChairman())
        {
            this.ThrowError(
                "ผู้อนุมัติคณะกรรมการไม่ตรงกับตำแหน่งที่กำหนด",
                StatusCodes.Status400BadRequest);
        }

        acceptor.Approve(req.Remark);
        acceptor.SetCurrent(false);
    }

    private void DepartmentDirectorApproval(PpMedianPrice medianPrice, ApproveMedianPriceRequest req)
    {
        var departmentDirectorAcceptors =
            medianPrice.Acceptors
                       .Where(a =>
                           a.Type == AcceptorType.DepartmentDirectorAgree &&
                           a is { Status: AcceptorStatus.Pending, IsActive: true })
                       .Select(DelegatorExtensions.DelegatorToAcceptor)
                       .OrderBy(a => a.Sequence)
                       .ToArray();

        var acceptor =
            departmentDirectorAcceptors
                .FirstOrDefault(a => a.Delegatee == null
                    ? a.UserId == UserId.From(req.UserId)
                    : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติส่วนสายงานที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        if (!acceptor.ArePreviousAcceptorsApproved(medianPrice.Acceptors))
        {
            this.ThrowError(
                "ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน",
                StatusCodes.Status400BadRequest);
        }

        var currentApproverUser =
            medianPrice.Acceptors
                       .First(a => a.Id == acceptor.Id);

        currentApproverUser
            .SetDelegatee(acceptor.DelegateeId)
            .Approve(req.Remark);

        acceptor.SetCurrent(false);
    }

    private void ApproverApproval(PpMedianPrice medianPrice, ApproveMedianPriceRequest req)
    {
        var medianPriceApprover =
            medianPrice.Acceptors
                       .Where(a =>
                           a is
                           {
                               Type: AcceptorType.Approver,
                               Status: AcceptorStatus.Pending,
                               IsActive: true
                           })
                       .ToArray();

        var medianPriceApproverDelegated =
            medianPriceApprover
                .Select(DelegatorExtensions.DelegatorToAcceptor)
                .OrderBy(a => a.Sequence)
                .ToArray();

        var currentApprover =
            medianPriceApproverDelegated
                .FirstOrDefault(a =>
                    a.Delegatee == null
                        ? a.UserId == UserId.From(req.UserId)
                        : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (currentApprover is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        if (!currentApprover.ArePreviousAcceptorsApproved(medianPrice.Acceptors))
        {
            this.ThrowError(
                "ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน",
                StatusCodes.Status400BadRequest);
        }

        var currentApproverUser =
            medianPrice.Acceptors
                       .First(a => a.Id == currentApprover.Id);

        currentApproverUser
            .SetDelegatee(currentApprover.DelegateeId)
            .Approve(req.Remark);

        currentApproverUser.SetCurrent(false);
    }

    private static void UpdateCommitteeCurrents(PpMedianPrice medianPrice)
    {
        var committee = medianPrice.Acceptors
                                   .Where(a => a.Type == AcceptorType.MedianPriceCommittee && a is { IsActive: true, IsUnableToPerformDuties: false })
                                   .ToList();

        if (committee.Count == 0)
        {
            return;
        }

        var chairman = committee.FirstOrDefault(IsChairman);
        var nonChair = chairman is null ? committee : [.. committee.Where(a => a.Id != chairman.Id)];

        foreach (var a in committee)
        {
            a.SetCurrent(false);
        }

        var pendingNonChair = nonChair.Where(a => a.Status == AcceptorStatus.Pending).ToList();

        if (pendingNonChair.Count > 0)
        {
            foreach (var p in pendingNonChair)
            {
                p.SetCurrent();
            }

            return;
        }

        var allNonChairReady = nonChair.All(a => a.Status == AcceptorStatus.Approved || a.IsUnableToPerformDuties);

        if (chairman is not null && chairman.Status == AcceptorStatus.Pending && allNonChairReady)
        {
            chairman.SetCurrent();

            _ = SendNotificationAsync(
                medianPrice,
                chairman.UserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(
                    NotificationConstant.WaitForLike.Message,
                    GetMdpName(medianPrice),
                    medianPrice.ReferenceNumber));
        }
    }

    private static string GetMdpName(PpMedianPrice mdp) =>
        mdp.IsChange ? ProgramConstant.ChangeMedianPrice.Name
        : mdp.IsCancel ? ProgramConstant.CancelMedianPrice.Name
        : ProgramConstant.PreProcurementMedianPrice.Name;

    private static bool IsChairman(PpMedianPriceAcceptor a)
    {
        // Either committee position code PosBoard001 or IsBoardChairman metadata
        if (a.CommitteePositionsCode != null && a.CommitteePositionsCode == ParameterCode.From("PosBoard001"))
        {
            return true;
        }

        return a.IsBoardChairman();
    }

    private static void UpdateSequentialCurrents(PpMedianPrice medianPrice, AcceptorType type)
    {
        var approvers = medianPrice.Acceptors
                                   .Where(a => a.Type == type && a is { IsActive: true, IsUnableToPerformDuties: false })
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

        var next = approvers.Select(DelegatorExtensions.DelegatorToAcceptor)
                            .FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

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
                    medianPrice,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, GetMdpName(medianPrice), medianPrice.ReferenceNumber));
            }
        }
        else if (next.Type == AcceptorType.Approver && isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    medianPrice,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, GetMdpName(medianPrice), medianPrice.ReferenceNumber));
            }
        }
    }

    private static async Task SendNotificationAsync(PpMedianPrice ppMedianPrice, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(ppMedianPrice.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Procurement.Url, ppMedianPrice.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationAssigneeAsync(PpMedianPrice ppMedianPrice, CancellationToken ct)
    {
        foreach (var targetUserId in ppMedianPrice.Assignees.Where(x => x.Type != AssigneeType.Director).SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.InformCommittee.Title,
                      string.Format(NotificationConstant.InformCommittee.Message, GetMdpName(ppMedianPrice), ppMedianPrice.ReferenceNumber),
                      NotificationProgram.Procurement)
                  .SetReferenceId(ppMedianPrice.Id.Value)
                  .SetLinkUrl(
                      string.Format(ProgramConstant.Procurement.Url, ppMedianPrice.Procurement.Id),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }
}