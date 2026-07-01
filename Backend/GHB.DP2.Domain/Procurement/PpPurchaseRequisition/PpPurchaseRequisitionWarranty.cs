namespace GHB.DP2.Domain.Procurement.PpPurchaseRequisition;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpPurchaseRequisitionWarrantyId
{
    public static PpPurchaseRequisitionWarrantyId New() => From(Guid.CreateVersion7());
}

public partial class PpPurchaseRequisitionWarranty : AuditableEntity<PpPurchaseRequisitionWarrantyId>
{
    public override PpPurchaseRequisitionWarrantyId Id { get; init; }

    public PpPurchaseRequisitionId PpPurchaseRequisitionId { get; init; }

    public bool HasWarranty { get; private set; }

    public int Period { get; private set; }

    public ParameterCode? PeriodTypeCode { get; private set; }

    public string? ConditionOther { get; private set; }

    public virtual PpPurchaseRequisition PpPurchaseRequisition { get; init; }

    public virtual SuParameter? PeriodType { get; init; }

    public static PpPurchaseRequisitionWarranty Create(
        PpPurchaseRequisitionId ppPurchaseRequisitionId,
        bool hasWarranty,
        int period,
        ParameterCode? periodTypeCode = null,
        string? conditionOther = null)
    {
        return new PpPurchaseRequisitionWarranty
        {
            Id = PpPurchaseRequisitionWarrantyId.New(),
            PpPurchaseRequisitionId = ppPurchaseRequisitionId,
            HasWarranty = hasWarranty,
            Period = period,
            PeriodTypeCode = periodTypeCode,
            ConditionOther = conditionOther,
        };
    }

    public Unit Update(
        bool hasWarranty,
        int period,
        ParameterCode? periodTypeCode = null,
        string? conditionOther = null)
    {
        this.HasWarranty = hasWarranty;
        this.Period = period;
        this.PeriodTypeCode = periodTypeCode;
        this.ConditionOther = conditionOther;

        return unit;
    }
}