namespace GHB.DP2.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when a file is not a valid Excel format or is corrupted.
/// </summary>
public class InvalidFileFormatException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFileFormatException"/> class.
    /// </summary>
    public InvalidFileFormatException()
        : base("The file is not a valid Excel format or is corrupted.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFileFormatException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidFileFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFileFormatException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InvalidFileFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}