using GHB.DP2.Infrastructure.Configurations;

namespace GHB.DP2.Api.Configurations;

public record EmailServiceConfiguration(
    string Host,
    int Port,
    string Username,
    string Password,
    string FromMail,
    string DisplayName,
    bool EnableSsl,
    string? HrEmail) : IServiceConfiguration
{
    public static string Key => "EmailService";
}