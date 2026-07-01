namespace GHB.DP2.Domain.Procurement.PPurchaseOrder;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using LanguageExt;
using Vogen;

public enum PurchaseOrderStatus
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
    /// จพ. มอบหมายงาน
    /// </summary>
    WaitingAssign,

    /// <summary>
    /// จพ. มอบหมายงาน
    /// </summary>
    WaitingComment,

    /// <summary>
    /// รออนุมัติ
    /// </summary>
    WaitingApproval,

    /// <summary>
    /// อนุมัติ
    /// </summary>
    Approved,

    RejectToAssignee,

    /// <summary>
    /// ส่งกลับแก้ไข
    /// </summary>
    Rejected,

    /// <summary>
    /// ยกเลิกรายการ
    /// </summary>
    Cancelled,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PurchaseOrderId
{
    public static PurchaseOrderId New() => From(Guid.CreateVersion7());
}

[ValueObject<string>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PurchaseOrderNumber
{
    public static PurchaseOrderNumber New(ProcurementNumber procurementNumber)
    {
        if (string.IsNullOrWhiteSpace(procurementNumber.Value))
        {
            throw new ArgumentException("Procurement number cannot be null or empty.", nameof(procurementNumber));
        }

        var newNumber = $"{procurementNumber.Value}-0701";

        return From(newNumber);
    }

    public PurchaseOrderNumber Next()
    {
        if (string.IsNullOrWhiteSpace(this.Value))
        {
            throw new InvalidOperationException("Current TorDraftNumber is null or empty.");
        }

        // Assuming the TOR draft number is the format "PYYXXXXX-07XX"
        if (this.Value.Length < 7 || !this.Value.StartsWith("P"))
        {
            throw new FormatException("Invalid TorDraftNumber format.");
        }

        // Running is the two last digits after splitting by '-'
        var parts = this.Value.Split('-');

        if (parts.Length != 2 || parts[1].Length != 4 || !int.TryParse(parts[1], out var running))
        {
            throw new FormatException("Invalid TorDraftNumber format.");
        }

        running++;

        var newNumber = $"{parts[0]}-{running:D4}";

        return From(newNumber);
    }
}

public partial class PPurchaseOrder : AuditableEntity<PurchaseOrderId>, IHasSoftDelete, IHasActivityInfo
{
    public override PurchaseOrderId Id { get; init; }

    public ProcurementId ProcurementId { get; init; }

    public PurchaseOrderNumber PurchaseOrderNumber { get; init; }

    public PurchaseOrderStatus Status { get; private set; }

    public bool? IsMigration { get; init; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public virtual Procurement Procurement { get; init; }

    public virtual IReadOnlyCollection<PPurchaseOrderEntrepreneur> Entrepreneurs { get; private set; }

    public virtual IReadOnlyCollection<PPurchaseOrderAcceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<PPurchaseOrderAssignee> Assignees { get; private set; }

    public virtual IReadOnlyCollection<PurchaseOrderDocumentHistory> DocumentHistories { get; private set; }

    public PurchaseOrderDocumentHistory? GetLatestDocumentHistory(PurchaseOrderDocumentType documentType)
    {
        return this.DocumentHistories
                   .Where(dh => dh.DocumentType == documentType)
                   .OrderVersions()
                   .FirstOrDefault();
    }

    public PurchaseOrderDocumentHistory? GetIsReplacedDocumentHistory(
        PurchaseOrderDocumentType documentType,
        Func<PurchaseOrderDocumentHistory, bool> predicate)
    {
        return this.DocumentHistories
                   .Where(dh => dh.DocumentType == documentType && dh.IsReplaced)
                   .Where(predicate)
                   .OrderVersions()
                   .FirstOrDefault();
    }

    public Unit AddDocumentHistory(
        PurchaseOrderDocumentType documentType,
        FileId fileId,
        bool? isReplace = false,
        bool incrementMajor = false)
    {
        var histories = this.DocumentHistories.ToHashSet();

        var existingHistory =
            histories
                .Where(dh => dh.DocumentType == documentType)
                .OrderVersions()
                .FirstOrDefault();

        var isIncreaseMajorVersion =
            existingHistory is null ||
            existingHistory.StatusState != this.Status;

        var version = this.DocumentHistories
                          .Where(p =>
                              p.DocumentType == documentType)
                          .NextVersion(incrementMajor || isIncreaseMajorVersion);

        histories.Add(PurchaseOrderDocumentHistory.Create(
            documentType,
            histories.Any() ? this.Status : PurchaseOrderStatus.Draft,
            version,
            fileId,
            isReplace));

        this.DocumentHistories = histories;

        return unit;
    }

    public static PPurchaseOrder Create(
        Procurement procurement)
    {
        if (procurement.ProcurementNumber is null)
        {
            throw new ArgumentException("Procurement number cannot be null.", nameof(procurement));
        }

        var newData = new PPurchaseOrder
        {
            Id = PurchaseOrderId.New(),
            ProcurementId = procurement.Id,
            PurchaseOrderNumber = PurchaseOrderNumber.New(procurement.ProcurementNumber!.Value),
            Status = PurchaseOrderStatus.Draft,
            Acceptors = [],
            Assignees = [],
            Entrepreneurs = [],
            DocumentHistories = [],
        };

        newData.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"สร้างข้อมูลการแจ้งข้อมูลขออนุุมัติสั่งซื้อ/สั่งจ้าง(จพ.006)",
            newData.Status.ToString()));

        return newData;
    }

    public PPurchaseOrder AddEntrepreneur(PPurchaseOrderEntrepreneur entrepreneur)
    {
        if (entrepreneur == null)
        {
            throw new ArgumentNullException(nameof(entrepreneur));
        }

        var entrepreneurs = this.Entrepreneurs.ToHashSet();

        if (entrepreneurs.Any(e => e.Id == entrepreneur.Id))
        {
            throw new InvalidOperationException("Entrepreneur already exists.");
        }

        entrepreneurs.Add(entrepreneur);

        this.Entrepreneurs = entrepreneurs;

        return this;
    }

    public PPurchaseOrder RemoveEntrepreneur(PPurchaseOrderEntrepreneur entrepreneur)
    {
        if (entrepreneur == null)
        {
            throw new ArgumentNullException(nameof(entrepreneur));
        }

        var entrepreneurs = this.Entrepreneurs.ToHashSet();

        if (!entrepreneurs.Remove(entrepreneur))
        {
            throw new InvalidOperationException("Entrepreneur not found.");
        }

        this.Entrepreneurs = entrepreneurs;

        return this;
    }

    public PPurchaseOrder AddAcceptor(PPurchaseOrderAcceptor acceptor)
    {
        if (acceptor == null)
        {
            throw new ArgumentNullException(nameof(acceptor));
        }

        var acceptors = this.Acceptors.ToHashSet();

        if (acceptors.Any(a => a.Id == acceptor.Id))
        {
            throw new InvalidOperationException("Acceptor with the same Id already exists.");
        }

        acceptors.Add(acceptor);

        this.Acceptors = acceptors;

        return this;
    }

    public PPurchaseOrder AddAssignee(PPurchaseOrderAssignee assign)
    {
        if (assign == null)
        {
            throw new ArgumentNullException(nameof(assign));
        }

        var assignees = (this.Assignees ?? []).ToHashSet();

        if (assignees.Any(a => a.Id == assign.Id))
        {
            throw new InvalidOperationException("Assignee with the same Id already exists.");
        }

        assignees.Add(assign);

        this.Assignees = assignees;

        return this;
    }

    public PPurchaseOrder RemoveAcceptor(PPurchaseOrderAcceptor acceptor)
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

    public PPurchaseOrder RemoveAssignee(PPurchaseOrderAssignee assign)
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

    public PPurchaseOrder SetDocumentDate(DateTimeOffset? date = null)
    {
        this.DocumentDate = date ?? DateTimeOffset.Now;

        return this;
    }

    public PPurchaseOrder SetEdit()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Recall,
            $"ส่งกลับแก้ไขข้อมูลขออนุุมัติสั่งซื้อ/สั่งจ้าง(จพ.006)",
            PurchaseOrderStatus.Edit.ToString()));

        this.Status = PurchaseOrderStatus.Edit;

        _ = this.Acceptors
                .Where(a => a is
                {
                    Type: AcceptorType.ProcurementCommittee,
                    IsActive: true
                })
                .Iter(a => a.Draft());

        return this;
    }

    public PPurchaseOrder SetWaitingCommitteeApproval()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.SendCommitteeApprove,
            ActivityLogActionTypeConstant.SendCommitteeApprove,
            nameof(PurchaseOrderStatus.WaitingCommitteeApproval)));

        this.Status = PurchaseOrderStatus.WaitingCommitteeApproval;

        _ = this.Acceptors
                .Where(a => a is
                {
                    Type: AcceptorType.ProcurementCommittee,
                    IsUnableToPerformDuties: false,
                    IsActive: true
                })
                .Iter(a => a.Pending());

        return this;
    }

    public PPurchaseOrder SetWaitingAcceptor()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.SendApprove,
            $"ส่งเห็นชอบ/อนุมัติข้อมูลขออนุุมัติสั่งซื้อ/สั่งจ้าง(จพ.006)",
            PurchaseOrderStatus.WaitingApproval.ToString()));

        this.Status = PurchaseOrderStatus.WaitingApproval;

        var approvers = this.Acceptors
                            .Where(p => p.Type == AcceptorType.Approver)
                            .OrderBy(a => a.Sequence)
                            .ToList();

        approvers.Iter(a =>
        {
            a.SetCurrent(false)
             .Pending();
        });

        var firstPending = approvers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

        if (firstPending != null)
        {
            firstPending.SetCurrent();
        }

        return this;
    }

    public bool HasMajorityRejection()
    {
        if (this.Status != PurchaseOrderStatus.WaitingCommitteeApproval)
        {
            throw new InvalidOperationException("Cannot evaluate committee approval in the current status.");
        }

        var committeesAble =
            this.Acceptors
                .Where(a => a is
                {
                    Type: AcceptorType.ProcurementCommittee,
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

    public PPurchaseOrder SetAssigned()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Assigned,
            $"รอเจ้าหน้าที่พัสดุให้ความเห็นข้อมูลขออนุุมัติสั่งซื้อ/สั่งจ้าง(จพ.006)",
            PurchaseOrderStatus.WaitingComment.ToString()));

        this.Status = PurchaseOrderStatus.WaitingComment;

        return this;
    }

    public PPurchaseOrder SetApproved(string? remark)
    {
        if (this.Entrepreneurs.Where(x => x.IsWinner).SelectMany(x => x.PJp006PriceDetails).Sum(x => x.AgreedPrice * x.ParcelQuantity) > 1000000)
        {
            this.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Approved,
                $"เห็นชอบ/อนุมัติข้อมูลขออนุุมัติสั่งซื้อ/สั่งจ้าง(จพ.006)",
                PurchaseOrderStatus.Approved.ToString(),
                remark));
        }
        else
        {
            this.AddActivity(new ActivityInfo(
                "สายงานเห็นชอบ/อนุมัตื",
                $"ผู้มีอำนาจเห็นชอบ/อนุมัติ",
                this.Status.ToString(),
                remark));
        }

        this.Status = PurchaseOrderStatus.Approved;
        this.Procurement.SetProcessType(ProcessType.PurchaseOrderApproval);

        return this;
    }

    public PPurchaseOrder SetWaitingAssignee()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.WaitingAssign,
            $"รอมอบหมายผู้รับผิดชอบข้อมูลขออนุุมัติสั่งซื้อ/สั่งจ้าง(จพ.006)",
            PurchaseOrderStatus.WaitingAssign.ToString()));

        this.Status = PurchaseOrderStatus.WaitingAssign;

        return this;
    }

    public PPurchaseOrder SetRejected()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            $"ส่งกลับแก้ไขข้อมูลขออนุุมัติสั่งซื้อ/สั่งจ้าง(จพ.006)",
            PurchaseOrderStatus.Rejected.ToString()));

        this.Status = PurchaseOrderStatus.Rejected;

        return this;
    }

    public PPurchaseOrder SetRejectToAssignee()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            $"่งกลับแก้ไขข้อมูลขออนุุมัติสั่งซื้อ/สั่งจ้าง(จพ.006)",
            PurchaseOrderStatus.RejectToAssignee.ToString()));

        this.Status = PurchaseOrderStatus.RejectToAssignee;

        return this;
    }
}