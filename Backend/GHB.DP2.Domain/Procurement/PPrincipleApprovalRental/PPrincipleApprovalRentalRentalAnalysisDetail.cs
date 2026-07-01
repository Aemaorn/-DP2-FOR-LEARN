namespace GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRentalRentalAnalysisDetailId
{
    public static PPrincipleApprovalRentalRentalAnalysisDetailId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalRentalRentalAnalysisDetail : AuditableEntity<PPrincipleApprovalRentalRentalAnalysisDetailId>
{
    public override PPrincipleApprovalRentalRentalAnalysisDetailId Id { get; init; }

    public int Year { get; set; }

    public decimal Amount { get; set; }

    public virtual PPrincipleApprovalRentalRentalAnalysis RentalAnalysis { get; init; }

    public static PPrincipleApprovalRentalRentalAnalysisDetail Create(
        int year,
        decimal amount)
    {
        return new PPrincipleApprovalRentalRentalAnalysisDetail
        {
            Id = PPrincipleApprovalRentalRentalAnalysisDetailId.New(),
            Year = year,
            Amount = amount,
        };
    }

    public PPrincipleApprovalRentalRentalAnalysisDetail Update(
        int year,
        decimal amount)
    {
        this.Year = year;
        this.Amount = amount;

        return this;
    }
}