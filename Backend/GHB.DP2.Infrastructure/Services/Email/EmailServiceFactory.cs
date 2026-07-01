namespace GHB.DP2.Infrastructure.Services.Email;

using FastEndpoints;
using GHB.DP2.Infrastructure.Configurations;

/// <summary>
/// Factory class to create fluent email builders, registered for DI.
/// </summary>
[RegisterService<IEmailServiceFactory>(LifeTime.Scoped)]
public class EmailServiceFactory : IEmailServiceFactory
{
    private readonly MailkitEmailConfiguration configuration;
    private readonly IEmailService emailService;

    public EmailServiceFactory(
        MailkitEmailConfiguration configuration,
        IEmailService emailService)
    {
        this.configuration = configuration;
        this.configuration = configuration;
        this.emailService = emailService;
    }

    public IFluentEmail Create()
        => new FluentEmailBuilder(this.configuration, this.emailService);
}