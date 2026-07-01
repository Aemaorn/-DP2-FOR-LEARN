namespace GHB.DP2.Application.Features.Procurement.ChangeCommittee;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Validators;
using GHB.DP2.Application.Features.Procurement.ChangeCommittee.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.ChangeCommittee;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ChangeCommitteeAcceptorDto(
    Guid UserId,
    string FullName,
    string PositionName,
    string BusinessUnitName,
    AcceptorType AcceptorType,
    int Sequence,
    string? CommitteePositionsCode = default,
    bool? IsUnableToPerformDuties = default,
    string? Remark = default,
    Guid? Id = default);

public record CreateChangeCommitteeRequest(
    Guid ProcurementId,
    SourceType SourceType,
    Guid SourceId,
    CommitteeType CommitteeType,
    IEnumerable<CommitteeMember> OldCommittees,
    IEnumerable<CommitteeMember> NewCommittees,
    IEnumerable<ChangeCommitteeAcceptorDto> Acceptors,
    string? Remark = null,
    IEnumerable<AttachmentsDto>? Attachments = null,
    bool IsJorPorComment = false,
    IEnumerable<AssigneeRequest>? Assignees = null,
    DateTimeOffset? DocumentDate = null);

public class CreateChangeCommitteeRequestValidator : Validator<CreateChangeCommitteeRequest>
{
    public CreateChangeCommitteeRequestValidator()
    {
        this.RuleForEach(x => x.Attachments)
            .ChildRules(a => a.RuleForEach(x => x.FileAttachments)
                .ChildRules(f => f.RuleFor(x => x.FileName).MustBeValidFileExtension()))
            .When(x => x.Attachments is not null);
    }
}

public class CreateChangeCommitteeEndpoint : ChangeCommitteeEndpointBase<CreateChangeCommitteeRequest, Results<Ok<CommitteeChangeId>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CreateChangeCommitteeEndpoint(
        Dp2DbContext dbContext,
        ILogger<CreateChangeCommitteeEndpoint> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("ChangeCommittee"));
        this.Post("change-committee");
    }

    protected override async ValueTask<Results<Ok<CommitteeChangeId>, NotFound<string>>> HandleRequestAsync(CreateChangeCommitteeRequest req, CancellationToken ct)
    {
        var procurement = await this.dbContext.Procurements
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(p => p.Id == ProcurementId.From(req.ProcurementId), ct);

        if (procurement == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการจัดซื้อจัดจ้าง");
        }

        var changeCommittee = CommitteeChanges.Create(
            ProcurementId.From(req.ProcurementId),
            req.SourceType,
            req.SourceId,
            req.CommitteeType,
            req.OldCommittees,
            req.NewCommittees,
            req.Remark);

        changeCommittee.SetIsJorPorComment(req.IsJorPorComment);

        this.dbContext.CommitteeChanges.Add(changeCommittee);

        if (req.Acceptors.Any())
        {
            var userIds = req.Acceptors.Select(s => UserId.From(s.UserId)).ToArray();

            var users = await this.dbContext.SuUsers
                                  .Include(u => u.Employee)
                                  .ThenInclude(s => s.View)
                                  .Where(u => userIds.Contains(u.Id))
                                  .ToArrayAsync(ct);

            var newAcceptors = req.Acceptors
                                  .Join(
                                      users,
                                      a => a.UserId,
                                      u => u.Id.Value,
                                      (a, u) =>
                                      {
                                          var acceptor = CommitteeChangeAcceptor.Create(
                                              a.AcceptorType,
                                              u,
                                              a.Sequence);

                                          _ = string.IsNullOrWhiteSpace(a.CommitteePositionsCode)
                                                     ? acceptor.SetCommitteePositionsCode(null)
                                                     : acceptor.SetCommitteePositionsCode(
                                                         ParameterCode.From(a.CommitteePositionsCode));

                                          acceptor.SetIsUnableToPerformDuties(a.IsUnableToPerformDuties ?? false);

                                          return acceptor;
                                      });

            foreach (var acceptor in newAcceptors)
            {
                changeCommittee.AddCommitteeChangeAcceptor(acceptor);
            }
        }

        if (req.Assignees?.Any() == true)
        {
            var assigneeUserIds = req.Assignees.Select(s => UserId.From(s.UserId)).ToArray();

            var assigneeUsers = await this.dbContext.SuUsers
                                          .Include(u => u.Employee)
                                          .ThenInclude(s => s.View)
                                          .Where(u => assigneeUserIds.Contains(u.Id))
                                          .ToArrayAsync(ct);

            var newAssignees = req.Assignees
                                  .Join(
                                      assigneeUsers,
                                      a => a.UserId,
                                      u => u.Id.Value,
                                      (a, u) => CommitteeChangeAssignee.Create(
                                          changeCommittee.Id,
                                          a.AssigneeGroup,
                                          a.AssigneeType,
                                          u,
                                          a.Sequence));

            foreach (var assignee in newAssignees)
            {
                changeCommittee.AddAssignee(assignee);
            }
        }

        if (req.Attachments?.Any() == true)
        {
            foreach (var attachmentGroup in req.Attachments)
            {
                var attachment = CommitteeChangeAttachment.Create(
                    changeCommittee.Id,
                    1,
                    ParameterCode.From(attachmentGroup.DocumentTypeCode));

                foreach (var (fileAttachment, index) in attachmentGroup.FileAttachments.Select((fa, i) => (fa, i)))
                {
                    var attachmentInfo = CommitteeChangeAttachmentInfo.Create(
                        attachment.Id,
                        index + 1,
                        FileId.From(fileAttachment.FileId),
                        fileAttachment.FileName,
                        fileAttachment.IsPublic);

                    attachment.AddAttachmentInfos(attachmentInfo);
                }

                changeCommittee.AddAttachment(attachment);
            }
        }

        if (req.DocumentDate is not null)
        {
            changeCommittee.SetDocumentDate(req.DocumentDate);
        }

        await this.SetDefaultDocumentTemplate(changeCommittee, false, req.IsJorPorComment, ct);

        await this.dbContext.SaveChangesAsync(ct);

        var changeCommitteeWithIncludes = await this.GetChangeCommitteeWithIncludesAsync(changeCommittee.Id, ct);

        if (changeCommitteeWithIncludes is not null)
        {
            var lastedDraft = changeCommitteeWithIncludes.LastedDraftDocument;

            if (lastedDraft is not null)
            {
                var documentService = this.Resolve<IDocumentService>();
                var replaceDto = await this.MapToReplaceDto(changeCommitteeWithIncludes, false, ct);
                var fontName = GetFontName(changeCommitteeWithIncludes);
                var copiedFileId = await documentService.CopyDocumentTemplateAsync(
                    lastedDraft.FileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto, fontName),
                    parentDirectory: $"{DocumentTemplateGroups.CommitteeChange}/{changeCommitteeWithIncludes.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                    cancellationToken: ct);

                if (copiedFileId.HasValue)
                {
                    changeCommitteeWithIncludes.AddDocumentHistory(copiedFileId.Value, false);
                    await this.dbContext.SaveChangesAsync(ct);
                }
            }
        }

        return TypedResults.Ok(changeCommittee.Id);
    }
}