namespace GHB.DP2.Application.Extensions.Document.ReplaceDocument;

using System.IO.Compression;
using System.Xml.Linq;
using Codehard.OdtKit;
using Codehard.OdtKit.Abstractions;
using Codehard.OdtKit.Representations.Styles;
using Codehard.OdtKit.Representations.Tables;

/// <summary>
/// Provides high-level operations for processing ODT documents
/// </summary>
public static class OdtDocumentProcessor
{
    /// <summary>
    /// Replaces ODT signature section with provided data
    /// </summary>
    /// <param name="content">The ODT document content as byte array</param>
    /// <param name="sectionName">Name of the section to replace</param>
    /// <param name="dto">Data objects to insert into the table</param>
    /// <returns>Modified ODT document as byte array</returns>
    /// <exception cref="ArgumentNullException">Thrown when dto, section, or table is null</exception>
    public static byte[] ReplaceOdtSignature(byte[] content, string sectionName, IEnumerable<object> dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var doc = new OdtDocument(content);
        var wrapped = doc.Wrap();

        var section = doc.FindNamedSection(sectionName);
        ArgumentNullException.ThrowIfNull(section);

        var wrappedSection = section.Wrap();
        var tableElement = wrappedSection.OfType<TableElement>().FirstOrDefault();
        ArgumentNullException.ThrowIfNull(tableElement);

        var placeHolderTabs = new PlaceholderTable(tableElement);

        foreach (var item in dto)
        {
            placeHolderTabs.InsertValues(item);
        }

        placeHolderTabs.OfType<TableRowElement>().Last().Remove();

        return doc.ToByteArray();
    }

    /// <summary>
    /// Replaces placeholders in ODT document with values from data object
    /// </summary>
    /// <param name="content">The ODT document content as byte array</param>
    /// <param name="dto">Data object containing replacement values</param>
    /// <returns>Modified ODT document as byte array</returns>
    public static byte[] ReplaceOdtDocument(byte[] content, object dto)
    {
        using var doc = new OdtDocument(content);
        var wrapped = doc.Wrap();

        var propertyPaths = dto.MapDtoPaths();

        DocumentTemplateEngine.ProcessSingleValuePlaceholders(wrapped, dto, propertyPaths.SingleValue);
        DocumentTemplateEngine.ProcessMultipleValuePlaceholders(wrapped, dto, propertyPaths.MultipleValue);

        return doc.ToByteArray();
    }

    /// <summary>
    /// Replaces placeholders in ODT document with values from data object, and changes the font family
    /// </summary>
    public static byte[] ReplaceOdtDocument(byte[] content, object dto, string fontName)
    {
        using var doc = new OdtDocument(content);
        var wrapped = doc.Wrap();

        var propertyPaths = dto.MapDtoPaths();

        DocumentTemplateEngine.ProcessSingleValuePlaceholders(wrapped, dto, propertyPaths.SingleValue);
        DocumentTemplateEngine.ProcessMultipleValuePlaceholders(wrapped, dto, propertyPaths.MultipleValue);

        if (!string.IsNullOrWhiteSpace(fontName))
        {
            FontStyleProcessor.ChangeFontInDocument(wrapped, fontName);
        }

        return doc.ToByteArray();
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
        using var docA = new OdtDocument(contentA);
        using var docB = new OdtDocument(contentB);
        docA.Wrap();
        docB.Wrap();

        foreach (var sectionName in sectionNames)
        {
            var sectionA = PlaceholderSection.FromDocument(docA, sectionName);
            var sectionB = PlaceholderSection.FromDocument(docB, sectionName);

            if (sectionA is not null && sectionB is not null)
            {
                sectionA.ReplaceChildContentFrom(sectionB);
            }
        }

        var wrapped = docA.Wrap();
        var propertyPaths = dto.MapDtoPaths();

        DocumentTemplateEngine.ProcessSingleValuePlaceholders(wrapped, dto, propertyPaths.SingleValue);
        DocumentTemplateEngine.ProcessMultipleValuePlaceholders(wrapped, dto, propertyPaths.MultipleValue);

        return docA.ToByteArray();
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
        using var docA = new OdtDocument(contentA);
        using var docB = new OdtDocument(contentB);
        docA.Wrap();
        docB.Wrap();

        foreach (var sectionName in sectionNames)
        {
            var sectionA = PlaceholderSection.FromDocument(docA, sectionName);
            var sectionB = PlaceholderSection.FromDocument(docB, sectionName);

            if (sectionA is not null && sectionB is not null)
            {
                sectionA.ReplaceChildContentFrom(sectionB);
            }
        }

        var wrapped = docA.Wrap();
        var propertyPaths = dto.MapDtoPaths();

        DocumentTemplateEngine.ProcessSingleValuePlaceholders(wrapped, dto, propertyPaths.SingleValue);
        DocumentTemplateEngine.ProcessMultipleValuePlaceholders(wrapped, dto, propertyPaths.MultipleValue);

        if (!string.IsNullOrWhiteSpace(fontName))
        {
            FontStyleProcessor.ChangeFontInDocument(wrapped, fontName);
        }

        return docA.ToByteArray();
    }

    /// <summary>
    /// Replaces section child content in File A with child content from File B's matching sections.
    /// Sections in File B are matched by exact name (with dots, e.g. "ContractDraftInfoDetail.Agreement"),
    /// but the target section in File A is derived from the part before the first dot (e.g. "ContractDraftInfoDetail").
    /// Sections without dots are matched by exact name in both documents.
    /// Then replaces all placeholder tags in File A.
    /// </summary>
    public static byte[] ReplaceOdtSectionFromElementAndDocument(
        byte[] contentA,
        byte[] contentB,
        string[] sectionNames,
        object dto)
    {
        (contentA, contentB) = MergeStylesFromSource(contentA, contentB);

        using var docA = new OdtDocument(contentA);
        using var docB = new OdtDocument(contentB);
        docA.Wrap();
        docB.Wrap();

        var replacedSections = new HashSet<string>();

        foreach (var sectionName in sectionNames)
        {
            var sectionB = PlaceholderSection.FromDocument(docB, sectionName);

            var sectionNameInA = sectionName.Contains('.')
                ? sectionName[..sectionName.IndexOf('.')]
                : sectionName;

            var sectionA = PlaceholderSection.FromDocument(docA, sectionNameInA);

            if (sectionA is not null && sectionB is not null)
            {
                if (replacedSections.Add(sectionNameInA))
                {
                    sectionA.ReplaceChildContentFrom(sectionB);
                }
                else
                {
                    sectionA.AppendChildContentFrom(sectionB);
                }
            }
        }

        var wrapped = docA.Wrap();
        var propertyPaths = dto.MapDtoPaths();

        DocumentTemplateEngine.ProcessSingleValuePlaceholders(wrapped, dto, propertyPaths.SingleValue);
        DocumentTemplateEngine.ProcessMultipleValuePlaceholders(wrapped, dto, propertyPaths.MultipleValue);

        return docA.ToByteArray();
    }

    /// <summary>
    /// Same as ReplaceOdtSectionFromElementAndDocument but also changes the font family
    /// across the entire document after replacing sections.
    /// </summary>
    public static byte[] ReplaceOdtSectionFromElementAndDocument(
        byte[] contentA,
        byte[] contentB,
        string[] sectionNames,
        object dto,
        string fontName)
    {
        (contentA, contentB) = MergeStylesFromSource(contentA, contentB);

        using var docA = new OdtDocument(contentA);
        using var docB = new OdtDocument(contentB);
        docA.Wrap();
        docB.Wrap();

        var replacedSections = new HashSet<string>();

        foreach (var sectionName in sectionNames)
        {
            var sectionB = PlaceholderSection.FromDocument(docB, sectionName);

            var sectionNameInA = sectionName.Contains('.')
                ? sectionName[..sectionName.IndexOf('.')]
                : sectionName;

            var sectionA = PlaceholderSection.FromDocument(docA, sectionNameInA);

            if (sectionA is not null && sectionB is not null)
            {
                if (replacedSections.Add(sectionNameInA))
                {
                    sectionA.ReplaceChildContentFrom(sectionB);
                }
                else
                {
                    sectionA.AppendChildContentFrom(sectionB);
                }
            }
        }

        var wrapped = docA.Wrap();
        var propertyPaths = dto.MapDtoPaths();

        DocumentTemplateEngine.ProcessSingleValuePlaceholders(wrapped, dto, propertyPaths.SingleValue);
        DocumentTemplateEngine.ProcessMultipleValuePlaceholders(wrapped, dto, propertyPaths.MultipleValue);

        if (!string.IsNullOrWhiteSpace(fontName))
        {
            FontStyleProcessor.ChangeFontInDocument(wrapped, fontName);
        }

        return docA.ToByteArray();
    }

    /// <summary>
    /// Changes the font family throughout the entire document
    /// </summary>
    /// <param name="content">The ODT document content as byte array</param>
    /// <param name="newFontName">The new font family name to apply</param>
    /// <returns>Modified ODT document as byte array</returns>
    public static byte[] ChangeFontInDocument(byte[] content, string newFontName)
    {
        using var doc = new OdtDocument(content);
        FontStyleProcessor.ChangeFontInDocument(doc, newFontName);

        return doc.ToByteArray();
    }

    /// <summary>
    /// Merges automatic styles and font-face declarations from source ODT into target ODT.
    /// When style names conflict (e.g. both have P1/T2 with different fonts), source styles
    /// are renamed with a "_src_" prefix and all references in the source body are updated.
    /// Returns (modifiedTarget, modifiedSource).
    /// </summary>
    private static (byte[] Target, byte[] Source) MergeStylesFromSource(byte[] targetBytes, byte[] sourceBytes)
    {
        XNamespace officeNs = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";
        XNamespace styleNs = "urn:oasis:names:tc:opendocument:xmlns:style:1.0";
        XNamespace textNs = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

        XDocument sourceContentXml;
        XDocument targetContentXml;

        using (var stream = new MemoryStream(sourceBytes))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
        {
            var entry = archive.GetEntry("content.xml");
            if (entry is null)
            {
                return (targetBytes, sourceBytes);
            }

            using var reader = new StreamReader(entry.Open());
            sourceContentXml = XDocument.Load(reader);
        }

        using (var stream = new MemoryStream(targetBytes))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
        {
            var entry = archive.GetEntry("content.xml");
            if (entry is null)
            {
                return (targetBytes, sourceBytes);
            }

            using var reader = new StreamReader(entry.Open());
            targetContentXml = XDocument.Load(reader);
        }

        var targetAutoStyles = targetContentXml.Descendants(officeNs + "automatic-styles").FirstOrDefault();
        var sourceAutoStyles = sourceContentXml.Descendants(officeNs + "automatic-styles").FirstOrDefault();

        // Collect existing style names in target to detect conflicts
        var existingNames = targetAutoStyles?
            .Elements(styleNs + "style")
            .Select(e => e.Attribute(styleNs + "name")?.Value)
            .Where(n => n is not null)
            .ToHashSet() ?? [];

        // For each style in source: rename it if conflicts with target, then add to target
        var renames = new Dictionary<string, string>();

        if (sourceAutoStyles is not null && targetAutoStyles is not null)
        {
            foreach (var styleEl in sourceAutoStyles.Elements(styleNs + "style").ToList())
            {
                var name = styleEl.Attribute(styleNs + "name")?.Value;
                if (name is null)
                {
                    continue;
                }

                var targetName = existingNames.Contains(name) ? $"_src_{name}" : name;
                renames[name] = targetName;

                var toAdd = new XElement(styleEl);
                toAdd.SetAttributeValue(styleNs + "name", targetName);
                targetAutoStyles.Add(toAdd);
                existingNames.Add(targetName);
            }
        }

        // Merge font-face-decls — only add fonts missing from target
        var targetFontFaces = targetContentXml.Descendants(officeNs + "font-face-decls").FirstOrDefault();
        var sourceFontFaces = sourceContentXml.Descendants(officeNs + "font-face-decls").FirstOrDefault();

        if (targetFontFaces is not null && sourceFontFaces is not null)
        {
            var existingFonts = targetFontFaces
                .Elements(styleNs + "font-face")
                .Select(e => e.Attribute(styleNs + "name")?.Value)
                .Where(n => n is not null)
                .ToHashSet()!;

            foreach (var fontFace in sourceFontFaces.Elements(styleNs + "font-face"))
            {
                var fontName = fontFace.Attribute(styleNs + "name")?.Value;
                if (fontName is not null && !existingFonts.Contains(fontName))
                {
                    targetFontFaces.Add(new XElement(fontFace));
                }
            }
        }

        // Update text:style-name references in source body to use the renamed style names
        if (renames.Count > 0)
        {
            var styleNameAttr = textNs + "style-name";
            var sourceBody = sourceContentXml.Descendants(officeNs + "body").FirstOrDefault();

            if (sourceBody is not null)
            {
                foreach (var el in sourceBody.DescendantsAndSelf())
                {
                    var attr = el.Attribute(styleNameAttr);
                    if (attr is not null && renames.TryGetValue(attr.Value, out var newName))
                    {
                        attr.SetValue(newName);
                    }
                }
            }
        }

        var newTargetBytes = RebuildZipWithContentXml(targetBytes, targetContentXml);
        var newSourceBytes = renames.Count > 0
            ? RebuildZipWithContentXml(sourceBytes, sourceContentXml)
            : sourceBytes;

        return (newTargetBytes, newSourceBytes);
    }

    private static byte[] RebuildZipWithContentXml(byte[] originalBytes, XDocument newContentXml)
    {
        using var resultStream = new MemoryStream();

        using (var readArchive = new ZipArchive(new MemoryStream(originalBytes), ZipArchiveMode.Read))
        using (var writeArchive = new ZipArchive(resultStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in readArchive.Entries)
            {
                var newEntry = writeArchive.CreateEntry(entry.FullName, CompressionLevel.Optimal);

                if (entry.FullName == "content.xml")
                {
                    using var writer = new StreamWriter(newEntry.Open());
                    newContentXml.Save(writer);
                }
                else
                {
                    using var src = entry.Open();
                    using var dst = newEntry.Open();
                    src.CopyTo(dst);
                }
            }
        }

        return resultStream.ToArray();
    }
}

/// <summary>
/// Handles font style changes in ODT documents
/// </summary>
public static class FontStyleProcessor
{
    private static readonly string[] FontAttributeNames =
    {
        "font-family",
        "font-name",
        "font-name-asian",
        "font-name-complex",
    };

    private const string DefaultGenericFontFamily = "swiss";

    /// <summary>
    /// Changes font family throughout the document
    /// </summary>
    /// <param name="doc">The ODT document to modify</param>
    /// <param name="newFontName">The new font family name</param>
    public static void ChangeFontInDocument(OdtDocument doc, string newFontName)
    {
        var wrapped = doc.Wrap();

        ChangeFontInDocument(wrapped, newFontName);
    }

    /// <summary>
    /// Changes font family using an already-wrapped document (avoids double Wrap() call)
    /// </summary>
    internal static void ChangeFontInDocument(XElementWrapper wrapped, string newFontName)
    {
        UpdateStyleElements(wrapped, newFontName);
        UpdateFontFaceDeclarations(wrapped, newFontName);

        // TODO: Replace with proper logging - Console.WriteLine removed for production
        // Font changed to: {newFontName}
    }

    private static void UpdateStyleElements(XElementWrapper wrapped, string newFontName)
    {
        var allStyleElements = wrapped.Descendants<StyleElement>().ToList();

        foreach (var styleElement in allStyleElements)
        {
            UpdateFontAttributesInElement(styleElement.Raw, newFontName);
        }
    }

    private static void UpdateFontFaceDeclarations(XElementWrapper wrapped, string newFontName)
    {
        var fontFaces = wrapped.Descendants<FontFaceElement>().ToList();

        foreach (var fontFace in fontFaces)
        {
            UpdateFontFaceElement(fontFace, newFontName);
        }
    }

    private static void UpdateFontFaceElement(dynamic fontFace, string newFontName)
    {
        var nameAttr = fontFace.Raw.Attribute(OdtDocument.StyleNamespace + "name");
        var familyAttr = fontFace.Raw.Attribute("font-family") ??
                         fontFace.Raw.Attribute(OdtDocument.SvgNamespace + "font-family");

        if (nameAttr != null)
        {
            nameAttr.Value = newFontName;
        }

        if (familyAttr != null)
        {
            familyAttr.Value = newFontName;
        }
    }

    private static void UpdateFontAttributesInElement(XElement element, string newFontName)
    {
        // Handle all possible font name attributes
        foreach (var attrName in FontAttributeNames)
        {
            var attr = element.Attribute(OdtDocument.StyleNamespace + attrName);
            if (attr != null)
            {
                attr.Value = newFontName;
            }
        }

        // Handle font-family-generic attribute
        var fontFamilyGenericAttr = element.Attribute(OdtDocument.StyleNamespace + "font-family-generic");
        if (fontFamilyGenericAttr != null)
        {
            fontFamilyGenericAttr.Value = DefaultGenericFontFamily;
        }
    }
}