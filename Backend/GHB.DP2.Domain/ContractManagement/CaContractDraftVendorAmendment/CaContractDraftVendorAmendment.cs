namespace GHB.DP2.Domain.ContractManagement.CaContractDraftVendorAmendment;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.ContractManagement;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CaContractDraftVendorAmendmentId
{
    public static CaContractDraftVendorAmendmentId New => From(Guid.CreateVersion7());
}

public enum CaContractDraftVendorAmendmentStatus
{
    Draft,

    WaitingApproval,

    Approved,

    Rejected,

    Cancelled,
}

public partial class CaContractDraftVendorAmendment : AuditableEntity<CaContractDraftVendorAmendmentId>, IHasActivityInfo
{
    public override CaContractDraftVendorAmendmentId Id { get; init; }

    public ContractManagementId ContractManagementId { get; private set; }

    public ContractDraftVendorEditId CaContractDraftVendorEditId { get; private set; }

    public CaContractDraftVendorAmendmentStatus Status { get; private set; }

    public virtual ContractManagement ContractManagement { get; init; }

    public virtual CaContractDraftVendorEdit CaContractDraftVendorEdit { get; init; }

    public virtual IReadOnlyCollection<CaContractDraftVendorAmendmentAcceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<CaContractDraftVendorAmendmentDocumentHistory> DocumentHistories { get; private set; }

    public CaContractDraftVendorAmendment SetStatus(CaContractDraftVendorAmendmentStatus status)
    {
        this.Status = status;

        return this;
    }

    public CaContractDraftVendorAmendment AddAcceptor(CaContractDraftVendorAmendmentAcceptor acceptor)
    {
        var acceptors = this.Acceptors.ToHashSet();
        acceptors.Add(acceptor);
        this.Acceptors = acceptors;

        return this;
    }

    public CaContractDraftVendorAmendment RemoveAcceptor(CaContractDraftVendorAmendmentAcceptor acceptor)
    {
        var acceptors = this.Acceptors.ToHashSet();
        acceptors.Remove(acceptor);
        this.Acceptors = acceptors;

        return this;
    }

    public static CaContractDraftVendorAmendment Create(
        ContractManagementId contractManagementId,
        ContractDraftVendorEditId caContractDraftVendorEditId)
    {
        var entity = new CaContractDraftVendorAmendment
        {
            Id = CaContractDraftVendorAmendmentId.New,
            ContractManagementId = contractManagementId,
            CaContractDraftVendorEditId = caContractDraftVendorEditId,
            Status = CaContractDraftVendorAmendmentStatus.Draft,
            Acceptors = new List<CaContractDraftVendorAmendmentAcceptor>(),
            DocumentHistories = new List<CaContractDraftVendorAmendmentDocumentHistory>(),
        };

        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"สร้างข้อมูลใหม่ {nameof(CaContractDraftVendorAmendment)}",
            entity.Status.ToString()));

        return entity;
    }
}
