namespace GHB.DP2.Domain.ContractAgreement.CaContractInvitation;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.Event;
using GHB.DP2.Domain.Procurement;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractInvitationId
{
    public static ContractInvitationId New() => From(Guid.CreateVersion7());
}

public enum ContractInvitationStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    /// <summary>
    /// เรียกคืนแก้ไข
    /// </summary>
    Edit,

    /// <summary>
    /// รออนุมัติ
    /// </summary>
    WaitingApproval,

    /// <summary>
    /// อนุมัติแล้ว
    /// </summary>
    Approved,

    /// <summary>
    /// ปฏิเสธ
    /// </summary>
    Rejected,
}

public partial class CaContractInvitation : AuditableEntity<ContractInvitationId>, IHasSoftDelete, IHasActivityInfo
{
    public override ContractInvitationId Id { get; init; }

    public ProcurementId ProcurementId { get; init; }

    public ContractInvitationStatus Status { get; private set; }

    public virtual Procurement Procurement { get; init; }

    public virtual IReadOnlyCollection<CaContractInvitationVendors> Vendors { get; set; }

    public virtual IReadOnlyCollection<CaContractInvitationAcceptor> Acceptors { get; set; }

    public static CaContractInvitation Create(ProcurementId procurementId)
    {
        var newContractInvitation = new CaContractInvitation
        {
            Id = ContractInvitationId.New(),
            ProcurementId = procurementId,
            Vendors = [],
            Acceptors = [],
        };

        newContractInvitation.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Create,
                $"สร้างข้อมูลหนังสือเชิญชวนทำสัญญา",
                nameof(ContractInvitationStatus.Draft)));

        return newContractInvitation;
    }

    public CaContractInvitation UpdateStatus(ContractInvitationStatus status)
    {
        if (this.Status == status)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    $"อัปเดตข้อมูล",
                    nameof(status)));
        }
        else
        {
            if (status == ContractInvitationStatus.WaitingApproval)
            {
                this.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.SendApprove,
                        $"ส่งเห็นชอบ/อนุมัติ",
                        nameof(ContractInvitationStatus.WaitingApproval)));

                this.SetAcceptorsToPending();
            }

            if (status == ContractInvitationStatus.Edit)
            {
                this.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.Recall,
                        $"เรียกคืนแก้ไข",
                        nameof(ContractInvitationStatus.Edit)));

                this.SetAcceptorsToDraft();
            }
        }

        this.Status = status;

        return this;
    }

    private void SetAcceptorsToPending()
    {
        var approvers = this.Acceptors
                            .Where(p => p.Type == AcceptorType.Approver)
                            .OrderBy(a => a.Sequence)
                            .ToList();

        approvers.Iter(a => a.SetCurrent(false).Pending());

        var firstPending = approvers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

        if (firstPending != null)
        {
            firstPending.SetCurrent();
        }
    }

    private void SetAcceptorsToDraft()
    {
        _ = this.Acceptors
                .Where(a => a is
                {
                    IsActive: true
                })
                .Iter(a => a.Draft());
    }

    public CaContractInvitation AddVendor(CaContractInvitationVendors vendor)
    {
        var vendors = (this.Vendors ?? []).ToHashSet();

        vendors.Add(vendor);

        this.Vendors = vendors;

        return this;
    }

    public CaContractInvitation AddAcceptor(CaContractInvitationAcceptor acceptor)
    {
        var acceptors = (this.Acceptors ?? []).ToHashSet();

        acceptors.Add(acceptor);

        this.Acceptors = acceptors;

        return this;
    }

    public CaContractInvitation EvaluateAcceptorApproval()
    {
        var isAllApproved =
            this.Acceptors
                .Where(a => a is
                {
                    Type: AcceptorType.Approver,
                    IsActive: true,
                })
                .All(a => a.Status == AcceptorStatus.Approved);

        if (!isAllApproved)
        {
            return this;
        }

        this.SetApproved();

        return this;
    }

    public void SetApproved(bool raiseCreateDraftEvent = true)
    {
        this.Status = ContractInvitationStatus.Approved;

        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Approved,
                $"อนุมัติหนังสือเชิญชวนทำสัญญา",
                nameof(ContractInvitationStatus.Approved)));

        if (raiseCreateDraftEvent)
        {
            this.AddDomainEvent(ContractInvitationApproveEvent.Create(this.Id));
        }
    }

    public CaContractInvitation SetRejected(string? remark)
    {
        this.Status = ContractInvitationStatus.Rejected;

        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Reject,
                $"ส่งกลับแก้ไขหนังสือเชิญชวนทำสัญญา",
                nameof(ContractInvitationStatus.Approved),
                remark));

        return this;
    }

    public CaContractInvitation RemoveAcceptor(CaContractInvitationAcceptor acceptor)
    {
        if (acceptor == null)
        {
            throw new ArgumentNullException(nameof(acceptor));
        }

        var acceptors = this.Acceptors.ToHashSet();

        if (!acceptors.Remove(acceptor))
        {
            throw new InvalidOperationException("Acceptor not found.");
        }

        this.Acceptors = acceptors;

        return this;
    }
}