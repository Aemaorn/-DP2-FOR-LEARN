namespace GHB.DP2.Infrastructure.Interceptors;

using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Codehard.Common.DomainModel;
using GHB.DP2.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using static Microsoft.EntityFrameworkCore.EntityState;

/// <summary>
/// Do everything in one interceptor to reduce the overhead on EF execution
/// As much as possible.
/// </summary>
public class AuditInfoInterceptor : SaveChangesInterceptor
{
    private sealed record Actor(Guid UserId, string Name);

    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<AuditInfoInterceptor> logger;

    public AuditInfoInterceptor(
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditInfoInterceptor> logger)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        var context = eventData.Context;

        if (context is null)
        {
            return result;
        }

        var actor = GetActor(this.httpContextAccessor.HttpContext);

        SetAuditInfo(context, actor);
        ModifySoftDeleteEntityTrackingState(context);
        ModifyAuditInfo(context, actor);
        SaveActivityInfo(context, actor);
        SetDocumentHistoryInfo(context, actor);
        SetVendorQualificationCheckerInfo(context, actor);

        context.ChangeTracker.Entries()
               .Filter(static entry => entry is { Entity: IEntity, State: not Unchanged })
               .Iter(this.LogEntityState);

        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;

        if (context is null)
        {
            return new(result);
        }

        var actor = GetActor(this.httpContextAccessor.HttpContext);

        SetAuditInfo(context, actor);
        ModifySoftDeleteEntityTrackingState(context);
        ModifyAuditInfo(context, actor);
        SaveActivityInfo(context, actor);
        SetDocumentHistoryInfo(context, actor);
        SetVendorQualificationCheckerInfo(context, actor);

        context.ChangeTracker.Entries()
               .Filter(static entry => entry is { Entity: IEntity, State: not Unchanged })
               .Iter(this.LogEntityState);

        return new(result);
    }

    private static Actor GetActor(HttpContext? httpContext)
    {
        var userId = httpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? Guid.Empty.ToString();
        var name = httpContext?.User.FindFirst(JwtRegisteredClaimNames.Name)?.Value ?? "System System";

        return new(Guid.Parse(userId), name);
    }

    private static void SetAuditInfo(DbContext context, Actor actor)
    {
        var addedEntities =
            context.ChangeTracker
                   .Entries()
                   .Where(e => e.Entity is IAuditableEntity)
                   .Where(e => e.State == Added)
                   .Select(e => e.Entity)
                   .OfType<IAuditableEntity>();

        addedEntities.Iter(entity =>
        {
            entity.Update(
                actor.UserId,
                actor.Name);
        });
    }

    private static void SaveActivityInfo(DbContext context, Actor actor)
    {
        var entitiesWithActivities =
            context.ChangeTracker
                   .Entries()
                   .Where(e => e.Entity is IHasActivityInfo activityEntity &&
                               activityEntity.Activities.Count != 0)
                   .Select(e => new
                   {
                       Entity = e.Entity as IHasActivityInfo,
                       PrimaryKey = GetPrimaryKeyValuesAsString(e),
                   })
                   .Where(x => x.Entity != null)
                   .ToList();

        var activityLogs = entitiesWithActivities
                           .Select(tuple =>
                               tuple.Entity!.Activities.Select(ai =>
                                   new ActivityLog(
                                       tuple.PrimaryKey,
                                       ai,
                                       new AuditInfo(actor.UserId, DateTimeOffset.UtcNow, actor.Name))))
                           .SelectMany(i => i);

        context.AddRange(activityLogs);

        entitiesWithActivities.Iter(ea => ea.Entity!.ClearActivity());
    }

    public static string GetPrimaryKeyValuesAsString(EntityEntry entry)
    {
        // Get the primary key properties
        var keyProperties = entry.Metadata.FindPrimaryKey()?.Properties;

        if (keyProperties == null || !keyProperties.Any())
        {
            return string.Empty;
        }

        // Get the values of the primary key properties
        var keyValues =
            keyProperties
                .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "null")
                .ToArray();

        // Join them into a single string (with a separator if it's a composite key)
        return string.Join(",", keyValues);
    }

    private static void SetDocumentHistoryInfo(DbContext context, Actor actor)
    {
        var modifiedEntities =
            context.ChangeTracker
                   .Entries()
                   .Where(e => e.Entity is IDocumentHistory)
                   .Where(e => e.State == Added)
                   .Select(e => e.Entity)
                   .OfType<IDocumentHistory>();

        modifiedEntities.Iter(entity =>
        {
            entity.Create(
                actor.UserId,
                actor.Name);
        });
    }

    private static void SetVendorQualificationCheckerInfo(DbContext context, Actor actor)
    {
        var modifiedEntities =
            context.ChangeTracker
                   .Entries()
                   .Where(e => e.Entity is IHasVendorQualificationCheckerInfo)
                   .Where(e => e.State == Added)
                   .Select(e => e.Entity);

        modifiedEntities.Iter(entity =>
        {
            if (entity is not IHasVendorQualificationCheckerInfo checkerInfo)
            {
                return;
            }

            checkerInfo.Create(
                actor.UserId,
                actor.Name);
        });
    }

    private static void ModifyAuditInfo(DbContext context, Actor actor)
    {
        var modifiedEntities =
            context.ChangeTracker
                   .Entries()
                   .Where(e => e.Entity is IAuditableEntity)
                   .Where(e => e.State == Modified)
                   .Select(e => e.Entity)
                   .OfType<IAuditableEntity>();

        modifiedEntities.Iter(entity =>
        {
            entity.Update(
                actor.UserId,
                actor.Name);
        });
    }

    private static void ModifySoftDeleteEntityTrackingState(DbContext context)
    {
        var entities =
            context.ChangeTracker
                   .Entries()
                   .Where(e => e.State == Deleted)
                   .ToHashSet();

        entities.Iter(entry =>
        {
            if (entry.Entity is not IHasSoftDelete isDeletedEntity)
            {
                return;
            }

            isDeletedEntity.Delete();
            entry.State = Modified;
        });

        if (entities.Any(e => e.Entity is IHasSoftDelete))
        {
            entities
                .Where(e =>
                    e.Entity is not IHasSoftDelete)
                .Iter(entity =>
                {
                    if (entity.Entity is AuditInfo)
                    {
                        return;
                    }

                    entity.State = Detached;
                });
        }
    }

    private void LogEntityState(EntityEntry entry)
    {
        var entity = entry.Entity;

        var type = entity.GetType();

        var sb = new StringBuilder();
        sb.AppendLine("--- Entity State Log ---");

        switch (entry.State)
        {
            case Modified:
                {
                    sb.AppendLine($"Entity {type} has been modified with changes to the following properties");

                    foreach (var prop in entry.Properties)
                    {
                        sb.AppendLine(
                            GetMessageWithFallback(
                                prop,
                                static p =>
                                    $"{p.Metadata.Name} was changed from '{p.OriginalValue}' to '{p.CurrentValue}'"));
                    }

                    break;
                }

            case Added:
                {
                    sb.AppendLine($"Entity {type} has been added with the following properties");

                    foreach (var prop in entry.Properties)
                    {
                        sb.AppendLine(GetFallbackMessage(prop));
                    }

                    break;
                }

            case Deleted:
                {
                    sb.AppendLine($"Entity {type} has been deleted with last known properties");

                    foreach (var prop in entry.Properties)
                    {
                        sb.AppendLine(GetFallbackMessage(prop));
                    }

                    break;
                }
        }

        this.logger.LogDebug(sb.ToString());

        static string GetMessageWithFallback(PropertyEntry propertyEntry, Func<PropertyEntry, string> message)
            => propertyEntry.IsModified
                ? message(propertyEntry)
                : GetFallbackMessage(propertyEntry);

        static string GetFallbackMessage(PropertyEntry propertyEntry)
            => $"{propertyEntry.Metadata.Name} is '{propertyEntry.OriginalValue}'";
    }
}