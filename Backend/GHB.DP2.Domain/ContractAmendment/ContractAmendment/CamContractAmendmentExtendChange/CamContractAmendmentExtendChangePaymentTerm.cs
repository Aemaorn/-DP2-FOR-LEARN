namespace GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractAmendmentExtendChangePaymentTerm
{
    public static ContractAmendmentExtendChangePaymentTerm New() => From(Guid.CreateVersion7());
}

public class CamContractAmendmentExtendChangePaymentTerm : AuditableEntity<ContractAmendmentExtendChangePaymentTerm>
{
    public override ContractAmendmentExtendChangePaymentTerm Id { get; init; }

    public int PaymentTermNo { get; private set; }

    public int LeadTime { get; private set; }

    public DateTimeOffset? DeliveryDate { get; private set; }

    public decimal InstallmentPercent { get; private set; }

    public decimal Amount { get; private set; }

    public decimal AdvanceDeductionAmount { get; private set; }

    public decimal PerformanceDeductionAmount { get; private set; }

    public string Description { get; private set; }

    public bool IsDelivery { get; private set; }

    public virtual CamContractAmendmentExtendChange CamContractAmendmentExtendChange { get; init; }

    public static CamContractAmendmentExtendChangePaymentTerm Create()
    {
        return new CamContractAmendmentExtendChangePaymentTerm
        {
            Id = ContractAmendmentExtendChangePaymentTerm.New(),
        };
    }

    public static CamContractAmendmentExtendChangePaymentTerm Create(Guid id)
    {
        return new CamContractAmendmentExtendChangePaymentTerm
        {
            Id = ContractAmendmentExtendChangePaymentTerm.From(id),
        };
    }

    public CamContractAmendmentExtendChangePaymentTerm SetPaymentTermNo(int paymentTermNo)
    {
        this.PaymentTermNo = paymentTermNo;

        return this;
    }

    public CamContractAmendmentExtendChangePaymentTerm SetLeadTime(int leadTime)
    {
        this.LeadTime = leadTime;

        return this;
    }

    public CamContractAmendmentExtendChangePaymentTerm SetDeliveryDate(DateTimeOffset? deliveryDate)
    {
        if (deliveryDate != null)
        {
            this.DeliveryDate = deliveryDate;
        }

        return this;
    }

    public CamContractAmendmentExtendChangePaymentTerm SetInstallmentPercent(decimal installmentPercent)
    {
        this.InstallmentPercent = installmentPercent;

        return this;
    }

    public CamContractAmendmentExtendChangePaymentTerm SetAmount(decimal amount)
    {
        this.Amount = amount;

        return this;
    }

    public CamContractAmendmentExtendChangePaymentTerm SetAdvanceDeductionAmount(decimal advanceDeductionAmount)
    {
        this.AdvanceDeductionAmount = advanceDeductionAmount;

        return this;
    }

    public CamContractAmendmentExtendChangePaymentTerm SetPerformanceDeductionAmount(decimal performanceDeductionAmount)
    {
        this.PerformanceDeductionAmount = performanceDeductionAmount;

        return this;
    }

    public CamContractAmendmentExtendChangePaymentTerm SetDescription(string description)
    {
        this.Description = description;

        return this;
    }

    public CamContractAmendmentExtendChangePaymentTerm SetIsDelivery(bool isDelivery)
    {
        this.IsDelivery = isDelivery;

        return this;
    }
}