namespace GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpsertCertificateRequisitionAttachmentRequest(
    Guid Id,
    AttachmentsDtoWithId[] Attachments);

public class UpsertCertificateRequisitionAttachmentEndpoint
    : EndpointBase<UpsertCertificateRequisitionAttachmentRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public UpsertCertificateRequisitionAttachmentEndpoint(
        ILogger<UpsertCertificateRequisitionAttachmentEndpoint> logger,
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
            b.WithTags("ContractAmendment/CertificateRequisition")
             .WithName("UpsertCertificateRequisitionAttachment")
             .Produces(StatusCodes.Status200OK)
             .Produces<string>(StatusCodes.Status404NotFound));
        this.Put("/certificate-requisition/{Id:guid}/attachment");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(
        UpsertCertificateRequisitionAttachmentRequest req,
        CancellationToken ct)
    {
        var entity =
            await this.dbContext.CamCertificateRequisitions
                      .Include(e => e.Attachments)
                      .Include(e => e.ContractDraftVendor)
                      .SingleOrDefaultAsync(
                          e => e.Id == CamCertificateRequisitionId.From(req.Id),
                          ct);

        if (entity == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลใบขอหนังสือรับรอง");
        }

        await this.UpsertAttachmentsAsync(entity, req.Attachments, ct);

        this.dbContext.CamCertificateRequisitions.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task UpsertAttachmentsAsync(
        CamCertificateRequisition entity,
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
            entity.ContractDraftVendor?.AddActivity(new ActivityInfo(
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
                  (e, f) => (Existing: e, New: f))
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
            entity.ContractDraftVendor?.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.UploadFile,
                ActivityLogActionTypeConstant.UploadFile,
                nameof(entity.Status),
                string.Join(", ", newAttachments.Select(a => a.FileName))));
        }
    }

    private static IEnumerable<CamCertificateRequisitionAttachment> MapToEntity(
        AttachmentsDtoWithId attachment)
    {
        foreach (var item in attachment.FileAttachments)
        {
            yield return
                CamCertificateRequisitionAttachment
                    .Create(
                        ParameterCode.From(attachment.DocumentTypeCode),
                        FileId.From(item.FileId),
                        item.FileName)
                    .SetPublic(item.IsPublic)
                    .SetSequence(item.Sequence);
        }
    }
}
