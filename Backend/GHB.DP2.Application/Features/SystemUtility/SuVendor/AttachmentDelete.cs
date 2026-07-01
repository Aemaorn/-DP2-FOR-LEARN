namespace GHB.DP2.Application.Features.SystemUtility.SuVendor;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class DeleteSuVendorAttachmentRequest
{
    public Guid Id { get; init; }

    public Guid AttachmentId { get; init; }

    public Guid FileId { get; init; }
}

public class DeleteSuVendorAttachment : SecureEndpointBase<DeleteSuVendorAttachmentRequest, NoContent>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public DeleteSuVendorAttachment(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IPermissionValidationService permissionValidationService,
        ILogger<DeleteSuVendorAttachment> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuVendor"));
        this.Delete("/st/st003/{id:guid}/attachment/{AttachmentId:guid}/file/{FileId:guid}");
    }

    protected override async ValueTask<NoContent> HandleRequestAsync(DeleteSuVendorAttachmentRequest req, CancellationToken ct)
    {
        var vendor = await this.dbContext.SuVendors
                               .Include(a => a.Attachments)
                              .SingleOrDefaultAsync(w => w.Id == SuVendorId.From(req.Id), ct);

        if (vendor is null)
        {
            this.ThrowError("ไม่พบข้อมูล", statusCode: StatusCodes.Status404NotFound);
        }

        vendor.RemoveAttachmentById(req.AttachmentId);

        this.dbContext.SuVendors.Update(vendor);
        await this.fileServiceClient.DeleteAsync(FileId.From(req.FileId), CancellationToken.None);

        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.NoContent();
    }
}