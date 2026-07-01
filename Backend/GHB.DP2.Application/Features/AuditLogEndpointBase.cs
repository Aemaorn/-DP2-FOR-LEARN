namespace GHB.DP2.Application.Features;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.EventHandlers.SuAuditLog;
using GHB.DP2.Application.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Delegate | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AuditLogAttribute : Attribute
{
    public string ProgramName { get; }

    public string Message { get; }

    public AuditLogAttribute(string programName, string message)
    {
        this.ProgramName = programName;
        this.Message = message;
    }
}

public abstract partial class EndpointBase<TRequest, TResponse>
{
    protected void AuditLog(string programName, string auditMessage)
    {
        this.Options(options =>
        {
            options.WithMetadata(new AuditLogAttribute(programName, auditMessage));
        });
    }

    public override async Task OnBeforeHandleAsync(TRequest req, CancellationToken ct)
    {
        var auditLogAttribute =
            this.HttpContext
                .GetEndpoint()?
                .Metadata
                .OfType<AuditLogAttribute>()
                .FirstOrDefault();

        if (auditLogAttribute is null)
        {
            // No audit log attribute, skip logging
            await base.OnBeforeHandleAsync(req, ct);

            return;
        }

        var program = auditLogAttribute.ProgramName;
        var message = auditLogAttribute.Message;

        var userId =
            Optional(this.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value)
                .Map(Guid.Parse);

        var ipAddress = this.HttpContext.TryGetIpAddress();

        await new SaveAuditLogEvent(
                message,
                program,
                userId,
                ipAddress?.ToString() ?? string.Empty)
            .PublishAsync(Mode.WaitForNone, cancellation: ct);

        await base.OnBeforeHandleAsync(req, ct);
    }
}