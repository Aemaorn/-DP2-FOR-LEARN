namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpTorDraftFineRateId
{
    public static PpTorDraftFineRateId New() => From(Guid.CreateVersion7());
}

public class PpTorDraftFineRate : AuditableEntity<PpTorDraftFineRateId>
{
    public override PpTorDraftFineRateId Id { get; init; }

    public int? Sequence { get; set; }

    public string? Description { get; set; }

    public decimal? Rate { get; set; }

    public ParameterCode? PeriodTypeCode { get; set; }

    public ParameterCode? ConditionCode { get; set; }

    public string? ConditionOther { get; set; }

    public virtual PpTorDraft PpTorDraft { get; init; }

    public virtual SuParameter? PeriodType { get; init; }

    public virtual SuParameter? Condition { get; init; }
}