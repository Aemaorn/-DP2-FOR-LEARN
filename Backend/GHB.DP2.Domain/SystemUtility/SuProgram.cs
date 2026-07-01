namespace GHB.DP2.Domain.SystemUtility;

using Codehard.Common.DomainModel;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ProgramId
{
    public static ProgramId New() => From(Guid.CreateVersion7());
}

public class SuProgram : Entity<ProgramId>
{
    public override ProgramId Id { get; init; }

    public ProgramId? ParentId { get; init; }

    public string Code { get; init; }

    public string Label { get; init; }

    public string? Path { get; init; }

    public int Sorting { get; init; }

    public bool IsActive { get; init; }

    public virtual SuProgram? Parent { get; init; }

    public virtual IReadOnlyCollection<SuProgram> Children { get; init; }
}