namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractDraftPaymentTermId
{
    public static ContractDraftPaymentTermId New => From(Guid.CreateVersion7());
}

public partial class CaContractDraftPaymentTerm : AuditableEntity<ContractDraftPaymentTermId>, IHasSoftDelete
{
    public override ContractDraftPaymentTermId Id { get; init; }

    public int? PaymentTermNo { get; private set; }

    public int? LeadTime { get; private set; }

    public DateTimeOffset? DeliveryDate { get; private set; }

    public decimal? InstallmentPercentage { get; private set; }

    public decimal? Amount { get; private set; }

    public decimal? AdvanceDeductionAmount { get; private set; }

    public decimal? PerformanceDeductionAmount { get; private set; }

    public string? Description { get; private set; }

    public int Sequence { get; private set; }

    public ParameterCode PeriodTypeCode { get; private set; }

    public virtual CaContractDraftVendor ContractDraftVendor { get; init; }

    public virtual SuParameter PeriodType { get; init; }

    public virtual IReadOnlyCollection<CmDeliveryAcceptancePeriod> DeliveryAcceptancePeriods { get; init; }

    public CaContractDraftPaymentTerm SetPaymentTermNo(int? paymentTermNo)
    {
        this.PaymentTermNo = paymentTermNo;

        return this;
    }

    public CaContractDraftPaymentTerm SetLeadTime(int? leadTime)
    {
        this.LeadTime = leadTime;

        return this;
    }

    public CaContractDraftPaymentTerm SetDeliveryDate(DateTimeOffset? deliveryDate)
    {
        if (deliveryDate != null)
        {
            this.DeliveryDate = deliveryDate;
        }

        return this;
    }

    public CaContractDraftPaymentTerm SetInstallmentPercentage(decimal? installmentPercentage)
    {
        this.InstallmentPercentage = installmentPercentage;

        return this;
    }

    public CaContractDraftPaymentTerm SetAmount(decimal? amount)
    {
        this.Amount = amount;

        return this;
    }

    public CaContractDraftPaymentTerm SetAdvanceDeductionAmount(decimal? advanceDeductionAmount)
    {
        this.AdvanceDeductionAmount = advanceDeductionAmount;

        return this;
    }

    public CaContractDraftPaymentTerm SetPerformanceDeductionAmount(decimal? performanceDeductionAmount)
    {
        this.PerformanceDeductionAmount = performanceDeductionAmount;

        return this;
    }

    public CaContractDraftPaymentTerm SetDescription(string? description)
    {
        this.Description = description;

        return this;
    }

    public CaContractDraftPaymentTerm SetSequence(int sequence)
    {
        this.Sequence = sequence;

        return this;
    }

    public static CaContractDraftPaymentTerm Create()
    {
        return new CaContractDraftPaymentTerm
        {
            Id = ContractDraftPaymentTermId.New,
        };
    }

    public static CaContractDraftPaymentTerm Create(Guid id)
    {
        return new CaContractDraftPaymentTerm
        {
            Id = ContractDraftPaymentTermId.From(id),
        };
    }

    public CaContractDraftPaymentTerm SetPeriodType(ParameterCode periodTypeCode)
    {
        this.PeriodTypeCode = periodTypeCode;

        return this;
    }
}