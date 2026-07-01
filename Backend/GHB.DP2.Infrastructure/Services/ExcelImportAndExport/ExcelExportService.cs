namespace GHB.DP2.Infrastructure.Services.ExcelImportAndExport;

using System.Globalization;
using System.Reflection;
using ClosedXML.Excel;
using FastEndpoints;
using GHB.DP2.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for exporting strongly typed C# objects to Excel files.
/// </summary>
[RegisterService<IExcelExportService>(LifeTime.Scoped)]
public class ExcelExportService : IExcelExportService
{
    private readonly ILogger<ExcelExportService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelExportService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for structured logging.</param>
    public ExcelExportService(ILogger<ExcelExportService> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, ExcelExportOptions? options = null, CancellationToken cancellationToken = default)
        where T : class
    {
        var stream = await this.ExportToExcelStreamAsync(data, options, cancellationToken);

        return stream.ToArray();
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string templatePath, ExcelExportOptions? options = null, CancellationToken cancellationToken = default)
        where T : class
    {
        var stream = await this.ExportToExcelStreamAsync(data, templatePath, options, cancellationToken);

        return stream.ToArray();
    }

    /// <inheritdoc />
    public async Task<MemoryStream> ExportToExcelStreamAsync<T>(IEnumerable<T> data, ExcelExportOptions? options = null, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(data);

        options ??= new ExcelExportOptions();

        try
        {
            var result = await Task.Run(() => this.ProcessExcelExport(data, options, cancellationToken), cancellationToken);

            return result;
        }
        catch (Exception ex) when (ex is not (ExcelExportException or ExcelDataValidationException or ExcelFileSizeExceededException or OperationCanceledException))
        {
            this.logger.LogError(ex, "An error occurred while exporting data to Excel");

            throw new ExcelExportException("An error occurred while exporting data to Excel. Please check the data and try again.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<MemoryStream> ExportToExcelStreamAsync<T>(IEnumerable<T> data, string templatePath, ExcelExportOptions? options = null, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(templatePath);

        options ??= new ExcelExportOptions();

        try
        {
            var result = await Task.Run(() => this.ProcessExcelExportWithTemplate(data, templatePath, options, cancellationToken), cancellationToken);

            return result;
        }
        catch (Exception ex) when (ex is not (ExcelExportException or ExcelTemplateMissingException or ExcelDataValidationException or ExcelFileSizeExceededException or OperationCanceledException))
        {
            this.logger.LogError(ex, "An error occurred while exporting data to Excel using template: {TemplatePath}", templatePath);

            throw new ExcelExportException($"An error occurred while exporting data to Excel using template: {templatePath}. Please check the template and data and try again.", ex);
        }
    }

    private MemoryStream ProcessExcelExport<T>(IEnumerable<T> data, ExcelExportOptions options, CancellationToken cancellationToken)
        where T : class
    {
        var workbook = new XLWorkbook();

        var worksheet = workbook.Worksheets.Add(options.WorksheetName);

        this.PopulateWorksheet(worksheet, data, options, cancellationToken);

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        // Check file size
        if (stream.Length > options.MaxFileSizeBytes)
        {
            stream.Dispose();

            throw new ExcelFileSizeExceededException(stream.Length, options.MaxFileSizeBytes);
        }

        return stream;
    }

    private MemoryStream ProcessExcelExportWithTemplate<T>(IEnumerable<T> data, string templatePath, ExcelExportOptions options, CancellationToken cancellationToken)
        where T : class
    {
        if (!File.Exists(templatePath))
        {
            throw new ExcelTemplateMissingException(templatePath);
        }

        XLWorkbook workbook;

        try
        {
            workbook = new XLWorkbook(templatePath);
        }
        catch (Exception ex)
        {
            throw new ExcelTemplateMissingException(templatePath, ex);
        }

        var worksheet = workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
        {
            throw new ExcelExportException($"Template file '{templatePath}' does not contain any worksheets.");
        }

        this.PopulateWorksheet(worksheet, data, options, cancellationToken);

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        // Check file size
        if (stream.Length > options.MaxFileSizeBytes)
        {
            stream.Dispose();

            throw new ExcelFileSizeExceededException(stream.Length, options.MaxFileSizeBytes);
        }

        return stream;
    }

    private void PopulateWorksheet<T>(IXLWorksheet worksheet, IEnumerable<T> data, ExcelExportOptions options, CancellationToken cancellationToken)
        where T : class
    {
        var dataList = data.ToList();
        var propertyMap = BuildPropertyMap<T>(options);

        var currentRow = 1;

        // Write headers if enabled
        if (options.IncludeHeaders)
        {
            WriteHeaders(worksheet, propertyMap, options, currentRow);
            currentRow++;
        }

        if (dataList.Count == 0)
        {
            this.logger.LogWarning("No data provided for Excel export");

            return;
        }

        // Write data in batches
        var batchSize = Math.Max(1, options.BatchSize);
        var processed = 0;

        foreach (var batch in dataList.Chunk(batchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var item in batch)
            {
                this.WriteDataRow(worksheet, item, propertyMap, options, currentRow, processed);
                currentRow++;
                processed++;
            }

            this.logger.LogDebug("Processed {ProcessedCount} of {TotalCount} records", processed, dataList.Count);
        }

        // Apply formatting and auto-fit columns
        if (options.AutoFitColumns)
        {
            worksheet.Columns().AdjustToContents();
        }

        if (options.ApplyBasicStyling)
        {
            ApplyBasicStyling(worksheet, propertyMap, options.IncludeHeaders);
        }

        this.logger.LogInformation("Successfully exported {RecordCount} records to Excel", processed);
    }

    private static PropertyMap BuildPropertyMap<T>(ExcelExportOptions options)
        where T : class
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.CanRead)
                             .ToList();

        var mappedProperties = new List<PropertyMapping>();

        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<ExcelColumnAttribute>();

            if (attribute?.Ignore == true)
            {
                continue;
            }

            var columnName = GetColumnName(property, attribute, options);
            var columnIndex = GetColumnIndex(attribute, mappedProperties.Count);

            mappedProperties.Add(new PropertyMapping
            {
                Property = property,
                Attribute = attribute,
                ColumnName = columnName,
                ColumnIndex = columnIndex,
            });
        }

        return new PropertyMap
        {
            Properties = [.. mappedProperties.OrderBy(p => p.ColumnIndex)],
        };
    }

    private static string GetColumnName(PropertyInfo property, ExcelColumnAttribute? attribute, ExcelExportOptions options)
    {
        // Check custom column mappings first
        if (options.ColumnMappings.TryGetValue(property.Name, out var mappedName))
        {
            return mappedName;
        }

        // Use attribute column name if available
        if (attribute != null && !string.IsNullOrEmpty(attribute.ColumnName))
        {
            return attribute.ColumnName;
        }

        // Default to property name
        return property.Name;
    }

    private static int GetColumnIndex(ExcelColumnAttribute? attribute, int defaultIndex)
    {
        if (attribute?.UsesColumnIndex == true)
        {
            return attribute.ColumnIndex;
        }

        return defaultIndex;
    }

    private static void WriteHeaders(IXLWorksheet worksheet, PropertyMap propertyMap, ExcelExportOptions options, int row)
    {
        var column = 1;

        // Use custom headers if provided
        if (options.CustomHeaders != null && options.CustomHeaders.Count > 0)
        {
            foreach (var header in options.CustomHeaders)
            {
                var cell = worksheet.Cell(row, column);
                cell.Value = header;

                if (options.ApplyBasicStyling)
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                column++;
            }
        }
        else
        {
            // Use default headers from property mappings
            foreach (var mapping in propertyMap.Properties)
            {
                var cell = worksheet.Cell(row, column);
                cell.Value = mapping.ColumnName;

                if (options.ApplyBasicStyling)
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                column++;
            }
        }
    }

    private void WriteDataRow<T>(IXLWorksheet worksheet, T item, PropertyMap propertyMap, ExcelExportOptions options, int row, int itemIndex)
        where T : class
    {
        var column = 1;

        foreach (var mapping in propertyMap.Properties)
        {
            try
            {
                var value = mapping.Property.GetValue(item);
                var formattedValue = FormatValue(value, mapping.Attribute, options);

                if (options.TrimStrings && formattedValue is string stringValue)
                {
                    formattedValue = stringValue.Trim();
                }

                worksheet.Cell(row, column).Value = formattedValue?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error setting value for property {PropertyName} at row {RowIndex}", mapping.Property.Name, itemIndex);

                throw new ExcelDataValidationException(
                    $"Error setting value for property '{mapping.Property.Name}' at row {itemIndex}",
                    mapping.Property.Name,
                    mapping.Property.GetValue(item),
                    itemIndex);
            }

            column++;
        }
    }

    private static object? FormatValue(object? value, ExcelColumnAttribute? attribute, ExcelExportOptions options)
    {
        if (value == null)
        {
            return attribute?.DefaultValue;
        }

        var culture = options.CultureInfo ?? CultureInfo.CurrentCulture;

        // Handle DateTime formatting
        if (value is DateTime dateTime && !string.IsNullOrEmpty(attribute?.DateFormat))
        {
            return dateTime.ToString(attribute.DateFormat, culture);
        }

        // Handle numeric formatting
        if (value is IFormattable formattable && !string.IsNullOrEmpty(attribute?.NumberFormat))
        {
            return formattable.ToString(attribute.NumberFormat, culture);
        }

        return value;
    }

    private static void ApplyBasicStyling(IXLWorksheet worksheet, PropertyMap propertyMap, bool hasHeaders)
    {
        var dataRange = worksheet.RangeUsed();

        if (dataRange != null)
        {
            // Apply borders
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Apply header styling
            if (hasHeaders)
            {
                var headerRange = worksheet.Range(1, 1, 1, propertyMap.Properties.Count);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            }
        }
    }

    private class PropertyMap
    {
        public List<PropertyMapping> Properties { get; set; } = new();
    }

    private class PropertyMapping
    {
        public PropertyInfo Property { get; set; } = null!;

        public ExcelColumnAttribute? Attribute { get; set; }

        public string ColumnName { get; set; } = string.Empty;

        public int ColumnIndex { get; set; }
    }
}