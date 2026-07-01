namespace GHB.DP2.Application.Features.Document;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Features.Document.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class CheckFileRequest
{
    public Guid FileId { get; init; }

    [BindFrom("readOnly")]
    public bool ReadOnly { get; init; }

    [BindFrom("userId")]
    public string UserId { get; init; } // Fixme: receive UserId from JWT token instead of request
}

public class CheckFileEndpoint : DocumentEndpointBase<CheckFileRequest, Results<Ok<FileInfoTemplate>, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CheckFileEndpoint(
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext,
        ILogger<CheckFileEndpoint> logger)
        : base(fileServiceClient, dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("wopi"));
        this.Get("/wopi/files/{FileId:guid}");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Results<Ok<FileInfoTemplate>, NotFound<string>, BadRequest<string>>>
        HandleRequestAsync(CheckFileRequest req, CancellationToken ct)
    {
        var userIdSplit = req.UserId.Split("?")[0];
        var userIdGuid = Guid.Parse(userIdSplit);
        var userId = UserId.From(userIdGuid);
        var documentId = FileId.From(req.FileId);
        var user = await this.dbContext.SuUsers.SingleOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
        {
            return TypedResults.NotFound("User not found.");
        }

        var documentMetadataQueryResult = await this.GetDocumentMetadataAsync(
            new GetDocumentMetadataRequest(documentId, userId), ct);

        return documentMetadataQueryResult switch
        {
            DocumentMetadataResult.Success result => TypedResults.Ok(new FileInfoTemplate
            {
                BaseFileName = result.FileName,
                Size = result.FileSize,
                UserCanWrite = result.State != DocumentState.Finalized && !req.ReadOnly,
                UserId = userIdGuid,
                UserFriendlyName = user.FullName,
                PostMessageOrigin = "*",
            }),
            DocumentMetadataResult.Failed failed => TypedResults.BadRequest(failed.Reason),
            DocumentMetadataResult.NotFound => TypedResults.NotFound("Document not found."),
            _ => throw new NotSupportedException(),
        };
    }
}