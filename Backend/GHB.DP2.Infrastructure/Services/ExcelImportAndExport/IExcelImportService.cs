namespace GHB.DP2.Infrastructure.Services.ExcelImportAndExport;

using GHB.DP2.Infrastructure.Exceptions;

/// <summary>
/// Service for importing Excel files and mapping data to strongly typed C# objects.
/// </summary>
public interface IExcelImportService
{
    /// <summary>
    /// Imports data from an Excel file stream and maps it to a list of strongly typed objects.
    /// </summary>
    /// <typeparam name="T">The type to map the Excel data to. Must be a class with a parameterless constructor.</typeparam>
    /// <param name="fileStream">The stream containing the Excel file data.</param>
    /// <param name="skipHeader">Whether to skip the first row (header row). Defaults to true.</param>
    /// <param name="worksheetIndex">The index of the worksheet to read (0-based). Defaults to 0.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of mapped objects.</returns>
    /// <exception cref="InvalidFileFormatException">Thrown when the file is not a valid Excel format.</exception>
    /// <exception cref="EmptyFileException">Thrown when the file contains no data.</exception>
    /// <exception cref="WorksheetNotFoundException">Thrown when the specified worksheet doesn't exist.</exception>
    /// <exception cref="DataMappingException">Thrown when data cannot be mapped to the target type.</exception>
    Task<List<T>> ImportExcelAsync<T>(Stream fileStream, bool skipHeader = true, int worksheetIndex = 0, CancellationToken cancellationToken = default)
        where T : class, new();

    /// <summary>
    /// Imports data from an Excel file stream and maps it to a list of strongly typed objects with advanced options.
    /// </summary>
    /// <typeparam name="T">The type to map the Excel data to. Must be a class with a parameterless constructor.</typeparam>
    /// <param name="fileStream">The stream containing the Excel file data.</param>
    /// <param name="options">The import options to configure the import process.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of mapped objects.</returns>
    /// <exception cref="InvalidFileFormatException">Thrown when the file is not a valid Excel format.</exception>
    /// <exception cref="EmptyFileException">Thrown when the file contains no data.</exception>
    /// <exception cref="WorksheetNotFoundException">Thrown when the specified worksheet doesn't exist.</exception>
    /// <exception cref="DataMappingException">Thrown when data cannot be mapped to the target type.</exception>
    Task<List<T>> ImportExcelAsync<T>(Stream fileStream, ExcelImportOptions options, CancellationToken cancellationToken = default)
        where T : class, new();
}