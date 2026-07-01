namespace GHB.DP2.Application.Services.Document;

using System.IO.Compression;
using System.Linq.Expressions;
using Codehard.FileService.Client;
using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.EntityFrameworkCore;

public interface IDocumentService
{
    /// <summary>
    /// Asynchronously retrieves the document template file identifier based on the provided predicate.
    /// </summary>
    /// <param name="predicate">
    /// A predicate expression to filter the document template based on specific conditions.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that allows the operation to be cancelled.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the file identifier of the document template if found; otherwise, null.
    /// </returns>
    public Task<FileId?> GetDocumentTemplateAsync(
        Expression<Func<SuDocumentTemplate, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the document template file identifier based on the provided predicate and parent directory.
    /// </summary>
    /// <param name="predicate">
    /// A predicate expression to filter the document template based on specific conditions.
    /// </param>
    /// <param name="parentDirectory">
    /// The parent directory path where document templates are stored.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that allows the operation to be cancelled.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the file identifier of the document template if found; otherwise, null.
    /// </returns>
    public Task<FileId?> GetDocumentTemplateAsync(
        Expression<Func<SuDocumentTemplate, bool>> predicate,
        string parentDirectory = "DocumentTemplates/",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the document template file identifier based on the provided queryable.
    /// </summary>
    public Task<FileId?> GetDocumentTemplateAsync(
        IQueryable<SuDocumentTemplate> query,
        string parentDirectory = "DocumentTemplates/",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously copies a document template to the specified parent directory.
    /// </summary>
    /// <param name="fileId">
    /// The identifier of the file to be copied.
    /// </param>
    /// <param name="parentDirectory">
    /// The target directory where the document template will be copied. Defaults to "DocumentTemplates/".
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that allows the operation to be cancelled.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the identifier of the copied file if the operation is successful; otherwise, null.
    /// </returns>
    public Task<FileId?> CopyDocumentTemplateAsync(
        FileId fileId,
        string parentDirectory = "DocumentTemplates/",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously creates a copy of a document template, processes its contents with the provided replacement logic,
    /// and saves the modified document in the specified directory.
    /// </summary>
    /// <param name="fileId">
    /// The identifier of the file to be copied and replaced.
    /// </param>
    /// <param name="replaceAction">
    /// A function that defines the replacement logic for the file's contents.
    /// The input to the function represents the original file content in bytes.
    /// </param>
    /// <param name="parentDirectory">
    /// The directory where the modified document copy will be stored.
    /// If not specified, defaults to "DocumentTemplates/".
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that allows the operation to be cancelled.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the file identifier of the new document if the operation succeeds; otherwise, null.
    /// </returns>
    public Task<FileId?> CopyDocumentTemplateAsync(
        FileId fileId,
        Func<byte[], byte[]> replaceAction,
        string parentDirectory = "DocumentTemplates/",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously creates a copy of a document template, merges content from a secondary file,
    /// processes with the provided replacement logic, and saves the modified document.
    /// </summary>
    /// <param name="fileId">The identifier of the main file (File A).</param>
    /// <param name="secondaryFileId">The identifier of the secondary file (File B) providing section content.</param>
    /// <param name="replaceAction">A function that takes both file contents (A, B) and returns the merged result.</param>
    /// <param name="parentDirectory">The directory where the modified document will be stored.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The file identifier of the new document if successful; otherwise, null.</returns>
    public Task<FileId?> CopyDocumentTemplateAsync(
        FileId fileId,
        FileId secondaryFileId,
        Func<byte[], byte[], byte[]> replaceAction,
        string parentDirectory = "DocumentTemplates/",
        CancellationToken cancellationToken = default);
}

[RegisterService<IDocumentService>(LifeTime.Scoped)]
public class DocumentService : IDocumentService
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;
    private const string FileExtension = ".odt";

    public static ContentType DetectContentType(byte[] content)
    {
        try
        {
            using var stream = new MemoryStream(content);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            if (archive.GetEntry("word/document.xml") is not null)
            {
                return new ContentType("docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            }
        }
        catch
        {
            // Not a valid ZIP, fallback to ODT
        }

        return new ContentType("odt", "application/vnd.oasis.opendocument.text");
    }

    public DocumentService(IFileServiceClient fileServiceClient, Dp2DbContext dbContext)
    {
        this.fileServiceClient = fileServiceClient;
        this.dbContext = dbContext;
    }

    public Task<FileId?> GetDocumentTemplateAsync(
        Expression<Func<SuDocumentTemplate, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return this.GetDocumentTemplateAsync(
            predicate,
            parentDirectory: $"DocumentTemplates/{Guid.NewGuid()}.{FileExtension}",
            cancellationToken: cancellationToken);
    }

    public async Task<FileId?> GetDocumentTemplateAsync(
        Expression<Func<SuDocumentTemplate, bool>> predicate,
        string parentDirectory = "DocumentTemplates/",
        CancellationToken cancellationToken = default)
    {
        var documentTemplate =
            await this.dbContext.SuDocumentTemplates
                      .Where(predicate)
                      .FirstOrDefaultAsync(cancellationToken);

        if (documentTemplate == null)
        {
            return null;
        }

        var downloadFile =
            await this.fileServiceClient
                      .DownloadAsync(
                          documentTemplate.FileId,
                          cancellationToken: cancellationToken);

        if (downloadFile == null)
        {
            throw new NullReferenceException("Download file not found.");
        }

        var uploadFile =
            await this.fileServiceClient
                      .UploadFileAsync(
                          downloadFile.Contents,
                          parentDirectory,
                          cancellationToken: cancellationToken);

        return uploadFile.Id;
    }

    public async Task<FileId?> GetDocumentTemplateAsync(
        IQueryable<SuDocumentTemplate> query,
        string parentDirectory = "DocumentTemplates/",
        CancellationToken cancellationToken = default)
    {
        var documentTemplate =
            await query.FirstOrDefaultAsync(cancellationToken);

        if (documentTemplate == null)
        {
            return null;
        }

        var downloadFile =
            await this.fileServiceClient
                      .DownloadAsync(
                          documentTemplate.FileId,
                          cancellationToken: cancellationToken);

        if (downloadFile == null)
        {
            throw new NullReferenceException("Download file not found.");
        }

        var uploadFile =
            await this.fileServiceClient
                      .UploadFileAsync(
                          downloadFile.Contents,
                          parentDirectory,
                          cancellationToken: cancellationToken);

        return uploadFile.Id;
    }

    public async Task<FileId?> CopyDocumentTemplateAsync(
        FileId fileId,
        string parentDirectory = "DocumentTemplates/",
        CancellationToken cancellationToken = default)
    {
        var downloadFile =
            await this.fileServiceClient
                      .DownloadAsync(
                          fileId,
                          cancellationToken: cancellationToken);

        if (downloadFile == null)
        {
            return null;
        }

        var uploadFile =
            await this.fileServiceClient
                      .UploadFileAsync(
                          downloadFile.Contents,
                          parentDirectory,
                          cancellationToken: cancellationToken);

        return uploadFile.Id;
    }

    public async Task<FileId?> CopyDocumentTemplateAsync(FileId fileId, Func<byte[], byte[]> replaceAction, string parentDirectory = "DocumentTemplates/", CancellationToken cancellationToken = default)
    {
        var downloadFile =
            await this.fileServiceClient
                      .DownloadAsync(fileId, cancellationToken: cancellationToken);

        if (downloadFile == null)
        {
            return null;
        }

        var replacedContents = replaceAction(downloadFile.Contents);

        var contentType = DetectContentType(replacedContents);

        var uploadFile =
            await this.fileServiceClient
                      .UploadFileAsync(
                          replacedContents,
                          parentDirectory,
                          contentType: contentType,
                          cancellationToken: cancellationToken);

        return uploadFile.Id;
    }

    public async Task<FileId?> CopyDocumentTemplateAsync(
        FileId fileId,
        FileId secondaryFileId,
        Func<byte[], byte[], byte[]> replaceAction,
        string parentDirectory = "DocumentTemplates/",
        CancellationToken cancellationToken = default)
    {
        var downloadFileA =
            await this.fileServiceClient
                      .DownloadAsync(fileId, cancellationToken: cancellationToken);

        if (downloadFileA == null)
        {
            return null;
        }

        var downloadFileB =
            await this.fileServiceClient
                      .DownloadAsync(secondaryFileId, cancellationToken: cancellationToken);

        if (downloadFileB == null)
        {
            return null;
        }

        var replacedContents = replaceAction(downloadFileA.Contents, downloadFileB.Contents);

        var contentType = DetectContentType(replacedContents);

        var uploadFile =
            await this.fileServiceClient
                      .UploadFileAsync(
                          replacedContents,
                          parentDirectory,
                          contentType: contentType,
                          cancellationToken: cancellationToken);

        return uploadFile.Id;
    }
}