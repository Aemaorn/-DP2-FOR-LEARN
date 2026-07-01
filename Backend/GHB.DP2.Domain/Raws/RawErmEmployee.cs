namespace GHB.DP2.Domain.Raws;

using Codehard.Common.DomainModel;
using Vogen;

public record OrganizationLevel(
    string Id,
    string SolId,
    string ShortName,
    string Name);

[ValueObject<int>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct RawErmEmployeeId;

public class RawErmEmployee : Entity<RawErmEmployeeId>
{
    public override RawErmEmployeeId Id { get; init; }

    public string Title { get; init; }

    public string EmployeeCode { get; init; }

    public string FirstName { get; init; }

    public string LastName { get; init; }

    public string CitizenCardId { get; private set; }

    public string? BirthDate { get; private set; }

    public string Grade { get; private set; }

    public string EmployeeType { get; private set; }

    public string PositionId { get; private set; }

    public string PositionName { get; private set; }

    public string ActingPosition { get; private set; }

    public string ManagerEmpId { get; private set; }

    public string Email { get; private set; }

    public string OrganizationLevel { get; private set; }

    public OrganizationLevel? OrganizationLevel1 { get; private set; }

    public OrganizationLevel? OrganizationLevel2 { get; private set; }

    public OrganizationLevel? OrganizationLevel3 { get; private set; }

    public OrganizationLevel? OrganizationLevel4 { get; private set; }

    public OrganizationLevel? OrganizationLevel5 { get; private set; }

    public OrganizationLevel? OrganizationLevel6 { get; private set; }

    public OrganizationLevel? OrganizationLevel7 { get; private set; }

    public OrganizationLevel? OrganizationLevel8 { get; private set; }

    public OrganizationLevel? OrganizationLevel9 { get; private set; }

    public DateTimeOffset? StartDate { get; private set; }

    public DateTimeOffset? StopDate { get; private set; }

    public DateTimeOffset? LastAction { get; private set; }

    public DateTimeOffset? DataDate { get; private set; }

    public RawErmEmployee SetEmployeeInfo(
        string grade,
        string employeeType,
        string citizenCardId,
        string? birthDate = default)
    {
        this.Grade = grade;
        this.EmployeeType = employeeType;
        this.CitizenCardId = citizenCardId;
        this.BirthDate = birthDate;

        return this;
    }

    public RawErmEmployee SetPositionInfo(
        string positionId,
        string positionName,
        string actingPosition,
        string managerEmpId)
    {
        this.PositionId = positionId;
        this.PositionName = positionName;
        this.ActingPosition = actingPosition;
        this.ManagerEmpId = managerEmpId;

        return this;
    }

    public RawErmEmployee SetOrganizationLevel(string level)
    {
        this.OrganizationLevel = level;

        return this;
    }

    public RawErmEmployee SetOrganizationLevel1(
        string id,
        string solId,
        string shortName,
        string name)
    {
        this.OrganizationLevel1 =
            new OrganizationLevel(
                id,
                solId,
                shortName,
                name);

        return this;
    }

    public RawErmEmployee SetOrganizationLevel2(
        string id,
        string solId,
        string shortName,
        string name)
    {
        this.OrganizationLevel2 =
            new OrganizationLevel(
                id,
                solId,
                shortName,
                name);

        return this;
    }

    public RawErmEmployee SetOrganizationLevel3(
        string id,
        string solId,
        string shortName,
        string name)
    {
        this.OrganizationLevel3 =
            new OrganizationLevel(
                id,
                solId,
                shortName,
                name);

        return this;
    }

    public RawErmEmployee SetOrganizationLevel4(
        string id,
        string solId,
        string shortName,
        string name)
    {
        this.OrganizationLevel4 =
            new OrganizationLevel(
                id,
                solId,
                shortName,
                name);

        return this;
    }

    public RawErmEmployee SetOrganizationLevel5(
        string id,
        string solId,
        string shortName,
        string name)
    {
        this.OrganizationLevel5 =
            new OrganizationLevel(
                id,
                solId,
                shortName,
                name);

        return this;
    }

    public RawErmEmployee SetOrganizationLevel6(
        string id,
        string solId,
        string shortName,
        string name)
    {
        this.OrganizationLevel6 =
            new OrganizationLevel(
                id,
                solId,
                shortName,
                name);

        return this;
    }

    public RawErmEmployee SetOrganizationLevel7(
        string id,
        string solId,
        string shortName,
        string name)
    {
        this.OrganizationLevel7 =
            new OrganizationLevel(
                id,
                solId,
                shortName,
                name);

        return this;
    }

    public RawErmEmployee SetOrganizationLevel8(
        string id,
        string solId,
        string shortName,
        string name)
    {
        this.OrganizationLevel8 =
            new OrganizationLevel(
                id,
                solId,
                shortName,
                name);

        return this;
    }

    public RawErmEmployee SetOrganizationLevel9(
        string id,
        string solId,
        string shortName,
        string name)
    {
        this.OrganizationLevel9 =
            new OrganizationLevel(
                id,
                solId,
                shortName,
                name);

        return this;
    }

    public RawErmEmployee SetStartDate(DateTimeOffset? startDate)
    {
        this.StartDate = startDate;

        return this;
    }

    public RawErmEmployee SetStopDate(DateTimeOffset? stopDate)
    {
        this.StopDate = stopDate;

        return this;
    }

    public RawErmEmployee SetLastAction(DateTimeOffset? lastAction)
    {
        this.LastAction = lastAction;

        return this;
    }

    public RawErmEmployee SetDataDate(DateTimeOffset? dataDate)
    {
        this.DataDate = dataDate;

        return this;
    }

    public static RawErmEmployee Create(
        int id,
        string title,
        string employeeCode,
        string firstName,
        string lastName,
        string email)
    {
        return new RawErmEmployee
        {
            Id = RawErmEmployeeId.From(id),
            Title = title,
            EmployeeCode = employeeCode,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
        };
    }
}