namespace GHB.DP2.Application.Features.Plan.Plan;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.AnnouncementInfo;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Plan.Plan.Abstract;
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

public record PlanActionRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    PlanAction Action,
    string? Remark,
    Guid? JorPorId,
    string? AssignSegmentCode,
    string? GroupEgpNumber,
    string? EgpNumber,
    bool IsPlanDocumentIdReplace,
    bool IsPlanAnnouncementDocumentIdReplace,
    DateTimeOffset? DocumentDate,
    AssigneeRequest[]? Assignees,
    AcceptorRequest[]? Acceptors,
    AttachmentsDto[]? Attachments);

public class PlanActionRequestValidator : Validator<PlanActionRequest>
{
    public PlanActionRequestValidator()
    {
        this.RuleFor(r => r.Action)
            .IsInEnum();

        this.RuleFor(r => r.JorPorId)
            .NotNull()
            .When(w => w.Action is PlanAction.ApprovePlan);

        this.RuleFor(r => r.Assignees)
            .Must(w => w != null && w.Length != 0)
            .When(w => w.Action is PlanAction.AssignAssignee or PlanAction.ApprovedAssignee)
            .WithMessage("Assignee must be one or more.");

        this.RuleFor(r => r.Acceptors)
            .Must(w => w != null && w.Length != 0)
            .When(w => w.Action is PlanAction.AssignAcceptor or PlanAction.ConfirmAcceptor)
            .WithMessage("Acceptor  must be one or more.");
    }
}

public class ActionPlan : PlanEndpointBase<PlanActionRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public ActionPlan(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<ActionPlan> logger)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Plan")
             .WithName("ActionPlan")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Put("plan/action/{Id:guid}");
        this.AuditLog("รายการจัดซื้อจัดจ้าง", "อนุมัติ/ตีกลับ/แก้ไขแผนจัดซื้อจัดจ้าง");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(PlanActionRequest req, CancellationToken ct)
    {
        var plan = await this.dbContext
                             .Plans
                             .Include(plan => plan.Acceptors)
                             .ThenInclude(a => a.Delegatee)
                             .Include(plan => plan.Acceptors)
                             .ThenInclude(a => a.User)
                             .ThenInclude(u => u.Employee)
                             .Include(plan => plan.Assignees)
                             .Include(plan => plan.Attachments)
                             .Include(auditableEntity => auditableEntity.AuditInfo)
                             .Include(plan => plan.DocumentHistories)
                             .SingleOrDefaultAsync(p => p.Id == PlanId.From(req.Id), ct);

        if (plan is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลแผน");
        }

        var createByUserData = await this.dbContext.SuUsers
                                         .FirstOrDefaultAsync(x => x.Id == UserId.From(plan.AuditInfo.CreatedBy), ct);

        switch (req.Action)
        {
            case PlanAction.RejectPlan:
                this.RejectPlanHandler(plan, UserId.From(req.UserId), req.Remark);
                foreach (var targetUserId in await this.dbContext.GetNotificationTargetsForUserAsync(UserId.From(plan.AuditInfo.CreatedBy), ct))
                {
                    await SendNotificationAsync(plan, targetUserId, NotificationConstant.ReturnToCreator.Title, NotificationConstant.ReturnToCreator.Message);
                }

                break;

            case PlanAction.EditPlan:
                EditPlanHandler(plan);

                break;

            case PlanAction.ApprovePlan:
                await this.OnApprovePlanAsync(plan, req, ct);

                break;

            case PlanAction.AssigneeRejected:
                await this.OnAssigneeRejectedAsync(plan, req, ct);
                foreach (var targetUserId in await this.dbContext.GetNotificationTargetsForUserAsync(UserId.From(plan.AuditInfo.CreatedBy), ct))
                {
                    await SendNotificationAsync(plan, targetUserId, NotificationConstant.ReturnToCreator.Title, NotificationConstant.ReturnToCreator.Message);
                }

                break;

            case PlanAction.AssignAssignee:
            case PlanAction.ApprovedAssignee:
                await this.OnAssigneeOrApproveAssigneeAsync(plan, req, ct);

                break;

            case PlanAction.RecallDocument:
                this.OnRecallDocument(plan, req);

                break;

            case PlanAction.AssignAcceptor:
            case PlanAction.ConfirmAcceptor:
                await this.OnAssignOrConfirmAcceptorAsync(plan, req, ct);

                break;

            case PlanAction.ApprovedAcceptor:
                await this.OnApproveAcceptorAsync(plan, req, ct);

                break;

            case PlanAction.RejectedAcceptor:
                this.OnRejectAcceptor(plan, req);
                foreach (var targetUserId in await this.dbContext.GetAssigneeNotificationTargetsWithSecretariesAsync(plan.Assignees.Where(x => x.Type != AssigneeType.Director), ct))
                {
                    await SendNotificationAsync(plan, targetUserId, NotificationConstant.ReturnToCreator.Title, NotificationConstant.ReturnToCreator.Message);
                }

                break;

            case PlanAction.Announcement:
                await this.OnAnnouncementPlanAsync(plan, req, ct);

                break;

            case PlanAction.ClosePlan:
                plan.SetClosed(req.Remark);
                AddClosedAttachments(plan, req.Attachments);

                break;

            case PlanAction.CancelClosePlan:
                plan.SetCancelClosed();

                break;

            default:
                throw new InvalidOperationException($"unknown plan action {req.Action}");
        }

        this.dbContext.Plans.Update(plan);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static void AddClosedAttachments(Plan plan, AttachmentsDto[]? attachments)
    {
        if (attachments is null || attachments.Length == 0)
        {
            return;
        }

        var existingFileIds = plan.Attachments.Select(a => a.FileId).ToHashSet();
        var sequence = plan.Attachments.Count == 0 ? 0 : plan.Attachments.Max(a => a.Sequence);

        var newFiles = attachments
                       .SelectMany(a => a.FileAttachments.Select(f => new { a.DocumentTypeCode, File = f }))
                       .Where(x => !existingFileIds.Contains(FileId.From(x.File.FileId)))
                       .ToArray();

        foreach (var item in newFiles)
        {
            sequence++;
            plan.AddAttachment(PlanAttachments.Create(
                ParameterCode.From(item.DocumentTypeCode),
                FileId.From(item.File.FileId),
                item.File.FileName,
                sequence,
                item.File.IsPublic));
        }
    }

    private async Task OnAnnouncementPlanAsync(Plan plan, PlanActionRequest req, CancellationToken ct)
    {
        await this.AnnouncementPlanAsync(plan, UserId.From(req.UserId), ct);

        if (this.PlanDocumentCondition(plan))
        {
            await this.UpdateDocumentAsync(
                plan,
                isReplace: true,
                hasAcceptors: false,
                hasAssignees: false,
                hasPublish: true,
                skipUpdateDocument: req.Action == PlanAction.AssignAcceptor,
                cancellationToken: ct);
        }

        var planType = plan.SupplyMethod.Code == SupplyMethodConstant.Sixty ? Section60.Plan : Section80.Plan;

        var file = await this.fileServiceClient.DownloadAsStreamAsync(plan.Document!.FileId, cancellationToken: ct);

        await AnnouncementData.Create(
                                  plan.Name,
                                  DateTimeOffset.UtcNow,
                                  plan.Budget,
                                  string.Empty,
                                  planType,
                                  file?.Stream)
                              .PublishEvent(ct);
    }

    private void OnRejectAcceptor(Plan plan, PlanActionRequest req)
    {
        this.AcceptorRejected(plan, UserId.From(req.UserId), req.Remark);

        // Add new documentHistory
        if (this.PlanDocumentCondition(plan))
        {
            plan.AddDocumentHistory(PlanDocumentType.Announcement, plan.AnnouncementDocument?.FileId);

            var planDocFileId = (plan.Type == PlanType.InYearPlan && plan.Budget > 500000) ? plan.DocumentByStatus(PlanStatus.DraftRecordDocument)?.FileId : plan.Document?.FileId;

            plan.AddDocumentHistory(PlanDocumentType.Plan, planDocFileId);
        }
    }

    private async Task OnApproveAcceptorAsync(Plan plan, PlanActionRequest req, CancellationToken ct)
    {
        await this.AcceptorApproved(plan, UserId.From(req.UserId), req.Remark, ct);

        await this.UpdateSequentialCurrentsAsync(plan, AcceptorType.Approver, ct);

        if (this.PlanDocumentCondition(plan))
        {
            await this.UpdateDocumentAsync(
                plan,
                isReplace: true,
                hasAcceptors: true,
                hasAssignees: false,
                hasPublish: false,
                skipUpdateDocument: req.Action == PlanAction.AssignAcceptor,
                cancellationToken: ct);
        }
    }

    private async Task OnAssignOrConfirmAcceptorAsync(Plan plan, PlanActionRequest req, CancellationToken ct)
    {
        if ((plan.SupplyMethodCode == SupplyMethodConstant.Sixty && plan.Type == PlanType.InYearPlan && plan.Budget > 500000)
            && (string.IsNullOrEmpty(req.GroupEgpNumber) || string.IsNullOrEmpty(req.EgpNumber)))
        {
            this.ThrowError("กรุณาระบุเลขกลุ่ม e-GP/เลขที่ e-GP", StatusCodes.Status404NotFound);
        }

        await this.ManageAssigneeAsync(plan, req.Assignees!, UserId.From(req.UserId), ct);
        var acceptors = req.Acceptors?.ToArray() ?? [];
        await this.MangeAcceptorsAsync(plan, acceptors, UserId.From(req.UserId), ct);

        if (req.Action == PlanAction.ConfirmAcceptor
            || req.DocumentDate is not null)
        {
            plan.SetDocumentDate(req.DocumentDate);
        }

        if (req.Action == PlanAction.AssignAcceptor)
        {
            plan.SetDraftRecordDocument(plan.Status);
        }

        if (req.GroupEgpNumber is not null)
        {
            plan.SetGroupEgpNumber(req.GroupEgpNumber);
        }

        if (req.EgpNumber is not null)
        {
            plan.SetEgpNumber(req.EgpNumber);
        }

        if (req.Action == PlanAction.ConfirmAcceptor)
        {
            plan.Acceptors
                .Where(w => w.Type == AcceptorType.Approver)
                .Iter(r => r.Pending());

            plan.SetWaitingAcceptor();

            var firstAcceptor = plan.Acceptors.Select(DelegatorExtensions.DelegatorToAcceptor).First(w => w is { Type: AcceptorType.Approver, IsCurrent: true });

            foreach (var targetUserId in await this.dbContext.GetNotificationTargetsWithSecretariesAsync(firstAcceptor, ct))
            {
                _ = SendNotificationAsync(
                    plan,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    NotificationConstant.WaitForLike.Message);
            }
        }

        if (this.PlanDocumentCondition(plan))
        {
            await this.UpdateDocumentAsync(
                plan,
                isReplace: true,
                hasAcceptors: false,
                hasAssignees: false,
                hasPublish: false,
                skipUpdateDocument: false,
                isPlanAnnouncementDocumentIdReplace: req.IsPlanAnnouncementDocumentIdReplace,
                cancellationToken: ct);
        }
    }

    private void OnRecallDocument(Plan plan, PlanActionRequest req)
    {
        this.RecallDocument(plan, UserId.From(req.UserId));

        if (this.PlanDocumentCondition(plan))
        {
            var planDocFileId = (plan.Type == PlanType.InYearPlan && plan.Budget > 500000) ? plan.DocumentByStatus(PlanStatus.DraftRecordDocument)?.FileId : plan.Document?.FileId;

            plan.AddDocumentHistory(PlanDocumentType.Announcement, plan.AnnouncementDocument?.FileId);
            plan.AddDocumentHistory(PlanDocumentType.Plan, planDocFileId);
        }
    }

    private async Task OnAssigneeOrApproveAssigneeAsync(Plan plan, PlanActionRequest req, CancellationToken ct)
    {
        await this.ManageAssigneeAsync(plan, req.Assignees!, UserId.From(req.UserId), ct);

        if (req.Action == PlanAction.ApprovedAssignee)
        {
            plan.SetDraftRecordDocument(PlanStatus.DraftRecordDocument);

            foreach (var targetUserId in await this.dbContext.GetAssigneeNotificationTargetsWithSecretariesAsync(plan.Assignees.Where(x => x.Type != AssigneeType.Director), ct))
            {
                await SendNotificationAsync(plan, targetUserId, NotificationConstant.Assignment.Title, NotificationConstant.Assignment.Message);
            }
        }

        if (req.AssignSegmentCode is not null)
        {
            plan.SetAssignSegment(ParameterCode.From(req.AssignSegmentCode));
        }

        if (this.PlanDocumentCondition(plan) && req.Action == PlanAction.ApprovedAssignee)
        {
            await this.UpdateDocumentAsync(plan, true, hasAcceptors: false, hasAssignees: false, hasPublish: false, cancellationToken: ct);
        }
    }

    private async Task OnAssigneeRejectedAsync(Plan plan, PlanActionRequest req, CancellationToken ct)
    {
        this.AssigneeRejected(plan, UserId.From(req.UserId), req.Remark);

        if (this.PlanDocumentCondition(plan))
        {
            await this.UpdateDocumentAsync(plan, false, hasAcceptors: false, hasAssignees: false, hasPublish: false, cancellationToken: ct);
        }
    }

    private async Task OnApprovePlanAsync(Plan plan, PlanActionRequest req, CancellationToken ct)
    {
        await this.ApprovePlanHandler(plan, UserId.From(req.UserId), req.Remark, ct, req.JorPorId);

        if (this.PlanDocumentCondition(plan))
        {
            if (plan.DocumentHistories.Count == 0)
            {
                await this.SetDefaultDocumentTemplate(plan, ct);
            }

            await this.UpdateDocumentAsync(plan, true, hasAcceptors: false, hasAssignees: true, hasPublish: false, forceStampReplace: true, cancellationToken: ct);
        }
    }

    private void AssigneeRejected(
        Plan plan,
        UserId userId,
        string? remark)
    {
        var assignee = plan.Assignees
                           .OrderBy(o => o.Sequence)
                           .Select(DelegatorExtensions.DelegatorToAssignee)
                           .FirstOrDefault(a => a.Delegatee == null
                               ? a.UserId == userId
                               : a.Delegatee?.SuUserId == userId);

        if (assignee is null)
        {
            this.ThrowError("ไม่พบผู้รับผิดชอบ", StatusCodes.Status404NotFound);
        }

        var user = plan.Assignees.FirstOrDefault(u => u.Id == assignee.Id);

        if (user is null)
        {
            this.ThrowError("User not found in acceptors.", StatusCodes.Status404NotFound);
        }

        user.SetDelegatee(assignee.DelegateeId)
            .Reject(remark);

        plan.SetAssigneeRejected(PlanStatus.RejectPlan);
    }

    private void RejectPlanHandler(
        Plan plan,
        UserId userId,
        string? remark)
    {
        var acceptor =
            plan.Acceptors
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
            this.ThrowError("User not found in acceptors.", StatusCodes.Status404NotFound);
        }

        var user = plan.Acceptors.FirstOrDefault(u => u.Id == acceptor.Id);

        if (user is null)
        {
            this.ThrowError("User not found in acceptors.", StatusCodes.Status404NotFound);
        }

        user.SetDelegatee(acceptor.DelegateeId)
            .Reject(remark);

        plan.SetDepartmentRejected(PlanStatus.RejectPlan, remark);
    }

    private static void EditPlanHandler(Plan plan)
    {
        plan.SetEdit(PlanStatus.EditPlan);
        plan.Acceptors.Iter(a => a.Draft());
    }

    private async Task ApprovePlanHandler(
        Plan plan,
        UserId userId,
        string? remark,
        CancellationToken ct,
        Guid? jorPorId = default)
    {
        var currentPlanAcceptor =
            plan.Acceptors
                .Where(a => a.Status == AcceptorStatus.Pending)
                .Map(DelegatorExtensions.DelegatorToAcceptor)
                .OrderBy(a => a.Sequence)
                .FirstOrDefault(a =>
                    a.IsActive &&
                    a.Delegatee?.SuUserId == null
                        ? a.UserId == userId
                        : a.Delegatee?.SuUserId == userId);

        if (currentPlanAcceptor is null)
        {
            this.ThrowError(
                "ไม่พบข้อมูลผู้มีอำนาจเห็นชอบ",
                StatusCodes.Status404NotFound);
        }

        var currentPlanAcceptorUser =
            plan.Acceptors
                .FirstOrDefault(u => u.Id == currentPlanAcceptor.Id);

        if (currentPlanAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบข้อมูลผู้ใช้งาน",
                StatusCodes.Status404NotFound);
        }

        currentPlanAcceptorUser
            .SetDelegatee(currentPlanAcceptor.DelegateeId)
            .Approve(remark);

        plan.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.ApprovedDepartment,
                $"เผยแพร่ข้อมูลรายการจัดซื้อจัดจ้าง",
                plan.Status.ToString(),
                remark));

        var acceptorType = currentPlanAcceptor.Type;

        var isDepartmentAcceptorsApproved =
            plan.Acceptors
                .Where(w => w.Type is AcceptorType.DepartmentDirectorAgree)
                .All(w => w.Status is AcceptorStatus.Approved);

        await this.UpdateSequentialCurrentsAsync(plan, acceptorType, ct);

        if (!isDepartmentAcceptorsApproved)
        {
            return;
        }

        switch (plan.IsCancel, plan.IsChange, plan.Budget, plan.Type)
        {
            case (true, false, > 500000, _):
            case (false, true, > 500000, _):
            case (false, false, > 500000, PlanType.InYearPlan):
                if (jorPorId.HasValue && !plan.Assignees.Any(w => w.Type is AssigneeType.Director))
                {
                    await this.AddAssigneeDirectorAsync(plan, UserId.From(jorPorId.Value), ct);
                }

                plan.SetWaitingAssign(PlanStatus.WaitingAssign);

                break;

            case (false, false, <= 500000, _):
            case (false, false, _, PlanType.AnnualPlan):
                plan.SetApprovePlan();
                foreach (var targetUserId in await this.dbContext.GetNotificationTargetsForUserAsync(UserId.From(plan.AuditInfo.CreatedBy), ct))
                {
                    await SendNotificationCreateByAsync(plan, targetUserId, ct);
                }

                break;

            case (true, false, <= 500000, _):
                await this.SetRefPlan(plan, ct);
                plan.SetCancelPlan();
                foreach (var targetUserId in await this.dbContext.GetNotificationTargetsForUserAsync(UserId.From(plan.AuditInfo.CreatedBy), ct))
                {
                    await SendNotificationCreateByAsync(plan, targetUserId, ct);
                }

                break;

            case (false, true, <= 500000, _):
                await this.SetRefPlan(plan, ct);
                plan.SetApprovePlan();
                foreach (var targetUserId in await this.dbContext.GetNotificationTargetsForUserAsync(UserId.From(plan.AuditInfo.CreatedBy), ct))
                {
                    await SendNotificationCreateByAsync(plan, targetUserId, ct);
                }

                break;
        }
    }

    private async Task SetRefPlan(Plan plan, CancellationToken ct)
    {
        if (plan.ReferenceId.HasValue)
        {
            var oldPlan = await this.dbContext.Plans
                                    .SingleOrDefaultAsync(c => c.Id == plan.ReferenceId!, ct);

            if (oldPlan is null)
            {
                this.ThrowError("ไม่พบแผนต้นทาง", statusCode: StatusCodes.Status404NotFound);
            }

            oldPlan.SetIsActive(false);
            this.dbContext.Plans.Update(oldPlan);
        }
    }

    private async Task AddAssigneeDirectorAsync(Plan plan, UserId jorPorId, CancellationToken ct)
    {
        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .ThenInclude(s => s.View)
                             .Where(s => s.Id == jorPorId)
                             .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            this.ThrowError("ไม่พบข้อมูลผู้อำนวยการฝ่าย", statusCode: StatusCodes.Status404NotFound);
        }

        plan.AddAssignee(PlanAssignee.Create(AssigneeType.Director, user, 1));

        var directors = plan.Assignees.FirstOrDefault(a => a.Group == AssigneeGroup.JorPor);

        if (directors is null)
        {
            return;
        }

        foreach (var targetUserId in await this.dbContext.GetAssigneeNotificationTargetsWithSecretariesAsync(directors, ct))
        {
            _ = SendNotificationAsync(plan, targetUserId, NotificationConstant.WaitForAssignment.Title, NotificationConstant.WaitForAssignment.Message);
        }
    }

    private async Task ManageAssigneeAsync(
        Plan plan,
        AssigneeRequest[] assignees,
        UserId userId,
        CancellationToken ct)
    {
        _ = plan.Assignees
                .ExceptBy(
                    assignees
                        .Where(w => w.Id.HasValue)
                        .Select(s => s.Id.Value),
                    a => a.Id.Value)
                .Iter(r => plan.RemoveAssigneeById(r.Id));

        _ = assignees.Where(w => w.Id.HasValue)
                     .Join(
                         plan.Assignees,
                         db => db.Id.Value,
                         payload => payload.Id.Value,
                         (payload, db) => new { db, payload })
                     .Iter(r => r.db.SetSequence(r.payload.Sequence)
                                    .SetSendToAcceptorId(userId));

        var newIds = assignees
                     .Where(w => !w.Id.HasValue)
                     .Select(s => UserId.From(s.UserId))
                     .ToArray();

        var userData = await this.dbContext.SuUsers
                                 .Include(e => e.Employee)
                                 .ThenInclude(v => v.View)
                                 .Where(w => newIds.Contains(w.Id))
                                 .ToArrayAsync(ct);

        this.ValidateUsers(userData, newIds);

        var newAssignees = assignees.Where(x => x.AssigneeType == AssigneeType.Assignee && !x.Id.HasValue).ToArray();

        var lastAssigneeUserId = plan.Assignees
                .Where(a => a.Type == AssigneeType.Assignee)
                .OrderByDescending(a => a.Sequence)
                .Select(a => (UserId?)a.UserId)
                .FirstOrDefault();

        _ = assignees.Where(w => !w.Id.HasValue)
                     .Join(
                         userData,
                         a => UserId.From(a.UserId),
                         u => u.Id,
                         (a, u) => PlanAssignee.Create(
                             a.AssigneeType,
                             u,
                             a.Sequence))
                     .Iter(a =>
                     {
                         a.SetSendToAcceptorId(lastAssigneeUserId ?? userId);
                         plan.AddAssignee(a);
                     });

        var newAssigneeUserIds = newAssignees.Select(s => UserId.From(s.UserId)).ToHashSet();

        foreach (var targetUserId in await this.dbContext.GetAssigneeNotificationTargetsWithSecretariesAsync(plan.Assignees.Where(a => a.Type == AssigneeType.Assignee && newAssigneeUserIds.Contains(a.UserId)), ct))
        {
            await SendNotificationAsync(
                plan,
                targetUserId,
                NotificationConstant.Assignment.Title,
                NotificationConstant.Assignment.Message);
        }

        plan.SetWaitingAssign(plan.Status);
    }

    private async Task MangeAcceptorsAsync(
        Plan plan,
        AcceptorRequest[] acceptors,
        UserId userId,
        CancellationToken ct)
    {
        if (plan.Assignees.Select(DelegatorExtensions.DelegatorToAssignee)
                .All(w => w.Delegatee == null
                    ? w.UserId != userId
                    : w.Delegatee?.SuUserId != userId))
        {
            this.ThrowError(
                "ไม่มีสิทธิ์เพิ่มผู้รับผิดชอบ",
                StatusCodes.Status400BadRequest);
        }

        _ = plan.Acceptors
                .Where(w => !acceptors.Select(s => s.Id).Contains(w.Id.Value))
                .Iter(r => plan.RemoveAcceptor(r));

        var userIds = acceptors.Map(a => a.UserId)
                               .Map(UserId.From)
                               .ToArray();

        var users = await this.dbContext.SuUsers
                              .Include(suUser => suUser.Employee)
                              .ThenInclude(rawEmployee => rawEmployee.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(ct);
        var userExists
            = userIds.Except(users.Map(u => u.Id)).ToArray();

        if (userExists.Length > 0)
        {
            this.ThrowError(
                "ไม่พบผู้ใช้งาน",
                StatusCodes.Status404NotFound);
        }

        _ = acceptors.Where(w => !w.Id.HasValue)
                     .Join(
                         users,
                         req => UserId.From(req.UserId),
                         usr => usr.Id,
                         (req, usr) => PlanAcceptor.Create(req.AcceptorType, usr, req.Sequence, plan.DepartmentId))
                     .Iter(r => plan.AddAcceptor(r));

        var lastAssigneeUserId = plan.Assignees
            .Where(a => a.Type == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)a.UserId)
            .FirstOrDefault();

        foreach (var existing in plan.Acceptors)
        {
            var match = acceptors.FirstOrDefault(e => e.UserId == existing.UserId && existing.Type == e.AcceptorType);

            if (match != null)
            {
                existing.SetSequence(match.Sequence)
                        .SetActive()
                        .SetSendToAcceptorId(
                            lastAssigneeUserId
                            ?? userId);
            }
        }
    }

    private void RecallDocument(Plan plan, UserId userId)
    {
        if (plan.Acceptors.Where(a => a.Type == AcceptorType.Approver)
                .Any(w => w.Status is AcceptorStatus.Approved or AcceptorStatus.Rejected))
        {
            this.ThrowError(
                "ผู้มีอำนาจเห็นชอบ/อนุมัติดำเนินการแล้ว",
                StatusCodes.Status403Forbidden);
        }

        if (plan.Assignees.All(w => w.UserId != userId))
        {
            this.ThrowError(
                "ไม่มีสิทธิ์ขอเปลี่ยนแปลงข้อมูล",
                StatusCodes.Status403Forbidden);
        }

        plan.Acceptors
            .Where(w => w.Type == AcceptorType.Approver)
            .Iter(r => r.Draft());
        plan.RecallDocument();
    }

    private async Task AcceptorApproved(Plan plan, UserId userId, string? remark, CancellationToken ct)
    {
        var acceptor =
            plan.Acceptors
                .Where(a => a.Status == AcceptorStatus.Pending)
                .Map(DelegatorExtensions.DelegatorToAcceptor)
                .OrderBy(a => a.Sequence)
                .FirstOrDefault(a => a.Delegatee == null
                    ? a.UserId == userId
                    : a.Delegatee?.SuUserId == userId && a.IsActive);

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบข้อมูลผู้ใช้งาน",
                StatusCodes.Status404NotFound);
        }

        var approveUser = plan.Acceptors.FirstOrDefault(w => w.Id == acceptor.Id);

        if (approveUser is null)
        {
            this.ThrowError(
                "ไม่พบข้อมูลผู้ใช้งาน",
                StatusCodes.Status404NotFound);
        }

        var currentPendingUser = plan.Acceptors
                                     .OrderBy(o => o.Sequence)
                                     .FirstOrDefault(w => w.Status == AcceptorStatus.Pending);

        if (currentPendingUser is null)
        {
            this.ThrowError(
                "ผู้มีอำนาจเห็นชอบต้องมีมากกว่าหนึ่งคน",
                StatusCodes.Status409Conflict);
        }

        if (approveUser.Sequence != currentPendingUser.Sequence)
        {
            this.ThrowError(
                "ลำดับผู้มีอำนาจเห็นชอบไม่ถูกต้อง",
                StatusCodes.Status409Conflict);
        }

        approveUser
            .SetDelegatee(acceptor.DelegateeId)
            .Approve(remark);

        plan.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Approved,
                string.Empty,
                plan.Status.ToString(),
                remark));

        if (plan.IsChange)
        {
            await this.SetRefPlan(plan, ct);
        }

        if (plan.Acceptors.All(w => w.Status == AcceptorStatus.Approved))
        {
            plan.SetWaitingAnnouncement();

            foreach (var targetUserId in await this.dbContext.GetNotificationTargetsForUserAsync(UserId.From(plan.AuditInfo.CreatedBy), ct))
            {
                await SendNotificationCreateByAsync(plan, targetUserId, ct);
            }

            var assigneesDirector = plan.Assignees
                                        .Where(x => x.Type == AssigneeType.Director)
                                        .ToList();

            foreach (var targetUserId in await this.dbContext.GetAssigneeNotificationTargetsWithSecretariesAsync(assigneesDirector, ct))
            {
                await SendNotificationCreateByAsync(plan, targetUserId, ct);
            }
        }
    }

    private void AcceptorRejected(Plan plan, UserId userId, string? remark)
    {
        var isRejected = plan.Acceptors
                             .Map(DelegatorExtensions.DelegatorToAcceptor)
                             .Any(w => w.Status == AcceptorStatus.Rejected);

        if (isRejected)
        {
            this.ThrowError(
                "ตีกลับเอกสารเรียบร้อยแล้ว",
                StatusCodes.Status400BadRequest);
        }

        var currentPendingUser = plan.Acceptors
                                     .Where(w => w.Status == AcceptorStatus.Pending)
                                     .Map(DelegatorExtensions.DelegatorToAcceptor)
                                     .OrderBy(o => o.Sequence)
                                     .FirstOrDefault() ??
                                 throw new InvalidOperationException($"Plan error, Acceptor must be one or more.");

        var rejectedUser = plan.Acceptors
                               .Where(w => w.Status == AcceptorStatus.Pending)
                               .Map(DelegatorExtensions.DelegatorToAcceptor)
                               .OrderBy(o => o.Sequence)
                               .FirstOrDefault() ??
                           throw new InvalidOperationException($"User {userId} does not have an accept or rejected.");

        if (rejectedUser.Sequence != currentPendingUser.Sequence)
        {
            this.ThrowError(
                "ลำดับผู้มีอำนาจเห็นชอบไม่ถูกต้อง",
                StatusCodes.Status400BadRequest);
        }

        var user = plan.Acceptors.FirstOrDefault(u => u.Id == rejectedUser.Id);

        if (user is null)
        {
            this.ThrowError("User not found in acceptors.", StatusCodes.Status404NotFound);
        }

        user.SetDelegatee(rejectedUser.DelegateeId)
            .Reject(remark);

        plan.SetRejectToAssignee(PlanStatus.RejectToAssignee, remark);
    }

    private async Task AnnouncementPlanAsync(Plan plan, UserId userId, CancellationToken ct)
    {
        if (!plan.Assignees.Select(DelegatorExtensions.DelegatorToAssignee)
                 .Any(w => w.Delegatee == null
                     ? w.UserId == userId
                     : w.Delegatee.SuUserId == userId
                       && w.Type == AssigneeType.Director))
        {
            this.ThrowError("ไม่มีสิทธิ์ในการเผยแพร่แผน", StatusCodes.Status400BadRequest);
        }

        _ = plan.IsCancel ? plan.SetCancelPlan() : plan.SetAnnouncement();

        await this.SetRefPlan(plan, ct);

        foreach (var targetUserId in await this.dbContext.GetNotificationTargetsForUserAsync(UserId.From(plan.AuditInfo.CreatedBy), ct))
        {
            await SendNotificationCreateByAnnouncementAsync(plan, targetUserId, ct);
        }

        foreach (var targetUserId in await this.dbContext.GetAssigneeNotificationTargetsWithSecretariesAsync(plan.Assignees, ct))
        {
            await SendNotificationAcceptorAnnouncementAsync(plan, targetUserId, ct);
        }
    }

    private void ValidateUsers(SuUser[] users, UserId[] userIds)
    {
        var foundUserIds = users.Select(u => u.Id).ToArray();

        var missingUserIds = userIds.Except(foundUserIds).ToArray();

        if (missingUserIds.Length > 0)
        {
            this.ThrowError("ไม่พบผู้ใช้งาน", StatusCodes.Status404NotFound);
        }
    }

    private async Task UpdateSequentialCurrentsAsync(Plan plan, AcceptorType type, CancellationToken ct)
    {
        var approvers = plan.Acceptors
                            .Where(a => a.Type == type && a.IsActive)
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
                            .FirstOrDefault(a =>
                                a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            var assignees = plan.Assignees.Where(w => w.Type == AssigneeType.Director)
                                .Select(DelegatorExtensions.DelegatorToAssignee)
                                .OrderBy(a => a.Sequence)
                                .FirstOrDefault();

            if (assignees == null)
            {
                return;
            }

            if (type == AcceptorType.DepartmentDirectorAgree)
            {
                foreach (var targetUserId in await this.dbContext.GetAssigneeNotificationTargetsWithSecretariesAsync(assignees, ct))
                {
                    _ = SendNotificationAsync(
                        plan,
                        targetUserId,
                        NotificationConstant.WaitForAssignment.Title,
                        NotificationConstant.WaitForAssignment.Message);
                }
            }
            else
            {
                foreach (var targetUserId in await this.dbContext.GetAssigneeNotificationTargetsWithSecretariesAsync(assignees, ct))
                {
                    _ = SendNotificationAsync(
                        plan,
                        targetUserId,
                        NotificationConstant.WaitForAnnouncement.Title,
                        NotificationConstant.WaitForAnnouncement.Message);
                }
            }

            return;
        }

        var pendingOfType = approvers.Where(a => a.Status == AcceptorStatus.Pending).ToList();
        var isLastPending = pendingOfType.Count == 1 && pendingOfType[0].Id == next.Id;

        if (next.Type == AcceptorType.DepartmentDirectorAgree || (next.Type == AcceptorType.Approver && !isLastPending))
        {
            foreach (var targetUserId in await this.dbContext.GetNotificationTargetsWithSecretariesAsync(next, ct))
            {
                _ = SendNotificationAsync(
                    plan,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    NotificationConstant.WaitForLike.Message);
            }
        }
        else if (next.Type == AcceptorType.Approver && isLastPending)
        {
            foreach (var targetUserId in await this.dbContext.GetNotificationTargetsWithSecretariesAsync(next, ct))
            {
                _ = SendNotificationAsync(
                    plan,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    NotificationConstant.WaitForApprove.Message);
            }
        }

        next.SetCurrent(true);
    }

    private static string GetPlanActionName(Plan plan) =>
        plan.IsChange ? ProgramConstant.ChangePlan.Name :
        plan.IsCancel ? ProgramConstant.CancelPlan.Name :
        ProgramConstant.Plan.Name;

    private static async Task SendNotificationRejectAsync(Plan plan, PlanAction action, string title, string message)
    {
        if (action == PlanAction.RejectedAcceptor)
        {
            foreach (var targetUserId in plan.Assignees.Where(x => x.Type != AssigneeType.Director).SelectMany(a => a.GetAssigneeNotificationTargets()))
            {
                await Notification
                      .Crate(
                          targetUserId,
                          NotificationConstant.Assignment.Title,
                          string.Format(NotificationConstant.Assignment.Message, GetPlanActionName(plan), plan.PlanNumber),
                          NotificationProgram.Plan)
                      .SetReferenceId(plan.Id.Value)
                      .SetLinkUrl(
                          string.Format(ProgramConstant.Plan.Url, plan.Id),
                          "ดูรายละเอียด")
                      .PublishAsync(CancellationToken.None);
            }

            return;
        }

        await Notification
              .Crate(
                  UserId.From(plan.AuditInfo.CreatedBy),
                  title,
                  string.Format(message, GetPlanActionName(plan), plan.PlanNumber),
                  NotificationProgram.Plan)
              .SetReferenceId(plan.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Plan.Url, plan.Id, plan.PlanNumber), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationAsync(Plan plan, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  string.Format(message, GetPlanActionName(plan), plan.PlanNumber),
                  NotificationProgram.Plan)
              .SetReferenceId(plan.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Plan.Url, plan.Id, plan.PlanNumber), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationAssigneeAsync(Plan plan, CancellationToken ct)
    {
        foreach (var targetUserId in plan.Assignees.Where(x => x.Type != AssigneeType.Director).SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.Assignment.Title,
                      string.Format(NotificationConstant.Assignment.Message, GetPlanActionName(plan), plan.PlanNumber),
                      NotificationProgram.Plan)
                  .SetReferenceId(plan.Id.Value)
                  .SetLinkUrl(
                      string.Format(ProgramConstant.Plan.Url, plan.Id),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }

    private static async Task SendNotificationCreateByAsync(Plan plan, UserId user, CancellationToken ct)
    {
        var (title, message) = (plan.IsChange || plan.IsCancel)
            ? (NotificationConstant.PlanActionApproved.Title, NotificationConstant.PlanActionApproved.Message)
            : (NotificationConstant.InformCommittee.Title, NotificationConstant.InformCommittee.Message);

        await Notification
              .Crate(
                  user,
                  title,
                  string.Format(message, GetPlanActionName(plan), plan.PlanNumber),
                  NotificationProgram.Plan)
              .SetReferenceId(plan.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Plan.Url, plan.Id), "ดูรายละเอียด")
              .PublishAsync(ct);
    }

    private static async Task SendNotificationCreateByAnnouncementAsync(Plan plan, UserId user, CancellationToken ct)
    {
        await Notification
              .Crate(
                  user,
                  NotificationConstant.InformAnnouncement.Title,
                  string.Format(NotificationConstant.InformAnnouncement.Message, GetPlanActionName(plan), plan.PlanNumber),
                  NotificationProgram.Plan)
              .SetReferenceId(plan.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Plan.Url, plan.Id), "ดูรายละเอียด")
              .PublishAsync(ct);
    }

    private static async Task SendNotificationAcceptorAnnouncementAsync(Plan plan, UserId user, CancellationToken ct)
    {
        await Notification
              .Crate(
                  user,
                  NotificationConstant.InformAnnouncement.Title,
                  string.Format(NotificationConstant.InformAnnouncement.Message, GetPlanActionName(plan), plan.PlanNumber),
                  NotificationProgram.Plan)
              .SetReferenceId(plan.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Plan.Url, plan.Id), "ดูรายละเอียด")
              .PublishAsync(ct);
    }
}