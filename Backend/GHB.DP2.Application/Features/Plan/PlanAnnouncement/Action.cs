namespace GHB.DP2.Application.Features.Plan.PlanAnnouncement;

using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Plan.PlanAnnouncement.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class PlanAnnouncementActionRequest
{
    public Guid PlanAnnouncementId { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public PlanAnnouncementAction Action { get; init; }

    public string? Remark { get; init; }

    public string? AnnouncementTitle { get; init; }

    public DateTimeOffset? AnnouncementDate { get; init; }
}

public class PlanAnnouncementActionRequestValidator : Validator<PlanAnnouncementActionRequest>
{
    public PlanAnnouncementActionRequestValidator()
    {
        this.RuleFor(r => r.PlanAnnouncementId)
            .NotEmpty()
            .WithMessage("Plan Announcement Id is required.");

        this.RuleFor(r => r.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        this.RuleFor(r => r.Action)
            .IsInEnum();

        this.RuleFor(r => r.Remark)
            .NotNull()
            .When(w => w.Action is PlanAnnouncementAction.AcceptorReject or PlanAnnouncementAction.AcceptorApprove)
            .WithMessage("Remark is required.");
    }
}

public class PlanAnnouncementActionEndpoint : PlanAnnouncementEndpointBase<PlanAnnouncementActionRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public PlanAnnouncementActionEndpoint(
        Dp2DbContext dbContext,
        ILogger<PlanAnnouncementActionEndpoint> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(PlanAnnouncement))
             .WithName("PlanAnnouncementAction")
             .Accepts<PlanAnnouncementActionRequest>("application/json"));
        this.Put("plan/announcement/action/{PlanAnnouncementId:guid}");
        this.AuditLog("ขออนุมัติเผยแพร่จัดซื้อจัดจ้าง", "อนุมัติ/ตีกลับ/แก้ไขแผนจัดซื้อจัดจ้าง");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(
        PlanAnnouncementActionRequest req,
        CancellationToken ct)
    {
        var data =
            await this.dbContext.PlanAnnouncements
                      .Include(a => a.Acceptors)
                      .ThenInclude(a => a.User)
                      .ThenInclude(u => u.Employee)
                      .Include(a => a.Assignees)
                      .ThenInclude(a => a.User)
                      .ThenInclude(u => u.Employee)
                      .Include(i => i.AnnouncementSelectedInformations)
                      .ThenInclude(i => i.Plan)
                      .Include(planAnnouncement => planAnnouncement.AnnouncementSelectedInformations)
                      .ThenInclude(planAnnouncementSelected => planAnnouncementSelected.Plan)
                      .ThenInclude(auditableEntity => auditableEntity.AuditInfo)
                      .SingleOrDefaultAsync(
                          w => w.Id == PlanAnnouncementId.From(req.PlanAnnouncementId),
                          ct);

        if (data is null)
        {
            return TypedResults.NotFound($"Plan announcement {req.PlanAnnouncementId} id is invalid.");
        }

        var userId = UserId.From(req.UserId);

        switch (req.Action)
        {
            case PlanAnnouncementAction.Recall:
                data.SetRecall(PlanAnnouncementStatus.WaitingAcceptor);

                break;

            case PlanAnnouncementAction.AcceptorReject:
                this.AcceptorRejected(data, userId, req.Remark);
                _ = await this.UpdateDocumentHistory(data, isReplace: false, hasPublicPlan: false, hasAcceptors: false, hasAssignees: false, ct);
                await this.SendNotificationRejectAssigneeAsync(data, NotificationConstant.ReturnToCreator.Title, NotificationConstant.ReturnToCreator.Message, ct);

                break;

            case PlanAnnouncementAction.AcceptorApprove:
                await this.AcceptorApproved(data, userId, req.Remark, ct);
                _ = await this.UpdateDocumentHistory(data, isReplace: true, hasPublicPlan: false, hasAcceptors: true, hasAssignees: false, ct);

                break;

            case PlanAnnouncementAction.DirectorAnnouncement:
                this.DirectorAnnouncement(data, userId);
                await this.SendNotificationRejectAssigneeAsync(data, NotificationConstant.InformAnnouncement.Title, NotificationConstant.InformAnnouncement.Message, ct);

                if (!string.IsNullOrWhiteSpace(req.AnnouncementTitle))
                {
                    data.SetAnnouncementTitle(req.AnnouncementTitle);
                }

                var plans = data.AnnouncementSelectedInformations.Select(x => x.Plan).ToList();

                await Task.WhenAll(plans.Select(async plan =>
                {
                    var creatorTargets = await this.dbContext.GetNotificationTargetsForUserAsync(UserId.From(plan.AuditInfo.CreatedBy), ct);
                    await Task.WhenAll(creatorTargets.Select(id => SendNotificationCreateByAnnouncementAsync(plan, id)));
                }));

                foreach (var targetUserId in await this.dbContext.GetAssigneeNotificationTargetsWithSecretariesAsync(data.Assignees, ct))
                {
                    await SendNotificationAcceptorAnnouncementAsync(data, targetUserId);
                }

                data.SetAnnouncementDate(DateTimeOffset.UtcNow);

                _ = await this.UpdateDocumentHistory(data, isReplace: true, hasPublicPlan: true, hasAcceptors: false, hasAssignees: false, ct);

                break;

            default: throw new InvalidOperationException($"Unknown plan announcement action: {req.Action}");
        }

        this.dbContext.PlanAnnouncements.Update(data);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private void AcceptorRejected(PlanAnnouncement announcement, UserId userId, string? remark)
    {
        var isRejected = announcement.Acceptors
                                     .Any(w => w.Status == AcceptorStatus.Rejected);

        if (isRejected)
        {
            this.ThrowError(
                "already rejected",
                StatusCodes.Status409Conflict);
        }

        var currentPendingUser = announcement.Acceptors
                                             .OrderBy(o => o.Sequence)
                                             .FirstOrDefault(w => w.Status == AcceptorStatus.Pending);

        if (currentPendingUser is null)
        {
            this.ThrowError(
                "There is no pending acceptor.",
                StatusCodes.Status409Conflict);
        }

        var acceptor =
            announcement.Acceptors
                        .Where(a => a.Status == AcceptorStatus.Pending)
                        .Map(DelegatorExtensions.DelegatorToAcceptor)
                        .OrderBy(a => a.Sequence)
                        .FirstOrDefault(a =>
                            a.IsActive &&
                            a.Delegatee == null
                                ? a.UserId == userId
                                : a.Delegatee?.SuUserId == userId);

        if (acceptor is null)
        {
            this.ThrowError(
                $"User {userId} does not have a pending accept.",
                StatusCodes.Status404NotFound);
        }

        var rejectUser = announcement.Acceptors.FirstOrDefault(w => w.Id == acceptor.Id);

        if (rejectUser is null)
        {
            this.ThrowError(
                $"User {userId} does not have an accept or approved.",
                StatusCodes.Status404NotFound);
        }

        if (rejectUser.Sequence != currentPendingUser.Sequence)
        {
            this.ThrowError(
                "The approval order is incorrect.",
                StatusCodes.Status409Conflict);
        }

        rejectUser
            .SetDelegatee(acceptor.DelegateeId)
            .Reject(remark);

        announcement.SetStatus(PlanAnnouncementStatus.Rejected, remark);
    }

    private async Task AcceptorApproved(
        PlanAnnouncement announcement,
        UserId userId,
        string? remark,
        CancellationToken ct)
    {
        var acceptor =
            announcement.Acceptors
                        .Where(a => a.Status == AcceptorStatus.Pending)
                        .Map(DelegatorExtensions.DelegatorToAcceptor)
                        .OrderBy(a => a.Sequence)
                        .FirstOrDefault(a =>
                            a.IsActive &&
                            a.Delegatee == null
                                ? a.UserId == userId
                                : a.Delegatee?.SuUserId.Value == userId.Value);

        if (acceptor is null)
        {
            this.ThrowError(
                $"User {userId} does not have a pending accept.",
                StatusCodes.Status404NotFound);
        }

        var approveUser = announcement.Acceptors.FirstOrDefault(w => w.Id == acceptor.Id);

        if (approveUser is null)
        {
            this.ThrowError(
                $"User {userId} does not have an accept or approved.",
                StatusCodes.Status404NotFound);
        }

        var currentPendingUser = announcement.Acceptors
                                             .OrderBy(o => o.Sequence)
                                             .FirstOrDefault(w => w.Status == AcceptorStatus.Pending);

        if (currentPendingUser is null)
        {
            this.ThrowError(
                "There is no pending acceptor.",
                StatusCodes.Status409Conflict);
        }

        if (approveUser.Sequence != currentPendingUser.Sequence)
        {
            this.ThrowError(
                "The approval order is incorrect.",
                StatusCodes.Status409Conflict);
        }

        approveUser
            .SetDelegatee(acceptor.DelegateeId)
            .Approve(remark);

        approveUser.SetIsCurrent(false);

        var nextAcceptor = announcement.Acceptors.Select(DelegatorExtensions.DelegatorToAcceptor)
                                       .FirstOrDefault(w => w.Sequence == currentPendingUser.Sequence + 1);

        if (nextAcceptor is not null)
        {
            nextAcceptor.SetIsCurrent(true);
        }

        var pendingOfType = announcement.Acceptors.Where(a => a.Status == AcceptorStatus.Pending).ToList();
        var isLastPending = pendingOfType.Count == 1 && pendingOfType[0].Id == nextAcceptor?.Id;

        if (nextAcceptor?.Type == AcceptorType.Approver && !isLastPending)
        {
            foreach (var targetUserId in await this.dbContext.GetNotificationTargetsWithSecretariesAsync(nextAcceptor, ct))
            {
                _ = SendNotificationAsync(
                    announcement,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.Plan.Name, announcement.PlanAnnouncementNumber));
            }
        }
        else if (nextAcceptor?.Type == AcceptorType.Approver && isLastPending)
        {
            foreach (var targetUserId in await this.dbContext.GetNotificationTargetsWithSecretariesAsync(nextAcceptor, ct))
            {
                _ = SendNotificationAsync(
                    announcement,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.Plan.Name, announcement.PlanAnnouncementNumber));
            }
        }

        var newStatus = announcement.Acceptors.All(w => w.Status == AcceptorStatus.Approved)
            ? PlanAnnouncementStatus.WaitingAnnouncement
            : PlanAnnouncementStatus.WaitingAcceptor;

        if (announcement.Acceptors.All(w => w.Status == AcceptorStatus.Approved))
        {
            var approvedPlans = announcement.AnnouncementSelectedInformations.Select(x => x.Plan).ToList();

            await Task.WhenAll(approvedPlans.Select(async plan =>
            {
                var creatorTargets = await this.dbContext.GetNotificationTargetsForUserAsync(UserId.From(plan.AuditInfo.CreatedBy), ct);
                await Task.WhenAll(creatorTargets.Select(id => SendNotificationCreateByAsync(plan, id)));

                var directorAssignee = plan.Assignees.FirstOrDefault(x => x.Type == AssigneeType.Director);
                if (directorAssignee is not null)
                {
                    var directorTargets = await this.dbContext.GetAssigneeNotificationTargetsWithSecretariesAsync(directorAssignee, ct);
                    await Task.WhenAll(directorTargets.Select(id => SendNotificationCreateByAsync(plan, id)));
                }
            }));
        }

        if (nextAcceptor is null)
        {
            var nextAssignee = announcement.Assignees
                                           .Select(DelegatorExtensions.DelegatorToAssignee)
                                           .FirstOrDefault(a => a.Type == AssigneeType.Director);

            if (nextAssignee is null)
            {
                return;
            }

            foreach (var targetUserId in await this.dbContext.GetAssigneeNotificationTargetsWithSecretariesAsync(nextAssignee, ct))
            {
                _ = SendNotificationAsync(
                    announcement,
                    targetUserId,
                    NotificationConstant.WaitForAnnouncement.Title,
                    string.Format(NotificationConstant.WaitForAnnouncement.Message, ProgramConstant.Plan.Name, announcement.PlanAnnouncementNumber));
            }
        }

        announcement.SetStatus(newStatus, remark);
    }

    private void DirectorAnnouncement(PlanAnnouncement announcement, UserId userId)
    {
        var hasPermission =
            announcement.Assignees
                        .Map(DelegatorExtensions.DelegatorToAssignee)
                        .Any(a =>
                            a.Delegatee == null
                                ? a.UserId == userId
                                : a.Delegatee?.SuUserId == userId
                                  && a.Type == AssigneeType.Director);

        if (!hasPermission)
        {
            throw new InvalidOperationException($"User {userId} does not have a permission.");
        }

        announcement.SetAnnouncementDate(DateTimeOffset.UtcNow);
        announcement.SetStatus(PlanAnnouncementStatus.Announcement);

        announcement.AnnouncementSelectedInformations
                    .Iter(r =>
                    {
                        r.Plan.SetStatus(PlanStatus.Announcement);

                        this.dbContext.Plans.Update(r.Plan);
                    });
    }

    private static async Task SendNotificationAsync(PlanAnnouncement planAnnouncement, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Plan)
              .SetReferenceId(planAnnouncement.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.PlanAnnouncement.Url, planAnnouncement.Id, planAnnouncement.PlanAnnouncementNumber), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private async Task SendNotificationRejectAssigneeAsync(PlanAnnouncement planAnnouncement, string title, string message, CancellationToken ct)
    {
        foreach (var targetUserId in await this.dbContext.GetAssigneeNotificationTargetsWithSecretariesAsync(planAnnouncement.Assignees.Where(x => x.Type != AssigneeType.Director), ct))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      title,
                      string.Format(message, ProgramConstant.PlanAnnouncement.Name, planAnnouncement.PlanAnnouncementNumber),
                      NotificationProgram.Plan)
                  .SetReferenceId(planAnnouncement.Id.Value)
                  .SetLinkUrl(
                      string.Format(ProgramConstant.PlanAnnouncement.Url, planAnnouncement.Id),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }

    private static async Task SendNotificationCreateByAsync(Plan plan, UserId user)
    {
        await Notification
              .Crate(
                  user,
                  NotificationConstant.InformCommittee.Title,
                  string.Format(NotificationConstant.InformCommittee.Message, ProgramConstant.Plan.Name, plan.PlanNumber),
                  NotificationProgram.Plan)
              .SetReferenceId(plan.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Plan.Url, plan.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationCreateByAnnouncementAsync(Plan plan, UserId user)
    {
        await Notification
              .Crate(
                  user,
                  NotificationConstant.InformAnnouncement.Title,
                  string.Format(NotificationConstant.InformAnnouncement.Message, ProgramConstant.Plan.Name, plan.PlanNumber),
                  NotificationProgram.Plan)
              .SetReferenceId(plan.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Plan.Url, plan.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationAcceptorAnnouncementAsync(PlanAnnouncement plan, UserId user)
    {
        await Notification
              .Crate(
                  user,
                  NotificationConstant.InformAnnouncement.Title,
                  string.Format(NotificationConstant.InformAnnouncement.Message, ProgramConstant.PlanAnnouncement.Name, plan.PlanAnnouncementNumber),
                  NotificationProgram.PlanAnnouncement)
              .SetReferenceId(plan.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.PlanAnnouncement.Url, plan.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}