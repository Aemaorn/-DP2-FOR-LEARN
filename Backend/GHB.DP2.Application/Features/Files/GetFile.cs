namespace GHB.DP2.Application.Features.Files;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public class GetFileRequest
{
    public Guid Id { get; init; }
}

public record GetFileResponse(string ContentType, byte[] Content);

public class GetFile : EndpointBase<GetFileRequest, IResult>
{
    private readonly IFileServiceClient fileServiceClient;

    public GetFile(IFileServiceClient fileServiceClient, ILogger<GetFile> logger)
        : base(logger)
    {
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("File"));
        this.Get("/files/{id:guid}");
        this.AllowAnonymous();
    }

    protected override async ValueTask<IResult> HandleRequestAsync(GetFileRequest req, CancellationToken ct)
    {
        var file = await this.fileServiceClient.DownloadAsync(
            FileId.From(req.Id),
            cancellationToken: ct);

        if (file == null)
        {
            return TypedResults.NotFound($"File with ID {req.Id} not found.");
        }

        var fileMeta = await this.fileServiceClient.GetFileMetadataAsync(FileId.From(req.Id), CancellationToken.None);

        return Results.File(file.Contents, file.MimeType, fileMeta?.FileName ?? DateTimeOffset.Now.ToString());
    }
}