namespace GHB.DP2.Domain.Procurement.PPrincipleApproval;

using GHB.DP2.Domain.Common;
using Vogen;

public enum RentalAnalysisType
{
    General,

    ProfitAndLoss,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRentalAnalysisId
{
    public static PPrincipleApprovalRentalAnalysisId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalRentalAnalysis : AuditableEntity<PPrincipleApprovalRentalAnalysisId>
{
    public override PPrincipleApprovalRentalAnalysisId Id { get; init; }

    public RentalAnalysisType Type { get; set; }

    public int Sequence { get; set; }

    public string Description { get; set; }

    public virtual PPrincipleApproval PPrincipleApproval { get; init; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalAnalysisDetail> PrincipleApprovalRentalAnalysisDetails { get; private set; }

    public static PPrincipleApprovalRentalAnalysis Create(
        int sequence,
        RentalAnalysisType type,
        string description)
    {
        return new PPrincipleApprovalRentalAnalysis
        {
            Id = PPrincipleApprovalRentalAnalysisId.New(),
            Type = type,
            Sequence = sequence,
            Description = description,
        };
    }

    public PPrincipleApprovalRentalAnalysis Update(
        int sequence,
        string description)
    {
        this.Sequence = sequence;
        this.Description = description;

        return this;
    }

    public PPrincipleApprovalRentalAnalysis AddApprovalRentalAnalysisDetail(PPrincipleApprovalRentalAnalysisDetail rentalAnalysisDetail)
    {
        var rentalAnalysisDetails = this.PrincipleApprovalRentalAnalysisDetails?.ToList() ?? new List<PPrincipleApprovalRentalAnalysisDetail>();
        rentalAnalysisDetails.Add(rentalAnalysisDetail);
        this.PrincipleApprovalRentalAnalysisDetails = rentalAnalysisDetails;

        return this;
    }

    public PPrincipleApprovalRentalAnalysis RemoveApprovalRentalAnalysisDetail(PPrincipleApprovalRentalAnalysisDetail rentalAnalysisDetail)
    {
        var list = this.PrincipleApprovalRentalAnalysisDetails?.ToList() ?? new List<PPrincipleApprovalRentalAnalysisDetail>();
        list.Remove(rentalAnalysisDetail);
        this.PrincipleApprovalRentalAnalysisDetails = list;

        return this;
    }
}