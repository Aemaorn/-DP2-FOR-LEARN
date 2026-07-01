namespace GHB.DP2.Infrastructure.Services.Coi;

/// <summary>
/// Exception thrown when an error occurs during COI (Conflict of Interest) API operations.
/// </summary>
public class CoiException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoiException"/> class.
    /// </summary>
    public CoiException()
        : base("An error occurred during COI API operation.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoiException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public CoiException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoiException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public CoiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}