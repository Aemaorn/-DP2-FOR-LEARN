namespace GHB.DP2.Infrastructure.Services.Watchlist;

/// <summary>
/// Exception thrown when an error occurs during Watchlist API operations.
/// </summary>
public class WatchlistException : Exception
{
    /// <summary>
    /// The business-level message returned in the Watchlist API response body
    /// (e.g. a validation message). Safe to surface to end users.
    /// Null for technical failures such as network errors or timeouts.
    /// </summary>
    public string? ApiMessage { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchlistException"/> class.
    /// </summary>
    public WatchlistException()
        : base("An error occurred during Watchlist API operation.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchlistException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public WatchlistException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchlistException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public WatchlistException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchlistException"/> class with a technical message
    /// and the business-level message returned by the Watchlist API.
    /// </summary>
    /// <param name="message">The technical message that describes the error (for logging).</param>
    /// <param name="apiMessage">The business-level message from the Watchlist API body, safe to show to users.</param>
    public WatchlistException(string message, string? apiMessage)
        : base(message)
    {
        this.ApiMessage = apiMessage;
    }
}