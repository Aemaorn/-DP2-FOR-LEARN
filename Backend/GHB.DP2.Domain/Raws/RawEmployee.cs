namespace GHB.DP2.Domain.Raws;

using Codehard.Common.DomainModel;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<string>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct EmployeeCode;

public class RawEmployee : Entity<EmployeeCode>
{
    public override EmployeeCode Id { get; init; }

    public string Title { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string FullName => $"{this.Title} {this.FirstName} {this.LastName}";

    public string Signature => $"{this.FirstName} {this.LastName}";

    public string CitizenCardId { get; private set; }

    public DateOnly? BirthDate { get; private set; }

    public string Email { get; private set; }

    public string? Remark { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public virtual ICollection<RawEmployeePosition> Positions { get; init; }

    public virtual IReadOnlyCollection<SuUser> Users { get; init; }

    public virtual RawEmployeeView? View { get; init; }

    public RawEmployee SetBirthDate(DateOnly birthDate)
    {
        this.BirthDate = birthDate;

        return this;
    }

    public RawEmployee SetCitizenCardId(string citizenCardId)
    {
        this.CitizenCardId = citizenCardId;

        return this;
    }

    public RawEmployee Update(
        string title,
        string firstName,
        string lastName,
        string email)
    {
        this.Title = title;
        this.FirstName = firstName;
        this.LastName = lastName;
        this.Email = email;
        this.UpdatedAt = DateTimeOffset.UtcNow;

        return this;
    }

    public static RawEmployee Create(
        string id,
        string title,
        string firstName,
        string lastName,
        string email)
    {
        return new RawEmployee
        {
            Id = EmployeeCode.From(id),
            Title = title,
            FirstName = firstName,
            LastName = lastName,
            CitizenCardId = string.Empty,
            BirthDate = DateOnly.MinValue,
            Email = email,
            Remark = null,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public RawEmployeePosition? PrimaryEmployeePosition =>
        this.Positions.FirstOrDefault(p =>
            p.Acting == EmployeeConstant.Acting.Primary);

    public RawBusinessUnit? PrimaryBusinessUnit =>
        this.Positions
            .Where(p => p.Acting == EmployeeConstant.Acting.Primary)
            .Select(p => p.BusinessUnit)
            .FirstOrDefault();

    public RawPosition? PrimaryPosition =>
        this.Positions.FirstOrDefault(p =>
                p.Acting == EmployeeConstant.Acting.Primary)?
            .Position;

    public string? PrimaryOrganizationLevel =>
        this.Positions
            .Where(p => p.Acting == EmployeeConstant.Acting.Primary)
            .Select(p => p.BusinessUnit.OrganizationLevel)
            .FirstOrDefault();

    public RawBusinessUnit? PrimaryDepartment =>
        this.Positions
            .Where(p => p.Acting == EmployeeConstant.Acting.Primary)
            .Select(p => p.BusinessUnit)
            .Select(GetPrimaryDepartment)
            .FirstOrDefault();

    public bool IsJorPor =>
        this.Positions
            .Where(p => p.Acting == EmployeeConstant.Acting.Primary)
            .Select(p => p.BusinessUnit)
            .Select(GetPrimaryDepartment)
            .Any(p => p.BusinessUnitCode == EmployeeConstant.DefaultJorPor.BusinessUnitCode);

    public bool IsGroupOperation =>
        this.Positions
            .Where(p => p.Acting == EmployeeConstant.Acting.Primary)
            .Select(p => p.BusinessUnit)
            .Any(this.TraverseIsGroupOperation);

    private bool TraverseIsGroupOperation(RawBusinessUnit businessUnit)
    {
        if (businessUnit.OrganizationLevel is EmployeeConstant.OrganizationLevel.Group)
        {
            return businessUnit.Id == BusinessUnitId.From(EmployeeConstant.DefaultGroupCode.Operations);
        }

        if (businessUnit.ParentId is null)
        {
            return false;
        }

        return this.TraverseIsGroupOperation(businessUnit.Parent);
    }

    private static RawBusinessUnit GetPrimaryDepartment(RawBusinessUnit rawBusinessUnit)
    {
        var bu = rawBusinessUnit;

        // Normal loop performs better than a recursive call
        // due to a call stack overhead.
        // Also, in this case it's not even easier to read.
        while (true)
        {
            // A Segment (ส่วน) directly under a Line (สายงาน) acts as its own
            // department, so it stops here instead of rolling up to the Line.
            if (bu.OrganizationLevel is EmployeeConstant.OrganizationLevel.Segment
                && bu.ParentId is not null
                && bu.Parent.OrganizationLevel is EmployeeConstant.OrganizationLevel.Line)
            {
                return bu;
            }

            if (bu.OrganizationLevel
                    is EmployeeConstant.OrganizationLevel.Branch or EmployeeConstant.OrganizationLevel.Zone or EmployeeConstant.OrganizationLevel.Department or EmployeeConstant.OrganizationLevel.Line or EmployeeConstant.OrganizationLevel.Group or EmployeeConstant.OrganizationLevel.Head
                || bu.ParentId is null)
            {
                return bu;
            }

            bu = bu.Parent;
        }
    }
}