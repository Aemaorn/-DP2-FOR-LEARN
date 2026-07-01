namespace GHB.DP2.Application.Features.Document;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Features.Document.Abstract;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record GetFileRequest
{
    public Guid FileId { get; init; }

    [BindFrom("readOnly")]
    public bool ReadOnly { get; init; }

    [BindFrom("userId")]
    public Guid UserId { get; init; } // Fixme: receive UserId from JWT token instead of request
} // Fixme: receive UserId from JWT token instead of request

public class GetFileEndpoint : DocumentEndpointBase<GetFileRequest, Results<FileContentHttpResult, NotFound<string>>>
{
    private readonly IFileServiceClient fileServiceClient;

    public GetFileEndpoint(
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext,
        ILogger<GetFileEndpoint> logger)
        : base(fileServiceClient, dbContext, logger)
    {
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("wopi"));
        this.Get("/wopi/files/{FileId:guid}/contents"); // Changed from Post to Get
        this.AllowAnonymous();
    }

    protected override async ValueTask<Results<FileContentHttpResult, NotFound<string>>> HandleRequestAsync(GetFileRequest req, CancellationToken ct)
    {
        var file = await this.fileServiceClient.DownloadAsync(FileId.From(req.FileId), cancellationToken: ct);

        if (file == null)
        {
            return TypedResults.NotFound("File not found.");
        }

        return TypedResults.File(file.Contents, contentType: file.MimeType);
    }
}