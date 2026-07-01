namespace GHB.DP2.Infrastructure.Services.ActiveDirectory;

public interface IActiveDirectoryService
{
    /// <summary>
    /// Validates the provided username and password against Active Directory.
    /// </summary>
    /// <param name="username">The username to validate.</param>
    /// <param name="password">The password associated with the username.</param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a boolean value
    /// indicating whether the validation was successful.
    /// </returns>
    Task<bool> ValidateAsync(string username, string password, CancellationToken cancellationToken = default);

    Task PerformDummyValidationAsync(CancellationToken cancellationToken = default);
}
