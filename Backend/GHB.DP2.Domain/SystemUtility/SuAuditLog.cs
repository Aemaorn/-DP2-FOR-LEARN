namespace GHB.DP2.Domain.SystemUtility;

using Codehard.Common.DomainModel;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct AuditLogId
{
    public static AuditLogId New() => From(Guid.CreateVersion7());
}

public class SuAuditLog : Entity<AuditLogId>
{
    public override AuditLogId Id { get; init; }

    public string Message { get; init; }

    public Guid? UserId { get; init; }

    public string? User { get; init; }

    public string Program { get; init; }

    public string IpAddress { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public static SuAuditLog Create(
        SuUser user,
        string message,
        string program,
        string ipIpAddress)
    {
        return new SuAuditLog
        {
            Id = AuditLogId.New(),
            Message = message,
            UserId = user.Id.Value,
            User = user.Employee.View?.FullName,
            Program = program,
            IpAddress = ipIpAddress,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public static SuAuditLog Create(
        string message,
        string program,
        string ipIpAddress)
    {
        return new SuAuditLog
        {
            Id = AuditLogId.New(),
            Message = message,
            Program = program,
            IpAddress = ipIpAddress,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}