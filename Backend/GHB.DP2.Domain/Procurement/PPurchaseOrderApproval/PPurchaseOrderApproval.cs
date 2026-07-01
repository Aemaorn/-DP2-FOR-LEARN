namespace GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Constants;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PurchaseOrderApprovalId
{
    public static PurchaseOrderApprovalId New() => From(Guid.CreateVersion7());
}

public enum PurchaseOrderApprovalStatus
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
    /// อนุมัติ
    /// </summary>
    WaitingAssign,

    /// <summary>
    /// ส่งกลับแก้ไข
    /// </summary>
    Rejected,

    /// <summary>
    /// ยืนยันมอบหมาย
    /// </summary>
    Assigned,
}

public partial class PPurchaseOrderApproval : AuditableEntity<PurchaseOrderApprovalId>, IHasSoftDelete, IHasActivityInfo
{
    public override PurchaseOrderApprovalId Id { get; init; }

    public ProcurementId ProcurementId { get; private set; }

    public string ContractType { get; private set; }

    public PurchaseOrderApprovalStatus Status { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public virtual Procurement Procurement { get; init; }

    public virtual ICollection<PPurchaseOrderApprovalContract> Contracts { get; set; }

    public virtual ICollection<PPurchaseOrderApprovalAcceptor> Acceptors { get; set; }

    public virtual ICollection<PPurchaseOrderApprovalAssignee> Assignees { get; set; }

    public virtual IReadOnlyCollection<PPurchaseOrderApprovalCommittee> Committees { get; private set; }

    public virtual ICollection<PPurchaseOrderApprovalBudget> PurchaseOrderApprovalBudget { get; set; }

    public virtual ICollection<PPurchaseOrderApprovalEntrepreneurs> PurchaseOrderApprovalEntrepreneurs { get; set; }

    public PPurchaseOrderApprovalAssignee? LastedAssignee => this.Assignees.MaxBy(s => s.Sequence);

    public PPurchaseOrderApproval SetContractType(string contractType)
    {
        this.ContractType = contractType;

        return this;
    }

    public PPurchaseOrderApproval SetDocumentDate()
    {
        this.DocumentDate = DateTimeOffset.Now;

        return this;
    }

    public PPurchaseOrderApproval SetApproved()
    {
        if (this.Procurement.Type is ProcurementType.Procurement && this.ContractType == PurchaseOrderApprovalContractType.Contract40)
        {
            this.Status = PurchaseOrderApprovalStatus.Assigned;
            this.Procurement.SetStatus(ProcurementStatus.Completed);

            return this;
        }

        if (this.Procurement.Type is ProcurementType.Rent && this.ContractType == PurchaseOrderApprovalContractType.Vendor)
        {
            this.Status = PurchaseOrderApprovalStatus.Assigned;
            this.Procurement.SetStatus(ProcurementStatus.Completed);

            return this;
        }

        this.Status = PurchaseOrderApprovalStatus.WaitingAssign;

        return this;
    }

    public PPurchaseOrderApproval SetAssigned()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Assigned,
            $"มอบหมายผู้รับผิดชอบ",
            PurchaseOrderApprovalStatus.Assigned.ToString()));

        this.Status = PurchaseOrderApprovalStatus.Assigned;

        if (this.ContractType != PurchaseOrderApprovalContractType.Contract40)
        {
            this.Procurement.SetProcurementStep(this.Procurement.Type, ProcurementStep.ContractAgreement);
            this.Procurement.SetProcessType(ProcessType.ContractInvitation);
        }

        return this;
    }

    public PPurchaseOrderApproval SetRejected(string? remark = null)
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            $"ส่งกลับแก้ไขข้อมูล",
            PurchaseOrderApprovalStatus.Rejected.ToString(),
            remark));

        this.Status = PurchaseOrderApprovalStatus.Rejected;

        return this;
    }

    public PPurchaseOrderApproval SetWaitingApproval()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.SendApprove,
            $"ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ",
            PurchaseOrderApprovalStatus.WaitingApproval.ToString()));

        this.Status = PurchaseOrderApprovalStatus.WaitingApproval;

        this.Acceptors
            .Where(p => p.Type == AcceptorType.Approver)
            .OrderBy(a => a.Sequence)
            .Iter(a =>
            {
                a.SetCurrent(false)
                 .Pending();
            });

        this.Acceptors
            .Where(p => p.Type == AcceptorType.Approver)
            .OrderBy(a => a.Sequence)
            .FirstOrDefault(a => a.Status == AcceptorStatus.Pending)
            ?.SetCurrent();

        return this;
    }

    public PPurchaseOrderApproval SetEdit()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Recall,
            $"ขอเปลี่ยนแปลงข้อมูล",
            PurchaseOrderApprovalStatus.Edit.ToString()));

        this.Status = PurchaseOrderApprovalStatus.Edit;

        _ = this.Acceptors
                .Iter(a => a.SetIsCurrent(false).Draft());

        return this;
    }

    public static PPurchaseOrderApproval Create(
        ProcurementId procurementId,
        string contractType,
        PurchaseOrderApprovalStatus status)
    {
        var newData = new PPurchaseOrderApproval
        {
            Id = PurchaseOrderApprovalId.New(),
            ContractType = contractType,
            ProcurementId = procurementId,
            Status = status,
            Contracts = [],
            Acceptors = [],
            Assignees = [],
        };

        newData.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"สร้างข้อมูล",
            newData.Status.ToString()));

        return newData;
    }

    public PPurchaseOrderApproval Update(
        ProcurementId procurementId,
        string contractType,
        PurchaseOrderApprovalStatus status)
    {
        this.ProcurementId = procurementId;
        this.ContractType = contractType;
        this.Status = status;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Update,
            $"อัปเดตข้อมูลใหม่ {nameof(PPurchaseOrderApproval)}",
            this.Status.ToString()));

        return this;
    }

    public PPurchaseOrderApproval AddAcceptor(PPurchaseOrderApprovalAcceptor acceptor)
    {
        var acceptors = this.Acceptors?.ToList() ?? new List<PPurchaseOrderApprovalAcceptor>();
        acceptors.Add(acceptor);
        this.Acceptors = acceptors;

        return this;
    }

    public PPurchaseOrderApproval RemoveAcceptor(PPurchaseOrderApprovalAcceptor acceptor)
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

    public PPurchaseOrderApproval AddAssignee(PPurchaseOrderApprovalAssignee assignee)
    {
        var assignees = this.Assignees?.ToList() ?? new List<PPurchaseOrderApprovalAssignee>();
        assignees.Add(assignee);
        this.Assignees = assignees;

        return this;
    }

    public PPurchaseOrderApproval RemoveAssignee(PPurchaseOrderApprovalAssignee assign)
    {
        if (assign == null)
        {
            throw new ArgumentNullException(nameof(assign));
        }

        var assignees = this.Assignees.ToHashSet();

        if (!assignees.Remove(assign))
        {
            throw new InvalidOperationException("Assignee not found.");
        }

        this.Assignees = assignees;

        return this;
    }

    public PPurchaseOrderApproval AddContractor(PPurchaseOrderApprovalContract contract)
    {
        var contracts = this.Contracts?.ToList() ?? new List<PPurchaseOrderApprovalContract>();
        contracts.Add(contract);
        this.Contracts = contracts;

        return this;
    }

    public PPurchaseOrderApproval RemoveContractor(PPurchaseOrderApprovalContract contract)
    {
        if (contract == null)
        {
            throw new ArgumentNullException(nameof(contract));
        }

        var contracts = this.Contracts.ToHashSet();

        if (!contracts.Remove(contract))
        {
            throw new InvalidOperationException("Contract not found.");
        }

        this.Contracts = contracts;

        return this;
    }

    public PPurchaseOrderApproval RemoveEntrepreneur(PPurchaseOrderApprovalEntrepreneurs? entrepreneur)
    {
        if (entrepreneur == null)
        {
            throw new ArgumentNullException(nameof(entrepreneur));
        }

        var entrepreneurs = this.PurchaseOrderApprovalEntrepreneurs.ToHashSet();

        if (!entrepreneurs.Remove(entrepreneur))
        {
            throw new InvalidOperationException("Entrepreneur not found.");
        }

        this.PurchaseOrderApprovalEntrepreneurs = entrepreneurs;

        return this;
    }

    public PPurchaseOrderApproval AddPPurchaseOrderApprovalCommittee(PPurchaseOrderApprovalCommittee committee)
    {
        var committees = this.Committees.ToHashSet();
        committees.Add(committee);

        this.Committees = committees;

        return this;
    }
}