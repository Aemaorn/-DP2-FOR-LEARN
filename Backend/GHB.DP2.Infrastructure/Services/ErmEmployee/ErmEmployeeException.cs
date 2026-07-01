namespace GHB.DP2.Infrastructure.Services.ErmEmployee;

public class ErmEmployeeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErmEmployeeException"/> class.
    /// </summary>
    public ErmEmployeeException()
        : base("An error occurred during COI API operation.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErmEmployeeException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ErmEmployeeException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErmEmployeeException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ErmEmployeeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}