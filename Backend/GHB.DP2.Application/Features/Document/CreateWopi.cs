namespace GHB.DP2.Application.Features.Document;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

public record PutFileTemplate
{
    public DateTimeOffset LastModifiedTime { get; set; }
}

// TODO: This controller can't use fast endpoints because ch editor can't send payload to it.
[ApiController]
[Route("wopi")]
[Tags("wopi")]
public class CreateWopiController : ControllerBase
{
    private readonly IFileServiceClient fileServiceClient;
    private readonly ILogger<CreateWopiController> logger;

    public CreateWopiController(
        IFileServiceClient fileServiceClient,
        ILogger<CreateWopiController> logger)
    {
        this.fileServiceClient = fileServiceClient;
        this.logger = logger;
    }

    [HttpPost("~/api/wopi/files/{fileId:guid}/contents")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateFile(Guid fileId, CancellationToken cancellationToken)
    {
        try
        {
            // Copy the request body to a memory stream
            await using var memStream = new MemoryStream();
            await this.HttpContext.Request.Body.CopyToAsync(memStream, cancellationToken);

            var contents = memStream.ToArray();

            // Update document metadata
            await this.fileServiceClient.ReplaceFileAsync(
                FileId.From(fileId),
                contents,
                cancellationToken: cancellationToken);

            return this.Ok(new PutFileTemplate { LastModifiedTime = DateTimeOffset.UtcNow });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error processing file upload for {FileId}", fileId);

            return this.Problem("An error occurred while processing the request.");
        }
    }
}