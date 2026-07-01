namespace GHB.DP2.Infrastructure.Services.Email;

using GHB.DP2.Infrastructure.Configurations;

public class MailKitEmailServiceBuilder : IMailKitEmailServiceBuilder
{
    public MailkitEmailConfiguration Configuration { get; private set; } = new();

    public void SetHost(string host)
    {
        this.Configuration = this.Configuration with
        {
            Host = host,
        };
    }

    public void SetPort(int port)
    {
        this.Configuration = this.Configuration with
        {
            Port = port,
        };
    }

    public void SetUsername(string username)
    {
        this.Configuration = this.Configuration with
        {
            Username = username,
        };
    }

    public void SetPassword(string password)
    {
        this.Configuration = this.Configuration with
        {
            Password = password,
        };
    }

    public void SetFromMail(string fromMail)
    {
        this.Configuration = this.Configuration with
        {
            FromMail = fromMail,
        };
    }

    public void SetDisplayName(string displayName)
    {
        this.Configuration = this.Configuration with
        {
            DisplayName = displayName,
        };
    }

    public void SetEnableSsl(bool enableSsl)
    {
        this.Configuration = this.Configuration with
        {
            UseSsl = enableSsl,
        };
    }
}