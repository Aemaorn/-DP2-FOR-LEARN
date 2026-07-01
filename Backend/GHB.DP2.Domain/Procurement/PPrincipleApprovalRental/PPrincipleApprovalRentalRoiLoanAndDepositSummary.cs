namespace GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRentalRoiLoanAndDepositSummaryId
{
    public static PPrincipleApprovalRentalRoiLoanAndDepositSummaryId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalRentalRoiLoanAndDepositSummary : AuditableEntity<PPrincipleApprovalRentalRoiLoanAndDepositSummaryId>
{
    public override PPrincipleApprovalRentalRoiLoanAndDepositSummaryId Id { get; init; }

    public int Sequence { get; private set; }

    public string ActivityDescription { get; private set; }

    public decimal AmountYear1 { get; private set; }

    public decimal AmountYear2 { get; private set; }

    public decimal AmountYear3 { get; private set; }

    public virtual PPrincipleApprovalRental PPrincipleApprovalRental { get; init; }

    public static PPrincipleApprovalRentalRoiLoanAndDepositSummary Create(
        int sequence,
        string activityDescription,
        decimal amountYear1,
        decimal amountYear2,
        decimal amountYear3)
    {
        return new PPrincipleApprovalRentalRoiLoanAndDepositSummary
        {
            Id = PPrincipleApprovalRentalRoiLoanAndDepositSummaryId.New(),
            Sequence = sequence,
            ActivityDescription = activityDescription,
            AmountYear1 = amountYear1,
            AmountYear2 = amountYear2,
            AmountYear3 = amountYear3,
        };
    }

    public PPrincipleApprovalRentalRoiLoanAndDepositSummary Update(
        int sequence,
        string activityDescription,
        decimal amountYear1,
        decimal amountYear2,
        decimal amountYear3)
    {
        this.Sequence = sequence;
        this.ActivityDescription = activityDescription;
        this.AmountYear1 = amountYear1;
        this.AmountYear2 = amountYear2;
        this.AmountYear3 = amountYear3;

        return this;
    }
}