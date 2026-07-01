namespace GHB.DP2.Domain.Report.RpAuditAndRevenue;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct RpAuditAndRevenueDetailId
{
    public static RpAuditAndRevenueDetailId New() => From(Guid.CreateVersion7());
}

public class RpAuditAndRevenueDetail : AuditableEntity<RpAuditAndRevenueDetailId>
{
    public override RpAuditAndRevenueDetailId Id { get; init; }

    public int Sequence { get; set; }

    public string? Description { get; set; }

    public virtual RpAuditAndRevenue RpAuditAndRevenue { get; init; }

    public virtual CaContractDraftVendor CaContractDraftVendor { get; set; }

    public static RpAuditAndRevenueDetail Create()
    {
        return new RpAuditAndRevenueDetail
        {
            Id = RpAuditAndRevenueDetailId.New(),
        };
    }

    public RpAuditAndRevenueDetail SetValue(
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