namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpTorDraftPaymentTermPeriodId
{
    public static PpTorDraftPaymentTermPeriodId New() => From(Guid.CreateVersion7());
}

public class PpTorPaymentTermPeriod : AuditableEntity<PpTorDraftPaymentTermPeriodId>
{
    public override PpTorDraftPaymentTermPeriodId Id { get; init; }

    public int Sequence { get; set; }

    public string? Description { get; set; }

    public int? Quantity { get; set; }

    public ParameterCode? PeriodTypeCode { get; set; }

    public int? TotalQuantity { get; set; }

    public ParameterCode? TotalPeriodTypeCode { get; set; }

    public virtual PpTorDraft PpTorDraft { get; init; }

    public virtual SuParameter? PeriodType { get; init; }

    public virtual SuParameter? TotalPeriodType { get; init; }
}
