namespace GHB.DP2.Application.Features.SystemUtility.SuVendor;

using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UpdateSuVendorAttachmentRequest
{
    public Guid Id { get; init; }

    [FromForm]
    public AttachmentUpdateRequest AttachmentRequest { get; init; }
}

public class AttachmentUpdateRequest
{
    public IEnumerable<AttachmentFileUpdate> Attachments { get; init; }
}

public record AttachmentFileUpdate(Guid FileId, int Sequence, bool IsPrivate);

public class UpdateSuVendorAttachment : SecureEndpointBase<UpdateSuVendorAttachmentRequest, Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateSuVendorAttachment(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<UpdateSuVendorAttachment> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Description(x => x.Accepts<UpdateSuVendorAttachmentRequest>("multipart/form-data"));
        this.Options(x => x.WithTags("SuVendor"));
        this.Put("/st/st003/{id:guid}/attachment");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(UpdateSuVendorAttachmentRequest req, CancellationToken ct)
    {
        var suVendors = await this.dbContext.SuVendors
                                  .Include(s => s.Attachments)
                                  .FirstOrDefaultAsync(x => x.Id == SuVendorId.From(req.Id), ct);

        if (suVendors is null)
        {
            return TypedResults.NotFound($"SuVendor with Id {req.Id} not found");
        }

        var fileIdsToUpdate = req.AttachmentRequest.Attachments.ToDictionary(a => a.FileId, a => a);

        var attachmentsToUpdate = suVendors.Attachments
                                           .Where(x => fileIdsToUpdate.ContainsKey(x.FileId.Value))
                                           .ToList();

        var foundFileIds = attachmentsToUpdate.Select(a => a.FileId.Value).ToHashSet();

        if (!fileIdsToUpdate.Keys.All(id => foundFileIds.Contains(id)))
        {
            var missingFileIds = fileIdsToUpdate.Keys.Except(foundFileIds);

            return TypedResults.NotFound($"SuVendorAttachments with FileIds {string.Join(", ", missingFileIds)} not found");
        }

        attachmentsToUpdate.Iter(a => a.Update(
                fileIdsToUpdate[a.FileId.Value].Sequence,
                fileIdsToUpdate[a.FileId.Value].IsPrivate));

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}