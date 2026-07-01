namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ApproveAdjustContractDurationRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid CamContractAmendmentId,
    Guid Id,
    string? Remark);

public class ApproveAdjustContractDurationEndpoint : AdjustContractDurationEndpointBase<ApproveAdjustContractDurationRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    public ApproveAdjustContractDurationEndpoint(ILogger<ApproveAdjustContractDurationEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment/AdjustContractDuration")
             .WithName("ApproveAdjustContractDuration")
             .Accepts<ApproveAdjustContractDurationRequest>("application/json"));
        this.Post("contract-amendment/{CamContractAmendmentId:guid}/adjust-contract-duration/{Id:guid}/approve");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApproveAdjustContractDurationRequest req, CancellationToken ct)
    {
        var entity =
            await this.DbContext.CamContractAmendmentExtendChanges
                      .FirstOrDefaultAsync(
                          e =>
                              e.Id == ContractAmendmentExtendChangeId.From(req.Id)
                              && e.CamContractAmendmentId == CamContractAmendmentId.From(req.CamContractAmendmentId),
                          ct);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการแก้ไขสัญญาที่ระบุ");
        }

        switch (entity.Status)
        {
            case ContractAmendmentExtendChangeStatus.WaitingCommitteeApproval:

                this.CommitteeApproved(entity, req);
                UpdateCommitteeCurrents(entity);

                break;

            case ContractAmendmentExtendChangeStatus.WaitingApproval:

                this.AcceptorApproved(entity, req);
                UpdateSequentialCurrents(entity, AcceptorType.Approver);

                await this.UpdateDocumentAsync(entity, UserId.From(req.UserId), isReplace: true, hasCreator: false, hasAcceptor: true, ct);
                break;

            default:
                return TypedResults.BadRequest("ไม่สามารถอนุมัติการแก้ไขสัญญาได้ เนื่องจากสถานะไม่ถูกต้อง");
        }

        this.DbContext.CamContractAmendmentExtendChanges.Update(entity);
        await this.DbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private void CommitteeApproved(CamContractAmendmentExtendChange entity, ApproveAdjustContractDurationRequest req)
    {
        var acceptors =
            entity.Acceptors
                  .Where(a =>
                      a is { IsActive: true, IsUnableToPerformDuties: false, Status: AcceptorStatus.Pending, Type: AcceptorType.AcceptanceCommittee })
                  .OrderBy(a => a.Sequence)
                  .ToList();

        var current = acceptors.FirstOrDefault(a => a.UserId == UserId.From(req.UserId));

        if (current == null)
        {
            this.ThrowError("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้", StatusCodes.Status400BadRequest);
        }

        current.Approve(req.Remark);
        current.SetCurrent(false);
        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.CommitteeApproved,
            $"คณะกรรมการเห็นชอบ",
            entity.Status.ToString(),
            req.Remark));

        if (current.IsBoardChairman())
        {
            entity.SetWaitingAssigned();
        }
    }

    private static void UpdateCommitteeCurrents(CamContractAmendmentExtendChange po)
    {
        var committee = po.Acceptors
                          .Where(a => a.Type == AcceptorType.TorDraftCommittee && a.IsActive && !a.IsUnableToPerformDuties)
                          .ToList();

        if (committee.Count == 0)
        {
            return;
        }

        var chairman = committee.FirstOrDefault(a =>
            (a.CommitteePositionsCode != null && a.CommitteePositionsCode == ParameterCode.From("PosBoard001")) || a.IsBoardChairman());
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
                p.SetCurrent(true);
            }

            return;
        }

        var allNonChairReady = nonChair.All(a => a.Status == AcceptorStatus.Approved || a.IsUnableToPerformDuties);

        if (chairman is not null && chairman.Status == AcceptorStatus.Pending && allNonChairReady)
        {
            _ = SendNotificationAsync(
                po,
                chairman.UserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.ContractAmendment.Name, string.Empty));
            chairman.SetCurrent(true);
        }
    }

    private void AcceptorApproved(CamContractAmendmentExtendChange po, ApproveAdjustContractDurationRequest req)
    {
        var acceptors = po.Acceptors
                          .Where(a => a is { IsActive: true, IsUnableToPerformDuties: false, Status: AcceptorStatus.Pending, Type: AcceptorType.Approver })
                          .OrderBy(a => a.Sequence)
                          .Select(DelegatorExtensions.DelegatorToAcceptor)
                          .ToList();

        var current = acceptors.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                            ? a.UserId == req.UserId
                            : a.Delegatee?.SuUserId == UserId.From(req.UserId)
                              && a.Status == AcceptorStatus.Pending);

        if (current == null)
        {
            this.ThrowError("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้", StatusCodes.Status400BadRequest);
        }

        var currentAcceptorUser = po.Acceptors.FirstOrDefault(a => a.Id == current.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(current.DelegateeId)
            .Approve(remark: req.Remark);

        current.SetCurrent(false);
        po.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Approved,
            $"ผู้มีอำนาจเห็นชอบ/อนุมัติ",
            po.Status.ToString(),
            req.Remark));

        if (po.Acceptors.Where(w => w.Type is AcceptorType.Approver).All(e => e.Status is AcceptorStatus.Approved))
        {
            po.SetApproval();
        }
    }

    private static void UpdateSequentialCurrents(CamContractAmendmentExtendChange po, AcceptorType type)
    {
        var approvers = po.Acceptors
                          .Where(a => a.Type == type && a.IsActive && !a.IsUnableToPerformDuties)
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

        var next = approvers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

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
                    po,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.ContractAmendment.Name, string.Empty));
            }
        }
        else if (next.Type == AcceptorType.Approver && isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    po,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.ContractAmendment.Name, string.Empty));
            }
        }
    }

    private static async Task SendNotificationAsync(CamContractAmendmentExtendChange po, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.ContractAmendment)
              .SetReferenceId(po.Id.Value)
              .SetLinkUrl(string.Empty, "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}