namespace GHB.DP2.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when an Excel file contains no data or no valid rows.
/// </summary>
public class EmptyFileException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyFileException"/> class.
    /// </summary>
    public EmptyFileException()
        : base("The Excel file contains no data or no valid rows.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyFileException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public EmptyFileException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyFileException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public EmptyFileException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}