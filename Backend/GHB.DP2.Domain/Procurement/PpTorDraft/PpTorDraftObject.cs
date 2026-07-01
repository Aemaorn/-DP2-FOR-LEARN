namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpTorDraftObjectId
{
    public static PpTorDraftObjectId New() => From(Guid.CreateVersion7());
}

public class PpTorDraftObject : AuditableEntity<PpTorDraftObjectId>
{
    public override PpTorDraftObjectId Id { get; init; }

    public int? Sequence { get; set; }

    public string? Description { get; set; }

    public virtual PpTorDraft PpTorDraft { get; init; }
}