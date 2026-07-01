namespace GHB.DP2.Domain.ContractAmendment.ContractAmendment;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CamContractAmendmentPaymentTermId
{
    public static CamContractAmendmentPaymentTermId New() => From(Guid.CreateVersion7());
}

public partial class CamContractAmendmentPoAddendumPaymentTerm : AuditableEntity<CamContractAmendmentPaymentTermId>
{
    public override CamContractAmendmentPaymentTermId Id { get; init; }

    public int PaymentTermNo { get; private set; }

    public int LeadTime { get; private set; }

    public DateTimeOffset DeliveryDate { get; private set; }

    public string Title { get; private set; }

    public decimal InstallmentPercentage { get; private set; }

    public decimal Amount { get; private set; }

    public decimal AdvanceDeductionAmount { get; private set; }

    public decimal PerformanceDeductionAmount { get; private set; }

    public string Description { get; private set; }

    public int Sequence { get; private set; }

    public virtual CamContractAmendmentPoAddendum.CamContractAmendmentPoAddendum CamContractAmendmentPoAddendum { get; init; }

    public static CamContractAmendmentPoAddendumPaymentTerm Create()
    {
        return new CamContractAmendmentPoAddendumPaymentTerm
        {
            Id = CamContractAmendmentPaymentTermId.New(),
        };
    }

    public record PaymentTermValues(
        string Title,
        int PaymentTermNo,
        int LeadTime,
        DateTimeOffset DeliveryDate,
        decimal InstallmentPercentage,
        decimal Amount,
        decimal AdvanceDeductionAmount,
        decimal PerformanceDeductionAmount,
        string Description,
        int Sequence);

    public CamContractAmendmentPoAddendumPaymentTerm SetValues(PaymentTermValues values)
    {
        this.PaymentTermNo = values.PaymentTermNo;
        this.LeadTime = values.LeadTime;
        this.DeliveryDate = values.DeliveryDate;
        this.InstallmentPercentage = values.InstallmentPercentage;
        this.Amount = values.Amount;
        this.AdvanceDeductionAmount = values.AdvanceDeductionAmount;
        this.PerformanceDeductionAmount = values.PerformanceDeductionAmount;
        this.Description = values.Description;
        this.Sequence = values.Sequence;
        this.Title = values.Title;

        return this;
    }
}