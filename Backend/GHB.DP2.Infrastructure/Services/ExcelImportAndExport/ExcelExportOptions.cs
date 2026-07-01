namespace GHB.DP2.Infrastructure.Services.ExcelImportAndExport;

using System.Globalization;

/// <summary>
/// Configuration options for Excel export operations.
/// </summary>
public class ExcelExportOptions
{
    /// <summary>
    /// Gets or sets the name of the worksheet to create when exporting to Excel.
    /// Default is "Sheet1".
    /// </summary>
    public string WorksheetName { get; set; } = "Sheet1";

    /// <summary>
    /// Gets or sets a value indicating whether to include header row in the exported Excel file.
    /// Default is true.
    /// </summary>
    public bool IncludeHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets the batch size for processing rows to optimize memory usage.
    /// The default is 1000 rows per batch.
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum file size in bytes that will be generated.
    /// This helps prevent memory issues with very large exports.
    /// Default is 50MB (52,428,800 bytes).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 52_428_800; // 50MB

    /// <summary>
    /// Gets or sets the culture info to use for formatting dates and numbers.
    /// If null, the current culture will be used.
    /// </summary>
    public CultureInfo? CultureInfo { get; set; }

    /// <summary>
    /// Gets or sets custom column mappings where the key is the property name and the value is the column name.
    /// This allows mapping object properties to Excel columns with different names.
    /// </summary>
    public Dictionary<string, string> ColumnMappings { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to enable ClosedXML event tracking.
    /// Disabling this improves performance significantly for large exports.
    /// Default is false for performance optimization.
    /// </summary>
    public bool EnableEventTracking { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to auto-fit column widths based on content.
    /// Default is true.
    /// </summary>
    public bool AutoFitColumns { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to apply basic styling to the exported Excel file.
    /// This includes header formatting and cell borders.
    /// Default is true.
    /// </summary>
    public bool ApplyBasicStyling { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to trim whitespace from string values during export.
    /// Default is true.
    /// </summary>
    public bool TrimStrings { get; set; } = true;

    /// <summary>
    /// Gets or sets custom headers to use instead of auto-generated headers.
    /// When specified, these headers will be used instead of property names or column mappings.
    /// The order of headers should match the order of properties in the exported data.
    /// </summary>
    public List<string>? CustomHeaders { get; set; }
}