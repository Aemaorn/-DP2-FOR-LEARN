namespace GHB.DP2.Application.Services.Pdf;

/// <summary>
/// Service for PDF operations including page counting and merging.
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// Gets the page count of a PDF file.
    /// </summary>
    /// <param name="fileContent">The PDF file content as byte array.</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <returns>The number of pages, or null if the file is not a valid PDF.</returns>
    int? GetPageCount(byte[] fileContent, string contentType);

    /// <summary>
    /// Merges multiple PDF files into a single PDF.
    /// </summary>
    /// <param name="pdfContents">Collection of PDF file contents as byte arrays.</param>
    /// <returns>The merged PDF as a byte array.</returns>
    byte[] MergePdfs(IEnumerable<byte[]> pdfContents);

    /// <summary>
    /// Merges multiple PDF files into a single PDF with cover pages for each document group.
    /// </summary>
    /// <param name="attachmentGroups">Collection of attachment groups, each containing a document name and PDF contents.</param>
    /// <returns>The merged PDF with cover pages as a byte array.</returns>
    byte[] MergePdfsWithCoverPages(IEnumerable<AttachmentGroup> attachmentGroups);

    byte[] MergePdfsWithCoverPages(IEnumerable<AttachmentGroupWithDescription> attachmentGroups);
}

/// <summary>
/// Represents a group of PDF attachments with a common document name.
/// </summary>
public record AttachmentGroup(string DocumentName, IEnumerable<byte[]> PdfContents);

public record AttachmentGroupWithDescription(string DocumentName, string? Description, int Sequence, IEnumerable<byte[]> PdfContents);