namespace GHB.DP2.Application.Features.Files;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Contracts;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Services.Pdf;
using GHB.DP2.Application.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public class UploadFileRequest
{
    [FromForm]
    public UploadFileRequestDto Request { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }
}

public record UploadFileRequestDto(IFormFile File) : IHasFile;

public record UploadFileResponse(Guid Id, Guid CreatedBy, int? PageCount);

public class UploadFileRequestValidation : Validator<UploadFileRequest>
{
    public UploadFileRequestValidation()
    {
        this.RuleFor(x => x.Request)
            .MustBeValidFile();
    }
}

public class UploadFile : EndpointBase<UploadFileRequest, Results<Ok<UploadFileResponse>, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;
    private readonly IPdfService pdfService;

    public UploadFile(
        IFileServiceClient fileServiceClient,
        IPdfService pdfService,
        ILogger<UploadFile> logger)
        : base(logger)
    {
        this.fileServiceClient = fileServiceClient;
        this.pdfService = pdfService;
    }

    public override void Configure()
    {
        this.Description(x => x.Accepts<UploadFileRequest>("multipart/form-data"));
        this.Options(x => x.WithTags("File"));
        this.Post("/files");
        this.AllowFileUploads();
    }

    protected override async ValueTask<Results<Ok<UploadFileResponse>, NotFound<string>>> HandleRequestAsync(UploadFileRequest req, CancellationToken ct)
    {
        var contents = await req.Request.File.ReadFileAsync(CancellationToken.None);
        var pageCount = this.pdfService.GetPageCount(contents, req.Request.File.ContentType);
        var type = new ContentType(Path.GetExtension(req.Request.File.FileName).TrimStart('.'), req.Request.File.ContentType);

        var uploadResult = await this.fileServiceClient.UploadFileAsync(
            contents,
            directoryPath: req.Request.File.FileName,
            contentType: type,
            cancellationToken: CancellationToken.None);

        return TypedResults.Ok(new UploadFileResponse(uploadResult.Id.Value, req.UserId, pageCount));
    }
}