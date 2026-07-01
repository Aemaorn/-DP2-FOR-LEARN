namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpTorDraftQualificationsId
{
    public static PpTorDraftQualificationsId New() => From(Guid.CreateVersion7());
}

public class PpTorDraftQualifications : AuditableEntity<PpTorDraftQualificationsId>
{
    public override PpTorDraftQualificationsId Id { get; init; }

    public int? Sequence { get; set; }

    public string? Description { get; set; }

    public virtual PpTorDraft PpTorDraft { get; init; }
}