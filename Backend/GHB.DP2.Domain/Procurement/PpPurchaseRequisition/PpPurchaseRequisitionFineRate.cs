namespace GHB.DP2.Domain.Procurement.PpPurchaseRequisition;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpPurchaseRequisitionFineRateId
{
    public static PpPurchaseRequisitionFineRateId New() => From(Guid.CreateVersion7());
}

public class PpPurchaseRequisitionFineRate : AuditableEntity<PpPurchaseRequisitionFineRateId>
{
    public override PpPurchaseRequisitionFineRateId Id { get; init; }

    public PpPurchaseRequisitionId PpPurchaseRequisitionId { get; init; }

    public int Sequence { get; private set; }

    public decimal Rate { get; private set; }

    public ParameterCode PeriodTypeCode { get; private set; }

    public ParameterCode ConditionCode { get; private set; }

    public string? ConditionOther { get; private set; }

    public virtual PpPurchaseRequisition PpPurchaseRequisition { get; init; }

    public virtual SuParameter PeriodType { get; init; }

    public virtual SuParameter Condition { get; init; }

    public static PpPurchaseRequisitionFineRate Create(
        PpPurchaseRequisitionId ppPurchaseRequisitionId,
        int sequence,
        decimal rate,
        ParameterCode periodTypeCode,
        ParameterCode conditionCode,
        string? conditionOther = null)
    {
        return new PpPurchaseRequisitionFineRate
        {
            Id = PpPurchaseRequisitionFineRateId.New(),
            PpPurchaseRequisitionId = ppPurchaseRequisitionId,
            Sequence = sequence,
            Rate = rate,
            PeriodTypeCode = periodTypeCode,
            ConditionCode = conditionCode,
            ConditionOther = conditionOther,
        };
    }

    public Unit Update(
        int sequence,
        decimal rate,
        ParameterCode periodTypeCode,
        ParameterCode conditionCode,
        string? conditionOther = null)
    {
        this.Sequence = sequence;
        this.Rate = rate;
        this.PeriodTypeCode = periodTypeCode;
        this.ConditionCode = conditionCode;
        this.ConditionOther = conditionOther;

        return unit;
    }
}