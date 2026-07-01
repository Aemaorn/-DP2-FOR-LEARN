namespace GHB.DP2.Infrastructure.Services.ExcelImportAndExport;

using GHB.DP2.Infrastructure.Exceptions;

/// <summary>
/// Service for exporting strongly typed C# objects to Excel files.
/// </summary>
public interface IExcelExportService
{
    /// <summary>
    /// Exports a collection of objects to an Excel file as a byte array.
    /// </summary>
    /// <typeparam name="T">The type of objects to export. Must be a class.</typeparam>
    /// <param name="data">The collection of objects to export to Excel.</param>
    /// <param name="options">The export options to configure the export process. If null, default options will be used.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the Excel file as a byte array.</returns>
    /// <exception cref="ExcelExportException">Thrown when an error occurs during export.</exception>
    /// <exception cref="ExcelDataValidationException">Thrown when data validation fails.</exception>
    /// <exception cref="ExcelFileSizeExceededException">Thrown when the exported file would exceed size limits.</exception>
    Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, ExcelExportOptions? options = null, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Exports a collection of objects to an Excel file using a template as a byte array.
    /// </summary>
    /// <typeparam name="T">The type of objects to export. Must be a class.</typeparam>
    /// <param name="data">The collection of objects to export to Excel.</param>
    /// <param name="templatePath">The path to the Excel template file to use for export.</param>
    /// <param name="options">The export options to configure the export process. If null, default options will be used.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the Excel file as a byte array.</returns>
    /// <exception cref="ExcelExportException">Thrown when an error occurs during export.</exception>
    /// <exception cref="ExcelTemplateMissingException">Thrown when the template file is not found.</exception>
    /// <exception cref="ExcelDataValidationException">Thrown when data validation fails.</exception>
    /// <exception cref="ExcelFileSizeExceededException">Thrown when the exported file would exceed size limits.</exception>
    Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string templatePath, ExcelExportOptions? options = null, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Exports a collection of objects to an Excel file stream.
    /// </summary>
    /// <typeparam name="T">The type of objects to export. Must be a class.</typeparam>
    /// <param name="data">The collection of objects to export to Excel.</param>
    /// <param name="options">The export options to configure the export process. If null, default options will be used.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the Excel file as a stream.</returns>
    /// <exception cref="ExcelExportException">Thrown when an error occurs during export.</exception>
    /// <exception cref="ExcelDataValidationException">Thrown when data validation fails.</exception>
    /// <exception cref="ExcelFileSizeExceededException">Thrown when the exported file would exceed size limits.</exception>
    Task<MemoryStream> ExportToExcelStreamAsync<T>(IEnumerable<T> data, ExcelExportOptions? options = null, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Exports a collection of objects to an Excel file stream using a template.
    /// </summary>
    /// <typeparam name="T">The type of objects to export. Must be a class.</typeparam>
    /// <param name="data">The collection of objects to export to Excel.</param>
    /// <param name="templatePath">The path to the Excel template file to use for export.</param>
    /// <param name="options">The export options to configure the export process. If null, default options will be used.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the Excel file as a stream.</returns>
    /// <exception cref="ExcelExportException">Thrown when an error occurs during export.</exception>
    /// <exception cref="ExcelTemplateMissingException">Thrown when the template file is not found.</exception>
    /// <exception cref="ExcelDataValidationException">Thrown when data validation fails.</exception>
    /// <exception cref="ExcelFileSizeExceededException">Thrown when the exported file would exceed size limits.</exception>
    Task<MemoryStream> ExportToExcelStreamAsync<T>(IEnumerable<T> data, string templatePath, ExcelExportOptions? options = null, CancellationToken cancellationToken = default)
        where T : class;
}