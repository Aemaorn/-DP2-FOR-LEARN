namespace GHB.DP2.Domain.Procurement.PPrincipleApproval;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalConsoPerfSupportDataDetailsId
{
    public static PPrincipleApprovalConsoPerfSupportDataDetailsId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalConsoPerfSupportDataDetails : AuditableEntity<PPrincipleApprovalConsoPerfSupportDataDetailsId>
{
    public override PPrincipleApprovalConsoPerfSupportDataDetailsId Id { get; init; }

    public int Sequence { get; private set; }

    public string ActivityDescription { get; private set; }

    public decimal AccountCountYear1 { get; private set; }

    public decimal AmountYear1 { get; private set; }

    public decimal AccountCountYear2 { get; private set; }

    public decimal AmountYear2 { get; private set; }

    public virtual PPrincipleApproval PPrincipleApproval { get; init; }

    public static PPrincipleApprovalConsoPerfSupportDataDetails Create(
        int sequence,
        string activityDescription,
        decimal accountCountYear1,
        decimal amountYear1,
        decimal accountCountYear2,
        decimal amountYear2)
    {
        return new PPrincipleApprovalConsoPerfSupportDataDetails
        {
            Id = PPrincipleApprovalConsoPerfSupportDataDetailsId.New(),
            Sequence = sequence,
            ActivityDescription = activityDescription,
            AccountCountYear1 = accountCountYear1,
            AmountYear1 = amountYear1,
            AccountCountYear2 = accountCountYear2,
            AmountYear2 = amountYear2,
        };
    }

    public PPrincipleApprovalConsoPerfSupportDataDetails Update(
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