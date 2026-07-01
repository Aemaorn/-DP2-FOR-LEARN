namespace GHB.DP2.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when the exported Excel file would exceed the configured size limits.
/// </summary>
public class ExcelFileSizeExceededException : ExcelExportException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelFileSizeExceededException"/> class.
    /// </summary>
    /// <param name="actualSize">The actual size of the file in bytes.</param>
    /// <param name="maxSize">The maximum allowed size in bytes.</param>
    public ExcelFileSizeExceededException(long actualSize, long maxSize)
        : base($"Excel file size ({actualSize} bytes) exceeds maximum allowed size ({maxSize} bytes).")
    {
        this.ActualSize = actualSize;
        this.MaxSize = maxSize;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelFileSizeExceededException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="actualSize">The actual size of the file in bytes.</param>
    /// <param name="maxSize">The maximum allowed size in bytes.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ExcelFileSizeExceededException(long actualSize, long maxSize, Exception innerException)
        : base($"Excel file size ({actualSize} bytes) exceeds maximum allowed size ({maxSize} bytes).", innerException)
    {
        this.ActualSize = actualSize;
        this.MaxSize = maxSize;
    }

    /// <summary>
    /// Gets the actual size of the file in bytes.
    /// </summary>
    public long ActualSize { get; }

    /// <summary>
    /// Gets the maximum allowed size in bytes.
    /// </summary>
    public long MaxSize { get; }
}