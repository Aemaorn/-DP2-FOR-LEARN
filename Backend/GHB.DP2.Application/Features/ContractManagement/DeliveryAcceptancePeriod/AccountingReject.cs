namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod;

using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record AccountingRejectDeliveryAcceptancePeriodRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid DeliveryAcceptanceId,
    Guid Id,
    string? Remark);

public class AccountingRejectDeliveryAcceptancePeriodValidator : Validator<AccountingRejectDeliveryAcceptancePeriodRequest>
{
    public AccountingRejectDeliveryAcceptancePeriodValidator()
    {
        this.RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ต้องระบุผู้ใช้งาน");

        this.RuleFor(x => x.DeliveryAcceptanceId)
            .NotEmpty().WithMessage("ต้องระบุการส่งมอบและตรวจรับงาน");

        this.RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ต้องระบุงวดส่งมอบและตรวจรับงาน");
    }
}

public class AccountingRejectDeliveryAcceptancePeriodEndpoint :
    DeliveryAcceptancePeriodEndpointBase<
        AccountingRejectDeliveryAcceptancePeriodRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public AccountingRejectDeliveryAcceptancePeriodEndpoint(
        Dp2DbContext dbContext,
        ILogger<AccountingRejectDeliveryAcceptancePeriodEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/DeliveryAcceptance/Period")
             .WithName("AccountingRejectDeliveryAcceptancePeriod")
             .AllowAnonymous()
             .Accepts<AccountingRejectDeliveryAcceptancePeriodRequest>("application/json"));
        this.Put("delivery-acceptance/{DeliveryAcceptanceId:guid?}/period/{Id:guid}/accounting-reject");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>>
        HandleRequestAsync(
            AccountingRejectDeliveryAcceptancePeriodRequest req,
            CancellationToken ct)
    {
        var periodExisting =
            await this.GetById(
                CmDeliveryAcceptanceId.From(req.DeliveryAcceptanceId),
                CmDeliveryAcceptancePeriodId.From(req.Id),
                ct);

        if (periodExisting == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลงวดส่งมอบและตรวจรับงาน");
        }

        if (periodExisting.Status != CmDeliveryAcceptancePeriodStatus.Approved)
        {
            return TypedResults.BadRequest("ไม่สามารถปฏิเสธได้ เนื่องจากสถานะงวดไม่ใช่อนุมัติแล้ว");
        }

        if (periodExisting.AccountStatus != CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval)
        {
            return TypedResults.BadRequest("ไม่สามารถปฏิเสธได้ เนื่องจากสถานะบัญชีไม่ใช่รอการอนุมัติ");
        }

        var cmDeliveryAcceptance = await this.dbContext.CmDeliveryAcceptances
                                             .Include(c => c.Department)
                                             .FirstOrDefaultAsync(c => c.Id == CmDeliveryAcceptanceId.From(req.DeliveryAcceptanceId), ct);

        var orgLevel = cmDeliveryAcceptance?.Department?.OrganizationLevel;
        var isBranch = orgLevel == EmployeeConstant.OrganizationLevel.Branch
            || orgLevel == EmployeeConstant.OrganizationLevel.Zone
            || orgLevel == EmployeeConstant.OrganizationLevel.Segment;

        var accountingAcceptors =
            periodExisting.Acceptors
                          .Where(a => (a.Type == AcceptorType.Accounting || a.Type == AcceptorType.AccountingOperator)
                                      && a.IsActive
                                      && a.Status == AcceptorStatus.Pending)
                          .Select(DelegatorExtensions.DelegatorToAcceptor)
                          .ToArray();

        if (isBranch && !accountingAcceptors.Any())
        {
            periodExisting.AccountingUpdateStatus(CmDeliveryAcceptancePeriodAccountStatus.AccountingRejected);

            periodExisting.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.AccountingReject,
                    string.Empty,
                    nameof(CmDeliveryAcceptancePeriodAccountStatus.AccountingRejected),
                    req.Remark));

            await this.ReplaceDocumentsForStatusAsync(
                periodExisting,
                req.UserId,
                CmDeliveryAcceptancePeriodStatus.Rejected,
                ct);

            this.dbContext.CmDeliveryAcceptancePeriods.Update(periodExisting);
            await this.dbContext.SaveChangesAsync(ct);

            var branchCommitteeMembers = periodExisting.Acceptors
                                                       .Where(a => a.Type == AcceptorType.AcceptanceCommittee && a.IsActive)
                                                       .ToList();

            foreach (var member in branchCommitteeMembers)
            {
                _ = SendNotificationAsync(
                    periodExisting,
                    member.UserId,
                    NotificationConstant.AccountingRejected.Title,
                    string.Format(
                        NotificationConstant.AccountingRejected.Message,
                        ProgramConstant.ContractAcceptancePeriod.Name,
                        periodExisting.AcceptanceNumber));
            }

            return TypedResults.Ok();
        }

        var acceptor =
            accountingAcceptors.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                ? a.UserId == UserId.From(req.UserId)
                : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติบัญชีที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        if (!acceptor.ArePreviousAcceptorsApproved(accountingAcceptors))
        {
            this.ThrowError(
                "ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน",
                StatusCodes.Status400BadRequest);
        }

        var currentAcceptorUser = periodExisting.Acceptors.FirstOrDefault(a => a.Id == acceptor.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติบัญชีที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(acceptor.DelegateeId)
            .Reject(remark: req.Remark);

        periodExisting.SetRejectedCommittee();
        periodExisting.AccountingUpdateStatus(CmDeliveryAcceptancePeriodAccountStatus.AccountingRejected);

        periodExisting.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.AccountingReject,
                string.Empty,
                nameof(CmDeliveryAcceptancePeriodAccountStatus.AccountingRejected),
                req.Remark));

        await this.ReplaceDocumentsForStatusAsync(
            periodExisting,
            req.UserId,
            CmDeliveryAcceptancePeriodStatus.Rejected,
            ct);

        this.dbContext.CmDeliveryAcceptancePeriods.Update(periodExisting);

        await this.dbContext.SaveChangesAsync(ct);

        var committeeMembers = periodExisting.Acceptors
                                             .Where(a => a.Type == AcceptorType.AcceptanceCommittee && a.IsActive)
                                             .ToList();

        foreach (var member in committeeMembers)
        {
            _ = SendNotificationAsync(
                periodExisting,
                member.UserId,
                NotificationConstant.AccountingRejected.Title,
                string.Format(
                    NotificationConstant.AccountingRejected.Message,
                    ProgramConstant.ContractAcceptancePeriod.Name,
                    periodExisting.AcceptanceNumber));
        }

        return TypedResults.Ok();
    }

    private static async Task SendNotificationAsync(
        CmDeliveryAcceptancePeriod period,
        UserId userId,
        string title,
        string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.ContractManagement)
              .SetReferenceId(period.Id.Value)
              .SetLinkUrl(
                  string.Format(
                      ProgramConstant.ContractAcceptancePeriod.Url,
                      period.CmDeliveryAcceptance.Id,
                      period.Id),
                  "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}