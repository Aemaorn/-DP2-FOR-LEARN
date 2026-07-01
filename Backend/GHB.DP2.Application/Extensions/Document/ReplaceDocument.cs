namespace GHB.DP2.Application.Extensions.Document;

using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using GHB.DP2.Application.Extensions.Document.ReplaceDocument;

/// <summary>
/// Main entry point for ODT document replacement operations.
/// This class serves as a facade over the refactored document processing components.
/// </summary>
public static class OdtDocumentExtensions
{
    /// <summary>
    /// Checks whether the given byte array is a valid ODT file by verifying it contains content.xml
    /// </summary>
    private static bool IsValidOdtFile(byte[] content)
    {
        try
        {
            using var stream = new MemoryStream(content);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            return archive.GetEntry("content.xml") is not null;
        }
        catch
        {
            return false;
        }
    }

    private static readonly XNamespace TextNamespace = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

    private static readonly string[] InlineTextParents = ["p", "span", "a", "h"];

    /// <summary>
    /// Converts in-paragraph whitespace-only text nodes (spaces/tabs) into ODF
    /// &lt;text:s/&gt; / &lt;text:tab/&gt; elements before the document is handed to OdtKit.
    /// OdtKit loads content.xml without preserving whitespace, so whitespace-only text nodes
    /// that sit between inline elements get dropped (e.g. the space between a run and a
    /// placeholder). Encoding them as elements keeps the spacing intact.
    /// Only nodes containing solely spaces/tabs (no newline) under text:p/span/a/h are touched,
    /// so formatting/indentation whitespace is left untouched.
    /// </summary>
    private static byte[] PreserveInlineSpaces(byte[] content)
    {
        XDocument contentXml;

        try
        {
            using var inStream = new MemoryStream(content);
            using var readArchive = new ZipArchive(inStream, ZipArchiveMode.Read);
            var entry = readArchive.GetEntry("content.xml");

            if (entry is null)
            {
                return content;
            }

            using var entryStream = entry.Open();
            contentXml = XDocument.Load(entryStream, LoadOptions.PreserveWhitespace);
        }
        catch
        {
            return content;
        }

        var whitespaceNodes = contentXml.DescendantNodes()
            .OfType<XText>()
            .Where(node => node.Parent is not null
                && node.Parent.Name.Namespace == TextNamespace
                && InlineTextParents.Contains(node.Parent.Name.LocalName)
                && node.Value.Length > 0
                && node.Value.All(c => c is ' ' or '\t'))
            .ToList();

        if (whitespaceNodes.Count == 0)
        {
            return content;
        }

        foreach (var node in whitespaceNodes)
        {
            var replacement = new List<object>();
            var spaceRun = 0;

            foreach (var c in node.Value)
            {
                if (c == ' ')
                {
                    spaceRun++;
                    continue;
                }

                if (spaceRun > 0)
                {
                    replacement.Add(CreateSpaceElement(spaceRun));
                    spaceRun = 0;
                }

                replacement.Add(new XElement(TextNamespace + "tab"));
            }

            if (spaceRun > 0)
            {
                replacement.Add(CreateSpaceElement(spaceRun));
            }

            node.ReplaceWith([.. replacement]);
        }

        using var outStream = new MemoryStream();

        using (var readArchive = new ZipArchive(new MemoryStream(content), ZipArchiveMode.Read))
        using (var writeArchive = new ZipArchive(outStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in readArchive.Entries)
            {
                var newEntry = writeArchive.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                using var dst = newEntry.Open();

                if (entry.FullName == "content.xml")
                {
                    using var writer = new StreamWriter(dst, new UTF8Encoding(false));
                    contentXml.Save(writer, SaveOptions.DisableFormatting);
                }
                else
                {
                    using var src = entry.Open();
                    src.CopyTo(dst);
                }
            }
        }

        return outStream.ToArray();

        static XElement CreateSpaceElement(int count)
        {
            var element = new XElement(TextNamespace + "s");

            if (count > 1)
            {
                element.SetAttributeValue(TextNamespace + "c", count);
            }

            return element;
        }
    }

    /// <summary>
    /// Replaces ODT signature section with provided data objects
    /// </summary>
    /// <param name="content">The ODT document content as byte array</param>
    /// <param name="sectionName">Name of the section containing the signature table</param>
    /// <param name="dto">Collection of data objects to insert into the table</param>
    /// <returns>Modified ODT document as byte array</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    public static byte[] ReplaceOdtSignature(byte[] content, string sectionName, IEnumerable<object> dto)
    {
        if (!IsValidOdtFile(content))
        {
            return content;
        }

        return OdtDocumentProcessor.ReplaceOdtSignature(PreserveInlineSpaces(content), sectionName, dto);
    }

    /// <summary>
    /// Replaces placeholders in ODT document with values from data object
    /// </summary>
    /// <param name="content">The ODT document content as byte array</param>
    /// <param name="dto">Data object containing replacement values</param>
    /// <returns>Modified ODT document as byte array</returns>
    public static byte[] ReplaceOdtDocument(byte[] content, object dto)
    {
        if (!IsValidOdtFile(content))
        {
            return content;
        }

        return OdtDocumentProcessor.ReplaceOdtDocument(PreserveInlineSpaces(content), dto);
    }

    /// <summary>
    /// Replaces placeholders in ODT document with values from data object, and changes the font family
    /// </summary>
    public static byte[] ReplaceOdtDocument(byte[] content, object dto, string fontName)
    {
        if (!IsValidOdtFile(content))
        {
            return content;
        }

        return OdtDocumentProcessor.ReplaceOdtDocument(PreserveInlineSpaces(content), dto, fontName);
    }

    /// <summary>
    /// Replaces section child content in File A with child content from File B's matching section,
    /// then replaces all placeholder tags in File A
    /// </summary>
    /// <param name="contentA">The main ODT document content as byte array</param>
    /// <param name="contentB">The source ODT document containing section child content</param>
    /// <param name="sectionNames">Names of the sections to find in both documents</param>
    /// <param name="dto">Data object for replacing placeholders</param>
    /// <returns>Modified ODT document as byte array</returns>
    public static byte[] ReplaceOdtSectionAndDocument(byte[] contentA, byte[] contentB, string[] sectionNames, object dto)
    {
        if (!IsValidOdtFile(contentA))
        {
            return contentA;
        }

        return OdtDocumentProcessor.ReplaceOdtSectionAndDocument(PreserveInlineSpaces(contentA), PreserveInlineSpaces(contentB), sectionNames, dto);
    }

    /// <summary>
    /// Replaces section child content in File A with child content from File B's matching section,
    /// then replaces all placeholder tags in File A, and changes the font family
    /// </summary>
    /// <param name="contentA">The main ODT document content as byte array</param>
    /// <param name="contentB">The source ODT document containing section child content</param>
    /// <param name="sectionNames">Names of the sections to find in both documents</param>
    /// <param name="dto">Data object for replacing placeholders</param>
    /// <param name="fontName">The font family name to apply throughout the document</param>
    /// <returns>Modified ODT document as byte array</returns>
    public static byte[] ReplaceOdtSectionAndDocument(byte[] contentA, byte[] contentB, string[] sectionNames, object dto, string fontName)
    {
        if (!IsValidOdtFile(contentA))
        {
            return contentA;
        }

        return OdtDocumentProcessor.ReplaceOdtSectionAndDocument(PreserveInlineSpaces(contentA), PreserveInlineSpaces(contentB), sectionNames, dto, fontName);
    }

    /// <summary>
    /// Replaces section child content in File A with child content from File B's matching sections.
    /// Sections are found in File B by exact name (with dots, e.g. "ContractDraftInfoDetail.Agreement"),
    /// but mapped to the section before the first dot in File A (e.g. "ContractDraftInfoDetail").
    /// Sections without dots are matched by exact name in both documents.
    /// Then replaces all placeholder tags in File A.
    /// </summary>
    public static byte[] ReplaceOdtSectionFromElementAndDocument(byte[] contentA, byte[] contentB, string[] sectionNames, object dto)
    {
        if (!IsValidOdtFile(contentA))
        {
            return contentA;
        }

        return OdtDocumentProcessor.ReplaceOdtSectionFromElementAndDocument(PreserveInlineSpaces(contentA), PreserveInlineSpaces(contentB), sectionNames, dto);
    }

    /// <summary>
    /// Same as ReplaceOdtSectionFromElementAndDocument but also changes the font family across the document.
    /// </summary>
    public static byte[] ReplaceOdtSectionFromElementAndDocument(byte[] contentA, byte[] contentB, string[] sectionNames, object dto, string fontName)
    {
        if (!IsValidOdtFile(contentA))
        {
            return contentA;
        }

        return OdtDocumentProcessor.ReplaceOdtSectionFromElementAndDocument(PreserveInlineSpaces(contentA), PreserveInlineSpaces(contentB), sectionNames, dto, fontName);
    }

    /// <summary>
    /// Changes the font family throughout the entire document
    /// </summary>
    /// <param name="content">The ODT document content as byte array</param>
    /// <param name="newFontName">The new font family name to apply</param>
    /// <returns>Modified ODT document as byte array</returns>
    public static byte[] ChangeFontInDocument(byte[] content, string newFontName)
    {
        if (!IsValidOdtFile(content))
        {
            return content;
        }

        return OdtDocumentProcessor.ChangeFontInDocument(content, newFontName);
    }
}

/// <summary>
/// Extension methods for mapping object properties to document template paths
/// </summary>
public static class DtoPathMapperExtensions
{
    /// <summary>
    /// Maps an object's properties to categorized path lists for document template processing
    /// </summary>
    /// <param name="dto">The object to map</param>
    /// <returns>Tuple containing single value paths and multiple value paths</returns>
    public static (List<string> SingleValue, List<string> MultipleValue) MapDtoPaths(this object dto)
    {
        return PropertyPathMapper.MapDtoPaths(dto);
    }

    /// <summary>
    /// Maps object properties to a dictionary with descriptions from DescriptionAttribute
    /// </summary>
    /// <param name="dto">The object to map</param>
    /// <returns>Dictionary mapping property paths to their descriptions</returns>
    public static Dictionary<string, string> MapDtoPathsWithDescriptions(this object dto)
    {
        return PropertyPathMapper.MapDtoPathsWithDescriptions(dto);
    }
}