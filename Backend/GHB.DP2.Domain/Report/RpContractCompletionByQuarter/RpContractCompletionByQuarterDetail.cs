namespace GHB.DP2.Domain.Report.RpContractCompletionByQuarter;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct RpContractCompletionByQuarterDetailId
{
    public static RpContractCompletionByQuarterDetailId New() => From(Guid.CreateVersion7());
}

public class RpContractCompletionByQuarterDetail : AuditableEntity<RpContractCompletionByQuarterDetailId>
{
    public override RpContractCompletionByQuarterDetailId Id { get; init; }

    public int Sequence { get; set; }

    public string? Description { get; set; }

    public virtual RpContractCompletionByQuarter RpContractCompletionByQuarter { get; init; }

    public virtual CaContractDraftVendor CaContractDraftVendor { get; set; }

    public static RpContractCompletionByQuarterDetail Create()
    {
        return new RpContractCompletionByQuarterDetail
        {
            Id = RpContractCompletionByQuarterDetailId.New(),
        };
    }

    public RpContractCompletionByQuarterDetail SetValue(
        int sequence,
        string? description,
        CaContractDraftVendor caContractDraftVendor)
    {
        this.Sequence = sequence;
        this.Description = description;
        this.CaContractDraftVendor = caContractDraftVendor;

        return this;
    }
}