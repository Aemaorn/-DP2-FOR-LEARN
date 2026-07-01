namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.Event;
using GHB.DP2.Domain.Procurement;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractDraftId
{
    public static ContractDraftId New => From(Guid.CreateVersion7());
}

public enum ContractDraftStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    /// <summary>
    /// รออนุมัติ
    /// </summary>
    Pending,

    /// <summary>
    /// อนุมัติแล้ว
    /// </summary>
    Approved,

    /// <summary>
    /// ปฏิเสธ
    /// </summary>
    Rejected,
}

/// <summary>
/// ร่างสัญญา
/// </summary>
public partial class CaContractDraft : AuditableEntity<ContractDraftId>, IHasSoftDelete, IHasActivityInfo
{
    public override ContractDraftId Id { get; init; }

    public ProcurementId ProcurementId { get; init; }

    public ContractDraftStatus Status { get; private set; }

    public virtual Procurement Procurement { get; init; }

    public virtual IReadOnlyCollection<CaContractDraftVendor> Vendors { get; private set; }

    public CaContractDraft AddVendor(CaContractDraftVendor vendor)
    {
        var vendors = this.Vendors.ToHashSet();

        vendors.Add(vendor);

        this.Vendors = vendors;

        return this;
    }

    public CaContractDraft SetApproved()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Approved,
            $"ผู้มีอำนาจเห็นชอบ/อนุมัติ",
            this.Status.ToString()));

        this.Status = ContractDraftStatus.Approved;
        this.Procurement.SetStatus(ProcurementStatus.Completed);

        this.AddDomainEvent(ContractDraftApproveEvent.Create(this.Id));

        return this;
    }

    public static CaContractDraft Create(
        ProcurementId procurementId)
    {
        return new CaContractDraft
        {
            Id = ContractDraftId.New,
            ProcurementId = procurementId,
            Status = ContractDraftStatus.Draft,
            Vendors = [],
        };
    }
}