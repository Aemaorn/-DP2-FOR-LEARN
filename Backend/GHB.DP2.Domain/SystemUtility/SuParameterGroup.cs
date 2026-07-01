namespace GHB.DP2.Domain.SystemUtility;

using GHB.DP2.Domain.Common;
using LanguageExt;
using Vogen;

[ValueObject<string>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct GroupCode
{
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct GroupId
{
    public static GroupId New() => From(Guid.CreateVersion7());
}

public partial class SuParameterGroup : AuditableEntity<GroupId>, IHasSoftDelete
{
    public override GroupId Id { get; init; }

    public GroupCode Code { get; private set; }

    public string Label { get; init; }

    public GroupId? ParentId { get; init; }

    public virtual SuParameterGroup? Parent { get; init; }

    public virtual IReadOnlyCollection<SuParameterGroup> Children { get; init; }

    public virtual IReadOnlyCollection<SuParameter> Parameters { get; init; }

    public Unit Update(GroupCode code)
    {
        this.Code = code;

        return unit;
    }
}
