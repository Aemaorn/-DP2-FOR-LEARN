namespace GHB.DP2.Application.CommandHandler;

using GHB.DP2.Domain.Raws;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Services.ErmEmployee;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class SyncErmEmployeeCommand : ICommand;

public class SyncErmEmployeeHandler : ICommandHandler<SyncErmEmployeeCommand>
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<SyncErmEmployeeHandler> logger;

    public SyncErmEmployeeHandler(
        ILogger<SyncErmEmployeeHandler> logger, IServiceProvider serviceProvider)
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
    }

    public async Task ExecuteAsync(SyncErmEmployeeCommand command, CancellationToken ct)
    {
        this.logger.LogInformation("Starting synchronization of ERM employees.");

        await using var scope = this.serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Dp2DbContext>();
        var ermEmployeeService = scope.ServiceProvider.GetRequiredService<IErmEmployeeService>();

        var rawErmEmployeeAny = await dbContext.RawErmEmployees.AnyAsync(ct);

        if (rawErmEmployeeAny)
        {
            dbContext.RawErmEmployees.RemoveRange(dbContext.RawErmEmployees);
        }

        var ermEmployeesResult =
            await ermEmployeeService.GetErmEmployeesAsync(ct);

        var ermEmployees = ermEmployeesResult.ToArray();

        this.logger.LogInformation("Retrieved {Count} employees from ERM service.", ermEmployees.Length);

        var entities =
            ermEmployees
                .Map(MapToEntity)
                .ToArray();

        await dbContext.RawErmEmployees.AddRangeAsync(entities, ct);

        this.logger.LogInformation("Saving {Count} employees to the database.", entities.Length);

        await dbContext.SaveChangesAsync(ct);

        this.logger.LogInformation("Synchronization of ERM employees completed.");
    }

    private static RawErmEmployee MapToEntity(ErmEmployeeResult employee)
    {
        var result = RawErmEmployee.Create(
            employee.Id,
            employee.Title,
            employee.EmployeeCode,
            employee.FirstName,
            employee.LastName,
            employee.Email);

        result.SetEmployeeInfo(
            employee.Grade,
            employee.EmployeeType,
            employee.CitizenCardId,
            employee.BirthDate);

        result.SetPositionInfo(
            employee.PositionId,
            employee.PositionName,
            employee.ActingPosition,
            employee.ManagerEmployeeId);

        result.SetOrganizationLevel(employee.OrganizationLevel);

        result.SetOrganizationLevel1(
            employee.OrganizationObjectId1,
            employee.OrganizationUnitSolidId1,
            employee.OrganizationShortName1,
            employee.OrganizationUnitName1);

        result.SetOrganizationLevel2(
            employee.OrganizationObjectId2,
            employee.OrganizationUnitSolidId2,
            employee.OrganizationShortName2,
            employee.OrganizationUnitName2);

        result.SetOrganizationLevel3(
            employee.OrganizationObjectId3,
            employee.OrganizationUnitSolidId3,
            employee.OrganizationShortName3,
            employee.OrganizationUnitName3);

        result.SetOrganizationLevel4(
            employee.OrganizationObjectId4,
            employee.OrganizationUnitSolidId4,
            employee.OrganizationShortName4,
            employee.OrganizationUnitName4);

        result.SetOrganizationLevel5(
            employee.OrganizationObjectId5,
            employee.OrganizationUnitSolidId5,
            employee.OrganizationShortName5,
            employee.OrganizationUnitName5);

        result.SetOrganizationLevel6(
            employee.OrganizationObjectId6,
            employee.OrganizationUnitSolidId6,
            employee.OrganizationShortName6,
            employee.OrganizationUnitName6);

        result.SetOrganizationLevel7(
            employee.OrganizationObjectId7,
            employee.OrganizationUnitSolidId7,
            employee.OrganizationShortName7,
            employee.OrganizationUnitName7);

        result.SetOrganizationLevel8(
            employee.OrganizationObjectId8,
            employee.OrganizationUnitSolidId8,
            employee.OrganizationShortName8,
            employee.OrganizationUnitName8);

        result.SetOrganizationLevel9(
            employee.OrganizationObjectId9,
            employee.OrganizationUnitSolidId9,
            employee.OrganizationShortName9,
            employee.OrganizationUnitName9);

        result.SetStartDate(employee.StartDate);
        result.SetStopDate(employee.StopDate);
        result.SetLastAction(employee.LastActionDate);
        result.SetDataDate(employee.DataDate);

        return result;
    }
}