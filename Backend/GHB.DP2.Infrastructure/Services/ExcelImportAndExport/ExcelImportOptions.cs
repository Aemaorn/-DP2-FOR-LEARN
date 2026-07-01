namespace GHB.DP2.Infrastructure.Services.ExcelImportAndExport;

using System.Globalization;

/// <summary>
/// Configuration options for Excel import operations.
/// </summary>
public class ExcelImportOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to skip the header row when reading Excel data.
    /// Default is true.
    /// </summary>
    public bool SkipHeader { get; set; } = true;

    /// <summary>
    /// Gets or sets the index of the worksheet to read (0-based).
    /// The default is 0 (first worksheet).
    /// </summary>
    public int WorksheetIndex { get; set; } = 0;

    /// <summary>
    /// Gets or sets the batch size for processing rows to optimize memory usage.
    /// The default is 1000 rows per batch.
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets custom column mappings where the key is the property name and the value is the column name.
    /// This allows mapping Excel columns with different names to object properties.
    /// </summary>
    public Dictionary<string, string> ColumnMappings { get; set; } = new();

    /// <summary>
    /// Gets or sets the maximum file size in bytes that will be processed.
    /// This helps prevent DoS attacks with very large files.
    /// Default is 50MB (52,428,800 bytes).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 52_428_800; // 50MB

    /// <summary>
    /// Gets or sets a value indicating whether to continue processing when encountering non-critical mapping errors.
    /// When true, rows with mapping errors will be skipped and logged.
    /// When false, the first mapping error will throw an exception.
    /// Default is false.
    /// </summary>
    public bool ContinueOnMappingError { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to trim whitespace from string values during mapping.
    /// Default is true.
    /// </summary>
    public bool TrimStrings { get; set; } = true;

    /// <summary>
    /// Gets or sets the culture info to use for parsing dates and numbers.
    /// If null, the current culture will be used.
    /// </summary>
    public CultureInfo? CultureInfo { get; set; }
}