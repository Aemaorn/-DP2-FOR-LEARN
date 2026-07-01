namespace GHB.DP2.Infrastructure.Configurations;

public sealed record MailkitEmailConfiguration
{
    public string Host { get; init; }

    public int Port { get; init; }

    public string Username { get; init; }

    public string Password { get; init; }

    public string FromMail { get; init; }

    public string DisplayName { get; init; }

    public bool UseSsl { get; init; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(this.Host))
        {
            throw new ArgumentException("Host is not set");
        }

        if (this.Port <= 0)
        {
            throw new ArgumentException("Port is not set");
        }

        if (!string.IsNullOrWhiteSpace(this.Username) && string.IsNullOrWhiteSpace(this.Password))
        {
            throw new ArgumentException("Password is not set");
        }

        if (string.IsNullOrWhiteSpace(this.FromMail))
        {
            throw new ArgumentException("From mail is not set");
        }

        if (string.IsNullOrWhiteSpace(this.DisplayName))
        {
            throw new ArgumentException("Display name is not set");
        }
    }
}