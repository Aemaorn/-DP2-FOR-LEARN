namespace GHB.DP2.Application.Services.Pdf;

using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

/// <summary>
/// Implementation of PDF service using PdfSharpCore library.
/// </summary>
public class PdfService : IPdfService
{
    private static readonly object FontResolverLock = new();
    private static bool fontResolverInitialized;

    /// <summary>
    /// Maximum allowed size for a single PDF file (50 MB).
    /// </summary>
    public const int MaxSinglePdfSizeBytes = 50 * 1024 * 1024;

    /// <summary>
    /// Maximum allowed total size for all PDFs combined (200 MB).
    /// </summary>
    public const int MaxTotalPdfSizeBytes = 200 * 1024 * 1024;

    /// <summary>
    /// Maximum number of PDF files that can be merged.
    /// </summary>
    public const int MaxPdfFileCount = 100;

    /// <summary>
    /// Static constructor to register the custom font resolver.
    /// </summary>
    static PdfService()
    {
        EnsureFontResolverInitialized();
    }

    private static void EnsureFontResolverInitialized()
    {
        if (fontResolverInitialized)
        {
            return;
        }

        lock (FontResolverLock)
        {
            if (fontResolverInitialized)
            {
                return;
            }

            // Always set our custom resolver - don't check for null
            // PdfSharpCore may have already initialized a default resolver
            GlobalFontSettings.FontResolver = new CordiaThaiFontResolver();
            fontResolverInitialized = true;
        }
    }

    /// <inheritdoc/>
    public int? GetPageCount(byte[] fileContent, string contentType)
    {
        if (contentType != "application/pdf")
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(fileContent);
            using var document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);

            return document.PageCount;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public byte[] MergePdfs(IEnumerable<byte[]> pdfContents)
    {
        var contentsList = pdfContents.ToList();

        ValidatePdfContents(contentsList);

        using var outputDocument = new PdfDocument();

        for (int idx = 0; idx < contentsList.Count; idx++)
        {
            var content = contentsList[idx];

            try
            {
                using var stream = new MemoryStream(content);
                using var inputDocument = PdfReader.Open(stream, PdfDocumentOpenMode.Import);

                for (int i = 0; i < inputDocument.PageCount; i++)
                {
                    outputDocument.AddPage(inputDocument.Pages[i]);
                }
            }
            catch (Exception ex) when (ex is not ArgumentException and not InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"Failed to process PDF at index {idx}. The file may be corrupted or password-protected.",
                    ex);
            }
        }

        using var outputStream = new MemoryStream();
        outputDocument.Save(outputStream);

        return outputStream.ToArray();
    }

    private static void ValidatePdfContents(IList<byte[]> contentsList)
    {
        if (contentsList.Count == 0)
        {
            throw new ArgumentException("No PDF contents provided for merging.", nameof(contentsList));
        }

        if (contentsList.Count > MaxPdfFileCount)
        {
            throw new ArgumentException(
                $"Too many PDF files. Maximum allowed is {MaxPdfFileCount}, but {contentsList.Count} were provided.",
                nameof(contentsList));
        }

        long totalSize = 0;

        for (int i = 0; i < contentsList.Count; i++)
        {
            var content = contentsList[i];

            if (content == null || content.Length == 0)
            {
                throw new ArgumentException($"PDF content at index {i} is null or empty.", nameof(contentsList));
            }

            if (content.Length > MaxSinglePdfSizeBytes)
            {
                throw new ArgumentException(
                    $"PDF at index {i} exceeds maximum size. Size: {content.Length / (1024 * 1024):F1} MB, Maximum: {MaxSinglePdfSizeBytes / (1024 * 1024)} MB.",
                    nameof(contentsList));
            }

            totalSize += content.Length;
        }

        if (totalSize > MaxTotalPdfSizeBytes)
        {
            throw new ArgumentException(
                $"Total PDF size exceeds maximum. Total: {totalSize / (1024 * 1024):F1} MB, Maximum: {MaxTotalPdfSizeBytes / (1024 * 1024)} MB.",
                nameof(contentsList));
        }
    }

    /// <inheritdoc/>
    public byte[] MergePdfsWithCoverPages(IEnumerable<AttachmentGroup> attachmentGroups)
    {
        var groupsList = attachmentGroups.ToList();

        if (groupsList.Count == 0)
        {
            throw new ArgumentException("No attachment groups provided for merging.", nameof(attachmentGroups));
        }

        // Validate all PDF contents across all groups
        var allPdfContents = groupsList.SelectMany(g => g.PdfContents).ToList();
        ValidatePdfContents(allPdfContents);

        using var outputDocument = new PdfDocument();
        int groupIndex = 0;

        foreach (var group in groupsList)
        {
            try
            {
                // 1. Create cover page
                var coverPage = outputDocument.AddPage();
                coverPage.Size = PdfSharpCore.PageSize.A4;

                using (var gfx = XGraphics.FromPdfPage(coverPage))
                {
                    var font = new XFont("Cordia New", CoverPageFontSize, XFontStyle.Regular);
                    var rect = new XRect(0, 0, coverPage.Width.Point, coverPage.Height.Point);
                    var documentName = group.DocumentName ?? "Untitled";
                    gfx.DrawString(documentName, font, XBrushes.Black, rect, XStringFormats.Center);
                }

                // 2. Add PDF files in this group
                int pdfIndex = 0;

                foreach (var pdfContent in group.PdfContents)
                {
                    try
                    {
                        using var stream = new MemoryStream(pdfContent);
                        using var inputDocument = PdfReader.Open(stream, PdfDocumentOpenMode.Import);

                        for (int i = 0; i < inputDocument.PageCount; i++)
                        {
                            outputDocument.AddPage(inputDocument.Pages[i]);
                        }
                    }
                    catch (Exception ex) when (ex is not ArgumentException and not InvalidOperationException)
                    {
                        throw new InvalidOperationException(
                            $"Failed to process PDF at group '{group.DocumentName}' (index {groupIndex}), file index {pdfIndex}. The file may be corrupted or password-protected.",
                            ex);
                    }

                    pdfIndex++;
                }
            }
            catch (Exception ex) when (ex is not ArgumentException and not InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"Failed to create cover page for group '{group.DocumentName}' (index {groupIndex}).",
                    ex);
            }

            groupIndex++;
        }

        using var outputStream = new MemoryStream();
        outputDocument.Save(outputStream);

        return outputStream.ToArray();
    }

    public byte[] MergePdfsWithCoverPages(IEnumerable<AttachmentGroupWithDescription> attachmentGroups)
    {
        var groupsList = attachmentGroups.ToList();

        if (groupsList.Count == 0)
        {
            throw new ArgumentException("No attachment groups provided for merging.", nameof(attachmentGroups));
        }

        // Validate all PDF contents across all groups
        var allPdfContents = groupsList.SelectMany(g => g.PdfContents).ToList();

        if (allPdfContents.Count > 0)
        {
            ValidatePdfContents(allPdfContents);
        }

        using var outputDocument = new PdfDocument();
        int groupIndex = 0;

        foreach (var group in groupsList)
        {
            try
            {
                // 1. Create cover page
                var coverPage = outputDocument.AddPage();
                coverPage.Size = PdfSharpCore.PageSize.A4;

                using (var gfx = XGraphics.FromPdfPage(coverPage))
                {
                    var hasDescription = !string.IsNullOrWhiteSpace(group.Description);
                    var lineSpacing = 30.0;
                    var totalLines = hasDescription ? 3 : 2;
                    var totalHeight = CoverPageFontSize + ((totalLines - 1) * lineSpacing);
                    var startY = (coverPage.Height.Point - totalHeight) / 2;

                    var sequenceLabel = $"ผนวก {group.Sequence}";
                    var sequenceFont = new XFont("TH SarabunNew", CoverPageFontSize, XFontStyle.Regular);
                    var sequenceRect = new XRect(0, startY, coverPage.Width.Point, CoverPageFontSize);
                    gfx.DrawString(sequenceLabel, sequenceFont, XBrushes.Black, sequenceRect, XStringFormats.TopCenter);

                    var documentName = group.DocumentName ?? "Untitled";
                    var nameFont = new XFont("TH SarabunNew", DescriptionFontSize, XFontStyle.Regular);
                    var nameRect = new XRect(0, startY + lineSpacing, coverPage.Width.Point, DescriptionFontSize);
                    gfx.DrawString(documentName, nameFont, XBrushes.Black, nameRect, XStringFormats.TopCenter);

                    if (hasDescription)
                    {
                        var descFont = new XFont("TH SarabunNew", DescriptionFontSize, XFontStyle.Regular);
                        var descRect = new XRect(0, startY + (lineSpacing * 2), coverPage.Width.Point, DescriptionFontSize);
                        gfx.DrawString(group.Description!, descFont, XBrushes.Black, descRect, XStringFormats.TopCenter);
                    }
                }

                // 2. Add PDF files in this group
                int pdfIndex = 0;

                foreach (var pdfContent in group.PdfContents)
                {
                    try
                    {
                        using var stream = new MemoryStream(pdfContent);
                        using var inputDocument = PdfReader.Open(stream, PdfDocumentOpenMode.Import);

                        for (int i = 0; i < inputDocument.PageCount; i++)
                        {
                            outputDocument.AddPage(inputDocument.Pages[i]);
                        }
                    }
                    catch (Exception ex) when (ex is not ArgumentException and not InvalidOperationException)
                    {
                        throw new InvalidOperationException(
                            $"Failed to process PDF at group '{group.DocumentName}' (index {groupIndex}), file index {pdfIndex}. The file may be corrupted or password-protected.",
                            ex);
                    }

                    pdfIndex++;
                }
            }
            catch (Exception ex) when (ex is not ArgumentException and not InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"Failed to create cover page for group '{group.DocumentName}' (index {groupIndex}).",
                    ex);
            }

            groupIndex++;
        }

        using var outputStream = new MemoryStream();
        outputDocument.Save(outputStream);

        return outputStream.ToArray();
    }

    private const int CoverPageFontSize = 22;
    private const int DescriptionFontSize = 20;
}