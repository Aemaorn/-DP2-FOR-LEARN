namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpTorImpedimentId
{
    public static PpTorImpedimentId New() => From(Guid.CreateVersion7());
}

public class PpTorImpediment : AuditableEntity<PpTorImpedimentId>
{
    public override PpTorImpedimentId Id { get; init; }

    public int? Sequence { get; set; }

    public string? Description { get; set; }

    public decimal? ImpedimentValue { get; set; }

    public virtual PpTorDraft PpTorDraft { get; init; }
}
