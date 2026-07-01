namespace GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRentalConsoPerfSupportDataDetailsId
{
    public static PPrincipleApprovalRentalConsoPerfSupportDataDetailsId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalRentalConsoPerfSupportDataDetails : AuditableEntity<PPrincipleApprovalRentalConsoPerfSupportDataDetailsId>
{
    public override PPrincipleApprovalRentalConsoPerfSupportDataDetailsId Id { get; init; }

    public int Sequence { get; private set; }

    public string ActivityDescription { get; private set; }

    public decimal AccountCountYear1 { get; private set; }

    public decimal AmountYear1 { get; private set; }

    public decimal AccountCountYear2 { get; private set; }

    public decimal AmountYear2 { get; private set; }

    public virtual PPrincipleApprovalRental PPrincipleApprovalRental { get; init; }

    public static PPrincipleApprovalRentalConsoPerfSupportDataDetails Create(
        int sequence,
        string activityDescription,
        decimal accountCountYear1,
        decimal amountYear1,
        decimal accountCountYear2,
        decimal amountYear2)
    {
        return new PPrincipleApprovalRentalConsoPerfSupportDataDetails
        {
            Id = PPrincipleApprovalRentalConsoPerfSupportDataDetailsId.New(),
            Sequence = sequence,
            ActivityDescription = activityDescription,
            AccountCountYear1 = accountCountYear1,
            AmountYear1 = amountYear1,
            AccountCountYear2 = accountCountYear2,
            AmountYear2 = amountYear2,
        };
    }

    public PPrincipleApprovalRentalConsoPerfSupportDataDetails Update(
        int sequence,
        string activityDescription,
        decimal accountCountYear1,
        decimal amountYear1,
        decimal accountCountYear2,
        decimal amountYear2)
    {
        this.Sequence = sequence;
        this.ActivityDescription = activityDescription;
        this.AccountCountYear1 = accountCountYear1;
        this.AmountYear1 = amountYear1;
        this.AccountCountYear2 = accountCountYear2;
        this.AmountYear2 = amountYear2;

        return this;
    }
}