namespace GHB.DP2.Domain.Procurement.ChangeCommittee;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using LanguageExt;
using Vogen;

public enum CommitteeChangeStatus
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
    /// อยู่ระหว่าง คกก. เห็นชอบ
    /// </summary>
    WaitingCommitteeApproval,

    /// <summary>
    /// รอ จพ. มอบหมาย
    /// </summary>
    WaitingAssign,

    /// <summary>
    /// รอ จพ. ให้ความเห็น
    /// </summary>
    WaitingComment,

    /// <summary>
    /// ส่งกลับแก้ไข
    /// </summary>
    RejectToAssignee,

    /// <summary>
    /// ผู้มีอำนาจเห็นชอบ/อนุมัติ
    /// </summary>
    WaitingApproval,

    /// <summary>
    /// อนุมัติแล้ว
    /// </summary>
    Approved,

    /// <summary>
    /// ตีกลับ
    /// </summary>
    Rejected,

    /// <summary>
    /// ยกเลิก
    /// </summary>
    Cancelled,
}

public enum CommitteeType
{
    /// <summary>
    /// TOR
    /// </summary>
    TOR,

    /// <summary>
    /// MedianPrice
    /// </summary>
    MedianPrice,

    /// <summary>
    /// Procurement
    /// </summary>
    ProcurementCommittee,

    /// <summary>
    /// Inspection
    /// </summary>
    InspectionCommittee,

    /// <summary>
    /// MaintenanceInspection
    /// </summary>
    MaintenanceInspectionCommittee,

    /// <summary>
    /// ConstructionSupervisor
    /// </summary>
    ConstructionSupervisor,

    /// <summary>
    /// RentCommittee
    /// </summary>
    RentCommittee,

    /// <summary>
    /// AcceptanceCommittee
    /// </summary>
    AcceptanceCommittee,
}

public enum SourceType
{
    /// <summary>
    /// Appoint
    /// </summary>
    Appoint,

    /// <summary>
    /// PurchaseRequisition
    /// </summary>
    PurchaseRequisition,

    /// <summary>
    /// Jp005
    /// </summary>
    Jp005,

    /// <summary>
    /// PrincipleApproval
    /// </summary>
    PrincipleApproval,

    /// <summary>
    /// PurchaseOrderApproval
    /// </summary>
    PurchaseOrderApproval,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CommitteeChangeId
{
    public static CommitteeChangeId New() => From(Guid.CreateVersion7());
}

public record CommitteeMember(
    Guid SuUserId,
    string FullName,
    string? FullPositionName,
    string CommitteePositionsCode,
    string CommitteePositionsName,
    int Sequence
);

public partial class CommitteeChanges : AuditableEntity<CommitteeChangeId>, IHasSoftDelete, IHasActivityInfo
{
    public override CommitteeChangeId Id { get; init; }

    public ProcurementId ProcurementId { get; private set; }

    public SourceType SourceType { get; private set; }

    public Guid SourceId { get; private set; }

    public CommitteeType CommitteeType { get; private set; }

    public CommitteeChangeStatus Status { get; private set; }

    public string? Remark { get; private set; }

    public bool IsJorPorComment { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public IEnumerable<CommitteeMember> OldCommittees { get; private set; }

    public IEnumerable<CommitteeMember> NewCommittees { get; private set; }

    public virtual Procurement Procurement { get; init; }

    public virtual IReadOnlyCollection<CommitteeChangeAcceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<CommitteeChangeAttachment> Attachments { get; private set; }

    public virtual IReadOnlyCollection<CommitteeChangeDocumentHistory> DocumentHistories { get; private set; }

    public virtual IReadOnlyCollection<CommitteeChangeAssignee> Assignees { get; private set; }

    public CommitteeChangeDocumentHistory? LastedDraftDocument =>
        this.DocumentHistories
            .Where(dh => dh.StatusState == CommitteeChangeStatus.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public CommitteeChangeDocumentHistory? LastedMaxDocument =>
    this.DocumentHistories
        .OrderVersions()
        .FirstOrDefault();

    public CommitteeChangeDocumentHistory? LastedNotReplacedCommitteeDocument =>
    this.DocumentHistories
        .Where(dh =>
            dh is
            {
                StatusState: CommitteeChangeStatus.WaitingCommitteeApproval,
                IsReplaced: false
            })
        .OrderVersions()
        .FirstOrDefault();

    public CommitteeChangeDocumentHistory? LastedNotReplacedWaitingApprovalDocument =>
    this.DocumentHistories
     .Where(dh =>
         dh is
         {
             StatusState: CommitteeChangeStatus.WaitingApproval,
             IsReplaced: false
         })
     .OrderVersions()
     .FirstOrDefault();

    public CommitteeChanges SetCommitteeChangeInfo(
        ProcurementId procurementId,
        SourceType sourceType,
        Guid sourceId,
        CommitteeType committeeType,
        IEnumerable<CommitteeMember> oldCommittees,
        IEnumerable<CommitteeMember> newCommittees,
        string? remark = null)
    {
        this.ProcurementId = procurementId;
        this.SourceType = sourceType;
        this.SourceId = sourceId;
        this.CommitteeType = committeeType;
        this.OldCommittees = oldCommittees;
        this.NewCommittees = newCommittees;
        this.Remark = remark;

        return this;
    }

    public CommitteeChanges SetDocumentDate(DateTimeOffset? date = null)
    {
        this.DocumentDate = date ?? DateTimeOffset.Now;

        return this;
    }

    public CommitteeChanges SetStatus(CommitteeChangeStatus status)
    {
        this.Status = status;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"แก้ไขข้อมูลการเปลี่ยนแปลงคณะกรรมการ",
            this.Status.ToString()));

        return this;
    }

    public CommitteeChanges SetRemark(string? remark)
    {
        this.Remark = remark;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"แก้ไขข้อมูลการเปลี่ยนแปลงคณะกรรมการ",
            this.Status.ToString()));

        return this;
    }

    public CommitteeChanges UpdateCommittees(IEnumerable<CommitteeMember> oldCommittees, IEnumerable<CommitteeMember> newCommittees)
    {
        this.OldCommittees = oldCommittees;
        this.NewCommittees = newCommittees;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"แก้ไขข้อมูลการเปลี่ยนแปลงคณะกรรมการ",
            this.Status.ToString()));

        return this;
    }

    public static CommitteeChanges Create(
        ProcurementId procurementId,
        SourceType sourceType,
        Guid sourceId,
        CommitteeType committeeType,
        IEnumerable<CommitteeMember> oldCommittees,
        IEnumerable<CommitteeMember> newCommittees,
        string? remark = null)
    {
        var newData = new CommitteeChanges
        {
            Id = CommitteeChangeId.New(),
            Status = CommitteeChangeStatus.Draft,
            Acceptors = [],
            Attachments = [],
            DocumentHistories = [],
            Assignees = [],
        };

        newData.SetCommitteeChangeInfo(
            procurementId,
            sourceType,
            sourceId,
            committeeType,
            oldCommittees,
            newCommittees,
            remark);

        newData.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"สร้างข้อมูลการเปลี่ยนแปลงคณะกรรมการ",
            newData.Status.ToString()));

        return newData;
    }

    public CommitteeChanges AddCommitteeChangeAcceptor(CommitteeChangeAcceptor committeeChangeAcceptor)
    {
        if (committeeChangeAcceptor == null)
        {
            throw new ArgumentNullException(nameof(committeeChangeAcceptor), "Acceptor cannot be null.");
        }

        if (this.Acceptors.Contains(committeeChangeAcceptor))
        {
            throw new InvalidOperationException("Acceptor already exists in the Committee Change.");
        }

        var acceptors = this.Acceptors.ToHashSet();

        acceptors.Add(committeeChangeAcceptor);

        this.Acceptors = acceptors;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Approved,
            $"เพิ่ม ผู้มีอำนาจเห็นชอบ/อนุมัติ เปลี่ยนแปลงคณะกรรมการ",
            nameof(CommitteeChangeStatus.Approved)));

        return this;
    }

    public Unit RemoveAcceptorById(AcceptorId acceptorId)
    {
        var acceptors = this.Acceptors.ToHashSet();

        acceptors.RemoveWhere(w => w.Id == acceptorId);

        this.Acceptors = acceptors;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Approved,
            $"แก้ไข ผู้มีอำนาจเห็นชอบ/อนุมัติ เปลี่ยนแปลงคณะกรรมการ",
            nameof(CommitteeChangeStatus.Approved)));

        return Unit.Default;
    }

    public Unit SetApproved(string? remark = null)
    {
        this.Status = CommitteeChangeStatus.Approved;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Approved,
            $"เห็นชอบ/อนุมัติการเปลี่ยนแปลงคณะกรรมการ",
            nameof(CommitteeChangeStatus.Approved),
            remark));

        return Unit.Default;
    }

    public Unit SetRejected(string? remark = null)
    {
        this.Status = CommitteeChangeStatus.Rejected;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            $"ส่งกลับแก้ไขการเปลี่ยนแปลงคณะกรรมการ",
            nameof(CommitteeChangeStatus.Rejected),
            remark));

        return Unit.Default;
    }

    public bool HasMajorityRejection()
    {
        if (this.Status != CommitteeChangeStatus.WaitingCommitteeApproval)
        {
            return false;
        }

        var committeesAble =
            this.Acceptors
                .Where(a => a.Type != AcceptorType.Approver && a is
                {
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

    public Unit SetRejectToAssignee(string? remark = null)
    {
        this.Status = CommitteeChangeStatus.RejectToAssignee;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            $"ส่งกลับแก้ไขการเปลี่ยนแปลงคณะกรรมการ",
            nameof(CommitteeChangeStatus.RejectToAssignee),
            remark));

        return Unit.Default;
    }

    public Unit SetCancelled()
    {
        this.Status = CommitteeChangeStatus.Cancelled;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Cancelled,
            $"ยกเลิกการเปลี่ยนแปลงคณะกรรมการ",
            nameof(CommitteeChangeStatus.Cancelled)));

        return Unit.Default;
    }

    public CommitteeChanges AddDocumentHistory(FileId fileId, bool isReplace)
    {
        var histories = this.DocumentHistories.ToHashSet();

        var latestHistory = this.DocumentHistories.OrderVersions().FirstOrDefault();
        var incrementMajor = latestHistory is not null && latestHistory.StatusState != this.Status;

        var version = this.DocumentHistories.NextVersion(incrementMajor);

        histories.Add(CommitteeChangeDocumentHistory.Create(
            this.Status,
            version,
            fileId,
            isReplace));

        this.DocumentHistories = histories;

        return this;
    }

    public CommitteeChanges AddAttachment(CommitteeChangeAttachment attachment)
    {
        if (attachment == null)
        {
            throw new ArgumentNullException(nameof(attachment), "Attachment cannot be null.");
        }

        var attachments = this.Attachments.ToHashSet();

        if (attachments.Any(a => a.Id == attachment.Id))
        {
            throw new InvalidOperationException("Attachment already exists.");
        }

        attachments.Add(attachment);

        this.Attachments = attachments;

        return this;
    }

    public CommitteeChanges RemoveAttachment(CommitteeChangeAttachment attachment)
    {
        if (attachment == null)
        {
            throw new ArgumentNullException(nameof(attachment), "Attachment cannot be null.");
        }

        var attachments = this.Attachments.ToHashSet();

        if (!attachments.Remove(attachment))
        {
            throw new InvalidOperationException("Attachment does not exist.");
        }

        this.Attachments = attachments;

        return this;
    }

    public CommitteeChanges SetIsJorPorComment(bool isJorPorComment)
    {
        this.IsJorPorComment = isJorPorComment;

        return this;
    }

    public CommitteeChanges AddAssignee(CommitteeChangeAssignee assignee)
    {
        if (assignee == null)
        {
            throw new ArgumentNullException(nameof(assignee), "Assignee cannot be null.");
        }

        var assignees = (this.Assignees ?? []).ToHashSet();

        if (assignees.Any(a => a.Id == assignee.Id))
        {
            throw new InvalidOperationException("Assignee with the same Id already exists.");
        }

        assignees.Add(assignee);

        this.Assignees = assignees;

        return this;
    }

    public CommitteeChanges RemoveAssignee(CommitteeChangeAssignee assignee)
    {
        if (assignee == null)
        {
            throw new ArgumentNullException(nameof(assignee), "Assignee cannot be null.");
        }

        var assignees = (this.Assignees ?? []).ToHashSet();

        assignees.RemoveWhere(a => a.Id == assignee.Id);

        this.Assignees = assignees;

        return this;
    }
}