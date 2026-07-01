namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpTorDraftTechnicalPeriodDetailId
{
    public static PpTorDraftTechnicalPeriodDetailId New() => From(Guid.CreateVersion7());
}

public class PpTorDraftTechnicalPeriodDetail : AuditableEntity<PpTorDraftTechnicalPeriodDetailId>
{
    public override PpTorDraftTechnicalPeriodDetailId Id { get; init; }

    public PpTorDraftTechnicalPeriodId PpTorDraftTechnicalPeriodId { get; init; }

    public string? Branch { get; set; }

    public string? PersonalCount { get; set; }

    public DateTimeOffset? StartDate { get; set; }

    public DateTimeOffset? EndDate { get; set; }

    public virtual PpTorDraftTechnicalPeriod PpTorDraftTechnicalPeriod { get; init; }
}