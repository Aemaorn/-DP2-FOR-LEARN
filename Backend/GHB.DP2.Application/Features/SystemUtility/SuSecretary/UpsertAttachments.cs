namespace GHB.DP2.Application.Features.SystemUtility.SuSecretary;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UpsertSt010AttachmentsRequest
{
    public Guid Id { get; init; }

    public AttachmentsDtoWithId[] Attachments { get; init; } = [];
}

public class UpsertSt010AttachmentsEndpoint : SecureEndpointBase<UpsertSt010AttachmentsRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpsertSt010AttachmentsEndpoint(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<UpsertSt010AttachmentsEndpoint> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSecretary"));
        this.Put("/st/st010/{Id:guid}/attachment");
        this.AuditLog("กำหนดเลขา", "แก้ไขเอกสารแนบ");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(
        UpsertSt010AttachmentsRequest req,
        CancellationToken ct)
    {
        var owner = await this.dbContext.SuSecretaryOwners
                              .Include(o => o.Attachments)
                              .SingleOrDefaultAsync(o => o.Id == SecretaryOwnerId.From(req.Id), ct);

        if (owner is null)
        {
            return TypedResults.NotFound($"SuSecretaryOwner with Id {req.Id} not found");
        }

        var incomingAttachments = req.Attachments
                                     .SelectMany(a => a.FileAttachments.Select(f => (
                                         a.DocumentTypeCode,
                                         FileId: FileId.From(f.FileId),
                                         f.FileName,
                                         f.Sequence,
                                         f.IsPublic)))
                                     .ToList();

        var existingAttachments = owner.Attachments.ToList();

        var removedAttachments = existingAttachments
            .Where(e => incomingAttachments.All(f => f.FileId != e.FileId))
            .ToList();

        foreach (var removed in removedAttachments)
        {
            owner.RemoveAttachmentById(removed.Id.Value);
        }

        this.dbContext.SuSecretaryAttachments.RemoveRange(removedAttachments);

        foreach (var existing in existingAttachments.Where(e => incomingAttachments.Any(f => f.FileId == e.FileId)))
        {
            var incoming = incomingAttachments.First(f => f.FileId == existing.FileId);
            existing.IsPublic = incoming.IsPublic;
            existing.Sequence = incoming.Sequence;
            existing.DocumentTypeCode = incoming.DocumentTypeCode;
        }

        var newAttachments = incomingAttachments
            .Where(f => existingAttachments.All(e => e.FileId != f.FileId))
            .ToList();

        foreach (var newAttach in newAttachments)
        {
            owner.AddAttachment(SuSecretaryAttachment.Create(
                owner.Id,
                newAttach.FileId,
                newAttach.FileName,
                newAttach.Sequence,
                newAttach.DocumentTypeCode,
                null,
                newAttach.IsPublic));
        }

        if (removedAttachments.Count > 0)
        {
            owner.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.DeleteFile,
                ActivityLogActionTypeConstant.DeleteFile,
                "กำหนดเลขา",
                string.Join(", ", removedAttachments.Select(a => a.FileName))));
        }

        if (newAttachments.Count > 0)
        {
            owner.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.UploadFile,
                ActivityLogActionTypeConstant.UploadFile,
                "กำหนดเลขา",
                string.Join(", ", newAttachments.Select(f => f.FileName))));
        }

        this.dbContext.SuSecretaryOwners.Update(owner);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}
