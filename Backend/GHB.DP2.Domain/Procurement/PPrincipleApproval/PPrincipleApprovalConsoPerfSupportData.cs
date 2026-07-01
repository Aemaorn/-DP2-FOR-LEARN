namespace GHB.DP2.Domain.Procurement.PPrincipleApproval;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalConsoPerfSupportDataId
{
    public static PPrincipleApprovalConsoPerfSupportDataId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalConsoPerfSupportData : AuditableEntity<PPrincipleApprovalConsoPerfSupportDataId>
{
    public override PPrincipleApprovalConsoPerfSupportDataId Id { get; init; }

    public int? TransactionVolume { get; private set; }

    public string? ActivityDescription { get; private set; }

    public int? PeriodYear { get; private set; }

    public int? StartMonth { get; private set; }

    public int? EndMonth { get; private set; }

    public virtual PPrincipleApproval PPrincipleApproval { get; init; }

    public static PPrincipleApprovalConsoPerfSupportData Create(
        int? transactionVolume,
        string? activityDescription,
        int? periodYear,
        int? startMonth,
        int? endMonth)
    {
        return new PPrincipleApprovalConsoPerfSupportData
        {
            Id = PPrincipleApprovalConsoPerfSupportDataId.New(),
            TransactionVolume = transactionVolume,
            ActivityDescription = activityDescription,
            PeriodYear = periodYear,
            StartMonth = startMonth,
            EndMonth = endMonth,
        };
    }

    public PPrincipleApprovalConsoPerfSupportData Update(
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