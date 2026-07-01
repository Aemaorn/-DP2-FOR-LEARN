namespace GHB.DP2.Application.Features.Files;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Contracts;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public class UploadTemplateFileRequest
{
    [FromForm]
    public UploadTemplateFileRequestDto Request { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }
}

public record UploadTemplateFileRequestDto(IFormFile File) : IHasFile;

// Template-only upload route. Lives next to /files but uses a stricter, document-template
// whitelist (currently only .odt) so the general /files endpoint can stay tight.
public class UploadTemplateFileValidation : Validator<UploadTemplateFileRequest>
{
    public UploadTemplateFileValidation()
    {
        this.RuleFor(x => x.Request)
            .MustBeValidTemplateFile();
    }
}

public class UploadTemplateFile : EndpointBase<UploadTemplateFileRequest, Results<Ok<UploadFileResponse>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;

    public UploadTemplateFile(
        IFileServiceClient fileServiceClient,
        ILogger<UploadTemplateFile> logger)
        : base(logger)
    {
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Description(x => x.Accepts<UploadTemplateFileRequest>("multipart/form-data"));
        this.Options(x => x.WithTags("File"));
        this.Post("/files/template");
        this.AllowFileUploads();
    }

    protected override async ValueTask<Results<Ok<UploadFileResponse>, NotFound<string>>> HandleRequestAsync(
        UploadTemplateFileRequest req,
        CancellationToken ct)
    {
        var contents = await req.Request.File.ReadFileAsync(CancellationToken.None);
        var type = new ContentType(
            Path.GetExtension(req.Request.File.FileName).TrimStart('.'),
            req.Request.File.ContentType);

        var uploadResult = await this.fileServiceClient.UploadFileAsync(
            contents,
            directoryPath: req.Request.File.FileName,
            contentType: type,
            cancellationToken: CancellationToken.None);

        // Templates are .odt — no PDF page count to compute.
        return TypedResults.Ok(new UploadFileResponse(uploadResult.Id.Value, req.UserId, PageCount: null));
    }
}
