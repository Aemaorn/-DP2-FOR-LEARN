namespace GHB.DP2.Application.Features.Procurement.ChangeCommittee;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.ChangeCommittee.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.ChangeCommittee;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateChangeCommitteeRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    SourceType SourceType,
    Guid SourceId,
    CommitteeType CommitteeType,
    CommitteeChangeStatus Status,
    IEnumerable<CommitteeMember> OldCommittees,
    IEnumerable<CommitteeMember> NewCommittees,
    string? Remark,
    bool IsResetTemplace,
    IEnumerable<ChangeCommitteeAcceptorDto> Acceptors,
    bool IsJorPorComment = false,
    IEnumerable<AssigneeRequest>? Assignees = null,
    DateTimeOffset? DocumentDate = null);

public class UpdateChangeCommitteeEndpoint : ChangeCommitteeEndpointBase<UpdateChangeCommitteeRequest, Results<Ok<CommitteeChangeId>, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateChangeCommitteeEndpoint(
        Dp2DbContext dbContext,
        ILogger<UpdateChangeCommitteeEndpoint> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("ChangeCommittee"));
        this.Put("change-committee/{id:guid}");
    }

    protected override async ValueTask<Results<Ok<CommitteeChangeId>, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdateChangeCommitteeRequest req, CancellationToken ct)
    {
        var changeCommitteeId = CommitteeChangeId.From(req.Id);

        var changeCommittee = await this.dbContext.CommitteeChanges
                                        .Include(c => c.Acceptors)
                                        .Include(c => c.Assignees)
                                        .Include(c => c.DocumentHistories)
                                        .Include(c => c.Procurement)
                                        .FirstOrDefaultAsync(x => x.Id == changeCommitteeId, ct);

        if (changeCommittee == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการเปลี่ยนแปลงคณะกรรมการ");
        }

        var previousStatus = changeCommittee.Status;

        // Update committee change information
        changeCommittee.SetCommitteeChangeInfo(
            changeCommittee.ProcurementId,
            req.SourceType,
            req.SourceId,
            req.CommitteeType,
            req.OldCommittees,
            req.NewCommittees,
            req.Remark);

        if (req.Status == CommitteeChangeStatus.WaitingCommitteeApproval || req.DocumentDate is not null)
        {
            changeCommittee.SetDocumentDate(req.DocumentDate);
        }

        changeCommittee.SetStatus(req.Status);
        changeCommittee.SetIsJorPorComment(req.IsJorPorComment);

        this.dbContext.CommitteeChanges.Update(changeCommittee);

        var lastAssigneeUserId = req.Assignees?
            .Where(a => a.AssigneeType == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)UserId.From(a.UserId))
            .FirstOrDefault();

        // Update acceptors with the new status
        await this.UpdateAcceptors(changeCommittee, req.Acceptors, req.Status, ct, lastAssigneeUserId ?? UserId.From(req.UserId));

        if (req.Assignees?.Any() == true)
        {
            var newAssignees = req.Assignees.Where(x => x is { AssigneeType: AssigneeType.Assignee, Id: null });

            foreach (var inComing in newAssignees)
            {
                await SendNotificationAsync(
                    changeCommittee,
                    UserId.From(inComing.UserId),
                    NotificationConstant.Assignment.Title,
                    string.Format(NotificationConstant.Assignment.Message, ProgramConstant.CommitteeChange.Name, changeCommittee.Procurement.ProcurementNumber?.ToString() ?? string.Empty));
            }
        }

        await this.UpdateAssignees(changeCommittee, req.Assignees, ct, UserId.From(req.UserId));

        if (changeCommittee.DocumentHistories == null || !changeCommittee.DocumentHistories.Any())
        {
            await this.SetDefaultDocumentTemplate(changeCommittee, false, req.IsJorPorComment, ct);
        }

        await SendNotificationsAsync(changeCommittee, req, previousStatus);

        await this.dbContext.SaveChangesAsync(ct);

        await this.UpdateAndReplaceDocumentAsync(changeCommittee.Id, req.IsResetTemplace, ct);

        return TypedResults.Ok(changeCommittee.Id);
    }

    private async ValueTask UpdateAndReplaceDocumentAsync(CommitteeChangeId id, bool isResetTemplate, CancellationToken ct)
    {
        var changeCommittee = await this.GetChangeCommitteeWithIncludesAsync(id, ct);

        if (changeCommittee is null)
        {
            return;
        }

        var lastedDraftDocument = changeCommittee.LastedMaxDocument;

        if (lastedDraftDocument is not null)
        {
            var documentService = this.Resolve<IDocumentService>();
            var replaceDto = await this.MapToReplaceDto(changeCommittee, false, ct);
            var fontName = GetFontName(changeCommittee);

            var templateFileId = isResetTemplate
                ? await documentService.GetDocumentTemplateAsync(
                    dt => dt.Group == DocumentTemplateGroups.CommitteeChange &&
                        dt.IsActive &&
                        (!changeCommittee.IsJorPorComment
                            ? dt.AdditionalInfo == null
                            : EF.Functions.JsonExists(dt.AdditionalInfo!, nameof(SuDocumentTemplate.IsJorPorComment)) &&
                              dt.AdditionalInfo!.RootElement
                                  .GetProperty(nameof(SuDocumentTemplate.IsJorPorComment))
                                  .GetBoolean() == changeCommittee.IsJorPorComment),
                    ct)
                : lastedDraftDocument.FileId;

            if (templateFileId is null)
            {
                return;
            }

            var copiedFileId = await documentService.CopyDocumentTemplateAsync(
                templateFileId.Value,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto, fontName),
                parentDirectory: $"{DocumentTemplateGroups.CommitteeChange}/{changeCommittee.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);

            if (copiedFileId.HasValue)
            {
                changeCommittee.AddDocumentHistory(copiedFileId.Value, false);
                await this.dbContext.SaveChangesAsync(ct);
            }
        }
    }

    private static async Task SendNotificationsAsync(CommitteeChanges changeCommittee, UpdateChangeCommitteeRequest req, CommitteeChangeStatus previousStatus)
    {
        var procurementNumber = changeCommittee.Procurement.ProcurementNumber?.ToString() ?? string.Empty;

        if (previousStatus != CommitteeChangeStatus.WaitingCommitteeApproval && req.Status == CommitteeChangeStatus.WaitingCommitteeApproval)
        {
            var committeeAcceptors = changeCommittee.Acceptors
                .Where(x => x.Type != AcceptorType.Approver && !x.IsUnableToPerformDuties && x.Status == AcceptorStatus.Pending)
                .ToList();

            foreach (var acceptor in committeeAcceptors)
            {
                await SendNotificationAsync(
                    changeCommittee,
                    acceptor.UserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.CommitteeChange.Name, procurementNumber));
            }
        }

        if (previousStatus != CommitteeChangeStatus.WaitingApproval && req.Status == CommitteeChangeStatus.WaitingApproval)
        {
            var firstPending = changeCommittee.Acceptors
                .Where(x => x.Type == AcceptorType.Approver)
                .OrderBy(a => a.Sequence)
                .Select(DelegatorExtensions.DelegatorToAcceptor)
                .FirstOrDefault(a => a is { Status: AcceptorStatus.Pending, IsCurrent: true });

            if (firstPending != null)
            {
                foreach (var targetUserId in firstPending.GetNotificationTargets())
                {
                    await SendNotificationAsync(
                        changeCommittee,
                        targetUserId,
                        NotificationConstant.WaitForLike.Title,
                        string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.CommitteeChange.Name, procurementNumber));
                }
            }
        }
    }

    private static async Task SendNotificationAsync(CommitteeChanges changeCommittee, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(changeCommittee.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.CommitteeChange.Url, changeCommittee.Id), ProgramConstant.CommitteeChange.Button)
              .PublishAsync(CancellationToken.None);
    }

    private async Task UpdateAssignees(CommitteeChanges changeCommittee, IEnumerable<AssigneeRequest>? assigneeDtos, CancellationToken ct, UserId? sendToAcceptorId = null)
    {
        var dtoList = assigneeDtos?.ToList() ?? [];

        var requestIds = dtoList
            .Where(a => a.Id.HasValue)
            .Select(a => CommitteeChangeAssigneeId.From(a.Id!.Value))
            .ToHashSet();

        // Remove only assignees that are not in the request
        var toRemove = changeCommittee.Assignees
            .Where(a => !requestIds.Contains(a.Id))
            .ToList();

        foreach (var assignee in toRemove)
        {
            changeCommittee.RemoveAssignee(assignee);
        }

        if (!dtoList.Any())
        {
            return;
        }

        var assigneeUserIds = dtoList.Select(s => UserId.From(s.UserId)).ToArray();

        var assigneeUsers = await this.dbContext.SuUsers
                                      .Include(u => u.Employee)
                                      .ThenInclude(s => s.View)
                                      .Where(u => assigneeUserIds.Contains(u.Id))
                                      .ToArrayAsync(ct);

        // Update sendToAcceptorId on existing assignees
        var existingAssignees = changeCommittee.Assignees.ToList();

        foreach (var existing in existingAssignees)
        {
            existing.SetSendToAcceptorId(sendToAcceptorId);
        }

        // Add new assignees (no Id)
        var newAssignees = dtoList
            .Where(a => !a.Id.HasValue)
            .Join(
                assigneeUsers,
                a => a.UserId,
                u => u.Id.Value,
                (a, u) => CommitteeChangeAssignee.Create(
                    changeCommittee.Id,
                    a.AssigneeGroup,
                    a.AssigneeType,
                    u,
                    a.Sequence))
            .ToList();

        foreach (var assignee in newAssignees)
        {
            assignee.SetSendToAcceptorId(sendToAcceptorId);
            changeCommittee.AddAssignee(assignee);
        }

        await this.dbContext.CommitteeChangeAssignees.AddRangeAsync(newAssignees, ct);
    }

    private async Task UpdateAcceptors(CommitteeChanges changeCommittee, IEnumerable<ChangeCommitteeAcceptorDto> acceptorDtos, CommitteeChangeStatus status, CancellationToken ct, UserId? sendToAcceptorId = null)
    {
        var dtoList = acceptorDtos.ToList();

        var requestIds = dtoList
            .Where(a => a.Id.HasValue)
            .Select(a => AcceptorId.From(a.Id!.Value))
            .ToHashSet();

        // Remove only acceptors that are not in the request
        var toRemove = changeCommittee.Acceptors
            .Where(a => !requestIds.Contains(a.Id))
            .ToList();

        foreach (var acceptor in toRemove)
        {
            changeCommittee.RemoveAcceptorById(acceptor.Id);
        }

        if (!dtoList.Any())
        {
            return;
        }

        var userIds = dtoList.Select(s => UserId.From(s.UserId)).ToArray();

        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(s => s.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(ct);

        var isUnableToPerformDutiesAllowed =
            status == CommitteeChangeStatus.WaitingCommitteeApproval ||
            status == CommitteeChangeStatus.Edit;

        // Update existing acceptors
        var existingAcceptors = changeCommittee.Acceptors.ToList();

        foreach (var dto in dtoList.Where(a => a.Id.HasValue))
        {
            var domainAcceptor = existingAcceptors.FirstOrDefault(a => a.Id == AcceptorId.From(dto.Id!.Value));

            if (domainAcceptor is null)
            {
                continue;
            }

            var user = users.FirstOrDefault(u => u.Id.Value == dto.UserId);

            if (user?.Employee?.View is null)
            {
                continue;
            }

            domainAcceptor.SetType(dto.AcceptorType)
                          .SetUser(
                              user.Id,
                              user.EmployeeCode,
                              user.Employee.View.FullName,
                              user.Employee.View.FullPositionName,
                              user.Employee.View.BusinessUnitName)
                          .SetSequence(dto.Sequence);

            _ = string.IsNullOrWhiteSpace(dto.CommitteePositionsCode)
                     ? domainAcceptor.SetCommitteePositionsCode(null)
                     : domainAcceptor.SetCommitteePositionsCode(ParameterCode.From(dto.CommitteePositionsCode));

            domainAcceptor.SetIsUnableToPerformDuties(isUnableToPerformDutiesAllowed && (dto.IsUnableToPerformDuties ?? false));

            if (isUnableToPerformDutiesAllowed && dto is { IsUnableToPerformDuties: true, Remark: not null })
            {
                domainAcceptor.UnableToPerformDuties(dto.Remark);
            }

            domainAcceptor.SetSendToAcceptorId(sendToAcceptorId);
        }

        // Add new acceptors (no Id)
        var newAcceptors = dtoList
            .Where(a => !a.Id.HasValue)
            .Join(
                users,
                a => a.UserId,
                u => u.Id.Value,
                (a, u) =>
                {
                    var acceptor = status == CommitteeChangeStatus.WaitingApproval
                        ? CommitteeChangeAcceptor.CreateWithPending(a.AcceptorType, u, a.Sequence)
                        : CommitteeChangeAcceptor.Create(a.AcceptorType, u, a.Sequence);

                    _ = string.IsNullOrWhiteSpace(a.CommitteePositionsCode)
                             ? acceptor.SetCommitteePositionsCode(null)
                             : acceptor.SetCommitteePositionsCode(ParameterCode.From(a.CommitteePositionsCode));

                    acceptor.SetIsUnableToPerformDuties(isUnableToPerformDutiesAllowed && (a.IsUnableToPerformDuties ?? false));

                    if (isUnableToPerformDutiesAllowed && a is { IsUnableToPerformDuties: true, Remark: not null })
                    {
                        acceptor.UnableToPerformDuties(a.Remark);
                    }

                    acceptor.SetSendToAcceptorId(sendToAcceptorId);

                    return acceptor;
                })
            .ToList();

        foreach (var acceptor in newAcceptors)
        {
            changeCommittee.AddCommitteeChangeAcceptor(acceptor);
        }

        await this.dbContext.CommitteeChangeAcceptors.AddRangeAsync(newAcceptors, ct);

        var newAcceptorStatus = status switch
        {
            CommitteeChangeStatus.WaitingCommitteeApproval or CommitteeChangeStatus.WaitingApproval => AcceptorStatus.Pending,
            CommitteeChangeStatus.Edit or CommitteeChangeStatus.WaitingComment => AcceptorStatus.Draft,
            _ => (AcceptorStatus?)null,
        };

        if (newAcceptorStatus is not null)
        {
            var approverOnly = status is CommitteeChangeStatus.WaitingApproval or CommitteeChangeStatus.WaitingComment;

            var targets = approverOnly
                ? changeCommittee.Acceptors.Where(x => x.Type == AcceptorType.Approver)
                : changeCommittee.IsJorPorComment
                    ? changeCommittee.Acceptors.Where(x => x.Type != AcceptorType.Approver)
                    : changeCommittee.Acceptors;

            foreach (var a in targets.Where(x => !x.IsUnableToPerformDuties))
            {
                a.SetStatus(newAcceptorStatus.Value);
            }
        }
    }
}