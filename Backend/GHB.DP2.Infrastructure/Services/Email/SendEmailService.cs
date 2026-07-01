namespace GHB.DP2.Infrastructure.Services.Email;

using FastEndpoints;
using GHB.DP2.Infrastructure.Configurations;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using MailKit.Security;
using MailKitClient = MailKit.Net.Smtp.SmtpClient;

public interface IEmailService
{
    /// <summary>
    /// Send raw message to receiver.
    /// </summary>
    /// <param name="receiver"></param>
    /// <param name="subject"></param>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SendMessageAsync(string receiver, string subject, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send HTML message to receiver.
    /// </summary>
    /// <param name="receiver"></param>
    /// <param name="subject"></param>
    /// <param name="html"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SendHtmlAsync(string receiver, string subject, string html, CancellationToken cancellationToken = default);

    Task SendMailAsync(Action<MimeMessage> configure, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a fluent email builder for composing complex emails.
    /// </summary>
    /// <returns></returns>
    IFluentEmail Compose();
}

[RegisterService<IEmailService>(LifeTime.Scoped)]
public class SendEmailService : IEmailService
{
    private readonly MailkitEmailConfiguration emailConfiguration;
    private readonly ILogger logger;

    public SendEmailService(
        MailkitEmailConfiguration emailConfiguration,
        ILogger<SendEmailService> logger)
    {
        this.emailConfiguration = emailConfiguration;
        this.logger = logger;
    }

    public Task SendMessageAsync(string receiver, string subject, string message, CancellationToken cancellationToken = default)
    {
        return this.SendMailAsync(
            mineMessage =>
            {
                mineMessage.From.Add(
                    new MailboxAddress(
                        this.emailConfiguration.DisplayName,
                        this.emailConfiguration.FromMail));

                mineMessage.To.Add(
                    new MailboxAddress(
                        string.Empty,
                        receiver));

                mineMessage.Subject = subject;

                mineMessage.Body = new TextPart(TextFormat.Plain)
                {
                    Text = message,
                };
            },
            cancellationToken);
    }

    public Task SendHtmlAsync(
        string receiver,
        string subject,
        string html,
        CancellationToken cancellationToken = default)
    {
        return this.SendMailAsync(
            mineMessage =>
            {
                mineMessage.From.Add(
                    new MailboxAddress(
                        this.emailConfiguration.DisplayName,
                        this.emailConfiguration.FromMail));

                mineMessage.To.Add(
                    new MailboxAddress(
                        receiver,
                        receiver));

                mineMessage.Subject = subject;

                mineMessage.Body = new TextPart(TextFormat.Html)
                {
                    Text = html,
                };
            },
            cancellationToken);
    }

    public async Task SendMailAsync(
        Action<MimeMessage> configure,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var mailKitClient = new MailKitClient();
            using var mineMessage = new MimeMessage();

            configure(mineMessage);

            var sslOptions = this.emailConfiguration.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.None;

            await mailKitClient.ConnectAsync(
                this.emailConfiguration.Host,
                this.emailConfiguration.Port,
                sslOptions,
                cancellationToken);

            if (!string.IsNullOrEmpty(this.emailConfiguration.Username))
            {
                await mailKitClient.AuthenticateAsync(
                    this.emailConfiguration.Username,
                    this.emailConfiguration.Password,
                    cancellationToken);
            }

            await mailKitClient.SendAsync(
                mineMessage,
                CancellationToken.None);

            await mailKitClient.DisconnectAsync(
                true,
                cancellationToken);
        }
        catch (Exception e)
        {
            this.logger.LogError(e, "Error sending email");

            throw;
        }
    }

    public IFluentEmail Compose()
        => new FluentEmailBuilder(this.emailConfiguration, this);
}
