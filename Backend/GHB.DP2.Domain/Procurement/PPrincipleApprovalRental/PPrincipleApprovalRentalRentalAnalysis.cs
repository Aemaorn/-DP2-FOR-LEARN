namespace GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRentalRentalAnalysisId
{
    public static PPrincipleApprovalRentalRentalAnalysisId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalRentalRentalAnalysis : AuditableEntity<PPrincipleApprovalRentalRentalAnalysisId>
{
    public override PPrincipleApprovalRentalRentalAnalysisId Id { get; init; }

    public RentalAnalysisType Type { get; set; }

    public int Sequence { get; set; }

    public string Description { get; set; }

    public virtual PPrincipleApprovalRental PPrincipleApprovalRental { get; init; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalRentalAnalysisDetail> PrincipleApprovalRentalRentalAnalysisDetails { get; private set; }

    public static PPrincipleApprovalRentalRentalAnalysis Create(
        int sequence,
        RentalAnalysisType type,
        string description)
    {
        return new PPrincipleApprovalRentalRentalAnalysis
        {
            Id = PPrincipleApprovalRentalRentalAnalysisId.New(),
            Type = type,
            Sequence = sequence,
            Description = description,
        };
    }

    public PPrincipleApprovalRentalRentalAnalysis Update(
        int sequence,
        string description)
    {
        this.Sequence = sequence;
        this.Description = description;

        return this;
    }

    public PPrincipleApprovalRentalRentalAnalysis AddApprovalRentalAnalysisDetail(PPrincipleApprovalRentalRentalAnalysisDetail rentalAnalysisDetail)
    {
        var rentalAnalysisDetails = this.PrincipleApprovalRentalRentalAnalysisDetails?.ToList() ?? new List<PPrincipleApprovalRentalRentalAnalysisDetail>();
        rentalAnalysisDetails.Add(rentalAnalysisDetail);
        this.PrincipleApprovalRentalRentalAnalysisDetails = rentalAnalysisDetails;

        return this;
    }

    public PPrincipleApprovalRentalRentalAnalysis RemoveApprovalRentalAnalysisDetail(PPrincipleApprovalRentalRentalAnalysisDetail rentalAnalysisDetail)
    {
        var list = this.PrincipleApprovalRentalRentalAnalysisDetails?.ToList() ?? new List<PPrincipleApprovalRentalRentalAnalysisDetail>();
        list.Remove(rentalAnalysisDetail);
        this.PrincipleApprovalRentalRentalAnalysisDetails = list;

        return this;
    }
}