namespace GHB.DP2.Application.Features.Procurement.Invite;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.AnnouncementInfo;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
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

public record ApproveInviteRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid InviteId,
    string? Remark);

public class ApproveInviteEndpoint : InviteEndpointBase<ApproveInviteRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public ApproveInviteEndpoint(
        Dp2DbContext dbContext,
        ILogger<ApproveInviteEndpoint> logger,
        IFileServiceClient fileServiceClient)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/Invite")
             .WithName("ApproveInvite")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError)
             .Accepts<ApproveInviteRequest>("application/json"));
        this.Post("procurement/{ProcurementId:guid}/invite/{InviteId:guid}/approve");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApproveInviteRequest req, CancellationToken ct)
    {
        var invite = await this.dbContext.PInvites
                               .Include(p => p.Procurement)
                               .ThenInclude(procurement => procurement.SupplyMethod)
                               .Include(x => x.Acceptors)
                               .ThenInclude(x => x.User)
                               .ThenInclude(x => x.Employee)
                               .Include(p => p.InvitedEntrepreneurs)
                               .Include(a => a.AuditInfo)
                               .SingleOrDefaultAsync(
                                   x => x.Id == PInviteId.From(req.InviteId) && x.ProcurementId == ProcurementId.From(req.ProcurementId),
                                   ct);

        if (invite == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        var currentAcceptors = invite.Acceptors
                                     .Where(a => a.IsActive && a.Type == AcceptorType.Approver)
                                     .Map(DelegatorExtensions.DelegatorToAcceptor)
                                     .OrderBy(a => a.Sequence)
                                     .ToList();

        var currentCommittees = invite.Acceptors
                                      .Where(a => a.IsActive && a.Type == AcceptorType.ProcurementCommittee)
                                      .OrderBy(a => a.Sequence)
                                      .ToList();

        var acceptors = currentCommittees.Concat(currentAcceptors);

        var current = acceptors.FirstOrDefault(a => a.Delegatee?.SuUserId == null
            ? a.UserId == req.UserId
            : a.Delegatee?.SuUserId == UserId.From(req.UserId)
              && a.Status == AcceptorStatus.Pending);

        if (current == null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        var isSixtyOver100K = invite.Procurement.Budget > 100000 && invite.Procurement.SupplyMethodCode == SupplyMethodConstant.Sixty;

        var currentAcceptorUser =
            invite.Acceptors
                  .FirstOrDefault(a => a.Id == current.Id);

        if (currentAcceptorUser == null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        currentAcceptorUser
            .SetDelegatee(current.DelegateeId)
            .Approve(remark: req.Remark);

        if (isSixtyOver100K)
        {
            UpdateSequentialCurrents(invite);
        }

        if (!isSixtyOver100K)
        {
            UpdateCommitteeCurrents(invite);
        }

        if (ShouldUpdateStatus())
        {
            invite.SetApproved(req.Remark);
        }
        else
        {
            if (invite.Procurement.SupplyMethodCode == "SMethod002" && invite.Procurement.Budget > 100000)
            {
                invite.AddActivity(new ActivityInfo(
                    "หัวหน้าส่วนเห็นชอบ/อนุมัติ",
                    $"ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ",
                    invite.Status.ToString(),
                    req.Remark));
            }
            else
            {
                invite.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Approved,
                    $"ส่งผู้มีอำนาจเห็ยชอบ/อนุมัติ",
                    PInviteStatus.Approved.ToString(),
                    req.Remark));
            }
        }

        if (invite.Status == PInviteStatus.Approved)
        {
            foreach (var entrepreneur in invite.InvitedEntrepreneurs)
            {
                await this.UpdateDocumentAsync(invite, entrepreneur, req.UserId, req.ProcurementId, false, true, ct);
            }

            var planType = invite.Procurement.SupplyMethod.Code == SupplyMethodConstant.Sixty ? Section60.Invite : Section80.Invite;

            var firstEntrepreneur = invite.InvitedEntrepreneurs.FirstOrDefault();

            if (firstEntrepreneur?.LastedDocument != null)
            {
                var file = await this.fileServiceClient.DownloadAsStreamAsync(firstEntrepreneur.LastedDocument.FileId, cancellationToken: ct);

                await AnnouncementData.Create(
                                          invite.Procurement.Name,
                                          DateTimeOffset.UtcNow,
                                          invite.Procurement.Budget ?? decimal.Zero,
                                          string.Empty,
                                          planType,
                                          file?.Stream)
                                      .PublishEvent(ct);
            }

            var jp004 = await this.dbContext.PpPurchaseRequisitions
                                  .Include(p => p.Assignees)
                                  .FirstOrDefaultAsync(w => w.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

            UserId[] assigneeUserIds = jp004?.LastedAssignee is { } lastedAssignee ? [lastedAssignee.UserId] : [];

            var recipients = new[] { UserId.From(invite.AuditInfo.CreatedBy) }
                .Concat(assigneeUserIds)
                .Distinct();

            foreach (var userId in recipients)
            {
                _ = SendNotificationAsync(
                    invite,
                    userId,
                    NotificationConstant.InformCommittee.Title,
                    string.Format(
                        NotificationConstant.InformCommittee.Message,
                        ProgramConstant.PreProcurementInvite.Name,
                        invite.InviteNumber));
            }
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();

        bool ShouldUpdateStatus()
        {
            if (isSixtyOver100K)
            {
                return invite.Acceptors
                             .Where(x => !x.IsUnableToPerformDuties)
                             .All(x => x.Status == AcceptorStatus.Approved);
            }

            return !isSixtyOver100K && IsChairman(currentAcceptorUser)
                && invite.Acceptors
                         .Where(x => !x.IsUnableToPerformDuties)
                         .All(x => x.Status != AcceptorStatus.Pending);
        }
    }

    private static void UpdateCommitteeCurrents(PInvite invites)
    {
        var committee = invites.Acceptors
                               .Where(a => a is
                               {
                                   Type: AcceptorType.ProcurementCommittee,
                                   IsActive: true,
                                   IsUnableToPerformDuties: false
                               })
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
            _ = SendNotificationAsync(
                invites,
                chairman.UserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(
                    NotificationConstant.WaitForLike.Message,
                    ProgramConstant.PreProcurementInvite.Name,
                    invites.InviteNumber));
            chairman.SetCurrent();
        }
    }

    private static void UpdateSequentialCurrents(PInvite invites)
    {
        var approvers = invites.Acceptors
                               .Where(a => a is { Type: AcceptorType.Approver, IsActive: true, IsUnableToPerformDuties: false })
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

        var next = approvers.Select(DelegatorExtensions.DelegatorToAcceptor).FirstOrDefault(a =>
            a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        next.SetCurrent();

        var pendingOfType = approvers.Where(a => a.Status == AcceptorStatus.Pending).ToList();
        var isLastPending = pendingOfType.Count == 1;

        if (next.Type == AcceptorType.Approver && !isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    invites,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(
                        NotificationConstant.WaitForLike.Message,
                        ProgramConstant.PreProcurementInvite.Name,
                        invites.InviteNumber));
            }
        }
        else if (next.Type == AcceptorType.Approver && isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    invites,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(
                        NotificationConstant.WaitForApprove.Message,
                        ProgramConstant.PreProcurementInvite.Name,
                        invites.InviteNumber));
            }
        }
    }

    private static async Task SendNotificationAsync(PInvite invites, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(invites.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Procurement.Url, invites.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static bool IsChairman(PInviteAcceptors a)
    {
        if (a.CommitteePositionsCode != null && a.CommitteePositionsCode == ParameterCode.From("PosBoard001"))
        {
            return true;
        }

        return a.IsBoardChairman();
    }
}