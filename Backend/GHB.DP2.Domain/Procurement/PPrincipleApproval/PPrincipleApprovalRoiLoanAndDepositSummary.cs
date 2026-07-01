namespace GHB.DP2.Domain.Procurement.PPrincipleApproval;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRoiLoanAndDepositSummaryId
{
    public static PPrincipleApprovalRoiLoanAndDepositSummaryId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalRoiLoanAndDepositSummary : AuditableEntity<PPrincipleApprovalRoiLoanAndDepositSummaryId>
{
    public override PPrincipleApprovalRoiLoanAndDepositSummaryId Id { get; init; }

    public int Sequence { get; private set; }

    public string ActivityDescription { get; private set; }

    public decimal AmountYear1 { get; private set; }

    public decimal AmountYear2 { get; private set; }

    public decimal AmountYear3 { get; private set; }

    public virtual PPrincipleApproval PPrincipleApproval { get; init; }

    public static PPrincipleApprovalRoiLoanAndDepositSummary Create(
        int sequence,
        string activityDescription,
        decimal amountYear1,
        decimal amountYear2,
        decimal amountYear3)
    {
        return new PPrincipleApprovalRoiLoanAndDepositSummary
        {
            Id = PPrincipleApprovalRoiLoanAndDepositSummaryId.New(),
            Sequence = sequence,
            ActivityDescription = activityDescription,
            AmountYear1 = amountYear1,
            AmountYear2 = amountYear2,
            AmountYear3 = amountYear3,
        };
    }

    public PPrincipleApprovalRoiLoanAndDepositSummary Update(
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