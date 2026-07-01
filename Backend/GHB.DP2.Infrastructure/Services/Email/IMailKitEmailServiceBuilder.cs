namespace GHB.DP2.Infrastructure.Services.Email;

public interface IMailKitEmailServiceBuilder
{
    /// <summary>
    /// Sets the host of the email service.
    /// </summary>
    /// <param name="host"></param>
    void SetHost(string host);

    /// <summary>
    /// Sets the port of the email service.
    /// </summary>
    /// <param name="port"></param>
    void SetPort(int port);

    /// <summary>
    /// Sets the username of the email service.
    /// </summary>
    /// <param name="username"></param>
    void SetUsername(string username);

    /// <summary>
    /// Sets the password of the email service.
    /// </summary>
    /// <param name="password"></param>
    void SetPassword(string password);

    /// <summary>
    /// Sets the from mail of the email service.
    /// </summary>
    /// <param name="fromMail"></param>
    void SetFromMail(string fromMail);

    /// <summary>
    /// Sets the display name of the email service.
    /// </summary>
    /// <param name="displayName"></param>
    void SetDisplayName(string displayName);

    void SetEnableSsl(bool enableSsl);
}