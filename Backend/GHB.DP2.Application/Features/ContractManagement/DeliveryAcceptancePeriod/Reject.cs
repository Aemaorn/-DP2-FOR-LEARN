namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod;

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record RejectDeliveryAcceptancePeriodRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid DeliveryAcceptanceId,
    Guid Id,
    AcceptorType Group,
    string? Remark
);

public class RejectDeliveryAcceptancePeriodValidator : Validator<RejectDeliveryAcceptancePeriodRequest>
{
    public RejectDeliveryAcceptancePeriodValidator()
    {
        this.RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ต้องระบุผู้ใช้งาน");

        this.RuleFor(x => x.DeliveryAcceptanceId)
            .NotEmpty().WithMessage("ต้องระบุการส่งมอบและตรวจรับงาน ");

        this.RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ต้องระบุงวดส่งมอบและตรวจรับงาน ");

        this.RuleFor(x => x.Group)
            .IsInEnum()
            .WithMessage("กลุ่มผู้อนุมัติไม่ถูกต้อง")
            .Must(x =>
                x == AcceptorType.AcceptanceCommittee ||
                x == AcceptorType.Approver)
            .WithMessage("กลุ่มผู้อนุมัติต้องเป็น บุคคล/คณะกรรมการตรวจรับพัสดุ หรือ ผู้มีอำนาจเห็นชอบ/อนุมัติ");
    }
}

public class RejectDeliveryAcceptancePeriodEndpoint :
    DeliveryAcceptancePeriodEndpointBase<
        RejectDeliveryAcceptancePeriodRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectDeliveryAcceptancePeriodEndpoint(
        Dp2DbContext dbContext,
        ILogger<RejectDeliveryAcceptancePeriodEndpoint> logger,
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
             .WithName("RejectDeliveryAcceptancePeriod")
             .AllowAnonymous()
             .Accepts<RejectDeliveryAcceptancePeriodRequest>("application/json"));
        this.Put("delivery-acceptance/{DeliveryAcceptanceId:guid?}/period/{Id:guid}/reject");
    }

    protected override async ValueTask
        <Results<Ok, NotFound<string>, BadRequest<string>>>
        HandleRequestAsync(
            RejectDeliveryAcceptancePeriodRequest req,
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

        switch (periodExisting.Status)
        {
            case CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval:
                this.CommitteeReject(periodExisting, req);
                SendNotificationToCommittee(periodExisting);

                break;

            case CmDeliveryAcceptancePeriodStatus.WaitingAssign:
            case CmDeliveryAcceptancePeriodStatus.WaitingComment:
                periodExisting.SetRejected();
                SendNotificationToCommittee(periodExisting);

                break;

            case CmDeliveryAcceptancePeriodStatus.WaitingAcceptance:
                this.ApproverReject(periodExisting, req);

                break;

            default:
                return TypedResults.BadRequest("ไม่สามารถปฏิเสธเอกสารที่อยู่ในสถานะนี้ได้");
        }

        await this.ReplaceDocumentsForStatusAsync(periodExisting, req.UserId, periodExisting.Status, ct);

        this.dbContext.CmDeliveryAcceptancePeriods.Update(periodExisting);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private void CommitteeReject(
        CmDeliveryAcceptancePeriod periodExisting,
        RejectDeliveryAcceptancePeriodRequest req)
    {
        var committeeAcceptor =
            periodExisting.Acceptors
                          .Where(a => a is
                          {
                              Type: AcceptorType.AcceptanceCommittee,
                              IsActive: true,
                              IsUnableToPerformDuties: false,
                              Status: AcceptorStatus.Pending
                          })
                          .ToArray();

        var acceptor =
            committeeAcceptor.FirstOrDefault(a => a.UserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบบุคคล/คณะกรรมการตรวจรับพัสดุที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        acceptor.Reject(req.Remark);

        if (periodExisting.HasMajorityRejection() || acceptor.IsBoardChairman())
        {
            periodExisting.SetRejectedCommittee(req.Remark);
        }
        else
        {
            periodExisting.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.CommitteeReject,
                    string.Empty,
                    periodExisting.Status.ToString(),
                    req.Remark));
        }
    }

    private void ApproverReject(
        CmDeliveryAcceptancePeriod periodExisting,
        RejectDeliveryAcceptancePeriodRequest req)
    {
        var approverAcceptor =
            periodExisting.Acceptors
                          .Where(a => a is
                          {
                              Type: AcceptorType.Approver,
                              IsActive: true,
                              Status: AcceptorStatus.Pending
                          })
                          .Select(DelegatorExtensions.DelegatorToAcceptor)
                          .ToArray();

        var acceptor =
            approverAcceptor.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                            ? a.UserId == UserId.From(req.UserId)
                                                            : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        if (!acceptor.ArePreviousAcceptorsApproved(approverAcceptor))
        {
            this.ThrowError(
                "ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน",
                StatusCodes.Status400BadRequest);
        }

        var currentAcceptorUser = periodExisting.Acceptors.FirstOrDefault(a => a.Id == acceptor.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(acceptor.DelegateeId)
            .Reject(remark: req.Remark);

        var isHasJorPorSection =
            this.IsHasJorPorSection(periodExisting);

        if (isHasJorPorSection)
        {
            periodExisting.SetRejectToAssignee(req.Remark);
            SendNotificationToAssignee(periodExisting);

            return;
        }

        periodExisting.SetRejected(req.Remark);
        SendNotificationToCommittee(periodExisting);
    }

    private static void SendNotificationToCommittee(CmDeliveryAcceptancePeriod period)
    {
        var committeeMembers = period.Acceptors
                                     .Where(a => a.Type == AcceptorType.AcceptanceCommittee && a.IsActive)
                                     .ToList();

        foreach (var member in committeeMembers)
        {
            _ = SendNotificationAsync(
                period,
                member.UserId,
                NotificationConstant.ReturnToCreator.Title,
                string.Format(
                    NotificationConstant.ReturnToCreator.Message,
                    ProgramConstant.ContractAcceptancePeriod.Name,
                    period.AcceptanceNumber));
        }
    }

    private static void SendNotificationToAssignee(CmDeliveryAcceptancePeriod period)
    {
        foreach (var targetUserId in period.Assignees.SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            _ = SendNotificationAsync(
                period,
                targetUserId,
                NotificationConstant.ReturnToCreator.Title,
                string.Format(
                    NotificationConstant.ReturnToCreator.Message,
                    ProgramConstant.ContractAcceptancePeriod.Name,
                    period.AcceptanceNumber));
        }
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