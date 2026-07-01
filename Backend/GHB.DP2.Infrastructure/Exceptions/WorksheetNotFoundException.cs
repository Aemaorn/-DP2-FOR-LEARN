namespace GHB.DP2.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when a specified worksheet is not found in the Excel file.
/// </summary>
public class WorksheetNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorksheetNotFoundException"/> class.
    /// </summary>
    public WorksheetNotFoundException()
        : base("The specified worksheet was not found in the Excel file.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorksheetNotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public WorksheetNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorksheetNotFoundException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public WorksheetNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets or sets the worksheet index that was not found.
    /// </summary>
    public int? WorksheetIndex { get; set; }

    /// <summary>
    /// Gets or sets the worksheet name that was not found.
    /// </summary>
    public string? WorksheetName { get; set; }
}