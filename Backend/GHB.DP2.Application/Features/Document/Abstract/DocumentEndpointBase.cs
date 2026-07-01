namespace GHB.DP2.Application.Features.Document.Abstract;

using System.Text.Json.Serialization;
using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetDocumentMetadataRequest(FileId DocumentId, UserId? UserId);

public record UpdateDocumentMetadataRequest(FileId DocumentId, byte[] Contents);

public record RejectDocumentChanges(byte[] Contents);

public record FileInfoTemplate
{
    [JsonPropertyName("BaseFileName")]
    public required string BaseFileName { get; set; }

    [JsonPropertyName("Size")]
    public required long Size { get; set; }

    [JsonPropertyName("UserId")]
    public required Guid UserId { get; set; }

    [JsonPropertyName("UserFriendlyName")]
    public required string UserFriendlyName { get; set; }

    [JsonPropertyName("UserCanWrite")]
    public required bool UserCanWrite { get; set; }

    [JsonPropertyName("PostMessageOrigin")]
    public required string PostMessageOrigin { get; set; }
}

public abstract class DocumentEndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TResponse : IResult
    where TRequest : notnull
{
    private readonly IFileServiceClient fileServiceClient;
    private readonly Dp2DbContext dbContext;

    protected DocumentEndpointBase(
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext,
        ILogger logger)
        : base(logger)
    {
        this.fileServiceClient = fileServiceClient;
        this.dbContext = dbContext;
    }

    protected async Task<DocumentMetadataResult> GetDocumentMetadataAsync(
        GetDocumentMetadataRequest request,
        CancellationToken cancellationToken)
    {
        var fileMetadata = await this.fileServiceClient
                                     .GetFileMetadataAsync(request.DocumentId, cancellationToken);

        if (fileMetadata == null)
        {
            return DocumentMetadataResult.CreateNotFound("File not found.");
        }

        var userOpt = await this.dbContext.SuUsers
                                .Where(u => u.Id == request.UserId)
                                .FirstOrDefaultAsync(cancellationToken);

        var documentMetadataOpt = await this.GetFileMetadata(request.DocumentId, cancellationToken);

        return DocumentMetadataResult.CreateSuccess(
            fileMetadata.FileName,
            fileMetadata.FileSize,
            fileMetadata.MimeType,
            userOpt != null ? userOpt.FullName : string.Empty,
            documentMetadataOpt.State);
    }

    protected async Task<UpdateDocumentResult> UpdateDocumentMetadataAsync(
        UpdateDocumentMetadataRequest request,
        CancellationToken cancellationToken)
    {
        var documentMetadata = await this.GetDocumentMetadataAsync(
            new GetDocumentMetadataRequest(request.DocumentId, UserId.From(Guid.Empty)),
            cancellationToken);

        var documentState = documentMetadata switch
        {
            DocumentMetadataResult.Success succ => succ.State,
            _ => throw new InvalidOperationException("Unable to read document state."),
        };

        var content = documentState == DocumentState.Finalized
            ? await DocumentEndpointBase<TRequest, TResponse>.CheckFinalizedDocumentState(request.Contents)
            : request.Contents;

        await this.fileServiceClient
                  .ReplaceFileAsync(
                      request.DocumentId,
                      content,
                      cancellationToken: cancellationToken);

        return new UpdateDocumentResult.Success();
    }

    // FIXME: handle if DocumentState is finalized reject changes
    private static Task<RejectDocumentChangesResult> RejectDocumentChanges(RejectDocumentChanges request)
    {
        return Task.FromResult((RejectDocumentChangesResult)new RejectDocumentChangesResult.Success(request.Contents));
    }

    private static async Task<byte[]> CheckFinalizedDocumentState(
        byte[] contents)
    {
        var rejectResult = await RejectDocumentChanges(new RejectDocumentChanges(contents));

        var contentsResult = rejectResult switch
        {
            RejectDocumentChangesResult.Success succ => succ.Contents,
            RejectDocumentChangesResult.Failure failed => throw new InvalidOperationException(failed.Exception.Message),
            _ => throw new NotSupportedException("Unknown result type."),
        };

        return contentsResult;
    }

    private async Task<DocumentMetadata> GetFileMetadata(FileId documentId, CancellationToken cancellationToken)
    {
        var documentMetadataOpt = await this.dbContext.FindAsync<DocumentMetadata>(documentId, cancellationToken);

        if (documentMetadataOpt == null)
        {
            var dm = DocumentMetadata.Create(documentId);
            this.dbContext.Add(dm);
            await this.dbContext.SaveChangesAsync(cancellationToken);

            return dm;
        }

        return documentMetadataOpt;
    }
}