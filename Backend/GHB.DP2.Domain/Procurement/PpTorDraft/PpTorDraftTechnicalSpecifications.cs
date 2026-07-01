namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpTorDraftTechnicalSpecificationsId
{
    public static PpTorDraftTechnicalSpecificationsId New() => From(Guid.CreateVersion7());
}

public class PpTorDraftTechnicalSpecifications : AuditableEntity<PpTorDraftTechnicalSpecificationsId>
{
    public override PpTorDraftTechnicalSpecificationsId Id { get; init; }

    public int? Sequence { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public int? Quantity { get; set; }

    public ParameterCode? UnitCode { get; set; }

    public virtual PpTorDraft PpTorDraft { get; init; }

    public virtual SuParameter? Unit { get; init; }
}