namespace GHB.DP2.Domain.Plan;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using System.ComponentModel;
using Vogen;

public enum PlanAction
{
    /// <summary>
    /// Reject Plan (ส่งกลับแก้ไขแผน)
    /// </summary>
    [Description("ส่งกลับแก้ไขแผน")]
    RejectPlan,

    /// <summary>
    /// Reject Plan (เรียกคืนแก้ไขแผน)
    /// </summary>
    [Description("เรียกคืนแก้ไขแผน")]
    EditPlan,

    /// <summary>
    /// Approve Plan (อนุมัติแผน)
    /// </summary>
    [Description("อนุมัติแผน")]
    ApprovePlan,

    /// <summary>
    /// Approve Plan (อนุมัติแผน)
    /// </summary>
    [Description("ผู้รับผิดชอบส่งกลับแก้ไข")]
    AssigneeRejected,

    /// <summary>
    /// Assign Assignee (มอบหมายผู้รับผิดชอบ)
    /// </summary>
    [Description("มอบหมายผู้รับผิดชอบ")]
    AssignAssignee,

    /// <summary>
    /// Approved Assignee (ยืนยันมอบหมายผู้รับผิดชอบ)
    /// </summary>
    [Description("ยืนยันมอบหมายผู้รับผิดชอบ")]
    ApprovedAssignee,

    /// <summary>
    /// Assign Acceptor (จัดการผู้มีอำนาจเห็นชอบ)
    /// </summary>
    [Description("จัดการผู้มีอำนาจเห็นชอบ")]
    AssignAcceptor,

    /// <summary>
    /// Confirm Acceptor (ยืนยันผู้มีอำนาจเห็นชอบ)
    /// </summary>
    [Description("ยืนยันผู้มีอำนาจเห็นชอบ")]
    ConfirmAcceptor,

    /// <summary>
    /// Recall Document (เรียกคืนแก้ไขเอกสาร)
    /// </summary>
    [Description("เรียกคืนแก้ไขเอกสาร")]
    RecallDocument,

    /// <summary>
    /// Approved Acceptor (เห็นชอบ)
    /// </summary>
    [Description("เห็นชอบ")]
    ApprovedAcceptor,

    /// <summary>
    /// Rejected Acceptor (ส่งกลับแก้ไขเอกสาร)
    /// </summary>
    [Description("ส่งกลับแก้ไขเอกสาร")]
    RejectedAcceptor,

    /// <summary>
    /// Announcement (ผอ.จพ.ประกาศเผยแพร่)
    /// Status Announcement
    /// </summary>
    [Description("ผอ.จพ.ประกาศเผยแพร่")]
    Announcement,

    /// <summary>
    /// ปิดงาน
    /// </summary>
    [Description("ปิดงาน")]
    ClosePlan,

    /// <summary>
    /// ยกเลิกปิดงาน
    /// </summary>
    [Description("ยกเลิกปิดงาน")]
    CancelClosePlan,
}

public enum PlanType
{
    /// <summary>
    /// Annual Plan (แผนรวมปี)
    /// </summary>
    [Description("แผนรวมปี")]
    AnnualPlan,

    /// <summary>
    /// Interim Plan (แผนระหว่างปี)
    /// </summary>
    [Description("แผนระหว่างปี")]
    InYearPlan,
}

public enum PlanStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    [Description("แบบร่าง")]
    DraftPlan,

    /// <summary>
    /// เรียกคืนแก้ไข
    /// </summary>
    [Description("เรียกคืนแก้ไข")]
    EditPlan,

    /// <summary>
    /// ส่งผอ.ฝ่ายเห็นชอบ
    /// </summary>
    [Description("ส่งผอ.ฝ่ายเห็นชอบ")]
    WaitingApprovePlan,

    /// <summary>
    /// รอมอบหมาย
    /// </summary>
    [Description("รอมอบหมาย")]
    WaitingAssign,

    /// <summary>
    /// จพ. มอบหมายงาน
    /// </summary>
    [Description("จพ. มอบหมายงาน")]
    Assigned,

    /// <summary>
    /// จัดทำเอกสารบันทึก
    /// </summary>
    [Description("จัดทำเอกสารบันทึก")]
    DraftRecordDocument,

    /// <summary>
    /// รออนุมัติ
    /// </summary>
    [Description("รออนุมัติ")]
    WaitingAcceptor,

    /// <summary>
    /// อนุมัติ
    /// </summary>
    [Description("อนุมัติ")]
    ApprovePlan,

    /// <summary>
    /// รอเผยแพร่ประกาศแผน
    /// </summary>
    [Description("รอเผยแพร่ประกาศแผน")]
    WaitingAnnouncement,

    /// <summary>
    /// เผยแพร่ประกาศแผน
    /// </summary>
    [Description("เผยแพร่ประกาศแผน")]
    Announcement,

    /// <summary>
    /// ส่งกลับแก้ไข
    /// </summary>
    [Description("ส่งกลับแก้ไข")]
    RejectPlan,

    /// <summary>
    /// ผู้มีอำนาจเห็นชอบส่งกลับแก้ไข
    /// </summary>
    [Description("ผู้มีอำนาจเห็นชอบส่งกลับแก้ไข")]
    RejectToAssignee,

    /// <summary>
    /// ยกเลิกรายการ
    /// </summary>
    [Description("ยกเลิกรายการ")]
    CancelPlan,

    /// <summary>
    /// ปิดงาน
    /// </summary>
    [Description("ปิดงาน")]
    Closed,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PlanId
{
    public static PlanId New() => From(Guid.CreateVersion7());
}

[ValueObject<string>(Conversions.EfCoreValueConverter)]
public partial struct PlanNumber
{
    public static PlanNumber New(int year)
    {
        if (year <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be greater than 0.");
        }

        // Generate a new plan number in the format "PYY00001"
        // where YY is the last two digits of the year and 00001 is the initial number
        var yearString = (year % 100).ToString("D2"); // Get last two digits of year and ensure it's two digits

        var planNumber = $"{RunningPrefixConstant.Plan}{yearString}00001";

        return From(planNumber);
    }

    public PlanNumber Next()
    {
        if (string.IsNullOrWhiteSpace(this.Value))
        {
            throw new InvalidOperationException("Plan number cannot be null or empty.");
        }

        // Assuming the plan number is in the format "DPYYXXXXX"
        if (this.Value.Length < 4 || (!this.Value.StartsWith(RunningPrefixConstant.Plan) && !this.Value.StartsWith("BP") && !this.Value.StartsWith("P") && !this.Value.StartsWith("B")))
        {
            throw new FormatException("Invalid plan number format.");
        }

        // NOTE: Add Condition For Support Both Prefix "DP" and ("P" or "BP" or "B")
        string yearPart = this.Value.StartsWith(RunningPrefixConstant.Plan) ? this.Value.Substring(2, 2) : this.Value.Substring(1, 2);
        string numberPart = this.Value.Substring(4);

        if (!int.TryParse(yearPart, out var year) || !int.TryParse(numberPart, out var number))
        {
            throw new FormatException("Invalid plan number format.");
        }

        // Increment the number part
        number++;

        // Format the new plan number
        var newPlanNumber = $"{RunningPrefixConstant.Plan}{year}{number:D5}";

        return From(newPlanNumber);
    }
}

public partial class Plan : AuditableEntity<PlanId>, IHasSoftDelete, IHasActivityInfo
{
    public override PlanId Id { get; init; }

    public PlanNumber PlanNumber { get; init; }

    public BusinessUnitId DepartmentId { get; private set; }

    public PlanType Type { get; set; }

    public ParameterCode SupplyMethodCode { get; private set; }

    public ParameterCode? SupplyMethodTypeCode { get; private set; }

    public ParameterCode? SupplyMethodSpecialTypeCode { get; private set; }

    public int BudgetYear { get; private set; }

    public string Name { get; private set; }

    public decimal Budget { get; private set; }

    public DateTimeOffset ExpectingProcurementAt { get; private set; }

    public bool IsStock { get; private set; }

    public bool? IsCommercialMaterial { get; private set; }

    public string? Remark { get; private set; }

    public string? RemarkClosed { get; private set; }

    public PlanStatus? LastStatusBeforeClosed { get; private set; }

    public string? Telephone { get; private set; }

    public ParameterCode? AssignSegmentCode { get; private set; }

    public string? GroupEgpNumber { get; private set; }

    public string? EgpNumber { get; private set; }

    public bool IsChange { get; private set; }

    public bool IsCancel { get; private set; }

    public bool IsActive { get; private set; }

    public PlanStatus Status { get; private set; }

    public PlanId? ReferenceId { get; init; }

    public int Sequence { get; init; }

    public string? CancelReason { get; private set; }

    public string? ChangeReason { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public virtual RawBusinessUnit Department { get; init; }

    public virtual SuParameter SupplyMethod { get; init; }

    public virtual SuParameter? SupplyMethodType { get; init; }

    public virtual SuParameter? SupplyMethodSpecialType { get; init; }

    public virtual SuParameter? AssignSegment { get; init; }

    public virtual IReadOnlyCollection<PlanAcceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<PlanAssignee> Assignees { get; private set; }

    public virtual IReadOnlyCollection<PlanAttachments> Attachments { get; private set; }

    public virtual IReadOnlyCollection<PlanAnnouncementSelected> AnnouncementSelectedInformation { get; init; }

    public virtual IReadOnlyCollection<PlanDocumentHistory> DocumentHistories { get; private set; }

    public PlanDocumentHistory? FirstWaitingAcceptorPlanDocument =>
        this.DocumentHistories
            .Where(d =>
                d is { DocumentType: PlanDocumentType.Plan, StatusState: PlanStatus.WaitingAcceptor, IsReplaced: false })
            .OrderVersions()
            .FirstOrDefault();

    public PlanDocumentHistory? Document =>
        this.DocumentHistories
            .Where(d =>
                d.DocumentType == PlanDocumentType.Plan)
            .OrderVersions()
            .FirstOrDefault();

    public PlanDocumentHistory? DocumentByStatus(PlanStatus status, bool isReplace = false) =>
        this.DocumentHistories
            .Where(d =>
                d.DocumentType == PlanDocumentType.Plan && d.StatusState == status && d.IsReplaced == isReplace)
            .OrderVersions()
            .FirstOrDefault();

    public bool? IsReplacedDoc =>
        this.DocumentHistories
            .Any(d =>
                d.DocumentType == PlanDocumentType.Plan && d.IsReplaced);

    public PlanDocumentHistory? LastDraftDocument =>
        this.DocumentHistories
            .Where(p => p is { DocumentType: PlanDocumentType.Plan, StatusState: PlanStatus.WaitingAssign or PlanStatus.WaitingAcceptor })
            .OrderVersions()
            .FirstOrDefault();

    public PlanDocumentHistory? AnnouncementDocument =>
        this.DocumentHistories
            .Where(d => d.DocumentType == PlanDocumentType.Announcement)
            .OrderVersions()
            .FirstOrDefault();

    public bool? IsReplacedAnnouncementDoc =>
        this.DocumentHistories
            .Any(d => d.DocumentType == PlanDocumentType.Announcement && d.IsReplaced);

    public PlanDocumentHistory? LastDraftAnnouncementDocument =>
        this.DocumentHistories
            .Where(p => p is { DocumentType: PlanDocumentType.Announcement, StatusState: PlanStatus.WaitingAssign or PlanStatus.WaitingAcceptor, IsReplaced: false })
            .OrderVersions()
            .FirstOrDefault();

    public Unit AddDocumentHistory(
        PlanDocumentType planDocumentType,
        FileId? fileId,
        bool isReplace = false,
        bool incrementMajor = false)
    {
        if (fileId is not null)
        {
            var histories = this.DocumentHistories.ToHashSet();

            var existingStatus =
                histories
                    .Where(p => p.DocumentType == planDocumentType)
                    .OrderVersions()
                    .FirstOrDefault();

            var isIncreasedMajor = existingStatus == null || existingStatus.StatusState == this.Status;

            var version = this.DocumentHistories
                              .Where(p => p.DocumentType == planDocumentType)
                              .NextVersion(incrementMajor || !isIncreasedMajor);

            histories.Add(PlanDocumentHistory.Create(
                planDocumentType,
                this.Status,
                version,
                fileId.Value,
                isReplace));

            this.DocumentHistories = histories;
        }

        return unit;
    }

    public Unit RemoveDocumentHistory(PlanDocumentHistoryId docId)
    {
        var docHistories = this.DocumentHistories.ToHashSet();

        docHistories.RemoveWhere(w => w.Id == docId);

        this.DocumentHistories = docHistories;

        return unit;
    }

    public Unit AddAcceptor(PlanAcceptor acceptor)
    {
        if (acceptor == null)
        {
            throw new ArgumentNullException(nameof(acceptor), "Acceptor cannot be null.");
        }

        if (this.Acceptors.Contains(acceptor))
        {
            throw new InvalidOperationException("Acceptor already exists in the plan.");
        }

        var acceptors = this.Acceptors.ToHashSet();

        acceptors.Add(acceptor);

        this.Acceptors = acceptors;

        return unit;
    }

    public Unit RemoveAcceptorById(AcceptorId acceptorId)
    {
        var acceptors = this.Acceptors.ToHashSet();

        acceptors.RemoveWhere(w => w.Id == acceptorId);

        this.Acceptors = acceptors;

        return unit;
    }

    public Plan RemoveAcceptor(PlanAcceptor acceptor)
    {
        var list = this.Acceptors.ToHashSet();
        list.Remove(acceptor);
        this.Acceptors = list;

        return this;
    }

    public Plan AddAttachment(PlanAttachments attachment)
    {
        if (attachment == null)
        {
            throw new ArgumentNullException(nameof(attachment), "Attachment cannot be null.");
        }

        if (this.Attachments.Contains(attachment))
        {
            throw new InvalidOperationException("Attachment already exists in the plan.");
        }

        var attachments = this.Attachments.ToHashSet();

        attachments.Add(attachment);

        this.Attachments = attachments;

        return this;
    }

    public Unit SetPlanDelete()
    {
        this.IsActive = false;
        this.IsDeleted = true;

        return unit;
    }

    public Unit SetIsActive(bool isActive)
    {
        this.IsActive = isActive;

        return unit;
    }

    public Plan RemoveAttachment(PlanAttachments attachment)
    {
        var list = this.Attachments.ToHashSet();
        list.Remove(attachment);
        this.Attachments = list;

        return this;
    }

    public Unit AddAssignee(PlanAssignee assignee)
    {
        if (assignee == null)
        {
            throw new ArgumentNullException(nameof(assignee), "Assignee cannot be null.");
        }

        if (this.Assignees.Contains(assignee))
        {
            throw new InvalidOperationException("Assignee already exists in the plan.");
        }

        var assignees = this.Assignees.ToHashSet();

        assignees.Add(assignee);

        this.Assignees = assignees;

        return unit;
    }

    public Unit RemoveAssigneeById(PlanAssigneeId assigneeId)
    {
        var assignees = this.Assignees.ToHashSet();

        assignees.RemoveWhere(w => w.Id == assigneeId);

        this.Assignees = assignees;

        return unit;
    }

    public Plan SetDepartment(BusinessUnitId departmentId)
    {
        this.DepartmentId = departmentId;

        return this;
    }

    public Plan SetSupplyMethod(ParameterCode supplyMethodCode)
    {
        this.SupplyMethodCode = supplyMethodCode;

        return this;
    }

    public Plan SetSupplyMethodType(ParameterCode? supplyMethodTypeCode)
    {
        this.SupplyMethodTypeCode = supplyMethodTypeCode;

        return this;
    }

    public Plan SetSupplyMethodSpecialType(ParameterCode? supplyMethodSpecialTypeCode)
    {
        this.SupplyMethodSpecialTypeCode = supplyMethodSpecialTypeCode;

        return this;
    }

    public Plan SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Plan name cannot be null or empty.", nameof(name));
        }

        this.Name = name;

        return this;
    }

    public Plan SetBudget(decimal budget)
    {
        if (budget <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(budget), "Budget must be greater than 0.");
        }

        this.Budget = budget;

        return this;
    }

    public Plan SetBudgetYear(int budgetYear)
    {
        if (budgetYear <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(budgetYear), "Budget year must be greater than 0.");
        }

        this.BudgetYear = budgetYear;

        return this;
    }

    public Plan SetExpectingProcurementAt(DateTimeOffset expectingProcurementAt)
    {
        this.ExpectingProcurementAt = expectingProcurementAt;

        return this;
    }

    public Plan SetIsStock(bool isStock)
    {
        this.IsStock = isStock;

        return this;
    }

    public Plan SetIsCommercialMaterial(bool? isCommercialMaterial)
    {
        this.IsCommercialMaterial = isCommercialMaterial;

        return this;
    }

    public Plan SetRemark(string? remark)
    {
        this.Remark = remark;

        return this;
    }

    public Plan SetType(PlanType type)
    {
        this.Type = type;

        return this;
    }

    public Plan SetTelephone(string? telephone)
    {
        this.Telephone = telephone;

        return this;
    }

    public Plan SetAssignSegment(ParameterCode? assignSegmentCode)
    {
        this.AssignSegmentCode = assignSegmentCode;

        return this;
    }

    public Plan SetGroupEgpNumber(string? groupEgpNumber)
    {
        this.GroupEgpNumber = groupEgpNumber;

        return this;
    }

    public Plan SetEgpNumber(string? egpNumber)
    {
        this.EgpNumber = egpNumber;

        return this;
    }

    public Plan SetDocumentDate(DateTimeOffset? documentDate = null)
    {
        this.DocumentDate = documentDate ?? DateTimeOffset.Now;

        return this;
    }

    public Plan SetStatus(PlanStatus status)
    {
        if (status == this.Status)
        {
            return this;
        }

        switch (status, this.Status)
        {
            case (PlanStatus.WaitingApprovePlan, _):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.SendApproveDepartment,
                    $"เปลี่ยนสถานะจาก {this.Status.ToString()} เปลี่ยนสถานะเป็น {status.ToString()}",
                    status.ToString()));

                break;

            case (PlanStatus.ApprovePlan, _):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.ApprovedDepartment,
                    $"เปลี่ยนสถานะจาก {this.Status.ToString()} เปลี่ยนสถานะเป็น {status.ToString()}",
                    status.ToString()));

                break;

            case (PlanStatus.Announcement, _):
                this.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Announcement,
                    $"เปลี่ยนสถานะจาก {this.Status.ToString()} เปลี่ยนสถานะเป็น {status.ToString()}",
                    status.ToString()));

                break;
        }

        this.Status = status;

        return this;
    }

    public static Plan Create(
        PlanType type,
        int budgetYear,
        PlanNumber planNumber)
    {
        if (budgetYear <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(budgetYear), "Budget year must be greater than 0.");
        }

        if (planNumber == null || string.IsNullOrWhiteSpace(planNumber.Value))
        {
            throw new ArgumentException("Plan number cannot be null or empty.", nameof(planNumber));
        }

        var plan = new Plan
        {
            Id = PlanId.New(),
            Type = type,
            BudgetYear = budgetYear,
            PlanNumber = planNumber,
            IsActive = true,
            Status = PlanStatus.DraftPlan,
            IsChange = false,
            IsCancel = false,
            Acceptors = [],
            Assignees = [],
            Attachments = [],
            DocumentHistories = [],
            Sequence = 1,
        };

        plan.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Create,
                $"สร้างข้อมูลรายการจัดซื้อจัดจ้าง",
                nameof(PlanStatus.DraftPlan)));

        return plan;
    }

    public Plan Clone(bool isChange, string? remark = default)
    {
        var message = isChange ? "ขอเปลี่ยนแปลง" : "ขอยกเลิก";

        this.AddActivity(
            new ActivityInfo(
                isChange ? ActivityLogActionTypeConstant.RequestChange : ActivityLogActionTypeConstant.RequestCancel,
                $"{message}",
                nameof(PlanStatus.DraftPlan),
                remark));

        var newPlan = new Plan
        {
            Id = PlanId.New(),
            ReferenceId = this.Id,
            Type = this.Type,
            BudgetYear = this.BudgetYear,
            PlanNumber = this.PlanNumber,
            IsActive = true,
            Status = PlanStatus.DraftPlan,
            IsChange = isChange,
            IsCancel = !isChange,
            Sequence = this.Sequence + 1,
            DepartmentId = this.DepartmentId,
            SupplyMethodCode = this.SupplyMethodCode,
            SupplyMethodTypeCode = this.SupplyMethodTypeCode,
            SupplyMethodSpecialTypeCode = this.SupplyMethodSpecialTypeCode,
            Name = this.Name,
            Budget = this.Budget,
            ExpectingProcurementAt = this.ExpectingProcurementAt,
            IsStock = this.IsStock,
            IsCommercialMaterial = this.IsCommercialMaterial,
            Remark = this.Remark,
            Telephone = this.Telephone,
            AssignSegmentCode = this.AssignSegmentCode,
            GroupEgpNumber = this.GroupEgpNumber,
            EgpNumber = this.EgpNumber,

            Acceptors = this.Acceptors.Map(s => s.Clone(this.DepartmentId)).ToHashSet(),
            Assignees = this.Assignees.Map(s => s.Clone()).ToHashSet(),
            Attachments = this.Attachments.Map(s => s.Clone()).ToHashSet(),
            DocumentHistories = [],
        };

        newPlan.AddActivity(
            new ActivityInfo(
                isChange ? ActivityLogActionTypeConstant.RequestChange : ActivityLogActionTypeConstant.RequestCancel,
                $"{message}",
                nameof(PlanStatus.DraftPlan)));

        return newPlan;
    }

    public Plan SetDraft()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                $"อัปเดตข้อมูลรายการจัดซื้อจัดจ้าง",
                nameof(PlanStatus.DraftPlan)));

        this.Status = PlanStatus.DraftPlan;

        return this;
    }

    public Plan SetEdit(PlanStatus reqStatus)
    {
        if (this.Status == reqStatus)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    $"อัปเดตข้อมูลรายการจัดซื้อจัดจ้าง",
                    this.Status.ToString()));
        }

        if (this.Status != reqStatus)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Recall,
                    $"เรียกคืนแก้ไขข้อมูลรายการจัดซื้อจัดจ้าง",
                    nameof(PlanStatus.EditPlan)));
        }

        this.Status = PlanStatus.EditPlan;

        return this;
    }

    public Plan SetWaitingApprovePlan()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.SendApproveDepartment,
                $"ส่งฝ่ายเห็นชอบ เห็นชอบ/อนุมัติ",
                nameof(PlanStatus.WaitingApprovePlan)));

        this.Status = PlanStatus.WaitingApprovePlan;

        var approvers = this.Acceptors
                            .Where(p => p.Type == AcceptorType.DepartmentDirectorAgree)
                            .OrderBy(a => a.Sequence)
                            .ToList();

        approvers.Iter(a =>
        {
            if (a.Status == AcceptorStatus.Draft || a.Status == AcceptorStatus.Rejected)
            {
                a.Pending();
            }

            a.SetCurrent(false);
        });

        var firstPending = approvers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);
        if (firstPending != null)
        {
            firstPending.SetCurrent(true);
        }

        return this;
    }

    public Plan SetWaitingAssign(PlanStatus waitingAssign)
    {
        if (this.Status != waitingAssign)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.WaitingAssign,
                    $"รอมอบหมายผู้รับผิดชอบข้อมูลรายการจัดซื้อจัดจ้าง",
                    nameof(PlanStatus.WaitingAssign)));
        }

        if (this.Status == waitingAssign)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    $"อัปเดตข้อมูลรายการจัดซื้อจัดจ้าง",
                    nameof(PlanStatus.WaitingAssign)));
        }

        if (this.Assignees.Any())
        {
            this.Assignees.Iter(r => r.Pending());
        }

        this.Status = PlanStatus.WaitingAssign;

        return this;
    }

    public Plan SetAssigned()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Assigned,
                $"ยืนยันมอบหมายข้อมูลรายการจัดซื้อจัดจ้าง",
                nameof(PlanStatus.Assigned)));

        this.Status = PlanStatus.WaitingAssign;

        return this;
    }

    public Plan RejectDocument()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Reject,
                $"ส่งกลับแก้ไขเอกสารข้อมูลรายการจัดซื้อจัดจ้าง",
                nameof(PlanStatus.DraftRecordDocument)));

        this.Status = PlanStatus.DraftRecordDocument;

        return this;
    }

    public Plan RecallDocument()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Recall,
                $"เรียกคืนแก้ไขเอกสารข้อมูลรายการจัดซื้อจัดจ้าง",
                nameof(PlanStatus.DraftRecordDocument)));

        this.Status = PlanStatus.DraftRecordDocument;

        return this;
    }

    public Plan SetDraftRecordDocument(PlanStatus draftRecordDocument)
    {
        if (this.Status == draftRecordDocument)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    $"อัปเดตข้อมูลรายการจัดซื้อจัดจ้าง",
                    this.Status.ToString()));
        }

        if (this.Status != draftRecordDocument)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Assigned,
                    $"ยืนยันมอบหมายข้อมูลรายการจัดซื้อจัดจ้าง",
                    nameof(PlanStatus.DraftRecordDocument)));
        }

        this.Status = PlanStatus.DraftRecordDocument;

        return this;
    }

    public Plan SetRejectToAssignee(PlanStatus reqStatus, string? remark = default)
    {
        if (this.Status == reqStatus)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    $"อัปเดตข้อมูลรายการจัดซื้อจัดจ้าง",
                    this.Status.ToString()));
        }

        if (this.Status != reqStatus)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Reject,
                    $"ส่งกลับแก้ไขข้อมูลรายการจัดซื้อจัดจ้าง",
                    nameof(PlanStatus.RejectToAssignee),
                    remark));
        }

        this.Status = PlanStatus.RejectToAssignee;

        return this;
    }

    public Plan SetWaitingAcceptor()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.SendApprove,
                $"ส่งเห็นชอบ/อนุมัติข้อมูลรายการจัดซื้อจัดจ้าง",
                nameof(PlanStatus.WaitingAcceptor)));

        this.Status = PlanStatus.WaitingAcceptor;

        var approvers = this.Acceptors
                            .Where(p => p.Type == AcceptorType.Approver)
                            .OrderBy(a => a.Sequence)
                            .ToList();

        approvers.Iter(a =>
        {
            if (a.Status == AcceptorStatus.Draft || a.Status == AcceptorStatus.Rejected)
            {
                a.Pending();
            }

            a.SetCurrent(false);
        });

        var firstPending = approvers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);
        if (firstPending != null)
        {
            firstPending.SetCurrent(true);
        }

        return this;
    }

    public Plan SetDepartmentRejected(PlanStatus reqStatus, string? remark = default)
    {
        if (this.Status == reqStatus)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    $"อัปเดตข้อมูลรายการจัดซื้อจัดจ้าง",
                    this.Status.ToString()));
        }

        if (this.Status != reqStatus)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.DepartmentReject,
                    $"ส่งกลับแก้ไขข้อมูลรายการจัดซื้อจัดจ้าง",
                    nameof(PlanStatus.RejectPlan),
                    remark));
        }

        this.Status = PlanStatus.RejectPlan;

        return this;
    }

    public Plan SetAssigneeRejected(PlanStatus reqStatus, string? remark = default)
    {
        if (this.Status == reqStatus)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    $"อัปเดตข้อมูลรายการจัดซื้อจัดจ้าง",
                    this.Status.ToString()));
        }

        if (this.Status != reqStatus)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.AssigneeReject,
                    $"ส่งกลับแก้ไขข้อมูลรายการจัดซื้อจัดจ้าง",
                    nameof(PlanStatus.RejectPlan),
                    remark));
        }

        this.Status = PlanStatus.RejectPlan;

        return this;
    }

    public Plan SetRejected(PlanStatus reqStatus, string? remark = default)
    {
        if (this.Status == reqStatus)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    $"อัปเดตข้อมูลรายการจัดซื้อจัดจ้าง",
                    this.Status.ToString()));
        }

        if (this.Status != reqStatus)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Reject,
                    $"ส่งกลับแก้ไขข้อมูลรายการจัดซื้อจัดจ้าง",
                    nameof(PlanStatus.RejectPlan),
                    remark));
        }

        this.Status = PlanStatus.RejectPlan;

        return this;
    }

    public Plan SetApprovePlan()
    {
        this.Status = PlanStatus.ApprovePlan;

        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.ApprovedDepartment,
                string.Empty,
                nameof(PlanStatus.ApprovePlan)));

        return this;
    }

    public Plan SetWaitingAnnouncement()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.WaitingAnnouncement,
                $"เห็นชอบ/อนุมัติและรอเผยแพร่ข้อมูลรายการจัดซื้อจัดจ้าง",
                nameof(PlanStatus.WaitingAnnouncement)));

        this.Status = PlanStatus.WaitingAnnouncement;

        return this;
    }

    public Plan SetAnnouncement()
    {
        if (this.IsChange)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Changed,
                    $"เปลี่ยนแปลงข้อมูลรายการจัดซื้อจัดจ้างสำเร็จ",
                    nameof(PlanStatus.Announcement)));
        }

        if (!this.IsChange)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Approved,
                    $"เผยแพร่ข้อมูลรายการจัดซื้อจัดจ้าง",
                    nameof(PlanStatus.Announcement)));
        }

        this.Status = PlanStatus.Announcement;

        return this;
    }

    public Plan SetCancelPlan()
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Cancelled,
                $"ยกเลิกข้อมูลรายการจัดซื้อจัดจ้างสำเร็จ",
                nameof(PlanStatus.CancelPlan)));

        this.Status = PlanStatus.CancelPlan;

        return this;
    }

    public Plan SetClosed(string? remarkClosed = default)
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Closed,
                $"ปิดงานรายการจัดซื้อจัดจ้างสำเร็จ",
                nameof(PlanStatus.Closed),
                remarkClosed));

        this.LastStatusBeforeClosed = this.Status;
        this.Status = PlanStatus.Closed;
        this.RemarkClosed = remarkClosed;

        return this;
    }

    public Plan SetCancelClosed()
    {
        if (this.LastStatusBeforeClosed is null)
        {
            throw new InvalidOperationException("ไม่พบสถานะก่อนปิดงาน");
        }

        var previousStatus = this.LastStatusBeforeClosed.Value;

        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.CancelClosed,
                $"ยกเลิกปิดงานรายการจัดซื้อจัดจ้างสำเร็จ",
                previousStatus.ToString()));

        this.Status = previousStatus;
        this.LastStatusBeforeClosed = null;
        this.RemarkClosed = null;

        return this;
    }

    public Plan SetCancelReason(string? reason)
    {
        this.CancelReason = reason;

        return this;
    }

    public Plan SetChangeReason(string? reason)
    {
        this.ChangeReason = reason;

        return this;
    }
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PlanAttachmentId
{
    public static PlanAttachmentId New() => From(Guid.CreateVersion7());
}

public class PlanAttachments : AuditableEntity<PlanAttachmentId>
{
    public ParameterCode DocumentTypeCode { get; private set; }

    public override PlanAttachmentId Id { get; init; }

    public FileId FileId { get; private init; }

    public string FileName { get; private init; }

    public bool IsPublic { get; private set; }

    public int Sequence { get; private set; }

    public virtual SuParameter DocumentType { get; init; }

    public static PlanAttachments Create(
        ParameterCode documentTypeCode,
        FileId fileId,
        string fileName,
        int sequence,
        bool isPublic)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        return new PlanAttachments
        {
            Id = PlanAttachmentId.New(),
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            FileId = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public PlanAttachments SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public PlanAttachments SetIsPublic(bool isPublic)
    {
        this.IsPublic = isPublic;

        return this;
    }

    public Unit SetDocumentType(ParameterCode documentTypeCode)
    {
        this.DocumentTypeCode = documentTypeCode;

        return unit;
    }

    public PlanAttachments Clone()
    {
        return new PlanAttachments
        {
            Id = PlanAttachmentId.New(),
            Sequence = this.Sequence,
            DocumentTypeCode = this.DocumentTypeCode,
            FileId = this.FileId,
            FileName = this.FileName,
            IsPublic = this.IsPublic,
        };
    }
}