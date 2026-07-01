namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpTorDraftPaymentTermId
{
    public static PpTorDraftPaymentTermId New() => From(Guid.CreateVersion7());
}

public class PpTorDraftPaymentTerm : AuditableEntity<PpTorDraftPaymentTermId>
{
    public override PpTorDraftPaymentTermId Id { get; init; }

    public ParameterCode? ProRateTypeCode { get; set; }

    public decimal? PaymentPercent { get; set; }

    public string? Description { get; set; }

    public int? Period { get; set; }

    public int? TotalPeriod { get; set; }

    public bool? IsMA { get; set; }

    public ParameterCode? TotalPeriodTypeCode { get; set; }

    public ParameterCode? PeriodTypeCode { get; set; }

    public virtual PpTorDraft PpTorDraft { get; init; }

    public virtual SuParameter? ProRateType { get; init; }

    public virtual SuParameter? PeriodType { get; init; }

    public virtual SuParameter? TotalPeriodType { get; init; }

    public virtual ICollection<PpTorDraftPaymentTermDetail>? PpTorDraftPaymentTermDetails { get; init; }
}