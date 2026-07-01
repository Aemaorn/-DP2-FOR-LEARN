namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Contracts;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Application.Validators;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
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

    public Guid DeliveryAcceptanceId { get; init; }

    public Guid Id { get; init; }

    public AttachmentsDtoWithId[] Attachments { get; init; }
}

public class UpsertAttachmentsRequestValidator : Validator<UpsertAttachmentsRequest>
{
    public UpsertAttachmentsRequestValidator()
    {
        this.AddAttachmentsRules();
    }
}

public class UpsertAttachmentsEndpoint : DeliveryAcceptancePeriodEndpointBase<UpsertAttachmentsRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpsertAttachmentsEndpoint(
        Dp2DbContext dbContext,
        ILogger<UpsertAttachmentsEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/DeliveryAcceptance/Period")
             .WithName("UpsertDeliveryAcceptancePeriodAttachments")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<UpsertAttachmentsRequest>("application/json"));
        this.Put("delivery-acceptance/{DeliveryAcceptanceId:guid}/period/{Id:guid}/attachment");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpsertAttachmentsRequest req, CancellationToken ct)
    {
        await this.ValidateUserAsync(UserId.From(req.UserId), ct);
        await this.ValidateDocumentTypeCode(req.Attachments, ct);

        var periodExisting = await this.GetById(
            CmDeliveryAcceptanceId.From(req.DeliveryAcceptanceId),
            CmDeliveryAcceptancePeriodId.From(req.Id),
            ct);

        if (periodExisting is null)
        {
            return TypedResults.NotFound("ไม่พบงวดการส่งมอบ/ตรวจรับ");
        }

        this.UpsertAttachments(periodExisting, req.Attachments);

        this.dbContext.CmDeliveryAcceptancePeriods.Update(periodExisting);
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

    private void UpsertAttachments(
        CmDeliveryAcceptancePeriod periodExisting,
        AttachmentsDtoWithId[] attachments)
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

        var incomingFileIds = fileList.Select(f => FileId.From(f.FileId)).ToHashSet();
        var existingFileIds = periodExisting.Attachments.Select(a => a.FileId).ToHashSet();

        var removedAttachments = periodExisting.Attachments
                                               .Where(a => !incomingFileIds.Contains(a.FileId))
                                               .ToArray();

        foreach (var attachment in removedAttachments)
        {
            periodExisting.RemoveAttachment(attachment);
        }

        if (removedAttachments.Length > 0)
        {
            periodExisting.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.DeleteFile,
                ActivityLogActionTypeConstant.DeleteFile,
                nameof(periodExisting.Status),
                string.Join(", ", removedAttachments.Select(a => a.FileName))));
        }

        var newFiles = fileList.Where(f => !existingFileIds.Contains(FileId.From(f.FileId))).ToArray();

        foreach (var f in newFiles)
        {
            periodExisting.AddAttachment(
                CmDeliveryAcceptancePeriodAttachment.Create(
                    periodExisting.Id,
                    ParameterCode.From(f.DocumentTypeCode),
                    FileId.From(f.FileId),
                    f.FileName,
                    f.Sequence,
                    f.IsPublic));
        }

        if (newFiles.Length > 0)
        {
            periodExisting.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.UploadFile,
                ActivityLogActionTypeConstant.UploadFile,
                nameof(periodExisting.Status),
                string.Join(", ", newFiles.Select(f => f.FileName))));
        }

        foreach (var existing in periodExisting.Attachments)
        {
            var match = fileList.FirstOrDefault(f => FileId.From(f.FileId) == existing.FileId);

            if (match != null)
            {
                existing.SetIsPublic(match.IsPublic)
                        .SetSequence(match.Sequence)
                        .SetDocumentType(ParameterCode.From(match.DocumentTypeCode));
            }
        }
    }
}
