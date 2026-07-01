namespace GHB.DP2.Infrastructure.Services.Email;

public interface IEmailServiceFactory
{
    IFluentEmail Create();
}