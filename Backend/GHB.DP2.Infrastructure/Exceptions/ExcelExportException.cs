namespace GHB.DP2.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when an error occurs during Excel export operations.
/// </summary>
public class ExcelExportException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelExportException"/> class.
    /// </summary>
    public ExcelExportException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelExportException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ExcelExportException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelExportException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ExcelExportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}