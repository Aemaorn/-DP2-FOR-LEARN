namespace GHB.DP2.Domain.Procurement.PpPurchaseRequisition;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpPurchaseRequisitionPaymentTermId
{
    public static PpPurchaseRequisitionPaymentTermId New() => From(Guid.CreateVersion7());
}

public partial class PpPurchaseRequisitionPaymentTerm : AuditableEntity<PpPurchaseRequisitionPaymentTermId>
{
    public override PpPurchaseRequisitionPaymentTermId Id { get; init; }

    public PpPurchaseRequisitionId PpPurchaseRequisitionId { get; init; }

    public int? TermNumber { get; private set; }

    public decimal? Percent { get; private set; }

    public int? Period { get; private set; }

    public int? TotalPeriod { get; private set; }

    public string? Description { get; private set; }

    public ParameterCode? PeriodTypeCode { get; private set; }

    public ParameterCode? TotalPeriodTypeCode { get; private set; }

    public ParameterCode? PaymentTypeCode { get; private set; }

    public bool? IsMA { get; private set; }

    public virtual SuParameter? PeriodType { get; init; }

    public virtual SuParameter? TotalPeriodType { get; init; }

    public virtual SuParameter? PaymentType { get; init; }

    public virtual PpPurchaseRequisition PpPurchaseRequisition { get; init; }

    public static PpPurchaseRequisitionPaymentTerm Create(
        PpPurchaseRequisitionId ppPurchaseRequisitionId,
        int? termNumber,
        decimal? percent,
        int? period,
        string? description,
        bool? isMA,
        string? paymentTypeCode,
        string? totalPeriodTypeCode,
        int? totalPeriod,
        string? periodTypeCode)
    {
        return new PpPurchaseRequisitionPaymentTerm
        {
            Id = PpPurchaseRequisitionPaymentTermId.New(),
            PpPurchaseRequisitionId = ppPurchaseRequisitionId,
            TermNumber = termNumber,
            Percent = percent,
            Period = period,
            Description = description,
            IsMA = isMA,
            PaymentTypeCode = !string.IsNullOrWhiteSpace(paymentTypeCode) ? ParameterCode.From(paymentTypeCode) : null,
            TotalPeriodTypeCode = !string.IsNullOrWhiteSpace(totalPeriodTypeCode) ? ParameterCode.From(totalPeriodTypeCode) : null,
            TotalPeriod = totalPeriod,
            PeriodTypeCode = !string.IsNullOrWhiteSpace(periodTypeCode) ? ParameterCode.From(periodTypeCode) : null,
        };
    }

    public Unit Update(
        int? termNumber,
        decimal? percent,
        int? period,
        string? description,
        bool? isMA,
        string? paymentTypeCode,
        string? totalPeriodTypeCode,
        int? totalPeriod,
        string? periodTypeCode)
    {
        this.TermNumber = termNumber;
        this.Percent = percent;
        this.Period = period;
        this.Description = description;
        this.IsMA = isMA;
        this.PaymentTypeCode = !string.IsNullOrWhiteSpace(paymentTypeCode) ? ParameterCode.From(paymentTypeCode) : null;
        this.TotalPeriodTypeCode = !string.IsNullOrWhiteSpace(totalPeriodTypeCode) ? ParameterCode.From(totalPeriodTypeCode) : null;
        this.TotalPeriod = totalPeriod;
        this.PeriodTypeCode = !string.IsNullOrWhiteSpace(periodTypeCode) ? ParameterCode.From(periodTypeCode) : null;

        return unit;
    }
}