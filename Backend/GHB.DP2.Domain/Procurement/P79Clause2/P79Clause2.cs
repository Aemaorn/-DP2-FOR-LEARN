namespace GHB.DP2.Domain.Procurement.P79Clause2;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using System.Linq;
using Vogen;

public enum P79Clause2Action
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
    /// RequestApproval (ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ)
    /// Status WaitingAcceptor
    /// </summary>
    RequestApproval,

    /// <summary>
    /// Approved Acceptor (เห็นชอบ/อนุมัติ)
    /// Status WaitingAcceptor
    /// </summary>
    ApprovedAcceptor,

    /// <summary>
    /// Rejected Acceptor (ส่งกลับแก้ไขเอกสาร)
    /// </summary>
    RejectedAcceptor,

    /// <summary>
    /// ยืนยันเบิกจ่าย
    /// </summary>
    ConfirmDisbursement,
}

public enum P79Clause2Status
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
    /// รอการอนุมัติจากผู้มีอำนาจ
    /// </summary>
    WaitingApproval,

    /// <summary>
    /// อนุมัติแล้ว
    /// </summary>
    Approved,

    /// <summary>
    /// ยกเลิก
    /// </summary>
    Cancelled,

    /// <summary>
    /// รอบัญชีเห็นชอบ/อนุมัติ
    /// </summary>
    WaitingAccountingApproval,

    /// <summary>
    /// รอบันทึกวันที่เบิกจ่าย
    /// </summary>
    WaitingDisbursementDate,

    /// <summary>
    /// เบิกจ่ายเสร็จสิ้น
    /// </summary>
    Paid,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct P79Clause2Id
{
    public static P79Clause2Id New() => From(Guid.CreateVersion7());
}

[ValueObject<string>(Conversions.EfCoreValueConverter)]
public partial struct P79Clause2Number
{
    public static P79Clause2Number New(int year)
    {
        if (year <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be greater than 0.");
        }

        // Generate a new P79Clause2 number in the format "BPYY00001"
        // where YY is the last two digits of the year and 00001 is the initial number
        var yearString = (year % 100).ToString("D2"); // Get last two digits of year and ensure it's two digits

        var P79Clause2Number = $"{RunningPrefixConstant.P79Clause2}{yearString}00001";

        return From(P79Clause2Number);
    }

    public P79Clause2Number Next()
    {
        if (string.IsNullOrWhiteSpace(this.Value))
        {
            throw new InvalidOperationException("P79Clause2 number cannot be null or empty.");
        }

        // Assuming the P79Clause2 number is in the format "LPYYXXXXX"
        if (this.Value.Length < 4 || (!this.Value.StartsWith(RunningPrefixConstant.P79Clause2) && !this.Value.StartsWith("P79")))
        {
            throw new FormatException("Invalid P79Clause2 number format.");
        }

        string yearPart = this.Value.Substring(1, 2);
        string numberPart = this.Value.Substring(3);

        if (!int.TryParse(yearPart, out var year) || !int.TryParse(numberPart, out var number))
        {
            throw new FormatException("Invalid P79Clause2 number format.");
        }

        // Increment the number part
        number++;

        // Format the new P79Clause2 number
        var newP79Clause2Number = $"{RunningPrefixConstant.P79Clause2}{year}{number:D5}";

        return From(newP79Clause2Number);
    }
}

public partial class P79Clause2 : AuditableEntity<P79Clause2Id>, IHasSoftDelete, IHasActivityInfo
{
    public override P79Clause2Id Id { get; init; }

    public P79Clause2Number P79Clause2Number { get; init; }

    public DateTimeOffset P79Clause2Date { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public BusinessUnitId DepartmentId { get; private set; }

    public ParameterCode? AssignSegmentCode { get; private set; }

    public int BudgetYear { get; private set; }

    public ParameterCode SupplyMethodCode { get; private set; }

    public ParameterCode SupplyMethodTypeCode { get; private set; }

    public ParameterCode? SupplyMethodSpecialTypeCode { get; private set; }

    public string Subject { get; private set; }

    public string Source { get; private set; }

    public string? Telephone { get; private set; }

    public decimal Budget { get; private set; }

    public decimal? MedianPrice { get; private set; }

    public string? ReasonItem1 { get; private set; }

    public string? ReasonItem2 { get; private set; }

    public string? ReasonItem3 { get; private set; }

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

    public decimal? DisbursementAmount { get; private set; }

    public string? DisbursementDescription { get; private set; }

    public P79Clause2Status Status { get; private set; }

    public bool IsActive { get; private set; }

    public FileId? DocumentId { get; init; }

    public FileId? AnnouncementDocumentId { get; init; }

    public DateTimeOffset? DeliveryDate { get; private set; }

    public string? ProcurementReasonItem1 { get; set; }

    public string? ProcurementReasonItem2 { get; set; }

    public virtual RawBusinessUnit Department { get; init; }

    public virtual SuParameter? AssignSegment { get; init; }

    public virtual SuParameter SupplyMethod { get; init; }

    public virtual SuParameter SupplyMethodType { get; init; }

    public virtual SuParameter? SupplyMethodSpecialType { get; init; }

    public virtual SuParameter AdvancePaymentMethod { get; init; }

    public virtual SuParameter? AdvanceBank { get; init; }

    public virtual IReadOnlyCollection<P79Clause2Vendor> Vendors { get; private set; }

    public virtual IReadOnlyCollection<P79Clause2GLAccount> GLAccounts { get; private set; }

    public virtual IReadOnlyCollection<P79Clause2Acceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<P79Clause2Attachments> Attachments { get; private set; }

    public virtual IReadOnlyCollection<P79Clause2DocumentHistory> DocumentHistories { get; private set; }

    public P79Clause2DocumentHistory? LastedDocumentVersions(P79Clause2DocumentType documentType) =>
    this.DocumentHistories
        .Where(dh => dh.DocumentType == documentType)
        .OrderVersions()
        .FirstOrDefault();

    public P79Clause2DocumentHistory? LastedDraftDocument(P79Clause2DocumentType documentType) =>
        this.DocumentHistories
            .Where(dh => dh.DocumentType == documentType)
            .Where(dh => dh.StatusState == P79Clause2Status.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public P79Clause2DocumentHistory? LastedNotReplacedDocument(P79Clause2DocumentType documentType) =>
        this.DocumentHistories
            .Where(dh => dh.DocumentType == documentType)
            .Where(dh => dh is
            {
                StatusState: P79Clause2Status.WaitingApproval,
                IsReplaced: false,
            })
            .OrderVersions()
            .FirstOrDefault();

    public static P79Clause2 Create(
        P79Clause2Number p79Clause2Number)
    {
        var p79Clause2 = new P79Clause2
        {
            Id = P79Clause2Id.New(),
            P79Clause2Number = p79Clause2Number,
            IsActive = true,
            Status = P79Clause2Status.Draft,
            Acceptors = [],
            Vendors = [],
            GLAccounts = [],
            Attachments = [],
            DocumentHistories = [],
        };

        return p79Clause2;
    }

    public void ClearDocumentHistories()
    {
        this.DocumentHistories = [];
    }

    public Unit AddDocumentHistory(
        P79Clause2DocumentType documentType,
        FileId fileId,
        bool isReplace = false,
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

        var version =
            this.DocumentHistories
                .Where(dh => dh.DocumentType == documentType)
                .NextVersion(incrementMajor || isIncreaseMajorVersion);

        histories.Add(
            P79Clause2DocumentHistory.Create(
                documentType,
                this.Status,
                version,
                fileId,
                isReplace));

        this.DocumentHistories = histories;

        return unit;
    }

    public P79Clause2 SetP79Clause2Date(DateTimeOffset p79Clause2Date)
    {
        this.P79Clause2Date = p79Clause2Date;

        return this;
    }

    public P79Clause2 SetDocumentDate(DateTimeOffset documentDate)
    {
        this.DocumentDate = documentDate;

        return this;
    }

    public P79Clause2 SetDeliveryDate(DateTimeOffset? deliveryDate)
    {
        this.DeliveryDate = deliveryDate;

        return this;
    }

    public P79Clause2 SetDisbursement(
      DateTimeOffset? disbursementDate,
      decimal? disbursementAmount,
      string? disbursementDescription)
    {
        this.DisbursementDate = disbursementDate;
        this.DisbursementAmount = disbursementAmount;
        this.DisbursementDescription = disbursementDescription;

        return this;
    }

    public P79Clause2 SetProcurementReasonItem(string? procurementReasonItem1, string? procurementReasonItem2)
    {
        this.ProcurementReasonItem1 = procurementReasonItem1;
        this.ProcurementReasonItem2 = procurementReasonItem2;

        return this;
    }

    public P79Clause2 SetDepartmentId(BusinessUnitId departmentId)
    {
        this.DepartmentId = departmentId;

        return this;
    }

    public P79Clause2 SetStatus(P79Clause2Status status)
    {
        this.Status = status;

        return this;
    }

    public P79Clause2 SetBudgetYear(int budgetYear)
    {
        this.BudgetYear = budgetYear;

        return this;
    }

    public P79Clause2 SetSupplyMethod(
        ParameterCode supplyMethodCode,
        ParameterCode supplyMethodTypeCode,
        ParameterCode? supplyMethodSpecialTypeCode)
    {
        this.SupplyMethodCode = supplyMethodCode;
        this.SupplyMethodTypeCode = supplyMethodTypeCode;
        this.SupplyMethodSpecialTypeCode = supplyMethodSpecialTypeCode;

        return this;
    }

    public P79Clause2 SetSubject(string subject)
    {
        this.Subject = subject;

        return this;
    }

    public P79Clause2 SetTelephone(string? telephone)
    {
        this.Telephone = telephone;

        return this;
    }

    public P79Clause2 SetSource(string source)
    {
        this.Source = source ?? string.Empty;

        return this;
    }

    public P79Clause2 SetBudget(decimal budget)
    {
        this.Budget = budget;

        return this;
    }

    public P79Clause2 SetMedianPrice(decimal? medianPrice)
    {
        this.MedianPrice = medianPrice;

        return this;
    }

    public P79Clause2 SetReasonItem(string? reasonItem1, string? reasonItem2, string? reasonItem3)
    {
        this.ReasonItem1 = reasonItem1;
        this.ReasonItem2 = reasonItem2;
        this.ReasonItem3 = reasonItem3;

        return this;
    }

    public P79Clause2 SetIsAdvance(
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

    public P79Clause2 SetAdvanceName(
        string? advanceName)
    {
        this.AdvanceName = advanceName;

        return this;
    }

    public P79Clause2 SetAdvancePayment(
        ParameterCode? advancePaymentMethodCode,
        DateTimeOffset? advancePaymentDate)
    {
        this.AdvancePaymentMethodCode = advancePaymentMethodCode;
        this.AdvancePaymentDate = advancePaymentDate;

        return this;
    }

    public P79Clause2 SetAdvanceBank(
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

    public P79Clause2 SetAdvanceDetail(
        string? advanceDetail)
    {
        this.AdvanceDetail = advanceDetail;

        return this;
    }

    public P79Clause2 SetIsActive(bool isActive)
    {
        this.IsActive = isActive;

        return this;
    }

    public Unit AddAttachment(P79Clause2Attachments attachment)
    {
        if (attachment == null)
        {
            throw new ArgumentNullException(nameof(attachment), "Attachment cannot be null.");
        }

        if (this.Attachments.Contains(attachment))
        {
            throw new InvalidOperationException("Attachment already exists in the P79Clause2.");
        }

        var attachments = this.Attachments.ToHashSet();

        attachments.Add(attachment);

        this.Attachments = attachments;

        return unit;
    }

    public P79Clause2 RemoveAttachment(P79Clause2Attachments attachment)
    {
        var list = this.Attachments.ToHashSet();
        list.Remove(attachment);
        this.Attachments = list;

        return this;
    }

    public Unit AddAcceptor(P79Clause2Acceptor acceptor)
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

    public Unit AddVendor(P79Clause2Vendor vendor)
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

    public Unit UpdateVendors(IEnumerable<P79Clause2Vendor> vendors)
    {
        this.Vendors = vendors.ToList();

        return unit;
    }

    public Unit AddGLAccount(P79Clause2GLAccount glAccount)
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

    public Unit UpdateGLAccounts(IEnumerable<P79Clause2GLAccount> glAccounts)
    {
        this.GLAccounts = glAccounts.ToList();

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

    public static P79Clause2Attachments Create(
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

        return new P79Clause2Attachments
        {
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            Id = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public P79Clause2 RemoveAcceptor(P79Clause2Acceptor acceptor)
    {
        if (acceptor == null)
        {
            throw new ArgumentNullException(nameof(acceptor));
        }

        var list = this.Acceptors.ToHashSet();
        list.Remove(acceptor);
        this.Acceptors = list;

        return this;
    }

    public P79Clause2 SetAssignSegment(ParameterCode? assignSegmentCode)
    {
        this.AssignSegmentCode = assignSegmentCode;

        return this;
    }
}

public class P79Clause2Attachments : AuditableEntity<FileId>
{
    public ParameterCode DocumentTypeCode { get; set; }

    public override FileId Id { get; init; }

    public string FileName { get; set; }

    public bool IsPublic { get; set; }

    public int Sequence { get; set; }

    public virtual P79Clause2 P79Clause2 { get; init; }

    public virtual SuParameter DocumentType { get; init; }

    public static P79Clause2Attachments Create(
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

        return new P79Clause2Attachments
        {
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            Id = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public P79Clause2Attachments SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public P79Clause2Attachments SetIsPublic(bool isPublic)
    {
        this.IsPublic = isPublic;

        return this;
    }

    public Unit SetDocumentType(ParameterCode documentTypeCode)
    {
        this.DocumentTypeCode = documentTypeCode;

        return unit;
    }
}