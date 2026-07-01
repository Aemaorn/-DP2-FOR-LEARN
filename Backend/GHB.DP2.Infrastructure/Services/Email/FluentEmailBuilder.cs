namespace GHB.DP2.Infrastructure.Services.Email;

using System.Text;
using GHB.DP2.Infrastructure.Configurations;
using MimeKit;
using MimeKit.Text;

internal sealed class FluentEmailBuilder : IFluentEmail
{
    private readonly MailkitEmailConfiguration configuration;
    private readonly IEmailService emailService;

    private readonly List<InternetAddress> to = new();
    private readonly List<InternetAddress> cc = new();
    private readonly List<InternetAddress> bcc = new();

    private string? fromEmail;
    private string? fromName;
    private string? subject;
    private string? htmlBody;
    private string? textBody;

    private readonly List<(string FileName, byte[] Content, string? ContentType)> attachments = new();

    public FluentEmailBuilder(
        MailkitEmailConfiguration configuration,
        IEmailService emailService)
    {
        this.configuration = configuration;
        this.emailService = emailService;
    }

    public IFluentEmail From(string email, string? name = null)
    {
        this.fromEmail = email;
        this.fromName = name;

        return this;
    }

    public IFluentEmail To(string email, string? name = null)
    {
        this.to.Add(new MailboxAddress(name ?? string.Empty, email));

        return this;
    }

    public IFluentEmail Cc(string email, string? name = null)
    {
        this.cc.Add(new MailboxAddress(name ?? string.Empty, email));

        return this;
    }

    public IFluentEmail Bcc(string email, string? name = null)
    {
        this.bcc.Add(new MailboxAddress(name ?? string.Empty, email));

        return this;
    }

    public IFluentEmail Subject(string subjectText)
    {
        this.subject = subjectText;

        return this;
    }

    public IFluentEmail Html(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            this.htmlBody = null;

            return this;
        }

        // Normalize Unicode characters and remove problematic ones
        var normalizedHtml = html.Normalize(NormalizationForm.FormC);

        // Remove control characters except essential whitespace
        var sanitizedHtml = new string([.. normalizedHtml.Where(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t')]);

        // Ensure proper encoding by converting to UTF-8 bytes and back
        var utf8Bytes = Encoding.UTF8.GetBytes(sanitizedHtml);
        this.htmlBody = Encoding.UTF8.GetString(utf8Bytes).Trim();

        return this;
    }

    public IFluentEmail Text(string text)
    {
        this.textBody = text;

        return this;
    }

    public IFluentEmail Attach(string fileName, byte[] content, string? contentType = null)
    {
        this.attachments.Add((fileName, content, contentType));

        return this;
    }

    public Task SendAsync(CancellationToken cancellationToken = default)
    {
        return this.emailService.SendMailAsync(this.ConfigureMessage, cancellationToken);
    }

    private void ConfigureMessage(MimeMessage message)
    {
        // From
        var fromAddress = new MailboxAddress(
            this.fromName ?? this.configuration.DisplayName,
            this.fromEmail ?? this.configuration.FromMail);
        message.From.Add(fromAddress);

        // To, CC, BCC
        if (this.to.Any())
        {
            message.To.AddRange(this.to);
        }

        if (this.cc.Any())
        {
            message.Cc.AddRange(this.cc);
        }

        if (this.bcc.Any())
        {
            message.Bcc.AddRange(this.bcc);
        }

        // Subject
        if (!string.IsNullOrWhiteSpace(this.subject))
        {
            message.Subject = this.subject!;
        }

        // Body & attachments
        if (this.attachments.Count == 0 && this.htmlBody is not null && this.textBody is null)
        {
            message.Body = new TextPart(TextFormat.Html) { Text = this.htmlBody };

            return;
        }

        if (this.attachments.Count == 0 && this.textBody is not null && this.htmlBody is null)
        {
            message.Body = new TextPart(TextFormat.Plain) { Text = this.textBody };

            return;
        }

        var builder = new BodyBuilder
        {
            HtmlBody = this.htmlBody,
            TextBody = this.textBody,
        };

        foreach (var (fileName, content, contentType) in this.attachments)
        {
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                builder.Attachments.Add(fileName, content, ContentType.Parse(contentType));
            }
            else
            {
                builder.Attachments.Add(fileName, content);
            }
        }

        message.Body = builder.ToMessageBody();
    }
}