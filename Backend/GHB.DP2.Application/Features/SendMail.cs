namespace GHB.DP2.Application.Features;

using GHB.DP2.Infrastructure.Services.Email;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public class SendMailEndpoint : EndpointBase<Ok>
{
    private readonly IEmailService emailService;

    public SendMailEndpoint(
        IEmailService emailService,
        ILogger<SendMailEndpoint> logger)
        : base(logger)
    {
        this.emailService = emailService;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Email"));
        this.Post("/mail");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Ok> HandleRequestAsync(CancellationToken ct)
    {
        // Example data for sending an email
        var receiver = "Ghb";
        var subject = "Test Email";
        var message = "This is a test email message.";

        // Example usage of the email service
        await this.emailService.SendMessageAsync(receiver, subject, message, ct);

        return TypedResults.Ok();
    }
}