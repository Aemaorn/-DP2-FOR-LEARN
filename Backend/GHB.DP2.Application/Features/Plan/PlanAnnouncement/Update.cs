namespace GHB.DP2.Application.Features.Plan.PlanAnnouncement;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Plan.PlanAnnouncement.Abstract;
using GHB.DP2.Application.Features.Plan.PlanAnnouncement.DTO;
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

public record UpdatePlanAnnouncementRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    string? GroupEgpNumber,
    string? Remark,
    string? AnnouncementTitle,
    string? Telephone,
    DateTimeOffset? AnnouncementDate,
    DateTimeOffset? DocumentDate,
    PlanAnnouncementStatus Status,
    Guid? AnnouncementDocumentId,
    bool? IsAnnouncementDocumentIdReplace,
    Guid? ApproveDocumentId,
    bool? IsApproveDocumentIdReplace,
    PlanSelectedRequest[] PlanSelected,
    AttachmentsDto[] Attachments,
    AssigneeRequest[] Assignees,
    AcceptorRequest[]? Acceptors,
    DateTimeOffset? LastModifiedAt);

public class UpdatePlanAnnouncementRequestValidator : Validator<UpdatePlanAnnouncementRequest>
{
    public UpdatePlanAnnouncementRequestValidator()
    {
        this.RuleFor(r => r.PlanSelected)
            .Must(x => x.Length != 0)
            .When(x => x.Status == PlanAnnouncementStatus.WaitingAssign || x.Status == PlanAnnouncementStatus.WaitingAcceptor)
            .WithMessage("ต้องมีแผนอย่างน้อยหนึ่งแผน");

        this.RuleForEach(r => r.PlanSelected)
            .ChildRules(pp =>
            {
                pp.RuleFor(r => r.PlanId)
                  .NotEmpty()
                  .WithMessage("Selected PlanId is required");
            });

        this.RuleForEach(x => x.Attachments)
            .ChildRules(attachment =>
            {
                attachment.RuleFor(a => a.DocumentTypeCode)
                          .NotEmpty()
                          .WithMessage("Document type code is required.");

                attachment.RuleForEach(a => a.FileAttachments)
                          .ChildRules(file =>
                          {
                              file.RuleFor(a => a.FileId)
                                  .NotEmpty()
                                  .WithMessage("File ID is required.");

                              file.RuleFor(a => a.FileName)
                                  .NotEmpty()
                                  .WithMessage("File name is required.");

                              file.RuleFor(a => a.IsPublic)
                                  .NotNull()
                                  .WithMessage("IsPublic must be specified.");
                          });
            });

        this.RuleFor(r => r.Status)
            .IsInEnum()
            .WithMessage("Invalid plan announcement type.");
    }
}

public record UpdatePlanAnnouncementResponse(Guid? NewApproveDocumentFileId, Guid? NewAnnouncementDocumentFileId);

public class UpdatePlanAnnouncement : PlanAnnouncementEndpointBase<UpdatePlanAnnouncementRequest, Results<Ok<UpdatePlanAnnouncementResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public UpdatePlanAnnouncement(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<UpdatePlanAnnouncement> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(PlanAnnouncement))
             .WithName("UpdatePlanAnnouncement")
             .Accepts<UpdatePlanAnnouncementRequest>("application/json"));
        this.Put("plan/announcement/{Id:guid}");
        this.AuditLog("ขออนุมัติเผยแพร่จัดซื้อจัดจ้าง", "แก้ไขประกาศเผยแพร่แผน");
    }

    protected override async ValueTask<Results<Ok<UpdatePlanAnnouncementResponse>, NotFound<string>>> HandleRequestAsync(UpdatePlanAnnouncementRequest req, CancellationToken ct)
    {
        var data =
            await this.dbContext.PlanAnnouncements
                      .Include(p => p.AnnouncementSelectedInformations)
                      .ThenInclude(s => s.Plan)
                      .Include(p => p.Acceptors)
                      .ThenInclude(a => a.User)
                      .ThenInclude(u => u.Employee)
                      .Include(p => p.Assignees)
                      .ThenInclude(a => a.User)
                      .ThenInclude(u => u.Employee)
                      .Include(p => p.AuditInfo)
                      .AsSplitQuery()
                      .SingleOrDefaultAsync(p => p.Id == PlanAnnouncementId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound($"Plan announcement with Id {req.Id} not found");
        }

        if (req.LastModifiedAt.HasValue &&
            data.AuditInfo.LastModifiedAt?.ToUnixTimeMilliseconds() != req.LastModifiedAt.Value.ToUnixTimeMilliseconds())
        {
            this.ThrowError("ข้อมูลถูกแก้ไขโดยผู้อื่นแล้ว", StatusCodes.Status409Conflict);
        }

        var previousStatus = data.Status;

        data.SetAnnouncementTitle(req.AnnouncementTitle)
            .SetTelephone(req.Telephone)
            .SetRemark(req.Remark)
            .SetStatus(req.Status)
            .SetEgpGroup(req.GroupEgpNumber);

        if (req.Status == PlanAnnouncementStatus.WaitingAcceptor
            || req.DocumentDate is not null)
        {
            data.SetDocumentDate(req.DocumentDate);
        }

        await this.ManagePlanAnnouncementSelected(data, req.PlanSelected, ct);
        await this.ManageAssigneeAsync(data, req.Assignees, UserId.From(req.UserId), previousStatus, ct);

        var statusToUpdateDocument = data.Status == PlanAnnouncementStatus.Draft || data.Status == PlanAnnouncementStatus.Rejected;

        FileId? newApproveDocumentFileId = null;
        FileId? newAnnouncementDocumentFileId = null;
        var isReplaceNewDocument = req.IsApproveDocumentIdReplace == true || req.IsAnnouncementDocumentIdReplace == true;

        if (req is { ApproveDocumentId: not null, IsApproveDocumentIdReplace: true }
            && statusToUpdateDocument)
        {
            newApproveDocumentFileId = await this.UpdateDocumentHistoryAsync(
                data,
                PlanAnnouncementDocumentType.Approve,
                FileId.From(req.ApproveDocumentId.Value),
                true,
                ct);
        }

        if (req is { AnnouncementDocumentId: not null, IsAnnouncementDocumentIdReplace: true }
            && statusToUpdateDocument)
        {
            newAnnouncementDocumentFileId = await this.UpdateDocumentHistoryAsync(
                data,
                PlanAnnouncementDocumentType.Announcement,
                FileId.From(req.AnnouncementDocumentId.Value),
                true,
                ct);
        }

        switch (req.Status)
        {
            case PlanAnnouncementStatus.WaitingAssign or PlanAnnouncementStatus.Rejected when req.Acceptors is not null:
                await this.ManageAcceptorAsync(data, req.Acceptors, UserId.From(req.UserId), ct);

                this.dbContext.PlanAnnouncements.Update(data);
                await this.dbContext.SaveChangesAsync(CancellationToken.None);

                var (approveFileId1, announceFileId1) = await this.UpdateDocumentHistory(data, isReplace: true, hasPublicPlan: false, hasAcceptors: false, hasAssignees: false, cancellationToken: ct);
                newApproveDocumentFileId = approveFileId1;
                newAnnouncementDocumentFileId = announceFileId1;

                break;

            case PlanAnnouncementStatus.WaitingAcceptor when req.Acceptors is not null:
                await this.ManageAcceptorAsync(data, req.Acceptors, UserId.From(req.UserId), ct);
                ResetAcceptorStatus(data);
                await this.InitializeApproverCurrentSequenceAsync(data, ct);

                this.dbContext.PlanAnnouncements.Update(data);
                await this.dbContext.SaveChangesAsync(CancellationToken.None);

                var (approveFileId2, announceFileId2) = await this.UpdateDocumentHistory(data, isReplace: true, hasPublicPlan: false, hasAcceptors: false, hasAssignees: true, cancellationToken: ct);
                newApproveDocumentFileId = approveFileId2;
                newAnnouncementDocumentFileId = announceFileId2;

                break;

            default:
                data.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    $"แก้ไขข้อมูลขออนุมัติเผยแพร่แผนจัดซื้อจัดจ้าง",
                    data.Status.ToString()));

                break;
        }

        if (isReplaceNewDocument || data.Status == PlanAnnouncementStatus.Draft)
        {
            await this.UpdateAndReplaceDocumentHistoryAsync(
                data,
                PlanAnnouncementDocumentType.Approve,
                true,
                isReplaceNewDocument,
                ct);

            await this.UpdateAndReplaceDocumentHistoryAsync(
                data,
                PlanAnnouncementDocumentType.Announcement,
                true,
                isReplaceNewDocument,
                ct);
        }

        var deleteFileIds = await this.ManageAttachments(data, req.Attachments, ct);

        this.dbContext.PlanAnnouncements.Update(data);
        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        foreach (var fileId in deleteFileIds)
        {
            await this.fileServiceClient.DeleteAsync(fileId, CancellationToken.None);
        }

        return TypedResults.Ok(new UpdatePlanAnnouncementResponse(
            newApproveDocumentFileId?.Value,
            newAnnouncementDocumentFileId?.Value));
    }

    private async Task ManagePlanAnnouncementSelected(
        PlanAnnouncement announcement,
        PlanSelectedRequest[] planSelectedReq,
        CancellationToken ct)
    {
        var planWithEgp = planSelectedReq
                          .Select(r =>
                          {
                              if (r.Id.HasValue)
                              {
                                  return new { PlanId = PlanId.From(r.PlanId), r.EgpNumber };
                              }

                              var planSelected = Domain.Plan.PlanAnnouncementSelected.Create(
                                  PlanId.From(r.PlanId),
                                  announcement.Id);

                              announcement.AddPlanAnnouncementSelected(planSelected);

                              return new { planSelected.PlanId, r.EgpNumber };
                          }).ToList();

        foreach (var item in planWithEgp)
        {
            var data = await this.dbContext.Plans
                                 .SingleOrDefaultAsync(w => w.Id == item.PlanId, ct);

            if (data is not null)
            {
                data.SetEgpNumber(item.EgpNumber);
                this.dbContext.Plans.Update(data);
            }
        }
    }

    private async Task ManageAssigneeAsync(
        PlanAnnouncement announcement,
        AssigneeRequest[] requestsAssignee,
        UserId userId,
        PlanAnnouncementStatus previousStatus,
        CancellationToken ct)
    {
        var lastAssigneeUserId = requestsAssignee
                .Where(a => a.AssigneeType == AssigneeType.Assignee)
                .OrderByDescending(a => a.Sequence)
                .Select(a => (UserId?)a.UserId)
                .FirstOrDefault();

        _ = announcement.Assignees
                        .ExceptBy(
                            requestsAssignee
                                .Where(w => w.Id.HasValue)
                                .Select(s => s.Id.Value),
                            a => a.Id.Value)
                        .Iter(r => announcement.RemoveAssigneeById(r.Id));

        _ = requestsAssignee.Where(w => w.Id.HasValue)
                            .Join(
                                announcement.Assignees,
                                db => db.Id.Value,
                                payload => payload.Id.Value,
                                (payload, db) => new { db, payload })
                            .Iter(r => r.db.SetSequence(r.payload.Sequence)
                                           .SetSendToAcceptorId(lastAssigneeUserId ?? userId));

        var assigneeIds = requestsAssignee
                          .Where(w => !w.Id.HasValue)
                          .Select(s => UserId.From(s.UserId))
                          .ToArray();

        var userData = await this.dbContext.SuUsers
                                 .Include(e => e.Employee)
                                 .ThenInclude(v => v.View)
                                 .Where(w => assigneeIds.Contains(w.Id))
                                 .ToArrayAsync(ct);

        this.ValidateUsers(userData, assigneeIds);

        var newAssignees = requestsAssignee.Where(x => x.AssigneeType == AssigneeType.Assignee && !x.Id.HasValue).ToArray();

        var newAssigneeTargets = await Task.WhenAll(newAssignees.Select(a =>
            this.dbContext.GetNotificationTargetsForUserAsync(UserId.From(a.UserId), ct)));

        foreach (var targetUserId in newAssigneeTargets.SelectMany(t => t))
        {
            await SendNotificationAsync(
                announcement,
                targetUserId,
                NotificationConstant.Assignment.Title,
                string.Format(NotificationConstant.Assignment.Message, ProgramConstant.PlanAnnouncement.Name, announcement.PlanAnnouncementNumber));
        }

        _ = requestsAssignee.Where(w => !w.Id.HasValue)
                     .Join(
                         userData,
                         a => UserId.From(a.UserId),
                         u => u.Id,
                         (a, u) => PlanAnnouncementAssignee.Create(a.AssigneeType, u, a.Sequence))
                     .Iter(a =>
                     {
                         a.SetSendToAcceptorId(lastAssigneeUserId ?? userId);
                         announcement.AddAssignee(a);
                     });

        if ((previousStatus == PlanAnnouncementStatus.Draft || previousStatus == PlanAnnouncementStatus.Rejected) && announcement.Status == PlanAnnouncementStatus.WaitingAssign)
        {
            await this.SendNotificationAssigneeAsync(planAnnouncement: announcement, ct);
        }
    }

    private void ValidateUsers(SuUser[] users, UserId[] userIds)
    {
        var foundUserIds = users.Select(u => u.Id).ToArray();

        var missingUserIds = userIds.Except(foundUserIds).ToArray();

        if (missingUserIds.Length > 0)
        {
            this.ThrowError(
                r => r.Assignees,
                $"Users with IDs {string.Join(", ", missingUserIds)} not found.",
                StatusCodes.Status404NotFound);
        }
    }

    private static void AddAssignee(PlanAnnouncement planAnnouncement, AssigneeRequest[] assignees, SuUser[] users)
    {
        _ = assignees
            .Join(
                users,
                a => UserId.From(a.UserId),
                u => u.Id,
                (a, u) => PlanAnnouncementAssignee.Create(a.AssigneeType, u, a.Sequence))
            .Iter(s => planAnnouncement.AddAssignee(s));
    }

    private async Task<IEnumerable<FileId>> ManageAttachments(
        PlanAnnouncement announcement,
        AttachmentsDto[] attachments,
        CancellationToken ct)
    {
        await this.ValidateDocumentTypes(attachments, ct);

        var fileList = attachments
                       .SelectMany(r => r.FileAttachments.Select(f => (
                           r.DocumentTypeCode,
                           f.FileId,
                           f.FileName,
                           f.Sequence,
                           f.IsPublic)))
                       .ToArray();

        var deleteIds = announcement.Attachments
                                    .ExceptBy(
                                        fileList.Select(s => s.FileId),
                                        w => w.Id.Value)
                                    .Select(s => s.Id)
                                    .Map(r =>
                                    {
                                        announcement.RemoveAttachmentById(r);

                                        return r;
                                    }) ?? [];

        _ = fileList
            .ExceptBy(
                announcement.Attachments.Select(s => s.Id.Value),
                w => w.FileId)
            .Map(a => PlanAnnouncementAttachments.Create(
                ParameterCode.From(a.DocumentTypeCode),
                FileId.From(a.FileId),
                a.FileName,
                a.Sequence,
                a.IsPublic))
            .Iter(a => announcement.AddAttachment(a));

        return deleteIds;
    }

    private async Task ValidateDocumentTypes(
        AttachmentsDto[] attachments,
        CancellationToken ct)
    {
        var documentTypeCodes = attachments.Select(a => ParameterCode.From(a.DocumentTypeCode)).ToArray();

        var documentTypes = await this.dbContext.SuParameters
                                      .Where(p => documentTypeCodes.Contains(p.Code))
                                      .ToArrayAsync(ct);

        var foundDocumentTypeCodes = documentTypes.Select(p => p.Code).ToArray();

        var missingDocumentTypeCodes = documentTypeCodes.Except(foundDocumentTypeCodes).ToArray();

        if (missingDocumentTypeCodes.Length > 0)
        {
            this.ThrowError(
                r => r.Attachments,
                $"Document types with codes {string.Join(", ", missingDocumentTypeCodes)} not found.",
                StatusCodes.Status404NotFound);
        }
    }

    private static void ResetAcceptorStatus(PlanAnnouncement announcement)
    {
        announcement.Acceptors.Iter(r => r.Pending());
    }

    private async Task InitializeApproverCurrentSequenceAsync(PlanAnnouncement announcement, CancellationToken ct)
    {
        var approvers = announcement.Acceptors
                                    .Where(p => p.Type == AcceptorType.Approver)
                                    .Select(DelegatorExtensions.DelegatorToAcceptor)
                                    .OrderBy(a => a.Sequence)
                                    .ToList();

        approvers.Iter(a =>
        {
            if (a.Status == AcceptorStatus.Draft || a.Status == AcceptorStatus.Rejected)
            {
                a.Pending();
            }

            a.SetCurrent(false);
        });

        var firstPending = approvers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

        if (firstPending is null)
        {
            return;
        }

        if (approvers.Count == 0)
        {
            foreach (var targetUserId in await this.dbContext.GetNotificationTargetsWithSecretariesAsync(firstPending, ct))
            {
                _ = SendNotificationAsync(
                    announcement,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(
                        NotificationConstant.WaitForApprove.Message,
                        ProgramConstant.PlanAnnouncement.Name,
                        announcement.PlanAnnouncementNumber));
            }
        }
        else
        {
            foreach (var targetUserId in await this.dbContext.GetNotificationTargetsWithSecretariesAsync(firstPending, ct))
            {
                _ = SendNotificationAsync(
                    announcement,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(
                        NotificationConstant.WaitForLike.Message,
                        ProgramConstant.Plan.Name,
                        announcement.PlanAnnouncementNumber));
            }
        }

        firstPending?.SetCurrent(true);
    }

    private async Task ManageAcceptorAsync(
        PlanAnnouncement announcement,
        AcceptorRequest[] acceptors,
        UserId userId,
        CancellationToken ct)
    {
        var hasNoPermission =
            announcement.Assignees
                        .Map(DelegatorExtensions.DelegatorToAssignee)
                        .All(a => (a.Delegatee?.SuUserId ?? a.UserId) != userId);

        if (hasNoPermission)
        {
            this.ThrowError(
                "no permission to add acceptors.",
                StatusCodes.Status403Forbidden);
        }

        _ = announcement.Acceptors
                        .ExceptBy(
                            acceptors
                                .Where(w => w.Id.HasValue)
                                .Select(s => s.Id.Value),
                            a => a.Id.Value)
                        .Iter(r => announcement.RemoveAcceptorById(r.Id));

        _ = acceptors.Where(w => w.Id.HasValue)
                     .Join(
                         announcement.Acceptors,
                         db => db.Id.Value,
                         payload => payload.Id.Value,
                         (payload, db) => new { db, payload })
                     .Iter(r => r.db.SetSequence(r.payload.Sequence));

        var acceptorIds = acceptors
                          .Where(w => !w.Id.HasValue)
                          .Select(s => UserId.From(s.UserId))
                          .ToArray();

        var userData = await this.dbContext.SuUsers
                                 .Include(e => e.Employee)
                                 .ThenInclude(v => v.View)
                                 .Where(w => acceptorIds.Contains(w.Id))
                                 .ToArrayAsync(ct);

        this.ValidateUsers(userData, acceptorIds);

        _ = acceptors.Where(w => !w.Id.HasValue)
                     .Join(
                         userData,
                         a => UserId.From(a.UserId),
                         u => u.Id,
                         (a, u) => PlanAnnouncementAcceptor.Create(a.AcceptorType, u, a.Sequence))
                     .Iter(s => announcement.AddAcceptor(s));

        var lastAssigneeUserId = announcement.Assignees
            .Where(a => a.Type == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)a.UserId)
            .FirstOrDefault();

        foreach (var existing in announcement.Acceptors)
        {
            var match = acceptors.FirstOrDefault(e => e.UserId == existing.UserId && existing.Type == e.AcceptorType);

            if (match != null)
            {
                existing.SetSequence(match.Sequence)
                        .SetSendToAcceptorId(
                            lastAssigneeUserId
                            ?? userId);
            }
        }
    }

    private async Task SendNotificationAssigneeAsync(PlanAnnouncement planAnnouncement, CancellationToken ct)
    {
        foreach (var targetUserId in await this.dbContext.GetAssigneeNotificationTargetsWithSecretariesAsync(planAnnouncement.Assignees.Where(x => x.Type != AssigneeType.Director), ct))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.Assignment.Title,
                      string.Format(NotificationConstant.Assignment.Message, ProgramConstant.PlanAnnouncement.Name, planAnnouncement.PlanAnnouncementNumber),
                      NotificationProgram.Plan)
                  .SetReferenceId(planAnnouncement.Id.Value)
                  .SetLinkUrl(
                      string.Format(ProgramConstant.PlanAnnouncement.Url, planAnnouncement.Id),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
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
}