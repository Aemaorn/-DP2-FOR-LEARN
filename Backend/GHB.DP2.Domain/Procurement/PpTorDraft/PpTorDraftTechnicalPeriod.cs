namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpTorDraftTechnicalPeriodId
{
    public static PpTorDraftTechnicalPeriodId New() => From(Guid.CreateVersion7());
}

public class PpTorDraftTechnicalPeriod : AuditableEntity<PpTorDraftTechnicalPeriodId>
{
    public override PpTorDraftTechnicalPeriodId Id { get; init; }

    public int? Period { get; set; }

    public ParameterCode? PeriodTypeCode { get; set; }

    public ParameterCode? PeriodConditionCode { get; set; }

    public DateTimeOffset? StartDate { get; set; }

    public DateTimeOffset? EndDate { get; set; }

    public ParameterCode? DeliveryConditionCode { get; set; }

    public DateTimeOffset? DeliveryDate { get; set; }

    public virtual PpTorDraft PpTorDraft { get; init; }

    public virtual SuParameter? PeriodType { get; init; }

    public virtual SuParameter? PeriodCondition { get; init; }

    public virtual SuParameter? DeliveryCondition { get; init; }

    public virtual ICollection<PpTorDraftTechnicalPeriodDetail>? PpTorDraftTechnicalPeriodDetails { get; init; }
}