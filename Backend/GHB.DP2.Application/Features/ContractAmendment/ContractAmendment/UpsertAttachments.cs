namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpsertAttachmentsRequest(
    Guid ContractAmendmentId,
    AttachmentsDtoWithId[] Attachments);

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
            b.WithTags("ContractAmendment")
             .WithName("UpsertAttachments")
             .Produces(StatusCodes.Status200OK)
             .Produces<string>(StatusCodes.Status404NotFound));
        this.Put("/contract-amendments/{contractAmendmentId:guid}/attachments");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpsertAttachmentsRequest req, CancellationToken ct)
    {
        var entity =
            await this.dbContext.CamContractAmendments
                      .Include(e => e.ContractDraftVendor)
                      .SingleOrDefaultAsync(
                          e =>
                              e.Id == CamContractAmendmentId.From(req.ContractAmendmentId),
                          ct);

        if (entity == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลบันทึกต่อท้ายสัญญา");
        }

        await this.UpsertAttachmentsAsync(
            entity,
            req.Attachments,
            ct);

        this.dbContext.CamContractAmendments.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task UpsertAttachmentsAsync(
        CamContractAmendment entity,
        IEnumerable<AttachmentsDtoWithId> attachments,
        CancellationToken ct)
    {
        var attachmentList =
            attachments
                .SelectMany(MapToEntity)
                .ToArray();

        var removedAttachments = entity.Attachments
                                       .Where(e => attachmentList.All(f => f.FileId != e.FileId))
                                       .ToArray();

        foreach (var a in removedAttachments)
        {
            await this.fileServiceClient.DeleteAsync(a.FileId, ct);
            entity.RemoveAttachment(a);
        }

        if (removedAttachments.Length > 0)
        {
            entity.ContractDraftVendor.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.DeleteFile,
                ActivityLogActionTypeConstant.DeleteFile,
                nameof(entity.Status),
                string.Join(", ", removedAttachments.Select(a => a.FileName))));
        }

        entity.Attachments
              .Join(
                  attachmentList,
                  e => e.FileId,
                  f => f.FileId,
                  (e, f) =>
                      (Existing: e, New: f))
              .Iter(pair =>
              {
                  pair.Existing
                      .ChangeDocumentType(pair.New.DocumentTypeCode)
                      .SetPublic(pair.New.IsPublic)
                      .SetSequence(pair.New.Sequence);
              });

        var newAttachments = attachmentList
                             .Where(f => entity.Attachments.All(e => e.FileId != f.FileId))
                             .ToArray();

        _ = newAttachments.Map(entity.AddAttachment).ToHashSet();

        if (newAttachments.Length > 0)
        {
            entity.ContractDraftVendor.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.UploadFile,
                ActivityLogActionTypeConstant.UploadFile,
                nameof(entity.Status),
                string.Join(", ", newAttachments.Select(a => a.FileName))));
        }
    }

    private static IEnumerable<CamContractAmendmentAttachment> MapToEntity(
        AttachmentsDtoWithId attachment)
    {
        foreach (var item in attachment.FileAttachments)
        {
            yield return
                CamContractAmendmentAttachment
                    .Create(
                        ParameterCode.From(attachment.DocumentTypeCode),
                        FileId.From(item.FileId),
                        item.FileName)
                    .SetPublic(item.IsPublic)
                    .SetSequence(item.Sequence);
        }
    }
}