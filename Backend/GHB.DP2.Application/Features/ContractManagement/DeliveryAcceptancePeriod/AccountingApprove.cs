namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod;

using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record AccountingApproverRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    string? Remark);

public class AccountingApproverValidator : Validator<AccountingApproverRequest>
{
    public AccountingApproverValidator()
    {
        this.RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ต้องระบุผู้ใช้งาน");

        this.RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ต้องระบุงวดส่งมอบและตรวจรับงาน ");
    }
}

public class AccountingApproverEndpoint : DeliveryAcceptancePeriodEndpointBase<AccountingApproverRequest, Ok>
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;

    public AccountingApproverEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<AccountingApproverEndpoint> logger)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/DeliveryAcceptance/Period")
             .WithName("AccountingApproveDeliveryAcceptancePeriod")
             .AllowAnonymous()
             .Accepts<ApproveDeliveryAcceptancePeriodRequest>("application/json"));
        this.Put("delivery-acceptance/period/{Id:guid}/approve");
    }

    protected override async ValueTask<Ok> HandleRequestAsync(AccountingApproverRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.CmDeliveryAcceptancePeriods
                               .Include(x => x.Acceptors)
                               .ThenInclude(a => a.Delegatee)
                               .Include(x => x.CmDeliveryAcceptance)
                               .ThenInclude(c => c.Department)
                               .FirstOrDefaultAsync(x => x.Id == CmDeliveryAcceptancePeriodId.From(req.Id), ct);

        if (entity == null)
        {
            this.ThrowError("ไม่พบงวดส่งมอบและตรวจรับงาน", StatusCodes.Status404NotFound);
        }

        var acceptor = entity.Acceptors
                             .Where(a =>
                                 (a.Type == AcceptorType.Accounting || a.Type == AcceptorType.AccountingOperator) &&
                                 a.IsActive)
                             .OrderBy(a => a.Type == AcceptorType.AccountingOperator ? 0 : 1)
                             .ThenBy(a => a.Sequence)
                             .ToList();

        var currentAcceptor = acceptor
            .Select(DelegatorExtensions.DelegatorToAcceptor)
            .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                ? a.UserId == UserId.From(req.UserId)
                : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (currentAcceptor == null)
        {
            this.ThrowError("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้", StatusCodes.Status400BadRequest);
        }

        if (!IsPreviousApproved(acceptor, currentAcceptor) &&
            entity.AccountStatus != CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval)
        {
            this.ThrowError("ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน", StatusCodes.Status400BadRequest);
        }

        var currentAcceptorUser =
            entity.Acceptors
                  .FirstOrDefault(a => a.Id == currentAcceptor.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(currentAcceptor.DelegateeId)
            .Approve(remark: req.Remark);

        currentAcceptor.SetCurrent(false);

        UpdateSequentialCurrents(entity);

        var allAcceptorsInGroupApproved =
            acceptor
                .Where(a =>
                    (a.Type == AcceptorType.Accounting || a.Type == AcceptorType.AccountingOperator) &&
                    !a.IsUnableToPerformDuties)
                .All(a => a.Status == AcceptorStatus.Approved);

        if (allAcceptorsInGroupApproved)
        {
            entity.AccountingUpdateStatus(CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate);

            entity.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.AccountingApproved,
                    string.Empty,
                    nameof(CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate),
                    req.Remark));
        }
        else
        {
            entity.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.AccountingApproved,
                    string.Empty,
                    nameof(CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval),
                    req.Remark));
        }

        if (entity.AccountStatus == CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate)
        {
            var sourceDepartmentOrganizationLevel = await this.GetSourceDepartmentOrganizationLevelAsync(entity, ct);

            var isBranchSource =
                sourceDepartmentOrganizationLevel == EmployeeConstant.OrganizationLevel.Branch ||
                sourceDepartmentOrganizationLevel == EmployeeConstant.OrganizationLevel.Zone ||
                sourceDepartmentOrganizationLevel == EmployeeConstant.OrganizationLevel.Segment;

            var confirmers = entity.Acceptors
                                   .Where(a => a.Type == AcceptorType.AccountingConfirmer && !a.IsDeleted)
                                   .ToList();

            List<UserId> accountingTargets;

            if (confirmers.Count > 0)
            {
                accountingTargets = confirmers
                    .SelectMany(c => c.GetNotificationTargets())
                    .ToList();
            }
            else if (!isBranchSource)
            {
                var segmentAccountingMembers = await this.operationService.GetSegmentAccountingMembersAsync(ct);

                accountingTargets = segmentAccountingMembers
                    .Select(m => m.UserId)
                    .ToList();
            }
            else
            {
                accountingTargets = [];
            }

            foreach (var targetUserId in accountingTargets)
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.AccountingSetDate.Title,
                    string.Format(NotificationConstant.AccountingSetDate.Message, ProgramConstant.ContractAcceptancePeriod.Name, entity.AcceptanceNumber));
            }
        }

        this.dbContext.CmDeliveryAcceptancePeriods.Update(entity);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static bool IsPreviousApproved(
        List<CmDeliveryAcceptancePeriodAcceptor> acceptors,
        CmDeliveryAcceptancePeriodAcceptor currentAcceptor)
    {
        var currentIdx = acceptors.FindIndex(a => a.Id == currentAcceptor.Id);

        if (currentIdx <= 0)
        {
            return true;
        }

        var prev = acceptors
            .Take(currentIdx)
            .Where(a => a.IsActive)
            .LastOrDefault();

        return
            prev == null ||
            prev.Status == AcceptorStatus.Approved;
    }

    private static void UpdateSequentialCurrents(CmDeliveryAcceptancePeriod period)
    {
        var approvers = period.Acceptors
                              .Where(a => (a.Type == AcceptorType.Accounting || a.Type == AcceptorType.AccountingOperator) && a.IsActive && !a.IsUnableToPerformDuties)
                              .OrderBy(a => a.Type == AcceptorType.AccountingOperator ? 0 : 1)
                              .ThenBy(a => a.Sequence)
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

        if ((next.Type == AcceptorType.Accounting || next.Type == AcceptorType.AccountingOperator) && !isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    period,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.ContractAcceptancePeriod.Name, period.AcceptanceNumber));
            }
        }
        else if ((next.Type == AcceptorType.Accounting || next.Type == AcceptorType.AccountingOperator) && isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    period,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.ContractAcceptancePeriod.Name, period.AcceptanceNumber));
            }
        }
    }

    private static async Task SendNotificationAsync(CmDeliveryAcceptancePeriod period, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.ContractManagement)
              .SetReferenceId(period.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.ContractAcceptancePeriod.Url, period.CmDeliveryAcceptance.Id, period.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}