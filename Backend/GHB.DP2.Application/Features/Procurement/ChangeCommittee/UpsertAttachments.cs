namespace GHB.DP2.Application.Features.Procurement.ChangeCommittee;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Contracts;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Validators;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.ChangeCommittee;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UpsertAttachmentsRequest : IHasAttachmentsWithId
{
    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public Guid CommitteeChangeId { get; init; }

    public AttachmentsDtoWithId[] Attachments { get; init; }
}

public class UpsertAttachmentsRequestValidator : Validator<UpsertAttachmentsRequest>
{
    public UpsertAttachmentsRequestValidator()
    {
        this.AddAttachmentsRules();
    }
}

public class UpsertAttachmentsEndpoint : EndpointBase<UpsertAttachmentsRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public UpsertAttachmentsEndpoint(
        ILogger<UpsertAttachmentsEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(ChangeCommittee))
             .WithName("UpsertChangeCommitteeAttachment")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<UpsertAttachmentsRequest>("application/json"));
        this.Put("change-committee/{committeeChangeId:guid}/attachments");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpsertAttachmentsRequest req, CancellationToken ct)
    {
        await this.ValidateUserAsync(UserId.From(req.UserId), ct);
        await this.ValidateDocumentTypeCode(req.Attachments, ct);

        var committeeChange = await this.dbContext.CommitteeChanges
                                        .Include(a => a.Attachments)
                                        .ThenInclude(a => a.CommitteeChangeAttachmentInfos)
                                        .SingleOrDefaultAsync(w => w.Id == CommitteeChangeId.From(req.CommitteeChangeId), ct);

        if (committeeChange is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการเปลี่ยนแปลงคณะกรรมการ");
        }

        await this.UpsertAttachments(committeeChange, req.Attachments);

        this.dbContext.CommitteeChanges.Update(committeeChange);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task ValidateUserAsync(UserId userId, CancellationToken ct)
    {
        var user = await this.dbContext
                             .SuUsers
                             .SingleOrDefaultAsync(w => w.Id == userId && w.IsActive, ct);

        if (user is null)
        {
            this.ThrowError("ไม่พบผู้ใช้งานในระบบ", StatusCodes.Status404NotFound);
        }
    }

    private async Task ValidateDocumentTypeCode(AttachmentsDtoWithId[] attachments, CancellationToken ct)
    {
        var docTypeCodes = attachments.Select(s => s.DocumentTypeCode)
                                      .Where(w => !string.IsNullOrWhiteSpace(w))
                                      .Select(ParameterCode.From)
                                      .ToArray();

        var docType = await this.dbContext.SuParameters
                                .Where(x => docTypeCodes.Contains(x.Code))
                                .ToArrayAsync(ct);

        var missingDocumentTypes = docTypeCodes
                                   .Except(docType.Select(dt => dt.Code))
                                   .ToArray();

        if (missingDocumentTypes.Any())
        {
            this.ThrowError(
                $"ไม่พบประเภทไฟล์",
                StatusCodes.Status404NotFound);
        }
    }

    private async Task UpsertAttachments(CommitteeChanges entity, AttachmentsDtoWithId[] attachments)
    {
        var fileList = attachments
                       .SelectMany(r => r.FileAttachments.Select(f => new
                       {
                           f.Id,
                           r.DocumentTypeCode,
                           f.FileId,
                           f.FileName,
                           f.Sequence,
                           f.IsPublic,
                       }))
                       .ToArray();

        var removedFileNames = new List<string>();
        var addedFileNames = new List<string>();

        var incomingFileIdSet = fileList.Select(f => FileId.From(f.FileId)).ToHashSet();

        // Remove attachments that are no longer present (by FileId)
        var attachmentsToRemove = entity.Attachments
                                        .Where(a => !a.CommitteeChangeAttachmentInfos.Any(info => incomingFileIdSet.Contains(info.FileId)))
                                        .ToList();

        foreach (var attachment in attachmentsToRemove)
        {
            foreach (var info in attachment.CommitteeChangeAttachmentInfos)
            {
                await this.fileServiceClient.DeleteAsync(info.FileId, CancellationToken.None);
                removedFileNames.Add(info.FileName);
            }

            entity.RemoveAttachment(attachment);
        }

        // Group files by document type code
        var groupedFiles = fileList.GroupBy(f => f.DocumentTypeCode);

        foreach (var group in groupedFiles)
        {
            var documentTypeCode = ParameterCode.From(group.Key);

            // Find existing attachment for this document type
            var existingAttachment = entity.Attachments
                                           .FirstOrDefault(a => a.TypeCode == documentTypeCode);

            if (existingAttachment == null)
            {
                // Create new attachment — all files in the group are new
                var newAttachment = CommitteeChangeAttachment.Create(
                    entity.Id,
                    group.Max(f => f.Sequence),
                    documentTypeCode);

                foreach (var file in group)
                {
                    var attachmentInfo = CommitteeChangeAttachmentInfo.Create(
                        newAttachment.Id,
                        file.Sequence,
                        FileId.From(file.FileId),
                        file.FileName,
                        file.IsPublic);

                    newAttachment.AddAttachmentInfos(attachmentInfo);
                    addedFileNames.Add(file.FileName);
                }

                entity.AddAttachment(newAttachment);
            }
            else
            {
                // Update existing attachment
                existingAttachment.SetSequence(group.Max(f => f.Sequence));

                var incomingGroupFileIds = group.Select(f => FileId.From(f.FileId)).ToHashSet();
                var existingInfoFileIds = existingAttachment.CommitteeChangeAttachmentInfos
                                                            .Select(i => i.FileId)
                                                            .ToHashSet();

                // Remove infos that are no longer present (by FileId)
                var infosToRemove = existingAttachment.CommitteeChangeAttachmentInfos
                                                      .Where(info => !incomingGroupFileIds.Contains(info.FileId))
                                                      .ToList();

                foreach (var info in infosToRemove)
                {
                    await this.fileServiceClient.DeleteAsync(info.FileId, CancellationToken.None);
                    removedFileNames.Add(info.FileName);
                    existingAttachment.RemoveAttachmentInfos(info);
                }

                // Add new infos (FileId not in existing)
                foreach (var file in group.Where(f => !existingInfoFileIds.Contains(FileId.From(f.FileId))))
                {
                    var attachmentInfo = CommitteeChangeAttachmentInfo.Create(
                        existingAttachment.Id,
                        file.Sequence,
                        FileId.From(file.FileId),
                        file.FileName,
                        file.IsPublic);

                    existingAttachment.AddAttachmentInfos(attachmentInfo);
                    addedFileNames.Add(file.FileName);
                }

                // Update existing infos (by FileId)
                foreach (var existing in existingAttachment.CommitteeChangeAttachmentInfos)
                {
                    var match = group.FirstOrDefault(f => FileId.From(f.FileId) == existing.FileId);

                    if (match != null)
                    {
                        existing.SetSequence(match.Sequence)
                                .SetPublic(match.IsPublic);
                    }
                }
            }
        }

        if (removedFileNames.Count > 0)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.DeleteFile,
                ActivityLogActionTypeConstant.DeleteFile,
                nameof(entity.Status),
                string.Join(", ", removedFileNames)));
        }

        if (addedFileNames.Count > 0)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.UploadFile,
                ActivityLogActionTypeConstant.UploadFile,
                nameof(entity.Status),
                string.Join(", ", addedFileNames)));
        }
    }
}