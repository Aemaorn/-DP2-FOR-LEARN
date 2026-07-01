namespace GHB.DP2.Domain.Procurement.PpMedianPrice;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct MedianPriceId
{
    public static MedianPriceId New() => From(Guid.CreateVersion7());
}

public enum MedianPriceStatus
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
    /// อยู่ระหว่างหน่วยงานเห็นชอบ
    /// </summary>
    WaitingUnitApproval,

    /// <summary>
    /// รอ จพ. มอบหมาย
    /// </summary>
    WaitingAssign,

    /// <summary>
    /// อยู่ระหว่าง จพ. ให้ความเห็น
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

    /// <summary>
    /// ส่งกลับแก้ไข (ส่งกลับแก้ไขไปยัง จพ.)
    /// </summary>
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

[ValueObject<string>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct MedianPriceNumber
{
    public static MedianPriceNumber New(ProcurementNumber procurementNumber)
    {
        if (string.IsNullOrWhiteSpace(procurementNumber.Value))
        {
            throw new ArgumentException("Procurement number cannot be null or empty.", nameof(procurementNumber));
        }

        var newNumber = $"{procurementNumber.Value}-0301";

        return From(newNumber);
    }

    public MedianPriceNumber Next()
    {
        if (string.IsNullOrWhiteSpace(this.Value))
        {
            throw new InvalidOperationException("Current TorDraftNumber is null or empty.");
        }

        // Assuming the TOR draft number is the format "PYYXXXXX-03XX"
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

/// <summary>
/// กำหนดราคากลาง
/// </summary>
public partial class PpMedianPrice : AuditableEntity<MedianPriceId>, IHasSoftDelete, IHasActivityInfo
{
    public override MedianPriceId Id { get; init; }

    public MedianPriceId? ReferenceId { get; init; }

    public ProcurementId ProcurementId { get; init; }

    public MedianPriceNumber ReferenceNumber { get; init; }

    public string Object { get; private set; }

    public string Reason { get; private set; }

    public string SpecialDescription { get; private set; }

    public string? JobDescription { get; private set; }

    public string PriceReasonablenessInfo { get; private set; }

    public MedianPriceStatus Status { get; private set; }

    public bool IsChange { get; init; }

    public bool IsCancel { get; init; }

    public bool IsActive { get; private set; }

    public bool? IsMigration { get; init; }

    public string? CancelReason { get; private set; }

    public string? ChangeReason { get; private set; }

    public string? Telephone { get; set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public SuDocumentTemplateId DocumentTemplateId { get; private set; }

    public virtual SuDocumentTemplate DocumentTemplate { get; init; }

    public virtual Procurement Procurement { get; init; }

    public virtual IReadOnlyCollection<PpMedianPriceBudgetAllocations> BudgetAllocations { get; private set; }

    public PpMedianPriceBudgetAllocations BudgetAllocation
        => this.BudgetAllocations.FirstOrDefault()
           ?? throw new InvalidOperationException("No budget allocations found.");

    public virtual IReadOnlyCollection<PpMedianPriceStaff> Staff { get; private set; }

    public PpMedianPriceStaff? StaffMember
        => this.Staff.FirstOrDefault();

    public virtual IReadOnlyCollection<PpMedianPriceAcceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<PpMedianPriceAssignee> Assignees { get; private set; }

    public PpMedianPriceAssignee? AssigneeDirector
        => this.Assignees
               .FirstOrDefault(a =>
                   a.Type == AssigneeType.Director && a.Status == AssigneeStatus.Draft);

    public virtual PpMedianPriceExpenseDescription? ExpenseDescription { get; private set; }

    public virtual IReadOnlyCollection<PpMedianPriceDocumentHistory> DocumentHistories { get; private set; }

    public PpMedianPriceDocumentHistory? LastedDocument =>
        this.DocumentHistories
            .OrderVersions()
            .FirstOrDefault();

    public PpMedianPriceDocumentHistory? LastedDraftDocument =>
        this.DocumentHistories
            .Where(dh =>
                dh.StatusState == MedianPriceStatus.Draft ||
                dh.StatusState == MedianPriceStatus.Edit ||
                dh.StatusState == MedianPriceStatus.Rejected ||
                dh.StatusState == MedianPriceStatus.WaitingComment)
            .OrderVersions()
            .FirstOrDefault();

    public PpMedianPriceDocumentHistory? LastedNotReplacedDocument =>
        this.DocumentHistories
            .Where(dh =>
                dh is
                {
                    StatusState: MedianPriceStatus.WaitingCommitteeApproval,
                    IsReplaced: false
                })
            .OrderVersions()
            .FirstOrDefault();

    public PpMedianPriceDocumentHistory? LastedNotReplacedWaitingApprovalDocument =>
        this.DocumentHistories
            .Where(dh =>
                dh is
                {
                    StatusState: MedianPriceStatus.WaitingApproval,
                    IsReplaced: false
                })
            .OrderVersions()
            .FirstOrDefault();

    public Unit AddDocumentHistory(
        FileId fileId,
        bool isReplace = false)
    {
        var histories = this.DocumentHistories.ToHashSet();

        var existingHistory =
            histories
                .OrderVersions()
                .FirstOrDefault();

        var isIncreaseMajorVersion =
            existingHistory is null ||
            existingHistory.StatusState != this.Status;

        var version =
            this.DocumentHistories
                .NextVersion(isIncreaseMajorVersion);

        histories.Add(PpMedianPriceDocumentHistory.Create(
            histories.Any() ? this.Status : MedianPriceStatus.Draft,
            version,
            fileId,
            isReplace));

        this.DocumentHistories = histories;

        return unit;
    }

    public PpMedianPrice SetDocumentTemplate(SuDocumentTemplateId documentTemplateId)
    {
        this.DocumentTemplateId = documentTemplateId;

        return this;
    }

    public PpMedianPrice SetTelephone(string? telephone)
    {
        this.Telephone = telephone;

        return this;
    }

    public PpMedianPrice AddBudgetAllocations(PpMedianPriceBudgetAllocations budgetAllocations)
    {
        if (budgetAllocations == null)
        {
            throw new ArgumentNullException(nameof(budgetAllocations));
        }

        var budgets = this.BudgetAllocations.ToHashSet();

        if (budgets.Any(b => b.Id == budgetAllocations.Id))
        {
            throw new InvalidOperationException("Budget allocations with the same Id already exists.");
        }

        budgets.Add(budgetAllocations);

        this.BudgetAllocations = budgets;

        return this;
    }

    public PpMedianPrice AddExpenseDescription(PpMedianPriceExpenseDescription expenseDescription)
    {
        this.ExpenseDescription = expenseDescription ?? throw new ArgumentNullException(nameof(expenseDescription));

        return this;
    }

    public PpMedianPrice AddStaff(PpMedianPriceStaff staff)
    {
        if (staff == null)
        {
            throw new ArgumentNullException(nameof(staff));
        }

        var staffs = this.Staff.ToHashSet();

        if (staffs.Any(s => s.Id == staff.Id))
        {
            throw new InvalidOperationException("Staff with the same Id already exists.");
        }

        staffs.Add(staff);

        this.Staff = staffs;

        return this;
    }

    public PpMedianPrice SetActive(bool isActive)
    {
        this.IsActive = isActive;

        return this;
    }

    public PpMedianPrice AddAcceptor(PpMedianPriceAcceptor acceptor)
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

    public PpMedianPrice AddAssignee(PpMedianPriceAssignee assign)
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

    public PpMedianPrice RemoveAcceptor(PpMedianPriceAcceptor acceptor)
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

    public PpMedianPrice RemoveAssignee(PpMedianPriceAssignee assign)
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

    public PpMedianPrice UpdateBudgetAllocations(
        BudgetAllocationsId budgetAllocationsId,
        Action<PpMedianPriceBudgetAllocations> updateAction)
    {
        if (updateAction == null)
        {
            throw new ArgumentNullException(nameof(updateAction));
        }

        var budgets = this.BudgetAllocations.ToHashSet();
        var budget = budgets.FirstOrDefault(b => b.Id == budgetAllocationsId);

        if (budget == null)
        {
            throw new InvalidOperationException("Budget allocations not found.");
        }

        updateAction(budget);

        this.BudgetAllocations = budgets;

        return this;
    }

    public PpMedianPrice UpdateExpenseDescription(
        Action<PpMedianPriceExpenseDescription> updateAction)
    {
        this.ExpenseDescription ??= PpMedianPriceExpenseDescription.Create();

        updateAction(this.ExpenseDescription);

        return this;
    }

    public PpMedianPrice UpdateStaff(
        MedianPriceStaffId staffId,
        Action<PpMedianPriceStaff> updateAction)
    {
        if (updateAction == null)
        {
            throw new ArgumentNullException(nameof(updateAction));
        }

        var staffs = this.Staff.ToHashSet();
        var staff = staffs.FirstOrDefault(s => s.Id == staffId);

        if (staff == null)
        {
            throw new InvalidOperationException("Staff not found.");
        }

        updateAction(staff);

        this.Staff = staffs;

        return this;
    }

    public PpMedianPrice SetObject(string @object)
    {
        this.Object = @object;

        return this;
    }

    public PpMedianPrice SetReason(string reason)
    {
        this.Reason = reason;

        return this;
    }

    public PpMedianPrice SetSpecialDescription(string specialDescription)
    {
        this.SpecialDescription = specialDescription;

        return this;
    }

    public PpMedianPrice SetDocumentDate(DateTimeOffset? documentDate = null)
    {
        this.DocumentDate = documentDate ?? DateTimeOffset.Now;

        return this;
    }

    public PpMedianPrice SetJobDescription(string? jobDescription)
    {
        this.JobDescription = jobDescription;

        return this;
    }

    public PpMedianPrice SetPriceReasonablenessInfo(string priceReasonablenessInfo)
    {
        this.PriceReasonablenessInfo = priceReasonablenessInfo;

        return this;
    }

    public PpMedianPrice SetEdit()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Recall,
            $"ขอเปลี่ยนแปลงข้อมูล",
            nameof(MedianPriceStatus.Edit)));

        this.Status = MedianPriceStatus.Edit;

        _ = this.Acceptors
                .Where(a => a is
                {
                    Type: AcceptorType.MedianPriceCommittee,
                    IsActive: true
                })
                .Iter(a => a.Draft());

        return this;
    }

    public PpMedianPrice SetWaitingCommitteeApproval()
    {
        this.AddActivity(new ActivityInfo(
            "ส่งบุคคล/คณะกรรมการกำหนดราคากลาง",
            $"ส่งคณะกรรมการเห็นชอบ/อนุมัติ",
            nameof(MedianPriceStatus.WaitingCommitteeApproval)));

        this.Status = MedianPriceStatus.WaitingCommitteeApproval;

        _ = this.Acceptors
                .Where(a => a is
                {
                    Type: AcceptorType.MedianPriceCommittee,
                    IsUnableToPerformDuties: false,
                    IsActive: true
                })
                .Iter(a => a.Pending());

        return this;
    }

    public PpMedianPrice SetWaitingAcceptor()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.SendApprove,
            $"ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ",
            nameof(MedianPriceStatus.WaitingApproval)));

        this.Status = MedianPriceStatus.WaitingApproval;

        var approvers = this.Acceptors
                            .Where(p => p.Type == AcceptorType.Approver)
                            .OrderBy(a => a.Sequence)
                            .ToList();

        approvers.Iter(a =>
        {
            if (a.Status == AcceptorStatus.Draft || a.Status == AcceptorStatus.Rejected || a.Status == AcceptorStatus.Approved)
            {
                a.Pending();
            }

            a.SetCurrent(false);
        });

        var firstPending = approvers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending || a.Status == AcceptorStatus.Rejected);

        if (firstPending != null)
        {
            firstPending.SetCurrent();
        }

        return this;
    }

    private PpMedianPrice SetWaitingUnitApproval()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.SendUnitApprove,
            $"ส่งสายงานเห็นชอบ/อนุมัติ",
            nameof(MedianPriceStatus.WaitingUnitApproval)));

        this.Status = MedianPriceStatus.WaitingUnitApproval;

        _ = this.Acceptors
                .Where(a => a is
                {
                    Type: AcceptorType.DepartmentDirectorAgree,
                    IsActive: true
                })
                .Iter(a => a.Pending());

        return this;
    }

    public bool HasMajorityRejection()
    {
        if (this.Status != MedianPriceStatus.WaitingCommitteeApproval)
        {
            return false;
        }

        var committeesAble =
            this.Acceptors
                .Where(a => a is
                {
                    Type: AcceptorType.MedianPriceCommittee,
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

    private PpMedianPrice EvaluateCommitteeApproval()
    {
        if (this.Status != MedianPriceStatus.WaitingCommitteeApproval)
        {
            throw new InvalidOperationException("Cannot evaluate committee approval in the current status.");
        }

        _ = this.Procurement.HasMd
            ? this.SetWaitingUnitApproval()
            : this.SetWaitingAcceptor();

        return this;
    }

    public PpMedianPrice SetWaitingAssign()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.WaitingAssign,
            $"รอเจ้าหน้าที่พัสดุมอบหมายผู้รับผิดชอบ",
            nameof(MedianPriceStatus.WaitingAssign)));

        this.Status = MedianPriceStatus.WaitingAssign;

        _ = this.Assignees
                .Iter(a => a.Pending());

        return this;
    }

    public PpMedianPrice SetWaitingComment()
    {
        this.AddActivity(new ActivityInfo(
            "ยืนยันมอบหมาย",
            $"รอเจ้าหน้าที่พัสดุใเห้ความเห็น",
            nameof(MedianPriceStatus.WaitingComment)));

        this.Status = MedianPriceStatus.WaitingComment;

        _ = this.Assignees
                .Where(w => w.Status is not AssigneeStatus.Pending)
                .Iter(a => a.Pending());

        _ = this.Assignees
                .Iter(r => r.Assigned());

        return this;
    }

    private PpMedianPrice SetApproved()
    {
        var type = this.IsChange ? ActivityLogActionTypeConstant.Changed : ActivityLogActionTypeConstant.Approved;

        this.AddActivity(new ActivityInfo(
            type,
            "ผู้มีอำนาจเห็นชอบ/อนุมัติ",
            nameof(MedianPriceStatus.Approved)));

        this.Status = MedianPriceStatus.Approved;

        this.Procurement.SetProcessType(ProcessType.PurchaseRequisition);

        return this;
    }

    private PpMedianPrice SetCancelled()
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Cancelled,
            $"ผู้มีอำนาจเห็นชอบ/อนุมัติ",
            nameof(MedianPriceStatus.Cancelled)));

        this.Status = MedianPriceStatus.Cancelled;
        this.Procurement.SetProcessType(ProcessType.MedianPrice);
        this.Procurement.SetStatus(ProcurementStatus.Cancelled);

        return this;
    }

    public PpMedianPrice SetRejectToAssignee(string? remark)
    {
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            $"ส่งกลับแก้ไข",
            nameof(MedianPriceStatus.RejectToAssignee),
            remark));

        this.Assignees.Iter(r => r.Draft());

        this.Status = MedianPriceStatus.RejectToAssignee;

        return this;
    }

    public PpMedianPrice SetRejected(string? remark, MedianPriceStatus? status = null)
    {
        switch (status)
        {
            case MedianPriceStatus.WaitingCommitteeApproval:
                this.AddActivity(new ActivityInfo(
                    "บุคคล/คณะกรรมการกำหนดราคากลาง ไม่เห็นชอบ",
                    $"ส่งกลับแก้ไข",
                    nameof(MedianPriceStatus.Rejected),
                    remark));

                break;

            case MedianPriceStatus.WaitingUnitApproval:
                this.AddActivity(new ActivityInfo(
                    "สายงานส่งกลับแก้ไข",
                    $"ส่งกลับแก้ไข",
                    nameof(MedianPriceStatus.Rejected),
                    remark));

                break;

            case MedianPriceStatus.WaitingAssign:
                this.AddActivity(new ActivityInfo(
                    "เจ้าหน้าที่พัสดุส่งกลับแก้ไข",
                    string.Empty,
                    nameof(MedianPriceStatus.Rejected),
                    remark));

                break;

            default:
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Reject,
                    $"ส่งกลับแก้ไข",
                    nameof(MedianPriceStatus.Rejected),
                    remark));

                break;
        }

        this.Status = MedianPriceStatus.Rejected;

        return this;
    }

    public PpMedianPrice EvaluateAcceptorApproval()
    {
        var acceptorType =
            this.Status switch
            {
                MedianPriceStatus.WaitingCommitteeApproval => AcceptorType.MedianPriceCommittee,
                MedianPriceStatus.WaitingUnitApproval => AcceptorType.DepartmentDirectorAgree,
                MedianPriceStatus.WaitingApproval => AcceptorType.Approver,
                _ => throw new InvalidOperationException("สถานะการอนุมัติไม่รองรับ"),
            };

        // Check if all acceptors of this type have approved
        var allApproved =
            this.Status == MedianPriceStatus.WaitingCommitteeApproval
                ? this.Acceptors
                      .Where(a =>
                          a.Type == acceptorType &&
                          a.IsActive &&
                          !a.IsUnableToPerformDuties &&
                          a.Status != AcceptorStatus.Rejected)
                      .All(a => a.Status == AcceptorStatus.Approved)
                : this.Acceptors
                      .Where(a =>
                          a.Type == acceptorType &&
                          a.IsActive)
                      .All(a => a.Status == AcceptorStatus.Approved);

        if (!allApproved)
        {
            return this;
        }

        _ = (acceptorType, this.IsCancel, this.IsChange) switch
        {
            (AcceptorType.MedianPriceCommittee, _, _) =>
                this.EvaluateCommitteeApproval(),

            (AcceptorType.DepartmentDirectorAgree, _, _) =>
                this.SetWaitingAssign(),

            (AcceptorType.Approver, false, _) =>
                this.SetApproved(),

            (AcceptorType.Approver, true, _) =>
                this.SetCancelled(),

            _ => throw new InvalidOperationException("สถานะการอนุมัติหรือกลุ่มไม่รองรับ"),
        };

        return this;
    }

    public PpMedianPrice EvaluateCommitteeRejection()
    {
        if (this.Status != MedianPriceStatus.WaitingCommitteeApproval)
        {
            throw new InvalidOperationException("Cannot evaluate committee rejection in the current status.");
        }

        if (this.HasMajorityRejection())
        {
            _ = this.SetRejected(null);
        }

        return this;
    }

    public static PpMedianPrice Create(Procurement procurement)
    {
        if (procurement.ProcurementNumber == null)
        {
            throw new ArgumentException("Procurement number cannot be null.", nameof(procurement));
        }

        var newData = new PpMedianPrice
        {
            Id = MedianPriceId.New(),
            ProcurementId = procurement.Id,
            ReferenceNumber = MedianPriceNumber.New(procurement.ProcurementNumber!.Value),
            Status = MedianPriceStatus.Draft,
            BudgetAllocations = [],
            Staff = [],
            Acceptors = [],
            Assignees = [],
            DocumentHistories = [],
            IsActive = true,
        };

        newData.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"สร้างข้อมูล",
            newData.Status.ToString()));

        return newData;
    }

    public PpMedianPrice SetChangeReason(string reason)
    {
        this.ChangeReason = reason;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.RequestChange,
            $"อัปเดตข้อมูล",
            this.Status.ToString(),
            reason));

        return this;
    }

    public PpMedianPrice SetCancelReason(string reason)
    {
        this.CancelReason = reason;

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.RequestCancel,
            $"อัปเดตข้อมูล",
            this.Status.ToString(),
            reason));

        return this;
    }

    public PpMedianPrice Clone(bool isCancel)
    {
        var message = isCancel ? "ขอยกเลิก" : "ขอเปลี่ยนแปลง";

        this.AddActivity(
            new ActivityInfo(
                isCancel ? ActivityLogActionTypeConstant.RequestCancel : ActivityLogActionTypeConstant.RequestChange,
                $"{message}",
                nameof(MedianPriceStatus.Draft)));

        this.IsActive = false;
        this.Procurement.SetProcessType(ProcessType.MedianPrice);

        if (this.Procurement.ProcurementNumber == null)
        {
            throw new InvalidOperationException("Procurement number cannot be null.");
        }

        var medianPriceNumber =
            this.ReferenceNumber.Value.StartsWith($"{this.Procurement.ProcurementNumber}-03")
                ? this.ReferenceNumber.Next()
                : MedianPriceNumber.New(this.Procurement.ProcurementNumber!.Value);

        var newMdp = new PpMedianPrice
        {
            Id = MedianPriceId.New(),
            ReferenceId = this.Id,
            ProcurementId = this.ProcurementId,
            ReferenceNumber = medianPriceNumber,
            Object = this.Object,
            Reason = this.Reason,
            SpecialDescription = this.SpecialDescription,
            JobDescription = this.JobDescription,
            PriceReasonablenessInfo = this.PriceReasonablenessInfo,
            Telephone = this.Telephone,
            Status = MedianPriceStatus.Draft,
            IsChange = !isCancel,
            IsCancel = isCancel,
            IsActive = true,
            BudgetAllocations = this.BudgetAllocations.Map(i => i.Clone()).ToHashSet(),
            Staff = this.Staff.Map(r => r.Clone()).ToHashSet(),
            ExpenseDescription = this.ExpenseDescription != null
                ? PpMedianPriceExpenseDescription
                  .Create()
                  .SetMaterialCost(this.ExpenseDescription.MaterialCost)
                  .SetOverseasTravelCost(this.ExpenseDescription.OverseasTravelCost)
                  .SetOtherExpenses(this.ExpenseDescription.OtherExpenses)
                  .SetHardwareCost(this.ExpenseDescription.HardwareCost)
                  .SetSoftwareCost(this.ExpenseDescription.SoftwareCost)
                  .SetSystemDevelopmentCost(this.ExpenseDescription.SystemDevelopmentCost)
                : null,
            Acceptors = this.Acceptors.Map(i => i.Clone()).ToHashSet(),
            Assignees = this.Assignees.Map(i => i.Clone()).ToHashSet(),
            DocumentHistories = [],
        };

        return newMdp;
    }
}