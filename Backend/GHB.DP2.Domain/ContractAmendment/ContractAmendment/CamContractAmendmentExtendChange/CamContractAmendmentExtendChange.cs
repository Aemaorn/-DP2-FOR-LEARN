namespace GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractAmendmentExtendChangeId
{
    public static ContractAmendmentExtendChangeId New() => From(Guid.CreateVersion7());
}

public enum ContractAmendmentExtendChangeStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    Edit,

    WaitingCommitteeApproval,

    WaitingAssigned,

    WaitingComment,

    /// <summary>
    /// รอเห็นชอบโดย ผู้มีอำนาจเห็นชอบ/อนุมัติ
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

public enum ContractAmendmentExtendChangeType
{
    /// <summary>
    /// ขยาย
    /// </summary>
    Extend,

    /// <summary>
    /// เปลี่ยนแปลง
    /// </summary>
    Change,
}

public partial class CamContractAmendmentExtendChange : AuditableEntity<ContractAmendmentExtendChangeId>, IHasActivityInfo
{
    public override ContractAmendmentExtendChangeId Id { get; init; }

    public CamContractAmendmentId CamContractAmendmentId { get; init; }

    public ContractAmendmentExtendChangeType ChangeType { get; private set; }

    public ParameterCode? PaymentTypeCode { get; private set; }

    public DateTimeOffset WorkStartDate { get; private set; }

    public DateTimeOffset NewEndDate { get; private set; }

    public ContractAmendmentExtendChangeStatus Status { get; private set; }

    public virtual CamContractAmendment CamContractAmendment { get; init; }

    public virtual SuParameter? PaymentType { get; init; }

    public virtual IReadOnlyCollection<CamContractAmendmentExtendChangeAcceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<CamContractAmendmentExtendChangeAssignee> Assignees { get; private set; }

    public virtual IReadOnlyCollection<CamContractAmendmentExtendChangePaymentTerm> PaymentTerms { get; private set; }

    public virtual IReadOnlyCollection<CamContractAmendmentExtendChangeDocumentHistory> DocumentHistories { get; private set; }

    public CamContractAmendmentExtendChangeDocumentHistory? LastedExtendChangeDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == ExtendChangeAcceptorDocumentType.ExtendChange)
            .OrderVersions()
            .FirstOrDefault();

    public CamContractAmendmentExtendChangeDocumentHistory? LastedApprovedDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == ExtendChangeAcceptorDocumentType.Approved)
            .OrderVersions()
            .FirstOrDefault();

    public CamContractAmendmentExtendChangeDocumentHistory? LastedDraftExtendChangeDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == ExtendChangeAcceptorDocumentType.ExtendChange)
            .Where(d => d.StatusState == ContractAmendmentExtendChangeStatus.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public CamContractAmendmentExtendChangeDocumentHistory? LastedDraftApprovedRequestDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == ExtendChangeAcceptorDocumentType.Approved)
            .Where(d => d.StatusState == ContractAmendmentExtendChangeStatus.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public CamContractAmendmentExtendChangeDocumentHistory? LastedWaitingCommitteeApprovalExtendChangeDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == ExtendChangeAcceptorDocumentType.ExtendChange)
            .Where(d => d.StatusState == ContractAmendmentExtendChangeStatus.WaitingCommitteeApproval)
            .OrderVersions()
            .FirstOrDefault();

    public CamContractAmendmentExtendChangeDocumentHistory? LastedWaitingCommitteeApprovalApprovedRequestDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == ExtendChangeAcceptorDocumentType.Approved)
            .Where(d => d.StatusState == ContractAmendmentExtendChangeStatus.WaitingCommitteeApproval)
            .OrderVersions()
            .FirstOrDefault();

    public Unit AddDocumentHistory(
        ExtendChangeAcceptorDocumentType documentType,
        FileId fileId,
        bool isReplace,
        bool incrementMajor = false)
    {
        var histories = this.DocumentHistories
                            .ToHashSet();

        var existingStatus =
            histories
                .Any(p => p.StatusState == this.Status && p.DocumentType == documentType);

        var version = this.DocumentHistories
                          .Where(p => p.DocumentType == documentType)
                          .NextVersion(incrementMajor || !existingStatus);

        histories.Add(
            CamContractAmendmentExtendChangeDocumentHistory.Create(
                documentType,
                this.Status,
                version,
                fileId,
                isReplace));

        this.DocumentHistories = histories;

        return unit;
    }

    public CamContractAmendmentExtendChange AddAcceptor(CamContractAmendmentExtendChangeAcceptor acceptor)
    {
        var list = this.Acceptors.ToHashSet();

        list.Add(acceptor);
        this.Acceptors = list;

        return this;
    }

    public CamContractAmendmentExtendChange AddAssignee(CamContractAmendmentExtendChangeAssignee assignee)
    {
        var list = this.Assignees.ToHashSet();

        list.Add(assignee);
        this.Assignees = list;

        return this;
    }

    public CamContractAmendmentExtendChange AddPaymentTerm(CamContractAmendmentExtendChangePaymentTerm paymentTerm)
    {
        var list = this.PaymentTerms.ToHashSet();

        list.Add(paymentTerm);
        this.PaymentTerms = list;

        return this;
    }

    public CamContractAmendmentExtendChange RemoveAcceptor(CamContractAmendmentExtendChangeAcceptor acceptor)
    {
        var list = this.Acceptors.ToHashSet();

        list.Remove(acceptor);
        this.Acceptors = list;

        return this;
    }

    public CamContractAmendmentExtendChange RemoveAssignee(CamContractAmendmentExtendChangeAssignee assignee)
    {
        var list = this.Assignees.ToHashSet();

        list.Remove(assignee);
        this.Assignees = list;

        return this;
    }

    public CamContractAmendmentExtendChange RemovePaymentTerm(CamContractAmendmentExtendChangePaymentTerm paymentTerm)
    {
        var list = this.PaymentTerms.ToHashSet();

        list.Remove(paymentTerm);
        this.PaymentTerms = list;

        return this;
    }

    public CamContractAmendmentExtendChange SetValues(
        ContractAmendmentExtendChangeType changeType,
        DateTimeOffset workStartDate,
        DateTimeOffset newEndDate)
    {
        this.ChangeType = changeType;
        this.WorkStartDate = workStartDate;
        this.NewEndDate = newEndDate;

        return this;
    }

    public CamContractAmendmentExtendChange SetStatus(ContractAmendmentExtendChangeStatus status)
    {
        this.Status = status;

        return this;
    }

    public CamContractAmendmentExtendChange SetEdit()
    {
        this.Status = ContractAmendmentExtendChangeStatus.Edit;

        this.Acceptors.Where(x => !x.IsUnableToPerformDuties)
            .Iter(a => a.Draft());

        return this;
    }

    public CamContractAmendmentExtendChange SetWaitingCommitteeApproval()
    {
        this.Assignees
            .Iter(a => a.Pending());

        this.Status = ContractAmendmentExtendChangeStatus.WaitingCommitteeApproval;

        this.Acceptors
            .Where(p => p.Type != AcceptorType.AcceptanceCommittee)
            .Iter(a => a.Draft());

        this.Acceptors
            .Where(p => p.Type == AcceptorType.AcceptanceCommittee && !p.IsUnableToPerformDuties)
            .Iter(a => a.Pending());

        return this;
    }

    public CamContractAmendmentExtendChange SetApproval()
    {
        this.Status = ContractAmendmentExtendChangeStatus.Approved;
        this.CamContractAmendment.SetStatus(CamContractAmendmentStatus.Completed);

        return this;
    }

    public CamContractAmendmentExtendChange SetWaitingComment()
    {
        this.Status = ContractAmendmentExtendChangeStatus.WaitingComment;

        _ = this.Assignees
                .Iter(a => a.Draft());

        return this;
    }

    public CamContractAmendmentExtendChange SetWaitingApproval()
    {
        this.Status = ContractAmendmentExtendChangeStatus.WaitingApproval;

        var approvers =
            this.Acceptors
                .Where(p => p.Type == AcceptorType.Approver)
                .OrderBy(p => p.Sequence)
                .ToList();

        approvers.Iter(a =>
        {
            a.SetCurrent(false)
             .Pending();
        });

        var firstPending = approvers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

        if (firstPending is not null)
        {
            firstPending.SetCurrent(true);
        }

        return this;
    }

    public CamContractAmendmentExtendChange SetWaitingAssigned()
    {
        this.Status = ContractAmendmentExtendChangeStatus.WaitingAssigned;

        _ = this.Assignees
                .Where(p => p.Type == AssigneeType.Director)
                .Iter(p => p.Assigned());

        return this;
    }

    public CamContractAmendmentExtendChange SetPaymentType(string paymentTypeCode)
    {
        this.PaymentTypeCode = ParameterCode.From(paymentTypeCode);

        return this;
    }

    public bool HasMajorityRejection()
    {
        if (this.Status != ContractAmendmentExtendChangeStatus.WaitingCommitteeApproval)
        {
            return false;
        }

        var committeesAble =
            this.Acceptors
                .Where(a => a is
                {
                    Type: AcceptorType.AcceptanceCommittee,
                    IsUnableToPerformDuties: false,
                    IsActive: true,
                })
                .ToHashSet();

        var totalCommittees = committeesAble.Count;

        if (totalCommittees == 0)
        {
            throw new InvalidOperationException(
                "Cannot evaluate committee approval when there are no committees able to perform duties.");
        }

        var totalReject =
            committeesAble.Count(a => a.Status == AcceptorStatus.Rejected);

        return totalReject > totalCommittees / 2.0;
    }

    public CamContractAmendmentExtendChange SetRejected(string? remark)
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            $"ส่งกลับแก้ไข",
            nameof(ContractAmendmentExtendChangeStatus.Rejected),
            remark));

        this.Status = ContractAmendmentExtendChangeStatus.Rejected;

        return this;
    }

    public static CamContractAmendmentExtendChange Create(
        CamContractAmendmentId contractAmendmentId,
        ContractAmendmentExtendChangeType changeType,
        DateTimeOffset workStartDate,
        DateTimeOffset newEndDate)
    {
        var entity = new CamContractAmendmentExtendChange
        {
            Id = ContractAmendmentExtendChangeId.New(),
            CamContractAmendmentId = contractAmendmentId,
            ChangeType = changeType,
            WorkStartDate = workStartDate,
            NewEndDate = newEndDate,
            Status = ContractAmendmentExtendChangeStatus.Draft,
            DocumentHistories = [],
            Acceptors = [],
            Assignees = [],
            PaymentTerms = [],
        };

        entity.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Create,
                $"สร้างคำขอแก้ไขสัญญา (ขยาย/เปลี่ยนแปลง)",
                entity.Status.ToString()));

        return entity;
    }
}