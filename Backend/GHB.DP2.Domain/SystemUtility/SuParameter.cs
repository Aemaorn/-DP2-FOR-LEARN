namespace GHB.DP2.Domain.SystemUtility;

using GHB.DP2.Domain.Common;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ParameterId
{
    public static ParameterId New() => From(Guid.CreateVersion7());
}

[ValueObject<string>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ParameterCode;

public partial class SuParameter : AuditableEntity<ParameterId>, IHasSoftDelete
{
    public override ParameterId Id { get; init; }

    public ParameterId? ParentId { get; private set; }

    public virtual SuParameter? Parent { get; init; }

    public virtual IReadOnlyCollection<SuParameter> Children { get; init; }

    public GroupCode GroupCode { get; private set; }

    public ParameterCode Code { get; init; }

    public virtual SuParameterGroup Group { get; init; }

    public string Label { get; private set; }

    public int Sequence { get; private set; }

    public Dictionary<string, ParameterValue> Values { get; private set; }

    public bool IsActive { get; private set; }

    public Unit Update(
        GroupCode parentGroupCode,
        SuParameterGroup? subGroup,
        ParameterId? parentId,
        int sequence,
        string name,
        Dictionary<string, ParameterValue> parameters,
        bool isActive)
    {
        this.GroupCode = parentGroupCode;
        subGroup?.Update(parentGroupCode);
        this.ParentId = parentId;
        this.Sequence = sequence;
        this.Label = name;
        this.Values = parameters;
        this.IsActive = isActive;

        return unit;
    }

    public override string ToString()
    {
        return this.Label;
    }

    public static SuParameter Create(
        GroupCode groupCode,
        ParameterId? parentId,
        string code,
        string name,
        int sequence,
        Dictionary<string, ParameterValue> parameters,
        bool isActive)
    {
        return new SuParameter
        {
            Id = ParameterId.New(),
            ParentId = parentId,
            GroupCode = groupCode,
            Code = ParameterCode.From(code),
            Label = name,
            Sequence = sequence,
            Values = parameters,
            IsActive = isActive,
        };
    }
}

public record ParameterValue(int Sequence, object? Value);