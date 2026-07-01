namespace GHB.DP2.Application.Features.SystemUtility.SuVendor;

using Codehard.FileService.Client;
using Codehard.FileService.Client.Abstractions;
using FluentValidation;
using GHB.DP2.Application.Contracts;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Services;
using GHB.DP2.Application.Validators;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UploadSuVendorAttachmentRequest
{
    public Guid Id { get; init; }

    [FromForm]
    public AttachmentMetadataRequest AttachmentRequest { get; init; }
}

public class AttachmentMetadataRequest
{
    public IEnumerable<AttachmentFilesWithMetadata> Attachments { get; init; }
}

public record AttachmentFilesWithMetadata(IFormFile File, bool IsPrivate) : IHasFile;

public class AttachmentMetadataValidation : Validator<AttachmentMetadataRequest>
{
    public AttachmentMetadataValidation()
    {
        this.RuleFor(x => x.Attachments)
            .NotNull()
            .NotEmpty()
            .WithMessage("Attachments are required");

        this.RuleForEach(x => x.Attachments)
            .MustBeValidFile();
    }
}

public class UploadSuVendorAttachment : SecureEndpointBase<UploadSuVendorAttachmentRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    public UploadSuVendorAttachment(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IPermissionValidationService permissionValidationService,
        ILogger<UploadSuVendorAttachment> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Description(x => x.Accepts<UploadSuVendorAttachmentRequest>("multipart/form-data"));
        this.Options(x => x.WithTags("SuVendor"));
        this.Post("/st/st003/{id:guid}/attachment");
        this.AllowFileUploads();
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UploadSuVendorAttachmentRequest req, CancellationToken ct)
    {
        var suVendor = await this.dbContext.SuVendors
                                 .Include(s => s.Attachments)
                                 .FirstOrDefaultAsync(x => x.Id == SuVendorId.From(req.Id), ct);

        if (suVendor is null)
        {
            return TypedResults.NotFound($"SuVendor with Id {req.Id} not found");
        }

        var sequence = suVendor.Attachments
                               .Select(x => x.Sequence)
                               .DefaultIfEmpty(0)
                               .Max() + 1;

        foreach (var item in req.AttachmentRequest.Attachments)
        {
            var contents = await item.File.ReadFileAsync(CancellationToken.None);
            var type = new ContentType(Path.GetExtension(item.File.FileName).TrimStart('.'), item.File.ContentType);

            var uploadFile = await this.fileServiceClient.UploadFileAsync(contents, item.File.FileName, contentType: type, cancellationToken: CancellationToken.None);

            suVendor.AddAttachment(item.File.FileName, sequence++, uploadFile.Id, item.IsPrivate);
        }

        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.Ok();
    }
}