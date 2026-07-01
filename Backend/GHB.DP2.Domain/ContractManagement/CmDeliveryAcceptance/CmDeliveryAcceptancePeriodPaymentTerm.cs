namespace GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CmDeliveryAcceptancePeriodPaymentTermId
{
    public static CmDeliveryAcceptancePeriodPaymentTermId New() => From(Guid.CreateVersion7());
}

public partial class CmDeliveryAcceptancePeriodPaymentTerm : AuditableEntity<CmDeliveryAcceptancePeriodPaymentTermId>, IHasSoftDelete
{
    public override CmDeliveryAcceptancePeriodPaymentTermId Id { get; init; }

    public CmDeliveryAcceptancePeriodId DeliveryAcceptancePeriodId { get; init; }

    public int Sequence { get; private set; }

    public int PaymentTerm { get; private set; }

    public string Description { get; private set; }

    public decimal Amount { get; private set; }

    public virtual CmDeliveryAcceptancePeriod Period { get; init; }

    public static CmDeliveryAcceptancePeriodPaymentTerm Create(
        CmDeliveryAcceptancePeriodId deliveryAcceptancePeriodId,
        int sequence,
        int paymentTerm,
        string description,
        decimal amount)
    {
        return new CmDeliveryAcceptancePeriodPaymentTerm
        {
            Id = CmDeliveryAcceptancePeriodPaymentTermId.New(),
            Sequence = sequence,
            DeliveryAcceptancePeriodId = deliveryAcceptancePeriodId,
            PaymentTerm = paymentTerm,
            Description = description,
            Amount = amount,
        };
    }

    public CmDeliveryAcceptancePeriodPaymentTerm Update(
        int sequence,
        int paymentTerm,
        string description,
        decimal amount)
    {
        this.Sequence = sequence;
        this.PaymentTerm = paymentTerm;
        this.Description = description;
        this.Amount = amount;

        return this;
    }
}