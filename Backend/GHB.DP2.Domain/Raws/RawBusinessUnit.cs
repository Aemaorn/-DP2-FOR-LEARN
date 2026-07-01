namespace GHB.DP2.Domain.Raws;

using Codehard.Common.DomainModel;
using Vogen;

[ValueObject<string>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct BusinessUnitId;

public class RawBusinessUnit : Entity<BusinessUnitId>
{
    public override BusinessUnitId Id { get; init; }

    public string BusinessUnitCode { get; private set; }

    public BusinessUnitId? ParentId { get; private set; }

    public string ShortName { get; private set; }

    public string Name { get; private set; }

    public string OrganizationLevel { get; private set; }

    public string Value { get; init; }

    public string Value2 { get; init; }

    public string Value3 { get; init; }

    public int Level { get; private set; }

    public string? Remark { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public virtual RawBusinessUnit Parent { get; init; }

    public virtual ICollection<RawBusinessUnit> Children { get; init; }

    public static RawBusinessUnit Create(
        string id,
        string code,
        string shortName,
        string name,
        string organizationLevel)
    {
        return new RawBusinessUnit
        {
            Id = BusinessUnitId.From(id),
            BusinessUnitCode = code,
            ShortName = shortName,
            Name = name,
            OrganizationLevel = organizationLevel,
            Level = MapLevel(organizationLevel),
            Value = string.Empty,
            Value2 = string.Empty,
            Value3 = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public static RawBusinessUnit Create(
        string id,
        string code,
        string shortName,
        string name,
        string organizationLevel,
        string parentId)
    {
        return new RawBusinessUnit
        {
            Id = BusinessUnitId.From(id),
            ParentId = BusinessUnitId.From(parentId),
            BusinessUnitCode = code,
            ShortName = shortName,
            Name = name,
            OrganizationLevel = organizationLevel,
            Level = MapLevel(organizationLevel),
            Value = string.Empty,
            Value2 = string.Empty,
            Value3 = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public RawBusinessUnit SetParent(BusinessUnitId parentId)
    {
        this.ParentId = parentId;

        return this;
    }

    public RawBusinessUnit Update(
        string code,
        string shortName,
        string name,
        string organizationLevel)
    {
        this.BusinessUnitCode = code;
        this.ShortName = shortName;
        this.Name = name;
        this.OrganizationLevel = organizationLevel;
        this.Level = MapLevel(organizationLevel);

        this.UpdatedAt = DateTimeOffset.UtcNow;

        return this;
    }

    private static int MapLevel(string organizationLevel) => organizationLevel switch
    {
        Constants.EmployeeConstant.OrganizationLevel.Head => 1,
        Constants.EmployeeConstant.OrganizationLevel.Group => 2,
        Constants.EmployeeConstant.OrganizationLevel.Line => 3,
        Constants.EmployeeConstant.OrganizationLevel.Department => 4,
        Constants.EmployeeConstant.OrganizationLevel.Center => 5,
        Constants.EmployeeConstant.OrganizationLevel.Zone => 6,
        Constants.EmployeeConstant.OrganizationLevel.Segment => 7,
        Constants.EmployeeConstant.OrganizationLevel.Branch => 8,
        _ => 0,
    };

    /// <summary>
    /// Returns a sequence of business units from this node up to the root.
    /// The sequence starts with the current node and traverses upward through parents.
    /// </summary>
    public IEnumerable<RawBusinessUnit> PathToRoot()
    {
        // Return the current node
        yield return this;

        // Base case: if this is the root node (has no parent), we're done
        if (this.ParentId == null)
        {
            yield break;
        }

        // Recursive case: return all nodes in the parent's path
        foreach (var ancestor in this.Parent.PathToRoot())
        {
            yield return ancestor;
        }
    }

    public IEnumerable<RawBusinessUnit> Traverse()
    {
        yield return this;

        foreach (var child in this.Children)
        {
            foreach (var grandChild in child.Traverse())
            {
                yield return grandChild;
            }
        }
    }
}