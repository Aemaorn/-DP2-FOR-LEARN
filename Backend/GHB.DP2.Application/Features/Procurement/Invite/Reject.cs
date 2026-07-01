namespace GHB.DP2.Application.Features.Procurement.Invite;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record RejectInviteRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid InviteId,
    string? Remark
);

public class RejectInviteEndpoint : EndpointBase<RejectInviteRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectInviteEndpoint(Dp2DbContext dbContext, ILogger<RejectInviteEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/Invite")
             .WithName("RejectInvite")
             .Accepts<RejectInviteRequest>("application/json"));
        this.Post("procurement/{ProcurementId:guid}/invite/{InviteId:guid}/reject");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RejectInviteRequest req, CancellationToken ct)
    {
        var invite = await this.dbContext.PInvites
                               .Include(c => c.Procurement)
                               .Include(x => x.Acceptors)
                               .SingleOrDefaultAsync(x => x.Id == PInviteId.From(req.InviteId) && x.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        if (invite == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลคำเชิญ");
        }

        if (invite.Status is PInviteStatus.Draft or PInviteStatus.Approved)
        {
            return TypedResults.BadRequest("ไม่สามารถปฏิเสธคำเชิญที่อยู่ในสถานะนี้ได้");
        }

        var jp004 = await this.dbContext.PpPurchaseRequisitions
                              .Include(p => p.Assignees)
                              .FirstOrDefaultAsync(w => w.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        this.ApproverReject(invite, req);

        UserId[] assigneeUserIds = jp004?.LastedAssignee is { } lastedAssignee ? [lastedAssignee.UserId] : [];
        _ = SendNotificationAsync(invite, assigneeUserIds);

        invite.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            "ตีกลับแก้ไข",
            invite.Status.ToString(),
            req.Remark));

        this.dbContext.PInvites.Update(invite);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private void ApproverReject(PInvite invite, RejectInviteRequest req)
    {
        var type = invite.Procurement.IsSixtyAndMoreThanOneHundredThousand ? AcceptorType.Approver : AcceptorType.ProcurementCommittee;

        var committeeAcceptor = invite.Acceptors
                                      .Where(a => a.Type == type &&
                                                  a is { IsActive: true, IsUnableToPerformDuties: false, Status: AcceptorStatus.Pending })
                                      .Map(DelegatorExtensions.DelegatorToAcceptor)
                                      .ToArray();

        var current =
            committeeAcceptor.FirstOrDefault(a => a.Delegatee == null
                ? a.UserId == req.UserId
                : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (current is null)
        {
            this.ThrowError(
                "ไม่พบคณะกรรมการที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        var currentAcceptorUser =
            invite.Acceptors
                  .FirstOrDefault(a => a.Id == current.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบคณะกรรมการที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        if (type == AcceptorType.ProcurementCommittee && committeeAcceptor.Any(c => !c.IsBoardChairman()) && current.IsBoardChairman())
        {
            this.ThrowError(
                "ผู้อนุมัติคณะกรรมการไม่ตรงกับตำแหน่งที่กำหนด",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(current.DelegateeId)
            .Reject(remark: req.Remark);

        if ((type == AcceptorType.ProcurementCommittee && (invite.HasMajorityRejection() || current.IsBoardChairman())) || type == AcceptorType.Approver)
        {
            invite.SetRejected(req.Remark, invite.Procurement.IsSixtyAndMoreThanOneHundredThousand);
        }
    }

    private static async Task SendNotificationAsync(PInvite invite, IEnumerable<UserId> assigneeUserIds)
    {
        var recipients = new[] { UserId.From(invite.AuditInfo.CreatedBy) }
            .Concat(assigneeUserIds)
            .Distinct();

        foreach (var userId in recipients)
        {
            await Notification
                  .Crate(
                      userId,
                      NotificationConstant.ReturnToCreator.Title,
                      string.Format(NotificationConstant.ReturnToCreator.Message, ProgramConstant.PreProcurementInvite.Name, invite.InviteNumber),
                      NotificationProgram.Procurement)
                  .SetReferenceId(invite.Id.Value)
                  .SetLinkUrl(string.Format(ProgramConstant.PreProcurementAppointment.Url, invite.Procurement.Id), "ดูรายละเอียด")
                  .PublishAsync(CancellationToken.None);
        }
    }
}