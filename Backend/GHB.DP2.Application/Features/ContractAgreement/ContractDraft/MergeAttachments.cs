namespace GHB.DP2.Application.Features.ContractAgreement.ContractDraft;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Services.Pdf;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Request for a single attachment group containing document name and file IDs.
/// </summary>
public record AttachmentGroupRequest(string DocumentName, Guid[] FileIds);

/// <summary>
/// Request for a single attachment group containing document name and description and file IDs.
/// </summary>
public record AttachmentGroupRequestWithDescription(string DocumentName, string? Description, int Sequence, Guid[] FileIds);

/// <summary>
/// Request for merging multiple PDF attachments into a single PDF file with cover pages.
/// </summary>
public record MergeAttachmentsRequest(AttachmentGroupRequest[] Attachments);

public record MergeAttachmentsRequestWithDescription(AttachmentGroupRequestWithDescription[] Attachments);

/// <summary>
/// Endpoint for merging multiple PDF files into a single downloadable PDF.
/// </summary>
public class MergeAttachments : EndpointBase<MergeAttachmentsRequestWithDescription, IResult>
{
    private readonly IFileServiceClient fileServiceClient;
    private readonly IPdfService pdfService;

    public MergeAttachments(
        IFileServiceClient fileServiceClient,
        IPdfService pdfService,
        ILogger<MergeAttachments> logger)
        : base(logger)
    {
        this.fileServiceClient = fileServiceClient;
        this.pdfService = pdfService;
    }

    public override void Configure()
    {
        this.Options(builder =>
            builder.WithTags(nameof(ContractDraft))
                   .ProducesProblem(StatusCodes.Status400BadRequest)
                   .ProducesProblem(StatusCodes.Status404NotFound)
                   .ProducesProblem(StatusCodes.Status500InternalServerError));
        this.Post("contract-draft/merge-attachments");
    }

    protected override async ValueTask<IResult> HandleRequestAsync(
        MergeAttachmentsRequestWithDescription req,
        CancellationToken ct)
    {
        if (req.Attachments == null || req.Attachments.Length == 0)
        {
            return TypedResults.BadRequest("No attachments provided.");
        }

        var attachmentGroups = new List<AttachmentGroupWithDescription>();

        foreach (var attachment in req.Attachments)
        {
            var pdfContents = new List<byte[]>();

            foreach (var fileId in attachment.FileIds)
            {
                var file = await this.fileServiceClient.DownloadAsync(
                    FileId.From(fileId),
                    cancellationToken: ct);

                if (file == null)
                {
                    return TypedResults.NotFound($"File with ID {fileId} not found.");
                }

                // Only accept PDF files
                if (!file.MimeType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return TypedResults.BadRequest($"File with ID {fileId} is not a PDF file.");
                }

                // Validate PDF content by checking PDF header signature (%PDF-)
                if (file.Contents.Length < 5 || file.Contents[0] != 0x25 || file.Contents[1] != 0x50 ||
                    file.Contents[2] != 0x44 || file.Contents[3] != 0x46 || file.Contents[4] != 0x2D)
                {
                    return TypedResults.BadRequest($"File with ID {fileId} is not a valid PDF document.");
                }

                pdfContents.Add(file.Contents);
            }

            attachmentGroups.Add(new AttachmentGroupWithDescription(attachment.DocumentName, attachment.Description, attachment.Sequence, pdfContents));
        }

        var mergedPdf = this.pdfService.MergePdfsWithCoverPages(attachmentGroups);
        var fileName = $"merged-attachments-{DateTimeOffset.Now:yyyyMMddHHmmss}.pdf";

        return Results.File(mergedPdf, "application/pdf", fileName);
    }
}