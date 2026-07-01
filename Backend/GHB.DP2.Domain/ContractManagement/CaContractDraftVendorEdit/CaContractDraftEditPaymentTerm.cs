namespace GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractDraftEditPaymentTermId
{
    public static ContractDraftEditPaymentTermId New => From(Guid.CreateVersion7());
}

public partial class CaContractDraftEditPaymentTerm : AuditableEntity<ContractDraftEditPaymentTermId>, IHasSoftDelete
{
    public override ContractDraftEditPaymentTermId Id { get; init; }

    public int? PaymentTermNo { get; private set; }

    public int? LeadTime { get; private set; }

    public DateTimeOffset? DeliveryDate { get; private set; }

    public decimal? InstallmentPercentage { get; private set; }

    public decimal? Amount { get; private set; }

    public decimal? AdvanceDeductionAmount { get; private set; }

    public decimal? PerformanceDeductionAmount { get; private set; }

    public string? Description { get; private set; }

    public int Sequence { get; private set; }

    public ParameterCode? PeriodTypeCode { get; private set; }

    public virtual CaContractDraftVendorEdit CaContractDraftVendorEdit { get; init; }

    public virtual SuParameter? PeriodType { get; init; }

    public CaContractDraftEditPaymentTerm SetPaymentTermNo(int? paymentTermNo)
    {
        this.PaymentTermNo = paymentTermNo;

        return this;
    }

    public CaContractDraftEditPaymentTerm SetLeadTime(int? leadTime)
    {
        this.LeadTime = leadTime;

        return this;
    }

    public CaContractDraftEditPaymentTerm SetDeliveryDate(DateTimeOffset? deliveryDate)
    {
        if (deliveryDate != null)
        {
            this.DeliveryDate = deliveryDate;
        }

        return this;
    }

    public CaContractDraftEditPaymentTerm SetInstallmentPercentage(decimal? installmentPercentage)
    {
        this.InstallmentPercentage = installmentPercentage;

        return this;
    }

    public CaContractDraftEditPaymentTerm SetAmount(decimal? amount)
    {
        this.Amount = amount;

        return this;
    }

    public CaContractDraftEditPaymentTerm SetAdvanceDeductionAmount(decimal? advanceDeductionAmount)
    {
        this.AdvanceDeductionAmount = advanceDeductionAmount;

        return this;
    }

    public CaContractDraftEditPaymentTerm SetPerformanceDeductionAmount(decimal? performanceDeductionAmount)
    {
        this.PerformanceDeductionAmount = performanceDeductionAmount;

        return this;
    }

    public CaContractDraftEditPaymentTerm SetDescription(string? description)
    {
        this.Description = description;

        return this;
    }

    public CaContractDraftEditPaymentTerm SetSequence(int sequence)
    {
        this.Sequence = sequence;

        return this;
    }

    public CaContractDraftEditPaymentTerm SetPeriodType(ParameterCode? periodTypeCode)
    {
        this.PeriodTypeCode = periodTypeCode;

        return this;
    }

    public static CaContractDraftEditPaymentTerm Create()
    {
        return new CaContractDraftEditPaymentTerm
        {
            Id = ContractDraftEditPaymentTermId.New,
        };
    }

    public static CaContractDraftEditPaymentTerm Create(Guid id)
    {
        return new CaContractDraftEditPaymentTerm
        {
            Id = ContractDraftEditPaymentTermId.From(id),
        };
    }
}
