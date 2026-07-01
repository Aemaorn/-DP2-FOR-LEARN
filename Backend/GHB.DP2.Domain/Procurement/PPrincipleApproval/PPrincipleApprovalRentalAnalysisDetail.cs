namespace GHB.DP2.Domain.Procurement.PPrincipleApproval;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRentalAnalysisDetailId
{
    public static PPrincipleApprovalRentalAnalysisDetailId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalRentalAnalysisDetail : AuditableEntity<PPrincipleApprovalRentalAnalysisDetailId>
{
    public override PPrincipleApprovalRentalAnalysisDetailId Id { get; init; }

    public PPrincipleApprovalRentalAnalysisId PPrincipleApprovalRentalAnalysisId { get; init; }

    public int Year { get; set; }

    public decimal Amount { get; set; }

    public virtual PPrincipleApprovalRentalAnalysis PPrincipleApprovalRentalAnalysis { get; init; }

    public static PPrincipleApprovalRentalAnalysisDetail Create(
        int year,
        decimal amount)
    {
        return new PPrincipleApprovalRentalAnalysisDetail
        {
            Id = PPrincipleApprovalRentalAnalysisDetailId.New(),
            Year = year,
            Amount = amount,
        };
    }

    public PPrincipleApprovalRentalAnalysisDetail Update(
        int year,
        decimal amount)
    {
        this.Year = year;
        this.Amount = amount;

        return this;
    }
}