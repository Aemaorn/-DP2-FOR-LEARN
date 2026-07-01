namespace GHB.DP2.Application.Features.Procurement.Procurement;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Contracts;
using GHB.DP2.Application.Validators;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UpsertAttachmentRequest
{
    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public Guid ProcurementId { get; init; }

    public IEnumerable<UpsertProcurementAttachmentsDto> Attachments { get; set; }
}

public record UpsertProcurementAttachmentsDto(
    ProcurementAttachmentId? Id,
    string DocumentTypeCode,
    int Sequence,
    string? Remark,
    IEnumerable<UpsertProcurementFileAttachments> FileAttachments);

public record UpsertProcurementFileAttachments(
    ProcurementAttachmentInfoId? Id,
    Guid FileId,
    string FileName,
    int Sequence,
    bool IsPublic) : IHasSequenceFileAttachment;

public class UpsertAttachmentRequestValidator : Validator<UpsertAttachmentRequest>
{
    public UpsertAttachmentRequestValidator()
    {
        this.RuleFor(x => x.Attachments)
            .NotNull()
            .WithMessage("ต้องระบุไฟล์แนบอย่างน้อย 1 ไฟล์");

        this.RuleForEach(x => x.Attachments)
            .ChildRules(attachment =>
            {
                attachment.RuleFor(x => x.DocumentTypeCode)
                          .NotEmpty()
                          .WithMessage("ต้องระบุประเภทเอกสาร");

                attachment.RuleFor(x => x.Sequence)
                          .GreaterThanOrEqualTo(0)
                          .WithMessage("ลำดับไฟล์แนบต้องมากกว่าหรือเท่ากับ 0");

                attachment
                    .RuleForEach(x => x.FileAttachments)
                    .ChildRules(file => file.AddSequenceFileAttachmentRule());
            });
    }
}

public class UpdateProcurementAttachmentEndpoint : EndpointBase<UpsertAttachmentRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateProcurementAttachmentEndpoint(
        ILogger<UpdateProcurementAttachmentEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(Procurement))
             .WithName("UpsertProcurementAttachment")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<UpsertAttachmentRequest>("application/json"));
        this.Put("procurement/{ProcurementId:guid}/attachment");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpsertAttachmentRequest req, CancellationToken ct)
    {
        var userResult = await this.ValidateUserAsync(req.UserId, ct);
        var procurementResult = await this.GetProcurementWithAttachmentsAsync(req.ProcurementId, ct);

        await this.UpsertAttachments(userResult, procurementResult, req.Attachments, ct);

        this.dbContext.Procurements.Update(procurementResult);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<SuUser> ValidateUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await this.dbContext.SuUsers
                             .AsNoTracking()
                             .FirstOrDefaultAsync(u => u.Id == UserId.From(userId), ct);

        if (user is null)
        {
            this.ThrowError($"ไม่พบผู้ใช้ที่มีรหัส {userId}", StatusCodes.Status404NotFound);
        }

        return user;
    }

    private async Task<Procurement> GetProcurementWithAttachmentsAsync(Guid procurementId, CancellationToken ct)
    {
        var procurement = await this.dbContext.Procurements
                                    .Include(p => p.Attachments)
                                    .ThenInclude(a => a.ProcurementAttachmentInfos)
                                    .FirstOrDefaultAsync(p => p.Id == ProcurementId.From(procurementId), ct);

        if (procurement is null)
        {
            this.ThrowError("ไม่พบข้อมูลการจัดซื้อจัดจ้างที่ระบุ", StatusCodes.Status404NotFound);
        }

        return procurement;
    }

    private async Task UpsertAttachments(
        SuUser user,
        Procurement procurement,
        IEnumerable<UpsertProcurementAttachmentsDto> attachments,
        CancellationToken cancellationToken = default)
    {
        // Validate document type codes
        await this.ValidateDocumentTypeCodes(attachments, cancellationToken);

        // Get request attachment IDs
        var requestAttachmentIds = attachments
                                   .Where(a => a.Id.HasValue)
                                   .Select(a => a.Id!.Value)
                                   .ToHashSet();

        var requestFileIds = attachments
                             .SelectMany(a => a.FileAttachments)
                             .Select(f => FileId.From(f.FileId))
                             .ToHashSet();

        var addedFileNames = new List<string>();
        var removedFileNames = new List<string>();

        // Process each attachment from a request
        foreach (var attachmentDto in attachments)
        {
            var attachment = this.UpsertSingleAttachment(procurement, attachmentDto);

            // Handle file attachments for this attachment
            this.UpsertFileAttachmentsForAttachment(user, attachment, attachmentDto.FileAttachments, addedFileNames, removedFileNames);
        }

        // Remove attachments that are not in the request
        RemoveUnreferencedAttachments(procurement, requestAttachmentIds, requestFileIds, removedFileNames);

        if (removedFileNames.Count > 0)
        {
            procurement.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.DeleteFile,
                ActivityLogActionTypeConstant.DeleteFile,
                nameof(procurement.Status),
                string.Join(", ", removedFileNames)));
        }

        if (addedFileNames.Count > 0)
        {
            procurement.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.UploadFile,
                ActivityLogActionTypeConstant.UploadFile,
                nameof(procurement.Status),
                string.Join(", ", addedFileNames)));
        }
    }

    private async Task ValidateDocumentTypeCodes(
        IEnumerable<UpsertProcurementAttachmentsDto> attachments,
        CancellationToken cancellationToken)
    {
        var documentTypeCodes = attachments
                                .Select(a => a.DocumentTypeCode)
                                .Where(code => !string.IsNullOrWhiteSpace(code))
                                .Select(ParameterCode.From)
                                .ToArray();

        var documentTypes = await this.dbContext.SuParameters
                                      .Where(p => documentTypeCodes.Contains(p.Code))
                                      .ToArrayAsync(cancellationToken);

        var missingDocumentTypes = documentTypeCodes
                                   .Except(documentTypes.Select(dt => dt.Code))
                                   .ToArray();

        if (missingDocumentTypes.Any())
        {
            this.ThrowError(
                $"Document type with code {string.Join(", ", missingDocumentTypes)} not found.",
                StatusCodes.Status404NotFound);
        }
    }

    private ProcurementAttachment UpsertSingleAttachment(
        Procurement procurement,
        UpsertProcurementAttachmentsDto attachmentDto)
    {
        if (attachmentDto.Id is null)
        {
            return this.CreateNewAttachment(procurement, attachmentDto);
        }

        var existingAttachment = procurement.Attachments
                                            .FirstOrDefault(a => a.Id == attachmentDto.Id);

        if (existingAttachment is null)
        {
            // Create a new attachment with specified ID (shouldn't happen in normal flow)
            return this.CreateNewAttachment(procurement, attachmentDto);
        }

        // Update existing attachment properties
        existingAttachment.SetSequence(attachmentDto.Sequence)
                          .SetRemark(attachmentDto.Remark)
                          .SetTypeCode(ParameterCode.From(attachmentDto.DocumentTypeCode));

        return existingAttachment;
    }

    private ProcurementAttachment CreateNewAttachment(
        Procurement procurement,
        UpsertProcurementAttachmentsDto attachmentDto)
    {
        var attachment = ProcurementAttachment.Create(
            procurement.Id,
            attachmentDto.Sequence,
            ParameterCode.From(attachmentDto.DocumentTypeCode),
            attachmentDto.Remark);

        procurement.AddAttachment(attachment);
        this.dbContext.ProcurementAttachments.Add(attachment);

        return attachment;
    }

    private static void RemoveUnreferencedAttachments(
        Procurement procurement,
        HashSet<ProcurementAttachmentId> requestAttachmentIds,
        HashSet<FileId> requestFileIds,
        List<string> removedFileNames)
    {
        var attachmentsToRemove = procurement.Attachments
                                             .Where(pa => !requestAttachmentIds.Contains(pa.Id) &&
                                                          !pa.ProcurementAttachmentInfos.Any(info => requestFileIds.Contains(info.FileId)))
                                             .ToArray();

        foreach (var attachmentToRemove in attachmentsToRemove)
        {
            removedFileNames.AddRange(attachmentToRemove.ProcurementAttachmentInfos.Select(f => f.FileName));
            procurement.RemoveAttachment(attachmentToRemove);
        }
    }

    private void UpsertFileAttachmentsForAttachment(
        SuUser user,
        ProcurementAttachment attachment,
        IEnumerable<UpsertProcurementFileAttachments> fileAttachments,
        List<string> addedFileNames,
        List<string> removedFileNames)
    {
        var fileAttachmentsArray = fileAttachments.ToArray();

        // Process each file attachment from a request
        foreach (var fileDto in fileAttachmentsArray)
        {
            this.UpsertSingleFileAttachment(user, attachment, fileDto, addedFileNames);
        }

        // Remove file attachments that are not in the request (by FileId)
        RemoveUnreferencedFileAttachments(attachment, fileAttachmentsArray, removedFileNames);
    }

    private void UpsertSingleFileAttachment(
        SuUser user,
        ProcurementAttachment attachment,
        UpsertProcurementFileAttachments fileDto,
        List<string> addedFileNames)
    {
        var existingFileAttachment = attachment.ProcurementAttachmentInfos
                                               .FirstOrDefault(i => i.FileId == FileId.From(fileDto.FileId));

        if (existingFileAttachment is null)
        {
            this.CreateNewFileAttachment(attachment, fileDto, addedFileNames);

            return;
        }

        // Update file attachment properties based on ownership
        UpdateExistingFileAttachment(user, existingFileAttachment, fileDto);
    }

    private void CreateNewFileAttachment(
        ProcurementAttachment attachment,
        UpsertProcurementFileAttachments fileDto,
        List<string> addedFileNames)
    {
        var fileAttachment = ProcurementAttachmentInfo.Create(
            attachment.Id,
            fileDto.Sequence,
            FileId.From(fileDto.FileId),
            fileDto.FileName,
            fileDto.IsPublic);

        attachment.AddAttachmentInfos(fileAttachment);
        this.dbContext.ProcurementAttachmentInfos.Add(fileAttachment);
        addedFileNames.Add(fileDto.FileName);
    }

    private static void UpdateExistingFileAttachment(
        SuUser user,
        ProcurementAttachmentInfo fileAttachment,
        UpsertProcurementFileAttachments fileDto)
    {
        var editorId = user.Id.Value;
        var fileOwnerId = fileAttachment.AuditInfo.CreatedBy;

        if (editorId == fileOwnerId)
        {
            // Owner can update all properties
            fileAttachment.SetSequence(fileDto.Sequence)
                          .SetPublic(fileDto.IsPublic);

            return;
        }

        // Non-owner can only update a sequence
        fileAttachment.SetSequence(fileDto.Sequence);
    }

    private static void RemoveUnreferencedFileAttachments(
        ProcurementAttachment attachment,
        UpsertProcurementFileAttachments[] requestFileAttachments,
        List<string> removedFileNames)
    {
        var incomingFileIds = requestFileAttachments.Select(f => FileId.From(f.FileId)).ToHashSet();

        var fileAttachmentsToRemove = attachment.ProcurementAttachmentInfos
                                                .Where(pai => !incomingFileIds.Contains(pai.FileId))
                                                .ToArray();

        foreach (var fileAttachmentToRemove in fileAttachmentsToRemove)
        {
            removedFileNames.Add(fileAttachmentToRemove.FileName);
            attachment.RemoveAttachmentInfos(fileAttachmentToRemove);
        }
    }
}