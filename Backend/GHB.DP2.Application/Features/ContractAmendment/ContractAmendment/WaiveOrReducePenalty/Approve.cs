namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ApproveWaiveOrReducePenaltyRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid CamContractAmendmentId,
    Guid WaiveOrReducePenaltyId,
    string? Remark);

public class ApproveWaiveOrReducePenaltyEndpoint : WaiveOrReducePenaltyEndpointBase<ApproveWaiveOrReducePenaltyRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    public ApproveWaiveOrReducePenaltyEndpoint(ILogger<ApproveWaiveOrReducePenaltyEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment/WaiveOrReducePenalty")
             .WithName("ApproveWaiveOrReducePenalty")
             .Accepts<ApproveWaiveOrReducePenaltyRequest>("application/json"));
        this.Post("contract-amendment/{CamContractAmendmentId:guid}/waive-or-reduce-penalty/{WaiveOrReducePenaltyId:guid}/approve");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApproveWaiveOrReducePenaltyRequest req, CancellationToken ct)
    {
        var entity =
            await this.DbContext.CamContractAmendmentWaiveOrReducePenalties
                      .FirstOrDefaultAsync(
                          e =>
                              e.Id == WaiveOrReducePenaltyId.From(req.WaiveOrReducePenaltyId)
                              && e.CamContractAmendmentId == CamContractAmendmentId.From(req.CamContractAmendmentId),
                          ct);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการแก้ไขสัญญาที่ระบุ");
        }

        switch (entity.Status)
        {
            case CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingCommitteeApproval:

                this.CommitteeApproved(entity, req);
                UpdateCommitteeCurrents(entity);

                break;

            case CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingApproval:

                this.AcceptorApproved(entity, req);
                UpdateSequentialCurrents(entity, AcceptorType.Approver);

                await this.UpdateDocumentAsync(entity, UserId.From(req.UserId), isReplace: true, hasCreator: false, hasAcceptor: true, ct: ct);

                break;
        }

        this.DbContext.CamContractAmendmentWaiveOrReducePenalties.Update(entity);
        await this.DbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private void CommitteeApproved(CamContractAmendmentWaiveOrReducePenalty entity, ApproveWaiveOrReducePenaltyRequest req)
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

    private static void UpdateCommitteeCurrents(CamContractAmendmentWaiveOrReducePenalty po)
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

    private void AcceptorApproved(CamContractAmendmentWaiveOrReducePenalty po, ApproveWaiveOrReducePenaltyRequest req)
    {
        var acceptors = po.Acceptors
                          .Where(a => a is { IsActive: true, IsUnableToPerformDuties: false, Status: AcceptorStatus.Pending, Type: AcceptorType.Approver })
                          .OrderBy(a => a.Sequence)
                          .ToList();

        var current = acceptors.FirstOrDefault(a => a.UserId == UserId.From(req.UserId));

        if (current == null)
        {
            this.ThrowError("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้", StatusCodes.Status400BadRequest);
        }

        current.Approve(req.Remark);
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

    private static void UpdateSequentialCurrents(CamContractAmendmentWaiveOrReducePenalty po, AcceptorType type)
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

    private static async Task SendNotificationAsync(CamContractAmendmentWaiveOrReducePenalty po, UserId userId, string title, string message)
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