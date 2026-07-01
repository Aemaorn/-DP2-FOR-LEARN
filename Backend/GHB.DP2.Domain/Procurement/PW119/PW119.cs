namespace GHB.DP2.Domain.Procurement.Pw119;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using System.Linq;
using Vogen;

public enum Pw119Action
{
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

public enum Pw119Status
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
public partial struct Pw119Id
{
    public static Pw119Id New() => From(Guid.CreateVersion7());
}

[ValueObject<string>(Conversions.EfCoreValueConverter)]
public partial struct Pw119Number
{
    public static Pw119Number New(int year)
    {
        if (year <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be greater than 0.");
        }

        // Generate a new Pw119 number in the format "BPYY00001"
        // where YY is the last two digits of the year and 00001 is the initial number
        var yearString = (year % 100).ToString("D2"); // Get last two digits of year and ensure it's two digits

        var Pw119Number = $"{RunningPrefixConstant.W119}{yearString}00001";

        return From(Pw119Number);
    }

    public Pw119Number Next()
    {
        if (string.IsNullOrWhiteSpace(this.Value))
        {
            throw new InvalidOperationException("Pw119 number cannot be null or empty.");
        }

        // Assuming the Pw119 number is in the format "LPYYXXXXX"
        if (this.Value.Length < 4 || (!this.Value.StartsWith(RunningPrefixConstant.W119) && !this.Value.StartsWith("LP")))
        {
            throw new FormatException("Invalid Pw119 number format.");
        }

        string yearPart = this.Value.Substring(1, 2);
        string numberPart = this.Value.Substring(3);

        if (!int.TryParse(yearPart, out var year) || !int.TryParse(numberPart, out var number))
        {
            throw new FormatException("Invalid Pw119 number format.");
        }

        // Increment the number part
        number++;

        // Format the new Pw119 number
        var newPw119Number = $"{RunningPrefixConstant.W119}{year}{number:D5}";

        return From(newPw119Number);
    }
}

public partial class Pw119 : AuditableEntity<Pw119Id>, IHasSoftDelete, IHasActivityInfo
{
    public override Pw119Id Id { get; init; }

    public Pw119Number Pw119Number { get; init; }

    public DateTimeOffset Pw119Date { get; private set; }

    public BusinessUnitId DepartmentId { get; private set; }

    public ParameterCode? AssignSegmentCode { get; private set; }

    public int BudgetYear { get; private set; }

    public ParameterCode SupplyMethodCode { get; private set; }

    public ParameterCode? SupplyMethodSpecialTypeCode { get; private set; }

    public string Subject { get; private set; }

    public string Source { get; private set; }

    public decimal Budget { get; private set; }

    public decimal? MedianPrice { get; private set; }

    public ParameterCode W119CategoriesCode { get; private set; }

    public string? Reason { get; private set; }

    public bool IsAdvance { get; private set; }

    public string? AdvanceName { get; private set; }

    public ParameterCode? AdvancePaymentMethodCode { get; private set; }

    public DateTimeOffset? AdvancePaymentDate { get; private set; }

    public ParameterCode? AdvanceBankCode { get; private set; }

    public string? AdvanceBankAccount { get; private set; }

    public string? AdvanceBankBranch { get; private set; }

    public string? AdvanceBankAccountName { get; private set; }

    public string? AdvanceDetail { get; private set; }

    public Pw119Status Status { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public FileId? DocumentId { get; init; }

    public FileId? AnnouncementDocumentId { get; init; }

    public int Sequence { get; init; }

    public string? Telephone { get; private set; }

    public DateTimeOffset? DisbursementDate { get; private set; }

    public decimal? DisbursementAmount { get; private set; }

    public string? DisbursementDescription { get; private set; }

    public virtual RawBusinessUnit Department { get; init; }

    public virtual SuParameter? AssignSegment { get; init; }

    public virtual SuParameter SupplyMethod { get; init; }

    public virtual SuParameter? SupplyMethodSpecialType { get; init; }

    public virtual SuParameter W119Categories { get; init; }

    public virtual SuParameter AdvancePaymentMethod { get; init; }

    public virtual SuParameter? AdvanceBank { get; init; }

    public virtual IReadOnlyCollection<Pw119Vendor> Vendors { get; private set; }

    public virtual IReadOnlyCollection<Pw119GLAccount> GLAccounts { get; private set; }

    public virtual IReadOnlyCollection<Pw119Acceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<Pw119Attachments> Attachments { get; private set; }

    public virtual IReadOnlyCollection<Pw119DocumentHistory> DocumentHistories { get; private set; }

    public Pw119DocumentHistory? LastedDocumentVersions(Pw119DocumentType documentType) =>
        this.DocumentHistories
            .Where(dh => dh.DocumentType == documentType)
            .OrderVersions()
            .FirstOrDefault();

    public Pw119DocumentHistory? LastedDraftDocument(Pw119DocumentType documentType) =>
        this.DocumentHistories
            .Where(dh => dh.DocumentType == documentType)
            .Where(dh => dh.StatusState == Pw119Status.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public Pw119DocumentHistory? LastedNotReplacedDocument(Pw119DocumentType documentType) =>
        this.DocumentHistories
            .Where(dh => dh.DocumentType == documentType)
            .Where(dh => dh is
            {
                StatusState: Pw119Status.WaitingApproval,
                IsReplaced: false,
            })
            .OrderVersions()
            .FirstOrDefault();

    public Pw119 SetStatus(Pw119Status status)
    {
        this.Status = status;

        return this;
    }

    public static Pw119 Create(
        Pw119Number pw119Number)
    {
        var pw119 = new Pw119
        {
            Id = Pw119Id.New(),
            Pw119Number = pw119Number,
            IsActive = true,
            Status = Pw119Status.Draft,
            Acceptors = [],
            Vendors = [],
            GLAccounts = [],
            Attachments = [],
            Sequence = 1,
            DocumentHistories = [],
        };

        pw119.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"สร้างข้อมูลใหม่ {nameof(Pw119)}",
            nameof(Pw119Status.Draft)));

        return pw119;
    }

    public Unit AddDocumentHistory(
        Pw119DocumentType documentType,
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
            Pw119DocumentHistory.Create(
                documentType,
                this.Status,
                version,
                fileId,
                isReplace));

        this.DocumentHistories = histories;

        return unit;
    }

    public Pw119 SetPw119Date(DateTimeOffset pw119Date)
    {
        this.Pw119Date = pw119Date;

        return this;
    }

    public Pw119 SetDocumentDate(DateTimeOffset documentDate)
    {
        this.DocumentDate = documentDate;

        return this;
    }

    public Pw119 SetDepartmentId(BusinessUnitId departmentId)
    {
        this.DepartmentId = departmentId;

        return this;
    }

    public Pw119 SetBudgetYear(int budgetYear)
    {
        this.BudgetYear = budgetYear;

        return this;
    }

    public Pw119 SetSupplyMethod(
        ParameterCode supplyMethodCode,
        ParameterCode? supplyMethodSpecialTypeCode)
    {
        this.SupplyMethodCode = supplyMethodCode;
        this.SupplyMethodSpecialTypeCode = supplyMethodSpecialTypeCode;

        return this;
    }

    public Pw119 SetSubject(string subject)
    {
        this.Subject = subject;

        return this;
    }

    public Pw119 SetTelephone(string? telephone)
    {
        this.Telephone = telephone;

        return this;
    }

    public Pw119 SetSource(string source)
    {
        this.Source = source;

        return this;
    }

    public Pw119 SetBudget(decimal budget)
    {
        this.Budget = budget;

        return this;
    }

    public Pw119 SetMedianPrice(decimal? medianPrice)
    {
        this.MedianPrice = medianPrice;

        return this;
    }

    public Pw119 SetReason(string? reason)
    {
        this.Reason = reason;

        return this;
    }

    public Pw119 SetIsAdvance(
        bool isAdvance)
    {
        this.IsAdvance = isAdvance;

        return this;
    }

    public Pw119 SetIsActive(bool isActive)
    {
        this.IsActive = isActive;

        return this;
    }

    public Pw119 SetW119CategoriesCode(ParameterCode w119CategoriesCode)
    {
        this.W119CategoriesCode = w119CategoriesCode;

        return this;
    }

    public Pw119 SetAdvanceName(
        string? advanceName)
    {
        this.AdvanceName = advanceName;

        return this;
    }

    public Pw119 SetAdvancePayment(
      ParameterCode? advancePaymentMethodCode,
      DateTimeOffset? advancePaymentDate)
    {
        this.AdvancePaymentMethodCode = advancePaymentMethodCode;
        this.AdvancePaymentDate = advancePaymentDate;

        return this;
    }

    public Pw119 SetDisbursement(
        DateTimeOffset? disbursementDate,
        decimal? disbursementAmount,
        string? disbursementDescription)
    {
        this.DisbursementDate = disbursementDate;
        this.DisbursementAmount = disbursementAmount;
        this.DisbursementDescription = disbursementDescription;

        return this;
    }

    public Pw119 SetAdvanceBank(
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

    public Pw119 SetAdvanceDetail(
        string? advanceDetail)
    {
        this.AdvanceDetail = advanceDetail;

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

    public Unit AddAttachment(Pw119Attachments attachment)
    {
        if (attachment == null)
        {
            throw new ArgumentNullException(nameof(attachment), "Attachment cannot be null.");
        }

        if (this.Attachments.Contains(attachment))
        {
            throw new InvalidOperationException("Attachment already exists in the Pw119.");
        }

        var attachments = this.Attachments.ToHashSet();

        attachments.Add(attachment);

        this.Attachments = attachments;

        return unit;
    }

    public Unit AddAcceptor(Pw119Acceptor acceptor)
    {
        if (acceptor == null)
        {
            throw new ArgumentNullException(nameof(acceptor), "Acceptor cannot be null.");
        }

        if (this.Acceptors.Contains(acceptor))
        {
            throw new InvalidOperationException("Acceptor already exists in the pw119.");
        }

        var acceptors = this.Acceptors.ToHashSet();

        acceptors.Add(acceptor);

        this.Acceptors = acceptors;

        return unit;
    }

    public Unit AddAcceptors(IEnumerable<Pw119Acceptor> acceptors)
    {
        this.Acceptors = acceptors.ToList();

        return unit;
    }

    public Unit AddVendors(IEnumerable<Pw119Vendor> vendors)
    {
        this.Vendors = vendors.ToList();

        return unit;
    }

    public Unit AddGLAccounts(IEnumerable<Pw119GLAccount> gLAccounts)
    {
        this.GLAccounts = gLAccounts.ToList();

        return unit;
    }

    public Unit AddVendor(Pw119Vendor vendor)
    {
        if (vendor == null)
        {
            throw new ArgumentNullException(nameof(vendor), "Pw119 Vendor cannot be null.");
        }

        if (this.Vendors.Contains(vendor))
        {
            throw new InvalidOperationException("Pw119 Vendor already exists in the pw119.");
        }

        var vendors = this.Vendors.ToHashSet();

        vendors.Add(vendor);

        this.Vendors = vendors;

        return unit;
    }

    public Unit AddGLAccount(Pw119GLAccount glAccount)
    {
        if (glAccount == null)
        {
            throw new ArgumentNullException(nameof(glAccount), "GL Account cannot be null.");
        }

        if (this.GLAccounts.Contains(glAccount))
        {
            throw new InvalidOperationException("GL Account already exists in the pw119.");
        }

        var glAccounts = this.GLAccounts.ToHashSet();

        glAccounts.Add(glAccount);

        this.GLAccounts = glAccounts;

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

    public Pw119 RemoveAttachment(Pw119Attachments attachment)
    {
        var list = (System.Collections.Generic.HashSet<Pw119Attachments>)this.Attachments;
        list.Remove(attachment);

        return this;
    }

    public Pw119 RemoveAcceptor(Pw119Acceptor acceptor)
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

    public Pw119 SetAssignSegment(ParameterCode? assignSegmentCode)
    {
        this.AssignSegmentCode = assignSegmentCode;

        return this;
    }
}

public class Pw119Attachments : AuditableEntity<FileId>
{
    public ParameterCode DocumentTypeCode { get; set; }

    public override FileId Id { get; init; }

    public string FileName { get; set; }

    public bool IsPublic { get; set; }

    public int Sequence { get; set; }

    public virtual Pw119 Pw119 { get; init; }

    public virtual SuParameter DocumentType { get; init; }

    public static Pw119Attachments Create(
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

        return new Pw119Attachments
        {
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            Id = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public Pw119Attachments SetIsPublic(bool isPublic)
    {
        this.IsPublic = isPublic;

        return this;
    }

    public Pw119Attachments SetSequence(int sequence)
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