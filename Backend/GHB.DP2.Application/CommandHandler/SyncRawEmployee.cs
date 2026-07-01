namespace GHB.DP2.Application.CommandHandler;

using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class SyncRawEmployeeCommand : ICommand;

public class SyncRawEmployeeHandler : ICommandHandler<SyncRawEmployeeCommand>
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<SyncRawEmployeeHandler> logger;

    public SyncRawEmployeeHandler(
        IServiceProvider serviceProvider,
        ILogger<SyncRawEmployeeHandler> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public async Task ExecuteAsync(SyncRawEmployeeCommand command, CancellationToken ct)
    {
        this.logger.LogInformation("Starting synchronization of raw employees.");

        await using var scope = this.serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Dp2DbContext>();

        var rawErmEmployeeCount = await dbContext.RawErmEmployees.CountAsync(CancellationToken.None);

        if (rawErmEmployeeCount == 0)
        {
            this.logger.LogWarning("No raw ERM employees found. Synchronization aborted.");

            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(CancellationToken.None);

        try
        {
            await SetDefaultPositionAsync(dbContext, this.logger, CancellationToken.None);
            await UpsertPositionAsync(dbContext, this.logger, CancellationToken.None);

            await SetDefaultBusinessUnitAsync(dbContext, this.logger, CancellationToken.None);
            await UpsertBusinessUnitHeadAsync(dbContext, this.logger, CancellationToken.None);
            await UpsertBusinessUnitGroupAsync(dbContext, this.logger, CancellationToken.None);
            await UpsertBusinessUnitLineAsync(dbContext, this.logger, CancellationToken.None);
            await UpsertBusinessUnitDepartmentAsync(dbContext, this.logger, CancellationToken.None);
            await UpsertBusinessUnitSegmentAsync(dbContext, this.logger, CancellationToken.None);
            await UpsertBusinessUnitCenterAsync(dbContext, this.logger, CancellationToken.None);
            await UpsertBusinessUnitZoneAsync(dbContext, this.logger, CancellationToken.None);
            await UpsertBusinessUnitBranchAsync(dbContext, this.logger, CancellationToken.None);

            await UpsertEmployeeAsync(dbContext, this.logger, CancellationToken.None);

            await UpsertEmployeePositionAsync(dbContext, this.logger, CancellationToken.None);

            await UpdateInRefCodeASync(dbContext, this.logger, CancellationToken.None);

            await UpsertSuUserAsync(dbContext, this.logger, CancellationToken.None);

            await transaction.CommitAsync(CancellationToken.None);
        }
        catch (Exception e)
        {
            this.logger.LogError(e, "An error occurred during synchronization of raw employees.");
            await transaction.RollbackAsync(CancellationToken.None);

            throw new InvalidOperationException("Failed to synchronize raw employees. Transaction has been rolled back.", e);
        }

        await RefreshMaterializedViewAsync(dbContext, this.logger, CancellationToken.None);

        this.logger.LogInformation("Synchronization of raw employees completed.");
    }

    private const string DefaultPositionId = "00000000";

    private const string DefaultBusinessUnitId = "00000000";

    private static async Task UpsertPositionAsync(Dp2DbContext dbContext, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("UpsertPositionAsync started.");
        var ermPosition =
            await dbContext.RawErmEmployees
                           .Select(e => new { e.PositionId, e.PositionName, e.Grade })
                           .Where(e => e.PositionId != DefaultPositionId)
                           .Distinct()
                           .OrderBy(e => e.PositionId)
                           .AsNoTracking()
                           .ToArrayAsync(ct);

        var rawPositonsErm = ermPosition
                             .Select((e, index) => RawPosition.Create(
                                 e.PositionId,
                                 e.Grade,
                                 index + 1,
                                 e.PositionName,
                                 string.Empty))
                             .ToArray();

        var positionIds = rawPositonsErm.Select(p => p.Id).ToArray();

        var existingPositions =
            await dbContext.RawPositions
                           .Where(p => positionIds.Contains(p.Id))
                           .ToListAsync(ct);

        _ = existingPositions
            .Join(
                rawPositonsErm,
                existing => existing.Id,
                incoming => incoming.Id,
                (existing, incoming) =>
                    existing.Update(incoming.Grade, incoming.Name, incoming.InRefCode))
            .ToHashSet();

        var newPositions = rawPositonsErm
                           .Where(p => existingPositions.All(ep => ep.Id != p.Id))
                           .Distinct();

        await dbContext.RawPositions.AddRangeAsync(newPositions, ct);

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("UpsertPositionAsync completed.");
    }

    private static async Task SetDefaultPositionAsync(Dp2DbContext dbContext, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("SetDefaultPositionAsync started.");
        var defaultPosition = await dbContext.RawPositions
                                             .FirstOrDefaultAsync(p => p.Id == PositionId.From(DefaultPositionId), ct);

        if (defaultPosition is null)
        {
            defaultPosition = RawPosition.Create(
                DefaultPositionId,
                string.Empty,
                0,
                "ลาออก หรือย้าย หรือ ไม่ใช้งาน",
                string.Empty);

            await dbContext.RawPositions.AddAsync(defaultPosition, ct);
            await dbContext.SaveChangesAsync(ct);
        }

        logger.LogInformation("SetDefaultPositionAsync completed.");
    }

    private static async Task UpsertBusinessUnitHeadAsync(Dp2DbContext dbContext, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("UpsertBusinessUnitHeadAsync started.");
        const string organizationLevel = EmployeeConstant.OrganizationLevel.Head;

        var ermBusinessUnits =
            await dbContext.RawErmEmployees
                           .Where(e => e.OrganizationLevel1 != null)
                           .AsNoTracking()
                           .Select(e => e.OrganizationLevel1)
                           .Distinct()
                           .ToArrayAsync(ct);

        var rawBusinessUnitErm = ermBusinessUnits
                                 .Where(e => e != null)
                                 .Select(h => RawBusinessUnit.Create(
                                     h!.Id,
                                     h.SolId,
                                     h.ShortName,
                                     h.Name,
                                     organizationLevel))
                                 .ToArray();

        var rawBusinessUnitId =
            rawBusinessUnitErm.Map(e => e.Id)
                              .ToArray();

        var rawBusinessUnits =
            await dbContext.RawBusinessUnits
                           .Where(e =>
                               e.OrganizationLevel == organizationLevel)
                           .Where(e => rawBusinessUnitId.Contains(e.Id))
                           .ToArrayAsync(ct);

        _ = rawBusinessUnits
            .Join(
                rawBusinessUnitErm,
                existing => existing.Id,
                incoming => incoming.Id,
                (existing, incoming)
                    => existing.Update(
                        incoming.BusinessUnitCode,
                        incoming.ShortName,
                        incoming.Name,
                        organizationLevel))
            .ToHashSet();

        var allExistingIds = (await dbContext.RawBusinessUnits
                                             .Where(e => rawBusinessUnitId.Contains(e.Id))
                                             .Select(e => e.Id)
                                             .ToArrayAsync(ct))
                             .ToHashSet();

        var newBusinessUnits =
            rawBusinessUnitErm.Where(b => !allExistingIds.Contains(b.Id))
                              .DistinctBy(b => b.Id);

        await dbContext.RawBusinessUnits.AddRangeAsync(newBusinessUnits, ct);

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("UpsertBusinessUnitHeadAsync completed.");
    }

    private static async Task UpsertBusinessUnitGroupAsync(Dp2DbContext dbContext, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("UpsertBusinessUnitGroupAsync started.");
        const string organizationLevel = EmployeeConstant.OrganizationLevel.Group;

        var ermBusinessUnits =
            await dbContext.RawErmEmployees
                           .Where(e => e.OrganizationLevel1 != null && e.OrganizationLevel2 != null)
                           .AsNoTracking()
                           .Select(e => new
                           {
                               Head = e.OrganizationLevel1,
                               Group = e.OrganizationLevel2,
                           })
                           .Distinct()
                           .ToArrayAsync(ct);

        var rawBusinessUnitErm = ermBusinessUnits
                                 .Where(e => e.Head != null && e.Group != null)
                                 .Select(h => RawBusinessUnit.Create(
                                     h.Group!.Id,
                                     h.Group.SolId,
                                     h.Group.ShortName,
                                     h.Group.Name,
                                     organizationLevel,
                                     h.Head!.Id))
                                 .ToArray();

        var rawBusinessUnitId =
            rawBusinessUnitErm.Map(e => e.Id)
                              .ToArray();

        var rawBusinessUnits =
            await dbContext.RawBusinessUnits
                           .Where(e =>
                               e.OrganizationLevel == organizationLevel)
                           .Where(e => rawBusinessUnitId.Contains(e.Id))
                           .ToArrayAsync(ct);

        _ = rawBusinessUnits
            .Join(
                rawBusinessUnitErm,
                existing => existing.Id,
                incoming => incoming.Id,
                (existing, incoming)
                    =>
                {
                    existing.Update(
                        incoming.BusinessUnitCode,
                        incoming.ShortName,
                        incoming.Name,
                        organizationLevel);

                    if (incoming.ParentId is not null)
                    {
                        existing.SetParent(incoming.ParentId.Value);
                    }

                    return existing;
                })
            .ToHashSet();

        var allExistingIds = (await dbContext.RawBusinessUnits
                                             .Where(e => rawBusinessUnitId.Contains(e.Id))
                                             .Select(e => e.Id)
                                             .ToArrayAsync(ct))
                             .ToHashSet();

        var newBusinessUnits =
            rawBusinessUnitErm.Where(b => !allExistingIds.Contains(b.Id))
                              .DistinctBy(b => b.Id);

        await dbContext.RawBusinessUnits.AddRangeAsync(newBusinessUnits, ct);

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("UpsertBusinessUnitGroupAsync completed.");
    }

    private static async Task UpsertBusinessUnitLineAsync(Dp2DbContext dbContext, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("UpsertBusinessUnitLineAsync started.");
        const string organizationLevel = EmployeeConstant.OrganizationLevel.Line;

        var ermBusinessUnits =
            await dbContext.RawErmEmployees
                           .Where(e => e.OrganizationLevel3 != null)
                           .AsNoTracking()
                           .Select(e => new
                           {
                               Head = e.OrganizationLevel1,
                               Group = e.OrganizationLevel2,
                               Line = e.OrganizationLevel3,
                           })
                           .Distinct()
                           .ToArrayAsync(ct);

        var rawBusinessUnitErm = ermBusinessUnits
                                 .Where(e => e.Line != null)
                                 .Map(h =>
                                 {
                                     var parentId =
                                         string.IsNullOrEmpty(h.Group?.Id)
                                             ? h.Head?.Id
                                             : h.Group.Id;

                                     var entity = RawBusinessUnit.Create(
                                         h.Line!.Id,
                                         h.Line.SolId,
                                         h.Line.ShortName,
                                         h.Line.Name,
                                         organizationLevel);

                                     if (parentId is not null)
                                     {
                                         entity.SetParent(BusinessUnitId.From(parentId));
                                     }

                                     return entity;
                                 })
                                 .ToArray();

        var rawBusinessUnitId =
            rawBusinessUnitErm.Map(e => e.Id)
                              .ToArray();

        var rawBusinessUnits =
            await dbContext.RawBusinessUnits
                           .Where(e =>
                               e.OrganizationLevel == organizationLevel)
                           .Where(e => rawBusinessUnitId.Contains(e.Id))
                           .ToArrayAsync(ct);

        _ = rawBusinessUnits
            .Join(
                rawBusinessUnitErm,
                existing => existing.Id,
                incoming => incoming.Id,
                (existing, incoming)
                    =>
                {
                    existing.Update(
                        incoming.BusinessUnitCode,
                        incoming.ShortName,
                        incoming.Name,
                        organizationLevel);

                    if (incoming.ParentId is not null)
                    {
                        existing.SetParent(incoming.ParentId.Value);
                    }

                    return existing;
                })
            .ToHashSet();

        var allExistingIds = (await dbContext.RawBusinessUnits
                                             .Where(e => rawBusinessUnitId.Contains(e.Id))
                                             .Select(e => e.Id)
                                             .ToArrayAsync(ct))
                             .ToHashSet();

        var newBusinessUnits =
            rawBusinessUnitErm.Where(b => !allExistingIds.Contains(b.Id))
                              .DistinctBy(b => b.Id);

        await dbContext.RawBusinessUnits.AddRangeAsync(newBusinessUnits, ct);

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("UpsertBusinessUnitLineAsync completed.");
    }

    private static async Task UpsertBusinessUnitDepartmentAsync(Dp2DbContext dbContext, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("UpsertBusinessUnitDepartmentAsync started.");
        const string organizationLevel = EmployeeConstant.OrganizationLevel.Department;

        var ermBusinessUnits =
            await dbContext.RawErmEmployees
                           .Where(e => e.OrganizationLevel4 != null)
                           .AsNoTracking()
                           .Select(e => new
                           {
                               Line = e.OrganizationLevel3,
                               Department = e.OrganizationLevel4,
                           })
                           .Distinct()
                           .ToArrayAsync(ct);

        var rawBusinessUnitErm = ermBusinessUnits
                                 .Where(e => e.Department != null && e.Line != null)
                                 .Map(h =>
                                 {
                                     var entity = RawBusinessUnit.Create(
                                         h.Department!.Id,
                                         h.Department.SolId,
                                         h.Department.ShortName,
                                         h.Department.Name,
                                         organizationLevel);

                                     if (!string.IsNullOrEmpty(h.Line?.Id))
                                     {
                                         entity.SetParent(BusinessUnitId.From(h.Line.Id));
                                     }

                                     return entity;
                                 })
                                 .ToArray();

        var rawBusinessUnitId =
            rawBusinessUnitErm.Map(e => e.Id)
                              .ToArray();

        var rawBusinessUnits =
            await dbContext.RawBusinessUnits
                           .Where(e =>
                               e.OrganizationLevel == organizationLevel)
                           .Where(e => rawBusinessUnitId.Contains(e.Id))
                           .ToArrayAsync(ct);

        _ = rawBusinessUnits
            .Join(
                rawBusinessUnitErm,
                existing => existing.Id,
                incoming => incoming.Id,
                (existing, incoming)
                    =>
                {
                    existing.Update(
                        incoming.BusinessUnitCode,
                        incoming.ShortName,
                        incoming.Name,
                        organizationLevel);

                    if (incoming.ParentId is not null)
                    {
                        existing.SetParent(incoming.ParentId.Value);
                    }

                    return existing;
                })
            .ToHashSet();

        var allExistingIds = (await dbContext.RawBusinessUnits
                                             .Where(e => rawBusinessUnitId.Contains(e.Id))
                                             .Select(e => e.Id)
                                             .ToArrayAsync(ct))
                             .ToHashSet();

        var newBusinessUnits =
            rawBusinessUnitErm.Where(b => !allExistingIds.Contains(b.Id))
                              .DistinctBy(b => b.Id);

        await dbContext.RawBusinessUnits.AddRangeAsync(newBusinessUnits, ct);

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("UpsertBusinessUnitDepartmentAsync completed.");
    }

    private static async Task UpsertBusinessUnitSegmentAsync(Dp2DbContext dbContext, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("UpsertBusinessUnitSegmentAsync started.");
        const string organizationLevel = EmployeeConstant.OrganizationLevel.Segment;

        var ermBusinessUnits =
            await dbContext.RawErmEmployees
                           .Where(e => e.OrganizationLevel7 != null)
                           .AsNoTracking()
                           .Select(e => new
                           {
                               Head = e.OrganizationLevel1,
                               Group = e.OrganizationLevel2,
                               Line = e.OrganizationLevel3,
                               Department = e.OrganizationLevel4,
                               Segment = e.OrganizationLevel7,
                           })
                           .Distinct()
                           .ToArrayAsync(ct);

        var rawBusinessUnitErm = ermBusinessUnits
                                 .Where(e => e.Department != null && e.Segment != null)
                                 .Map(h =>
                                 {
                                     var parentId =
                                         new[] { h.Department?.Id, h.Line?.Id, h.Group?.Id, h.Head?.Id }
                                             .FirstOrDefault(id => !string.IsNullOrEmpty(id));

                                     var entity = RawBusinessUnit.Create(
                                         h.Segment!.Id,
                                         h.Segment.SolId,
                                         h.Segment.ShortName,
                                         h.Segment.Name,
                                         organizationLevel);

                                     if (!string.IsNullOrEmpty(parentId))
                                     {
                                         entity.SetParent(BusinessUnitId.From(parentId));
                                     }

                                     return entity;
                                 })
                                 .ToArray();

        var rawBusinessUnitId =
            rawBusinessUnitErm.Map(e => e.Id)
                              .ToArray();

        var rawBusinessUnits =
            await dbContext.RawBusinessUnits
                           .Where(e =>
                               e.OrganizationLevel == organizationLevel)
                           .Where(e => rawBusinessUnitId.Contains(e.Id))
                           .ToArrayAsync(ct);

        _ = rawBusinessUnits
            .Join(
                rawBusinessUnitErm,
                existing => existing.Id,
                incoming => incoming.Id,
                (existing, incoming)
                    =>
                {
                    existing.Update(
                        incoming.BusinessUnitCode,
                        incoming.ShortName,
                        incoming.Name,
                        organizationLevel);

                    if (incoming.ParentId is not null)
                    {
                        existing.SetParent(incoming.ParentId.Value);
                    }

                    return existing;
                })
            .ToHashSet();

        var allExistingIds = (await dbContext.RawBusinessUnits
                                             .Where(e => rawBusinessUnitId.Contains(e.Id))
                                             .Select(e => e.Id)
                                             .ToArrayAsync(ct))
                             .ToHashSet();

        var newBusinessUnits =
            rawBusinessUnitErm.Where(b => !allExistingIds.Contains(b.Id))
                              .DistinctBy(b => b.Id);

        await dbContext.RawBusinessUnits.AddRangeAsync(newBusinessUnits, ct);

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("UpsertBusinessUnitSegmentAsync completed.");
    }

    private static async Task UpsertBusinessUnitCenterAsync(Dp2DbContext dbContext, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("UpsertBusinessUnitCenterAsync started.");
        const string organizationLevel = EmployeeConstant.OrganizationLevel.Center;

        var ermBusinessUnits =
            await dbContext.RawErmEmployees
                           .Where(e => e.OrganizationLevel5 != null)
                           .AsNoTracking()
                           .Select(e => new
                           {
                               Department = e.OrganizationLevel4,
                               Center = e.OrganizationLevel5,
                           })
                           .Distinct()
                           .ToArrayAsync(ct);

        var rawBusinessUnitErm = ermBusinessUnits
                                 .Where(e => e.Department != null && e.Center != null)
                                 .Map(h =>
                                 {
                                     var entity = RawBusinessUnit.Create(
                                         h.Center!.Id,
                                         h.Center.SolId,
                                         h.Center.ShortName,
                                         h.Center.Name,
                                         organizationLevel);

                                     if (!string.IsNullOrEmpty(h.Department?.Id))
                                     {
                                         entity.SetParent(BusinessUnitId.From(h.Department.Id));
                                     }

                                     return entity;
                                 })
                                 .ToArray();

        var rawBusinessUnitId =
            rawBusinessUnitErm.Map(e => e.Id)
                              .ToArray();

        var rawBusinessUnits =
            await dbContext.RawBusinessUnits
                           .Where(e =>
                               e.OrganizationLevel == organizationLevel)
                           .Where(e => rawBusinessUnitId.Contains(e.Id))
                           .ToArrayAsync(ct);

        _ = rawBusinessUnits
            .Join(
                rawBusinessUnitErm,
                existing => existing.Id,
                incoming => incoming.Id,
                (existing, incoming)
                    =>
                {
                    existing.Update(
                        incoming.BusinessUnitCode,
                        incoming.ShortName,
                        incoming.Name,
                        organizationLevel);

                    if (incoming.ParentId is not null)
                    {
                        existing.SetParent(incoming.ParentId.Value);
                    }

                    return existing;
                })
            .ToHashSet();

        var allExistingIds = (await dbContext.RawBusinessUnits
                                             .Where(e => rawBusinessUnitId.Contains(e.Id))
                                             .Select(e => e.Id)
                                             .ToArrayAsync(ct))
                             .ToHashSet();

        var newBusinessUnits =
            rawBusinessUnitErm.Where(b => !allExistingIds.Contains(b.Id))
                              .DistinctBy(b => b.Id);

        await dbContext.RawBusinessUnits.AddRangeAsync(newBusinessUnits, ct);

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("UpsertBusinessUnitCenterAsync completed.");
    }

    private static async Task UpsertBusinessUnitZoneAsync(Dp2DbContext dbContext, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("UpsertBusinessUnitZoneAsync started.");
        const string organizationLevel = EmployeeConstant.OrganizationLevel.Zone;

        var ermBusinessUnits =
            await dbContext.RawErmEmployees
                           .Where(e => e.OrganizationLevel6 != null)
                           .AsNoTracking()
                           .Select(e => new
                           {
                               Department = e.OrganizationLevel4,
                               Zone = e.OrganizationLevel6,
                           })
                           .Distinct()
                           .ToArrayAsync(ct);

        var rawBusinessUnitErm = ermBusinessUnits
                                 .Where(e => e.Department != null && e.Zone != null)
                                 .Map(h =>
                                 {
                                     var entity = RawBusinessUnit.Create(
                                         h.Zone!.Id,
                                         h.Zone.SolId,
                                         h.Zone.ShortName,
                                         h.Zone.Name,
                                         organizationLevel);

                                     if (!string.IsNullOrEmpty(h.Department?.Id))
                                     {
                                         entity.SetParent(BusinessUnitId.From(h.Department.Id));
                                     }

                                     return entity;
                                 })
                                 .ToArray();

        var rawBusinessUnitId =
            rawBusinessUnitErm.Map(e => e.Id)
                              .ToArray();

        var rawBusinessUnits =
            await dbContext.RawBusinessUnits
                           .Where(e =>
                               e.OrganizationLevel == organizationLevel)
                           .Where(e => rawBusinessUnitId.Contains(e.Id))
                           .ToArrayAsync(ct);

        _ = rawBusinessUnits
            .Join(
                rawBusinessUnitErm,
                existing => existing.Id,
                incoming => incoming.Id,
                (existing, incoming)
                    =>
                {
                    existing.Update(
                        incoming.BusinessUnitCode,
                        incoming.ShortName,
                        incoming.Name,
                        organizationLevel);

                    if (incoming.ParentId is not null)
                    {
                        existing.SetParent(incoming.ParentId.Value);
                    }

                    return existing;
                })
            .ToHashSet();

        var allExistingIds = (await dbContext.RawBusinessUnits
                                             .Where(e => rawBusinessUnitId.Contains(e.Id))
                                             .Select(e => e.Id)
                                             .ToArrayAsync(ct))
                             .ToHashSet();

        var newBusinessUnits =
            rawBusinessUnitErm.Where(b => !allExistingIds.Contains(b.Id))
                              .DistinctBy(b => b.Id);

        await dbContext.RawBusinessUnits.AddRangeAsync(newBusinessUnits, ct);

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("UpsertBusinessUnitZoneAsync completed.");
    }

    private static async Task UpsertBusinessUnitBranchAsync(Dp2DbContext dbContext, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("UpsertBusinessUnitBranchAsync started.");
        const string organizationLevel = EmployeeConstant.OrganizationLevel.Branch;

        var ermBusinessUnits =
            await dbContext.RawErmEmployees
                           .Where(e => e.OrganizationLevel8 != null)
                           .AsNoTracking()
                           .Select(e => new
                           {
                               Zone = e.OrganizationLevel6,
                               Branch = e.OrganizationLevel8,
                           })
                           .Distinct()
                           .ToArrayAsync(ct);

        var rawBusinessUnitErm = ermBusinessUnits
                                 .Where(e => e.Zone != null && e.Branch != null)
                                 .Map(h =>
                                 {
                                     var entity = RawBusinessUnit.Create(
                                         h.Branch!.Id,
                                         h.Branch.SolId,
                                         h.Branch.ShortName,
                                         h.Branch.Name,
                                         organizationLevel);

                                     if (!string.IsNullOrEmpty(h.Zone?.Id))
                                     {
                                         entity.SetParent(BusinessUnitId.From(h.Zone.Id));
                                     }

                                     return entity;
                                 })
                                 .ToArray();

        var rawBusinessUnitId =
            rawBusinessUnitErm.Map(e => e.Id)
                              .ToArray();

        var rawBusinessUnits =
            await dbContext.RawBusinessUnits
                           .Where(e =>
                               e.OrganizationLevel == organizationLevel)
                           .Where(e => rawBusinessUnitId.Contains(e.Id))
                           .ToArrayAsync(ct);

        _ = rawBusinessUnits
            .Join(
                rawBusinessUnitErm,
                existing => existing.Id,
                incoming => incoming.Id,
                (existing, incoming)
                    =>
                {
                    existing.Update(
                        incoming.BusinessUnitCode,
                        incoming.ShortName,
                        incoming.Name,
                        organizationLevel);

                    if (incoming.ParentId is not null)
                    {
                        existing.SetParent(incoming.ParentId.Value);
                    }

                    return existing;
                })
            .ToHashSet();

        var allExistingIds = (await dbContext.RawBusinessUnits
                                             .Where(e => rawBusinessUnitId.Contains(e.Id))
                                             .Select(e => e.Id)
                                             .ToArrayAsync(ct))
                             .ToHashSet();

        var newBusinessUnits =
            rawBusinessUnitErm.Where(b => !allExistingIds.Contains(b.Id))
                              .DistinctBy(b => b.Id);

        await dbContext.RawBusinessUnits.AddRangeAsync(newBusinessUnits, ct);

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("UpsertBusinessUnitBranchAsync completed.");
    }

    private static async Task SetDefaultBusinessUnitAsync(Dp2DbContext dbContext, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("SetDefaultBusinessUnitAsync started.");
        var defaultBusinessUnit =
            await dbContext.RawBusinessUnits
                           .FirstOrDefaultAsync(
                               b => b.Id == BusinessUnitId.From(DefaultBusinessUnitId),
                               ct);

        if (defaultBusinessUnit is null)
        {
            defaultBusinessUnit = RawBusinessUnit.Create(
                DefaultBusinessUnitId,
                string.Empty,
                "ลาออก หรือย้าย หรือไม่ใช้งาน",
                "ลาออก หรือย้าย หรือไม่ใช้งาน",
                EmployeeConstant.OrganizationLevel.None);

            await dbContext.RawBusinessUnits.AddAsync(defaultBusinessUnit, ct);

            await dbContext.SaveChangesAsync(ct);
        }

        logger.LogInformation("SetDefaultBusinessUnitAsync completed.");
    }

    private static async Task UpsertEmployeeAsync(Dp2DbContext dbContext, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("UpsertEmployeeAsync started.");
        var ermEmployees =
            await dbContext.RawErmEmployees
                           .Select(e => new
                           {
                               e.EmployeeCode,
                               e.CitizenCardId,
                               e.Title,
                               e.FirstName,
                               e.LastName,
                               e.BirthDate,
                               e.Email,
                               e.ActingPosition,
                           })
                           .Distinct()
                           .AsNoTracking()
                           .ToArrayAsync(ct);

        var rawEmployees = ermEmployees
                           .GroupBy(e => e.EmployeeCode)
                           .Select(g => g.OrderBy(e => e.ActingPosition == EmployeeConstant.Acting.Primary ? 0 : 1).First())
                           .Select(e =>
                           {
                               var entity =
                                   RawEmployee
                                       .Create(
                                           e.EmployeeCode,
                                           e.Title,
                                           e.FirstName,
                                           e.LastName,
                                           e.Email)
                                       .SetCitizenCardId(e.CitizenCardId);

                               if (e.BirthDate.TryParseFlexible(out var birthDate))
                               {
                                   entity.SetBirthDate(birthDate);
                               }

                               return entity;
                           }).ToArray();

        var employeeCodes = rawEmployees.Select(e => e.Id).ToArray();

        var existingEmployees =
            await dbContext.RawEmployees
                           .Where(e => employeeCodes.Contains(e.Id))
                           .ToListAsync(ct);

        _ = existingEmployees
            .Join(
                rawEmployees,
                existing => existing.Id,
                incoming => incoming.Id,
                (existing, incoming) =>
                {
                    existing.Update(
                        incoming.Title,
                        incoming.FirstName,
                        incoming.LastName,
                        incoming.Email);

                    existing.SetCitizenCardId(incoming.CitizenCardId);

                    if (incoming.BirthDate is not null)
                    {
                        existing.SetBirthDate(incoming.BirthDate.Value);
                    }

                    return existing;
                })
            .ToHashSet();

        var newEmployees = rawEmployees
                           .Where(e => existingEmployees.All(ee => ee.Id != e.Id))
                           .Distinct();

        await dbContext.RawEmployees.AddRangeAsync(newEmployees, ct);

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("UpsertEmployeeAsync completed.");
    }

    private static async Task UpsertEmployeePositionAsync(Dp2DbContext dbContext, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("UpsertEmployeePositionAsync started.");
        var ermEmployeePositions =
            await dbContext.RawErmEmployees
                           .AsNoTracking()
                           .ToArrayAsync(ct);

        var employeePositions = ermEmployeePositions
                                .Select(e =>
                                {
                                    var mapBusinessUnitId = e.OrganizationLevel switch
                                    {
                                        EmployeeConstant.OrganizationLevel.Head => e.OrganizationLevel1?.Id,
                                        EmployeeConstant.OrganizationLevel.Group => e.OrganizationLevel2?.Id,
                                        EmployeeConstant.OrganizationLevel.Line => e.OrganizationLevel3?.Id,
                                        EmployeeConstant.OrganizationLevel.Department => e.OrganizationLevel4?.Id,
                                        EmployeeConstant.OrganizationLevel.Center => e.OrganizationLevel5?.Id,
                                        EmployeeConstant.OrganizationLevel.Zone => e.OrganizationLevel6?.Id,
                                        EmployeeConstant.OrganizationLevel.Segment => e.OrganizationLevel7?.Id,
                                        EmployeeConstant.OrganizationLevel.Branch => e.OrganizationLevel8?.Id,
                                        _ => null,
                                    };

                                    var businessUnitId = mapBusinessUnitId ?? DefaultBusinessUnitId;

                                    var entity =
                                        RawEmployeePosition.Create(
                                            e.EmployeeCode,
                                            e.PositionId,
                                            businessUnitId,
                                            e.EmployeeType,
                                            e.ActingPosition);

                                    if (!string.IsNullOrEmpty(e.ManagerEmpId) &&
                                        e.ManagerEmpId != "00000")
                                    {
                                        entity.SetManager(EmployeeCode.From(e.ManagerEmpId));
                                    }

                                    return entity;
                                })
                                .ToArray();

        var rawEmployeePositions = await dbContext.RawEmployeePositions
                                                  .ToListAsync(ct);

        _ = rawEmployeePositions
            .Join(
                employeePositions,
                existing => new { existing.EmployeeCode, existing.PositionId, existing.BusinessUnitId },
                incoming => new { incoming.EmployeeCode, incoming.PositionId, incoming.BusinessUnitId },
                (existing, incoming) =>
                {
                    existing.SetEmployeeType(incoming.EmployeeType);

                    existing.SetManager(incoming.ManagerEmployeeCode ?? null);

                    return existing;
                })
            .ToHashSet();

        var rawEmployeePositionKeys = rawEmployeePositions
                                      .Select(e => new { e.EmployeeCode, e.PositionId, e.BusinessUnitId, e.Acting })
                                      .Distinct()
                                      .ToArray();

        var newEmployeePositions =
            employeePositions.ExceptBy(
                                 rawEmployeePositionKeys,
                                 e => new { e.EmployeeCode, e.PositionId, e.BusinessUnitId, e.Acting })
                             .Distinct();

        await dbContext.RawEmployeePositions.AddRangeAsync(newEmployeePositions, ct);

        var employeePositionKeys = employeePositions
                                   .Select(e => new { e.EmployeeCode, e.PositionId, e.BusinessUnitId, e.Acting })
                                   .Distinct();

        var removeEmployeePositions =
            rawEmployeePositions.ExceptBy(
                                    employeePositionKeys,
                                    e => new { e.EmployeeCode, e.PositionId, e.BusinessUnitId, e.Acting })
                                .Distinct();

        dbContext.RawEmployeePositions.RemoveRange(removeEmployeePositions);

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("UpsertEmployeePositionAsync completed.");
    }

    private static async Task UpsertSuUserAsync(Dp2DbContext dbContext, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("UpsertSuUserAsync started.");
        var ermEmployeePositions =
            await dbContext.RawEmployeePositions
                           .Where(e => e.Acting == EmployeeConstant.Acting.Primary)
                           .Select(e => new
                           {
                               e.EmployeeCode,
                               e.PositionId,
                           })
                           .AsNoTracking()
                           .ToArrayAsync(ct);

        var suUsers =
            await dbContext.SuUsers
                           .Include(u => u.Roles)
                           .ToListAsync(ct);

        // Inactivate SuUsers
        var employeeCodes =
            ermEmployeePositions.Select(e => e.EmployeeCode)
                                .ToArray();

        _ = suUsers.ExceptBy(
                       employeeCodes,
                       e => e.EmployeeCode)
                   .Iter(e => e.Inactivate());

        var roleDefault = await dbContext.SuRoles
                                         .FirstOrDefaultAsync(x => x.Code == RoleCode.From("U01"), ct);

        // Update existing SuUsers
        _ = suUsers
            .Join(
                ermEmployeePositions,
                existing => existing.EmployeeCode,
                incoming => incoming.EmployeeCode,
                (existing, incoming) =>
                {
                    existing.SetIsActive(incoming.PositionId != PositionId.From(DefaultPositionId));

                    // Add default role if user has no roles
                    if (roleDefault is not null &&
                        (existing.Roles is null || existing.Roles.Count == 0))
                    {
                        existing.AddRole(roleDefault);
                    }

                    return existing;
                })
            .ToHashSet();

        // Insert new SuUsers
        var userEmployeeCodes = suUsers.Select(e => e.EmployeeCode).ToArray();

        var newSuUsers =
            ermEmployeePositions
                .Where(e => userEmployeeCodes.All(ec => ec != e.EmployeeCode))
                .Select(e =>
                {
                    var user = SuUser.Create(e.EmployeeCode);
                    user.SetIsActive(e.PositionId != PositionId.From(DefaultPositionId));

                    return user;
                })
                .ToList();

        if (roleDefault is not null)
        {
            newSuUsers.Iter(x => x.AddRole(roleDefault));
        }

        await dbContext.SuUsers.AddRangeAsync(newSuUsers, ct);

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("UpsertSuUserAsync completed.");
    }

    private static async Task UpdateInRefCodeASync(Dp2DbContext dbContext, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("UpdateInRefCodeASync started.");
        var rawEmployeePositions =
            await dbContext.RawEmployeePositions
                           .Include(e => e.Position)
                           .Include(e => e.BusinessUnit)
                           .Where(e => e.PositionId != PositionId.From(DefaultPositionId))
                           .ToListAsync(ct);

        //---------------กรรมการธนาคาร--------------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("กรรมการธนาคาร"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp001));

        //---------------กรรมการผู้จัดการ---------------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("กรรมการผู้จัดการ"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp002));

        //---------------รองกรรมการผู้จัดการ-------------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("รองกรรมการผู้จัดการ"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp003));

        //------------ผู้อำนวยการศูนย์ข้อมูลอสังหาริมทรัพย์--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้อำนวยการศูนย์ข้อมูลอสังหาริมทรัพย์"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp005));

        //--------------ผู้ช่วยกรรมการผู้จัดการ -------------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้ช่วยกรรมการผู้จัดการ"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp006));

        //--------------ผู้ช่วยกรรมการผู้จัดการ สายงานสนับสนุน-----------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้ช่วยกรรมการผู้จัดการ") &&
                        e.BusinessUnit.Name.Equals("สายงานสนับสนุน"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp024));

        //--------------ผู้ช่วยกรรมการผู้จัดการ สายงานสื่อสารการตลาดและภาพลักษณ์องค์กร-----------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้ช่วยกรรมการผู้จัดการ") &&
                        e.BusinessUnit.Name.Equals("สายงานสื่อสารการตลาดและภาพลักษณ์องค์กร"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp025));

        //------------ผู้อำนวยการฝ่ายสื่อสารองค์กร--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้อำนวยการฝ่าย") &&
                        e.BusinessUnit.Name.Equals("ฝ่ายสื่อสารองค์กร"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp007));

        //------------ผู้อำนวยการฝ่ายสื่อสารการตลาด--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้อำนวยการฝ่าย") &&
                        e.BusinessUnit.Name.Equals("ฝ่ายสื่อสารการตลาด"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp028));

        //------------ผู้อำนวยการฝ่ายจัดหาและการพัสดุ--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้อำนวยการฝ่าย") &&
                        e.BusinessUnit.Name.Equals("ฝ่ายจัดหาและการพัสดุ"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp008));

        //------------ผู้อำนวยการฝ่าย อื่นๆ--------------
        var departmentNotSet =
            new[]
            {
                "ฝ่ายจัดหาและการพัสดุ",
                "ฝ่ายสื่อสารองค์กร",
                "ฝ่ายบริหารสำนักงานและกิจการสาขา",
                "ฝ่ายบริหาร NPA",
                "ฝ่ายบริหารหนี้ภูมิภาค 1",
                "ฝ่ายบริหารหนี้ภูมิภาค 2",
                "ฝ่ายสื่อสารการตลาด",
            };
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้อำนวยการฝ่าย") &&
                        !departmentNotSet.Contains(e.BusinessUnit.Name))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp009));

        //------------ผู้อำนวยการสำนัก--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้อำนวยการสำนัก"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp010));

        //------------ผู้ช่วยผู้อำนวยการฝ่ายจัดหาและการพัสดุ--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้ช่วยผู้อำนวยการฝ่าย") &&
                        e.BusinessUnit.Name.Equals("ฝ่ายจัดหาและการพัสดุ"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp011));

        //------------ผู้ช่วยผู้อำนวยการฝ่ายการบัญชี--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้ช่วยผู้อำนวยการฝ่าย") &&
                        e.BusinessUnit.Name.Equals("ฝ่ายการบัญชี"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp011));

        //------------หัวหน้าส่วนจัดหาทั่วไป ฝ่ายจัดหาและการพัสดุ--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("หัวหน้าส่วน") &&
                        e.BusinessUnit.Name.Equals("ส่วนจัดหาทั่วไป"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp012));

        //------------หัวหน้าส่วนจัดหาระบบเทคโนโลยีสารสนเทศฯ ฝ่ายจัดหาและการพัสดุ--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("หัวหน้าส่วน") &&
                        e.BusinessUnit.Name.Equals("ส่วนจัดหาระบบเทคโนโลยีสารสนเทศฯ"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp013));

        //------------ผู้อำนวยการภาค--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้อำนวยการภาค"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp014));

        //------------ผู้อำนวยการศูนย์วิเคราะห์สินเชื่อ--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้อำนวยการศูนย์วิเคราะห์สินเชื่อ"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp015));

        //------------ผู้จัดการเขต--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้จัดการเขต"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp016));

        //------------ผู้จัดการสาขา--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้จัดการสาขา"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp017));

        //------------ผู้จัดการสาขาอาวุโส--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้จัดการสาขาอาวุโส"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp018));

        //------------ผู้จัดการศูนย์วิเคราะห์สินเชื่อ--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้จัดการ DEC"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp019));

        //------------ผู้อำนวยการฝ่ายบริหารสำนักงานและกิจการสาขา--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้อำนวยการฝ่าย") &&
                        e.BusinessUnit.Name.Equals("ฝ่ายบริหารสำนักงานและกิจการสาขา"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp020));

        //------------ผู้อำนวยการฝ่ายบริหาร NPA--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้อำนวยการฝ่าย") &&
                        e.BusinessUnit.Name.Equals("ฝ่ายบริหาร NPA"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp021));

        //------------ฝ่ายบริหารหนี้ภูมิภาค 1--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้อำนวยการฝ่าย") &&
                        e.BusinessUnit.Name.Equals("ฝ่ายบริหารหนี้ภูมิภาค 1"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp026));

        //------------ฝ่ายบริหารหนี้ภูมิภาค 2--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("ผู้อำนวยการฝ่าย") &&
                        e.BusinessUnit.Name.Equals("ฝ่ายบริหารหนี้ภูมิภาค 2"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp027));

        //------------หัวหน้าส่วน ฝ่ายการบัญชี--------------
        rawEmployeePositions
            .Where(e => e.Position.Name.Equals("หัวหน้าส่วน") &&
                        e.BusinessUnit.Name.Equals("ส่วนบัญชีค่าใช้จ่าย"))
            .Iter(e => e.Position.SetInRefCode(InRefCodeConstant.Bp023));

        dbContext.UpdateRange(rawEmployeePositions);

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("UpdateInRefCodeASync completed.");
    }

    private static async Task RefreshMaterializedViewAsync(Dp2DbContext dbContext, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("RefreshMaterializedViewAsync started.");
        var sql = """
                  REFRESH MATERIALIZED VIEW "Raws".raw_employee_view WITH DATA;
                  """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, ct);
        logger.LogInformation("RefreshMaterializedViewAsync completed.");
    }
}