namespace GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRentalConsoPerfSupportDataId
{
    public static PPrincipleApprovalRentalConsoPerfSupportDataId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalRentalConsoPerfSupportData : AuditableEntity<PPrincipleApprovalRentalConsoPerfSupportDataId>
{
    public override PPrincipleApprovalRentalConsoPerfSupportDataId Id { get; init; }

    public int? TransactionVolume { get; private set; }

    public string? ActivityDescription { get; private set; }

    public int? PeriodYear { get; private set; }

    public int? StartMonth { get; private set; }

    public int? EndMonth { get; private set; }

    public virtual PPrincipleApprovalRental PPrincipleApprovalRental { get; init; }

    public static PPrincipleApprovalRentalConsoPerfSupportData Create(
        int? transactionVolume,
        string? activityDescription,
        int? periodYear,
        int? startMonth,
        int? endMonth)
    {
        return new PPrincipleApprovalRentalConsoPerfSupportData
        {
            Id = PPrincipleApprovalRentalConsoPerfSupportDataId.New(),
            TransactionVolume = transactionVolume,
            ActivityDescription = activityDescription,
            PeriodYear = periodYear,
            StartMonth = startMonth,
            EndMonth = endMonth,
        };
    }

    public PPrincipleApprovalRentalConsoPerfSupportData Update(
        int? transactionVolume,
        string? activityDescription,
        int? periodYear,
        int? startMonth,
        int? endMonth)
    {
        this.TransactionVolume = transactionVolume;
        this.ActivityDescription = activityDescription;
        this.PeriodYear = periodYear;
        this.StartMonth = startMonth;
        this.EndMonth = endMonth;

        return this;
    }
}