namespace GHB.DP2.Application.EventHandlers.SuAuditLog;

using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public record SaveAuditLogEvent(
    string Message,
    string Program,
    Option<Guid> UserIdOpt,
    string IpAddress) : IEvent;

public class SaveAuditLogEventHandler : IEventHandler<SaveAuditLogEvent>
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger logger;

    public SaveAuditLogEventHandler(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<SaveAuditLogEventHandler> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    public async Task HandleAsync(SaveAuditLogEvent eventModel, CancellationToken ct)
    {
        this.logger.LogInformation("Handling SaveAuditLogEvent with message: {Message}", eventModel.Message);

        await using var scope = this.serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Dp2DbContext>();

        var createAuditLogAsync =
            eventModel.UserIdOpt
                      .Map(UserId.From)
                      .MapAsync(MapAuditLogAsync)
                      .IfNone(MapToAuditLog);

        var auditLog = await createAuditLogAsync;

        dbContext.Add(auditLog);

        await dbContext.SaveChangesAsync(ct);

        this.logger.LogInformation("Audit log saved successfully for message: {Message}", eventModel.Message);

        return;

        Task<SuAuditLog> MapAuditLogAsync(UserId userId) =>
            dbContext.SuUsers
                     .AsNoTracking()
                     .SingleOrNoneAsync(
                         suUser => suUser.Id == userId,
                         ct)
                     .Map(MapUserToAuditLog);

        SuAuditLog MapUserToAuditLog(Option<SuUser> userOpt)
        {
            return userOpt.Match(
                user => SuAuditLog.Create(
                    user,
                    eventModel.Message,
                    eventModel.Program,
                    eventModel.IpAddress),
                MapToAuditLog);
        }

        SuAuditLog MapToAuditLog() =>
            SuAuditLog.Create(
                eventModel.Message,
                eventModel.Program,
                eventModel.IpAddress);
    }
}