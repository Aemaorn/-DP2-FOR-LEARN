namespace GHB.DP2.Application.Features.Document.Abstract;

using GHB.DP2.Domain.Common;

public abstract record DocumentMetadataResult
{
    private DocumentMetadataResult()
    {
    }

    public sealed record Success(
        string FileName,
        long FileSize,
        string ContentType,
        string FullName,
        DocumentState State) : DocumentMetadataResult;

    public sealed record Failed(string? Reason) : DocumentMetadataResult;

    public sealed record NotFound(string? Message) : DocumentMetadataResult;

    public static DocumentMetadataResult CreateSuccess(
        string fileName,
        long fileSize,
        string contentType,
        string fullName,
        DocumentState state) =>
        new Success(fileName, fileSize, contentType, fullName, state);

    public static DocumentMetadataResult CreateNotFound(string? message = null) =>
        new NotFound(message);
}