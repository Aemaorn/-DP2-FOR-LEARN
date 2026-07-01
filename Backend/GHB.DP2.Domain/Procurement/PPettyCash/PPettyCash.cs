namespace GHB.DP2.Domain.Procurement.PPettyCash;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using System.Linq;
using Vogen;

public enum PettyCashAction
{
    /// <summary>
    /// Draft (แบบร่าง)
    /// Status Draft
    /// </summary>
    Draft,

    /// <summary>
    /// Edit (แก้ไข)
    /// Status Edit
    /// </summary>
    Edit,

    /// <summary>
    /// Recall (เรียกคืนแก้ไข)
    /// Status Edit
    /// </summary>
    Recall,

    /// <summary>
    /// RequestToDirectorAgree (ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ)
    /// Status WaitingApproval
    /// </summary>
    RequestToDirectorAgree,

    /// <summary>
    /// Approved DirectorAgree (ฝ่ายเห็นชอบ/อนุมัติ)
    /// Status WaitingApproval
    /// </summary>
    DirectorAgreeApproved,

    /// <summary>
    /// Rejected DirectorAgree (ฝ่ายส่งกลับแก้ไขเอกสาร)
    /// Status Rejected
    /// </summary>
    DirectorAgreeRejected,

    /// <summary>
    /// Approved InspectionCommittee (ตรวจรับพัสดุ)
    /// Status WaitingAcceptor
    /// </summary>
    InspectionCommitteeApproved,

    /// <summary>
    /// Rejected WaitingForInspector (ผู้ตรวจรับส่งกลับแก้ไข)
    /// Status Rejected
    /// </summary>
    InspectionCommitteeRejected,

    /// <summary>
    /// Director save assignee (บันทึกผู้รับผิดชอบ)
    /// Status WaitingForAssignment
    /// </summary>
    SaveAssignee,

    /// <summary>
    /// Director confirm assigned(มอบหมายผู้รับผิดชอบ)
    /// Status WaitingForAssignment
    /// </summary>
    Assignment,

    /// <summary>
    /// Approved Acceptor (ยืนยันมอบหมายผู้รับผิดชอบ)
    /// Status WaitingForCompletion
    /// </summary>
    ConfirmAssignment,

    /// <summary>
    /// Assignee (ยืนยันมอบหมายผู้รับผิดชอบ)
    /// Status Completed
    /// </summary>
    ConfirmCompleted,

    /// <summary>
    /// ส่งต่อผู้ถือเงินสดย่อย (กรณีไม่ออกแบบฟอร์ม จพ. 001)
    /// Status WaitingForCompletion
    /// </summary>
    SendToAssignee,

    /// <summary>
    /// ผู้ถือเงินสดย่อยส่งกลับแก้ไข
    /// Status Rejected
    /// </summary>
    AssigneeRejected,
}

public enum PettyCashStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    /// <summary>
    /// ตีกลับ
    /// </summary>
    Rejected,

    /// <summary>
    /// เรียกคืนแก้ไข
    /// </summary>
    Edit,

    /// <summary>
    /// รอให้ความเห็น
    /// </summary>
    WaitingApproval,

    /// <summary>
    /// รอผู้ตรวจรับเห็นชอบ
    /// </summary>
    WaitingForInspector,

    /// <summary>
    /// รอหัวหน้าส่วนมอบหมาย
    /// </summary>
    WaitingForAssignment,

    /// <summary>
    /// รอผู้ถือเงินสดย่อยยืนยันลงวันที่เบิกจ่าย
    /// </summary>
    WaitingForCompletion,

    /// <summary>
    /// ดำเนินการเสร็จสิ้น
    /// </summary>
    Completed,

    /// <summary>
    /// ยกเลิก
    /// </summary>
    Cancelled,
}

public enum CashType
{
    /// <summary>
    /// เงินสดย่อย
    /// </summary>
    Standard,

    /// <summary>
    /// เงินสดย่อย
    /// </summary>
    Convenient,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PettyCashId
{
    public static PettyCashId New() => From(Guid.CreateVersion7());
}

[ValueObject<string>(Conversions.EfCoreValueConverter)]
public partial struct PettyCashNumber
{
    public static PettyCashNumber New(int year)
    {
        if (year <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be greater than 0.");
        }

        // Generate a new PettyCash number in the format "BPYY00001"
        // where YY is the last two digits of the year and 00001 is the initial number
        var yearString = (year % 100).ToString("D2"); // Get last two digits of year and ensure it's two digits

        var PettyCashNumber = $"{RunningPrefixConstant.PettyCash}{yearString}00001";

        return From(PettyCashNumber);
    }

    public PettyCashNumber Next()
    {
        if (string.IsNullOrWhiteSpace(this.Value))
        {
            throw new InvalidOperationException("PettyCash number cannot be null or empty.");
        }

        // Assuming the PettyCash number is in the format "LPYYXXXXX"
        if (this.Value.Length < 4 || (!this.Value.StartsWith(RunningPrefixConstant.PettyCash) && !this.Value.StartsWith("PT")))
        {
            throw new FormatException("Invalid PettyCash number format.");
        }

        string yearPart = this.Value.Substring(1, 2);
        string numberPart = this.Value.Substring(3);

        if (!int.TryParse(yearPart, out var year) || !int.TryParse(numberPart, out var number))
        {
            throw new FormatException("Invalid PettyCash number format.");
        }

        // Increment the number part
        number++;

        // Format the new PettyCash number
        var newPettyCashNumber = $"{RunningPrefixConstant.PettyCash}{year}{number:D5}";

        return From(newPettyCashNumber);
    }
}

public partial class PPettyCash : AuditableEntity<PettyCashId>, IHasSoftDelete, IHasActivityInfo
{
    public override PettyCashId Id { get; init; }

    public PettyCashNumber PettyCashNumber { get; init; }

    public DateTimeOffset PettyCashDate { get; private set; }

    public BusinessUnitId DepartmentId { get; private set; }

    public int BudgetYear { get; private set; }

    public ParameterCode SupplyMethodCode { get; private set; }

    public ParameterCode SupplyMethodTypeCode { get; private set; }

    public ParameterCode? SupplyMethodSpecialTypeCode { get; private set; }

    public string Subject { get; private set; }

    public string Source { get; private set; }

    public string? ProcurementScope { get; set; }

    public decimal Budget { get; private set; }

    public int? DeliveryPeriod { get; private set; }

    public string? Telephone { get; private set; }

    public string? Reasons { get; private set; }

    public DateTimeOffset? DeliveryDate { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public string? PettyCaseDepartmentCode { get; private set; }

    public ParameterCode? DeliveryPeriodTypeCode { get; private set; }

    public ParameterCode? DeliveryConditionCode { get; private set; }

    public bool IsAdvance { get; private set; }

    public string? AdvanceName { get; private set; }

    public ParameterCode? AdvancePaymentMethodCode { get; private set; }

    public DateTimeOffset? AdvancePaymentDate { get; private set; }

    public ParameterCode? AdvanceBankCode { get; private set; }

    public string? AdvanceBankAccount { get; private set; }

    public string? AdvanceBankBranch { get; private set; }

    public string? AdvanceBankAccountName { get; private set; }

    public string? AdvanceDetail { get; private set; }

    public DateTimeOffset? DisbursementDate { get; private set; }

    public PettyCashStatus Status { get; private set; }

    public bool IsActive { get; private set; }

    public FileId? DocumentId { get; init; }

    public FileId? AnnouncementDocumentId { get; init; }

    public CashType CashType { get; private set; }

    public bool? IsFromJorPor001 { get; private set; }

    public virtual RawBusinessUnit Department { get; init; }

    public virtual SuParameter SupplyMethod { get; init; }

    public virtual SuParameter SupplyMethodType { get; init; }

    public virtual SuParameter SupplyMethodSpecialType { get; init; }

    public virtual SuParameter AdvancePaymentMethod { get; init; }

    public virtual SuParameter AdvanceBank { get; init; }

    public virtual SuParameter DeliveryPeriodType { get; init; }

    public virtual SuParameter DeliveryCondition { get; init; }

    public virtual IReadOnlyCollection<PPettyCashCategories> Categories { get; private set; }

    public virtual IReadOnlyCollection<PPettyCashVendor> Vendors { get; private set; }

    public virtual IReadOnlyCollection<PPettyCashGLAccount> GLAccounts { get; private set; }

    public virtual IReadOnlyCollection<PPettyCashCommittee> Committees { get; private set; }

    public virtual IReadOnlyCollection<PPettyCashAcceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<PPettyCashAssignee> Assignees { get; private set; }

    public virtual IReadOnlyCollection<PPettyCashAttachments> Attachments { get; private set; }

    public virtual IReadOnlyCollection<PPettyCashDocumentHistory> DocumentHistories { get; private set; }

    public static PPettyCash Create(
        PettyCashNumber pettyCashNumber)
    {
        var pettyCash = new PPettyCash
        {
            Id = PettyCashId.New(),
            PettyCashNumber = pettyCashNumber,
            IsActive = true,
            Status = PettyCashStatus.Draft,
            Categories = [],
            Committees = [],
            Acceptors = [],
            Assignees = [],
            Vendors = [],
            GLAccounts = [],
            Attachments = [],
            DocumentHistories = [],
        };

        return pettyCash;
    }

    public PPettyCashDocumentHistory? LastedDraftDocument() =>
        this.DocumentHistories
            .Where(dh => dh.StatusState == PettyCashStatus.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public PPettyCashDocumentHistory? LastedDocument() =>
    this.DocumentHistories
        .Where(dh => dh.StatusState is PettyCashStatus.Draft or PettyCashStatus.Edit or PettyCashStatus.Rejected)
        .OrderVersions()
        .FirstOrDefault();

    public PPettyCashDocumentHistory? LastedNotReplacedDocument() =>
        this.DocumentHistories
            .Where(dh => dh is
            {
                StatusState: PettyCashStatus.WaitingApproval,
                IsReplaced: false,
            })
            .OrderVersions()
            .FirstOrDefault();

    public Unit AddDocumentHistory(
        FileId fileId,
        bool isReplace = false,
        bool forceMinorVersion = false)
    {
        var histories = this.DocumentHistories.ToHashSet();

        var existingHistory =
            histories
                .OrderVersions()
                .FirstOrDefault();

        var isIncreaseMajorVersion =
            !forceMinorVersion &&
            (existingHistory is null ||
            existingHistory.StatusState != this.Status);

        var version =
            this.DocumentHistories
                .NextVersion(isIncreaseMajorVersion);

        histories.Add(
            PPettyCashDocumentHistory.Create(
                this.Status,
                version,
                fileId,
                isReplace));

        this.DocumentHistories = histories;

        return unit;
    }

    public PPettyCash SetPettyCashDate(DateTimeOffset pettyCashDate)
    {
        this.PettyCashDate = pettyCashDate;

        return this;
    }

    public PPettyCash SetDocumentDate(DateTimeOffset documentDate)
    {
        this.DocumentDate = documentDate;

        return this;
    }

    public PPettyCash SetDepartmentId(BusinessUnitId departmentId)
    {
        this.DepartmentId = departmentId;

        return this;
    }

    public PPettyCash SetPettyCaseDepartmentCode(string? departmentCode)
    {
        if (!string.IsNullOrWhiteSpace(departmentCode))
        {
            this.PettyCaseDepartmentCode = departmentCode;
        }

        return this;
    }

    public PPettyCash SetReasons(string? reasons)
    {
        if (!string.IsNullOrWhiteSpace(reasons))
        {
            this.Reasons = reasons;
        }

        return this;
    }

    public PPettyCash SetDeliveryDate(DateTimeOffset? deliveryDate)
    {
        if (deliveryDate != null)
        {
            this.DeliveryDate = deliveryDate;
        }

        return this;
    }

    public PPettyCash SetStatus(PettyCashStatus status)
    {
        this.Status = status;

        return this;
    }

    public PPettyCash SetBudgetYear(int budgetYear)
    {
        this.BudgetYear = budgetYear;

        return this;
    }

    public PPettyCash SetCashType(CashType type)
    {
        this.CashType = type;

        return this;
    }

    public PPettyCash SetIsFromJorPor001(bool? isFromJorPor001)
    {
        this.IsFromJorPor001 = isFromJorPor001;

        return this;
    }

    public PPettyCash SetActive(bool active)
    {
        this.IsActive = active;

        return this;
    }

    public PPettyCash SetSupplyMethod(
        ParameterCode supplyMethodCode,
        ParameterCode supplyMethodTypeCode,
        ParameterCode? supplyMethodSpecialTypeCode)
    {
        this.SupplyMethodCode = supplyMethodCode;
        this.SupplyMethodTypeCode = supplyMethodTypeCode;
        this.SupplyMethodSpecialTypeCode = supplyMethodSpecialTypeCode;

        return this;
    }

    public PPettyCash SetSubject(string? subject)
    {
        this.Subject = subject ?? string.Empty;

        return this;
    }

    public PPettyCash SetSource(string source)
    {
        this.Source = source;

        return this;
    }

    public PPettyCash SetBudget(decimal budget)
    {
        this.Budget = budget;

        return this;
    }

    public PPettyCash SetDeliveryPeriod(int? deliveryPeriod, ParameterCode? deliveryPeriodTypeCode, ParameterCode? deliveryConditionCode)
    {
        this.DeliveryPeriod = deliveryPeriod;
        this.DeliveryPeriodTypeCode = deliveryPeriodTypeCode;
        this.DeliveryConditionCode = deliveryConditionCode;

        return this;
    }

    public PPettyCash SetIsAdvance(
        bool isAdvance)
    {
        this.IsAdvance = isAdvance;

        return this;
    }

    public Unit SetNonAdvance()
    {
        this.AdvanceName = null;
        this.AdvancePaymentMethodCode = null;
        this.AdvancePaymentDate = null;
        this.AdvanceBankCode = null;
        this.AdvanceBankAccount = null;
        this.AdvanceBankBranch = null;
        this.AdvanceBankAccountName = null;
        this.AdvanceDetail = null;

        return unit;
    }

    public PPettyCash SetAdvanceName(
        string? advanceName)
    {
        this.AdvanceName = advanceName;

        return this;
    }

    public PPettyCash SetAdvancePayment(
        ParameterCode? advancePaymentMethodCode,
        DateTimeOffset? advancePaymentDate)
    {
        this.AdvancePaymentMethodCode = advancePaymentMethodCode;
        this.AdvancePaymentDate = advancePaymentDate;

        return this;
    }

    public PPettyCash SetAdvanceBank(
        ParameterCode? advanceBankCode,
        string? advanceBankAccount,
        string? advanceBankBranch,
        string? advanceBankAccountName)
    {
        this.AdvanceBankCode = advanceBankCode;
        this.AdvanceBankAccount = advanceBankAccount;
        this.AdvanceBankBranch = advanceBankBranch;
        this.AdvanceBankAccountName = advanceBankAccountName;

        return this;
    }

    public PPettyCash SetAdvanceDetail(
        string? advanceDetail)
    {
        this.AdvanceDetail = advanceDetail;

        return this;
    }

    public PPettyCash SetDisbursementDate(
        DateTimeOffset disbursementDate)
    {
        this.DisbursementDate = disbursementDate;

        return this;
    }

    public Unit AddAttachment(PPettyCashAttachments attachment)
    {
        if (attachment == null)
        {
            throw new ArgumentNullException(nameof(attachment), "Attachment cannot be null.");
        }

        if (this.Attachments.Contains(attachment))
        {
            throw new InvalidOperationException("Attachment already exists in the PettyCash.");
        }

        var attachments = this.Attachments.ToHashSet();

        attachments.Add(attachment);

        this.Attachments = attachments;

        return unit;
    }

    public Unit AddAcceptor(PPettyCashAcceptor acceptor)
    {
        if (acceptor == null)
        {
            throw new ArgumentNullException(nameof(acceptor), "ไม่พบค่าผู้มีอำนาจเห็นชอบ/อนุมัติ");
        }

        if (this.Acceptors.Contains(acceptor))
        {
            throw new InvalidOperationException("ข้อมูลผู้มีอำนาจเห็นชอบ/อนุมัติแล้ว");
        }

        var acceptors = this.Acceptors.ToHashSet();

        acceptors.Add(acceptor);

        this.Acceptors = acceptors;

        return unit;
    }

    public Unit AddVendor(PPettyCashVendor vendor)
    {
        if (vendor == null)
        {
            throw new ArgumentNullException(nameof(vendor), "ไม่พบค่าผู้ค้าเห็นชอบ/อนุมัติ");
        }

        if (this.Vendors.Contains(vendor))
        {
            throw new InvalidOperationException("มีข้อมูลผู้ค้าแล้ว");
        }

        var vendors = this.Vendors.ToHashSet();

        vendors.Add(vendor);

        this.Vendors = vendors;

        return unit;
    }

    public Unit AddVendors(IEnumerable<PPettyCashVendor> vendors)
    {
        this.Vendors = vendors.ToList();

        return unit;
    }

    public Unit AddGLAccount(PPettyCashGLAccount glAccount)
    {
        if (glAccount == null)
        {
            throw new ArgumentNullException(nameof(glAccount), "ไม่พบค่า GL Account");
        }

        if (this.GLAccounts.Contains(glAccount))
        {
            throw new InvalidOperationException("มีข้อมูล GL Account แล้ว");
        }

        var glAccounts = this.GLAccounts.ToHashSet();

        glAccounts.Add(glAccount);

        this.GLAccounts = glAccounts;

        return unit;
    }

    public Unit AddCategory(PPettyCashCategories category)
    {
        if (category == null)
        {
            throw new ArgumentNullException(nameof(category), "ไม่พบค่า Categories");
        }

        if (this.Categories.Contains(category))
        {
            throw new InvalidOperationException("มีข้อมูล Categories แล้ว");
        }

        var categories = this.Categories.ToHashSet();

        categories.Add(category);

        this.Categories = categories;

        return unit;
    }

    public Unit AddCategories(IEnumerable<PPettyCashCategories> category)
    {
        this.Categories = category.ToList();

        return unit;
    }

    public Unit RemoveCategories(PPettyCashCategories category)
    {
        var categories = this.Categories.ToHashSet();

        if (!categories.Remove(category))
        {
            throw new InvalidOperationException("Acceptor not found.");
        }

        this.Categories = categories;

        return unit;
    }

    public Unit AddCommittee(PPettyCashCommittee committee)
    {
        if (committee == null)
        {
            throw new ArgumentNullException(nameof(committee), "ไม่พบค่า Committee");
        }

        if (this.Committees.Contains(committee))
        {
            throw new InvalidOperationException("มีข้อมูล Committee แล้ว");
        }

        var committees = this.Committees.ToHashSet();

        committees.Add(committee);

        this.Committees = committees;

        return unit;
    }

    public Unit AddGLAccounts(IEnumerable<PPettyCashGLAccount> glAccounts)
    {
        this.GLAccounts = glAccounts.ToList();

        return unit;
    }

    public Unit AddCommittees(IEnumerable<PPettyCashCommittee> committees)
    {
        this.Committees = committees.ToList();

        return unit;
    }

    public Unit RemoveAcceptorById(AcceptorId acceptorId)
    {
        var acceptors = this.Acceptors.ToHashSet();

        acceptors.RemoveWhere(w => w.Id == acceptorId);

        this.Acceptors = acceptors;

        return unit;
    }

    public Unit RemoveAttachmentById(FileId attachmentId)
    {
        var attachments = this.Attachments.ToHashSet();

        attachments.RemoveWhere(w => w.Id == attachmentId);

        this.Attachments = attachments;

        return unit;
    }

    public PPettyCash RemoveAttachment(PPettyCashAttachments attachment)
    {
        var list = this.Attachments.ToHashSet();
        list.Remove(attachment);
        this.Attachments = list;

        return this;
    }

    public static PPettyCashAttachments Create(
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

        return new PPettyCashAttachments
        {
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            Id = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public PPettyCash RemoveAcceptor(PPettyCashAcceptor acceptor)
    {
        if (acceptor == null)
        {
            throw new ArgumentNullException(nameof(acceptor));
        }

        var acceptors = this.Acceptors.ToHashSet();

        acceptors.RemoveWhere(a => a.Id == acceptor.Id);

        this.Acceptors = acceptors;

        return this;
    }

    public Unit RemoveAssigneeById(PPettyCashAssigneeId assigneeId)
    {
        var assignees = this.Assignees.ToHashSet();

        assignees.RemoveWhere(w => w.Id == assigneeId);

        this.Assignees = assignees;

        return unit;
    }

    public Unit AddAssignee(PPettyCashAssignee assignee)
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

    public PPettyCash SetWaitingAssign(PettyCashStatus waitingAssign)
    {
        if (this.Status != waitingAssign)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.WaitingAssign,
                    $"รอมอบหมายผู้รับผิดชอบข้อมูลรายการจัดซื้อจัดจ้าง",
                    nameof(PettyCashStatus.WaitingForAssignment)));
        }

        if (this.Status == waitingAssign)
        {
            this.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.Update,
                    $"อัปเดตข้อมูลรายการจัดซื้อจัดจ้าง",
                    nameof(PettyCashStatus.WaitingForAssignment)));
        }

        this.Status = PettyCashStatus.WaitingForCompletion;

        return this;
    }
}

public class PPettyCashAttachments : AuditableEntity<FileId>
{
    public ParameterCode DocumentTypeCode { get; set; }

    public override FileId Id { get; init; }

    public string FileName { get; set; }

    public bool IsPublic { get; set; }

    public int Sequence { get; set; }

    public virtual SuParameter DocumentType { get; init; }

    public static PPettyCashAttachments Create(
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

        return new PPettyCashAttachments
        {
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            Id = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public PPettyCashAttachments SetIsPublic(bool isPublic)
    {
        this.IsPublic = isPublic;

        return this;
    }

    public PPettyCashAttachments SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public Unit SetDocumentType(ParameterCode documentTypeCode)
    {
        this.DocumentTypeCode = documentTypeCode;

        return unit;
    }
}