namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpTorDraftPaymentTermDetailId
{
    public static PpTorDraftPaymentTermDetailId New() => From(Guid.CreateVersion7());
}

public class PpTorDraftPaymentTermDetail : AuditableEntity<PpTorDraftPaymentTermDetailId>
{
    public override PpTorDraftPaymentTermDetailId Id { get; init; }

    public PpTorDraftPaymentTermId PpTorDraftPaymentTermId { get; init; }

    public int? TermNumber { get; set; }

    public decimal? Percent { get; set; }

    public int? Period { get; set; }

    public string? Description { get; set; }

    public virtual PpTorDraftPaymentTerm PpTorDraftPaymentTerm { get; init; }
}