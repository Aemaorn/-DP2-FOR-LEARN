namespace GHB.DP2.Infrastructure.Services.Email;

public interface IFluentEmail
{
    IFluentEmail From(string email, string? name = null);

    IFluentEmail To(string email, string? name = null);

    IFluentEmail Cc(string email, string? name = null);

    IFluentEmail Bcc(string email, string? name = null);

    IFluentEmail Subject(string subjectText);

    IFluentEmail Html(string html);

    IFluentEmail Text(string text);

    IFluentEmail Attach(string fileName, byte[] content, string? contentType = null);

    Task SendAsync(CancellationToken cancellationToken = default);
}