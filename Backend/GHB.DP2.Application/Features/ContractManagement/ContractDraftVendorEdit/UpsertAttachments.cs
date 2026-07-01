namespace GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Contracts;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Abstract;
using GHB.DP2.Application.Validators;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UpsertContractDraftVendorEditAttachmentsRequest : IHasAttachmentsWithId
{
    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public Guid Id { get; init; }

    public AttachmentsDtoWithId[] Attachments { get; init; }
}

public class UpsertContractDraftVendorEditAttachmentsRequestValidator : Validator<UpsertContractDraftVendorEditAttachmentsRequest>
{
    public UpsertContractDraftVendorEditAttachmentsRequestValidator()
    {
        this.AddAttachmentsRules();
    }
}

public class UpsertContractDraftVendorEditAttachmentsEndpoint
    : ContractDraftVendorEditEndpoint<UpsertContractDraftVendorEditAttachmentsRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpsertContractDraftVendorEditAttachmentsEndpoint(
        ILogger<UpsertContractDraftVendorEditAttachmentsEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractDraftVendorEdit")
             .WithName("UpsertContractDraftVendorEditAttachments")
             .Produces<Ok>()
             .Produces<NotFound<string>>()
             .Accepts<UpsertContractDraftVendorEditAttachmentsRequest>("application/json"));
        this.Put("contract/contract-draft-vendor-edit/{Id:guid}/attachment");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(
        UpsertContractDraftVendorEditAttachmentsRequest req,
        CancellationToken ct)
    {
        await this.ValidateUserAsync(UserId.From(req.UserId), ct);
        await this.ValidateDocumentTypeCodeAsync(req.Attachments, ct);

        var entity = await this.GetEditByIdAsync(ContractDraftVendorEditId.From(req.Id), ct);

        this.UpsertGeneralAttachments(entity, req.Attachments);

        this.dbContext.CaContractDraftVendorEdits.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task ValidateUserAsync(UserId userId, CancellationToken ct)
    {
        var user = await this.dbContext.SuUsers
                             .SingleOrDefaultAsync(w => w.Id == userId && w.IsActive, ct);

        if (user is null)
        {
            this.ThrowError("ไม่พบผู้ใช้งานในระบบ", StatusCodes.Status404NotFound);
        }
    }

    private async Task ValidateDocumentTypeCodeAsync(AttachmentsDtoWithId[] attachments, CancellationToken ct)
    {
        var docTypeCodes = attachments
                           .Select(s => s.DocumentTypeCode)
                           .Where(w => !string.IsNullOrWhiteSpace(w))
                           .Select(ParameterCode.From)
                           .ToArray();

        if (!docTypeCodes.Any())
        {
            return;
        }

        var existingDocTypes = await this.dbContext.SuParameters
                                         .Where(x => docTypeCodes.Contains(x.Code))
                                         .ToArrayAsync(ct);

        var missingDocumentTypes = docTypeCodes
                                   .Except(existingDocTypes.Select(dt => dt.Code))
                                   .ToArray();

        if (missingDocumentTypes.Any())
        {
            this.ThrowError("ไม่พบประเภทไฟล์", StatusCodes.Status404NotFound);
        }
    }

    private void UpsertGeneralAttachments(CaContractDraftVendorEdit entity, AttachmentsDtoWithId[] attachments)
    {
        var existingAttachments = entity.CheckerAttachment
                                        .Where(a => a.Type == EntrepreneurAttachmentType.General)
                                        .ToArray();

        var incomingFileIds = attachments
                              .SelectMany(dto => dto.FileAttachments)
                              .Select(f => FileId.From(f.FileId))
                              .ToHashSet();

        var existingFileIds = existingAttachments
                              .Select(a => a.FileId)
                              .ToHashSet();

        var trulyRemovedAttachments = existingAttachments
                                      .Where(a => !incomingFileIds.Contains(a.FileId))
                                      .ToArray();

        foreach (var existing in existingAttachments)
        {
            entity.RemoveAttachment(existing);
        }

        if (trulyRemovedAttachments.Length > 0)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.DeleteFile,
                ActivityLogActionTypeConstant.DeleteFile,
                nameof(entity.Status),
                string.Join(", ", trulyRemovedAttachments.Select(a => a.FileName))));
        }

        var newFileNames = new List<string>();
        var sequence = 1;

        foreach (var attachmentDto in attachments)
        {
            foreach (var file in attachmentDto.FileAttachments)
            {
                entity.AddAttachment(
                    CaContractDraftEditVendorCheckerAttachments.Create(
                        ParameterCode.From(attachmentDto.DocumentTypeCode),
                        FileId.From(file.FileId),
                        file.FileName,
                        EntrepreneurAttachmentType.General,
                        sequence++,
                        file.IsPublic));

                if (!existingFileIds.Contains(FileId.From(file.FileId)))
                {
                    newFileNames.Add(file.FileName);
                }
            }
        }

        if (newFileNames.Count > 0)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.UploadFile,
                ActivityLogActionTypeConstant.UploadFile,
                nameof(entity.Status),
                string.Join(", ", newFileNames)));
        }
    }
}
