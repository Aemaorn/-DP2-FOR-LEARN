namespace GHB.DP2.Domain.Procurement.Pw184;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

public enum Pw184Action
{
    /// <summary>
    /// เห็นชอบ/อนุมัติ (ผู้มีอำนาจ)
    /// </summary>
    ApproveAcceptor,

    /// <summary>
    /// ส่งกลับแก้ไข (ผู้มีอำนาจ)
    /// </summary>
    RejectAcceptor,

    /// <summary>
    /// คณะกรรมการตรวจรับเห็นชอบ
    /// </summary>
    CommitteeApprove,

    /// <summary>
    /// คณะกรรมการตรวจรับส่งกลับแก้ไข
    /// </summary>
    CommitteeReject,

    /// <summary>
    /// บัญชีเห็นชอบ/อนุมัติ
    /// </summary>
    AccountingApprove,

    /// <summary>
    /// บัญชีส่งกลับแก้ไข
    /// </summary>
    AccountingReject,
}

public enum Pw184Status
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
    /// ส่งกลับแก้ไข
    /// </summary>
    Rejected,

    /// <summary>
    /// รอผู้มีอำนาจเห็นชอบ/อนุมัติ
    /// </summary>
    WaitingApproval,

    /// <summary>
    /// รอคณะกรรมการตรวจรับเห็นชอบ
    /// </summary>
    WaitingCommitteeApprove,

    /// <summary>
    /// รอบัญชีเห็นชอบ/อนุมัติ
    /// </summary>
    WaitingAccounting,

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
public partial struct Pw184Id
{
    public static Pw184Id New() => From(Guid.CreateVersion7());
}

[ValueObject<string>(Conversions.EfCoreValueConverter)]
public partial struct Pw184Number
{
    public static Pw184Number New(int year)
    {
        if (year <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be greater than 0.");
        }

        var yearString = (year % 100).ToString("D2");
        var number = $"{RunningPrefixConstant.W184}{yearString}00001";

        return From(number);
    }

    public Pw184Number Next()
    {
        if (string.IsNullOrWhiteSpace(this.Value))
        {
            throw new InvalidOperationException("Pw804 number cannot be null or empty.");
        }

        if (this.Value.Length < 4 || !this.Value.StartsWith(RunningPrefixConstant.W184))
        {
            throw new FormatException("Invalid Pw804 number format.");
        }

        string yearPart = this.Value.Substring(1, 2);
        string numberPart = this.Value.Substring(3);

        if (!int.TryParse(yearPart, out var year) || !int.TryParse(numberPart, out var number))
        {
            throw new FormatException("Invalid Pw184 number format.");
        }

        number++;

        var newNumber = $"{RunningPrefixConstant.W184}{year}{number:D5}";

        return From(newNumber);
    }
}

public partial class Pw184 : AuditableEntity<Pw184Id>, IHasSoftDelete, IHasActivityInfo
{
    public override Pw184Id Id { get; init; }

    public Pw184Number Pw184Number { get; init; }

    public DateTimeOffset Pw184Date { get; private set; }

    public BusinessUnitId DepartmentId { get; private set; }

    public int BudgetYear { get; private set; }

    public ParameterCode SupplyMethodCode { get; private set; }

    public ParameterCode? SupplyMethodSpecialTypeCode { get; private set; }

    public string Subject { get; private set; }

    public string Source { get; private set; }

    public string? Reason { get; private set; }

    public decimal Budget { get; private set; }

    public bool IsAdvance { get; private set; }

    public string? AdvanceName { get; private set; }

    public ParameterCode? AdvancePaymentMethodCode { get; private set; }

    public DateTimeOffset? AdvancePaymentDate { get; private set; }

    public ParameterCode? AdvanceBankCode { get; private set; }

    public string? AdvanceBankAccount { get; private set; }

    public string? AdvanceBankBranch { get; private set; }

    public string? AdvanceBankAccountName { get; private set; }

    public string? AdvanceDetail { get; private set; }

    public Pw184Status Status { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    /// <summary>
    /// Tracks which InspectionCommittee member (by sequence) is currently approving.
    /// </summary>
    public int CurrentCommitteeSequence { get; private set; }

    public DateTimeOffset? DisbursementDate { get; private set; }

    public decimal? DisbursementAmount { get; private set; }

    public string? DisbursementDescription { get; private set; }

    public virtual RawBusinessUnit Department { get; init; }

    public virtual SuParameter SupplyMethod { get; init; }

    public virtual SuParameter? SupplyMethodSpecialType { get; init; }

    public virtual SuParameter? AdvancePaymentMethod { get; init; }

    public virtual SuParameter? AdvanceBank { get; init; }

    public virtual IReadOnlyCollection<Pw184Vendor> Vendors { get; private set; }

    public virtual IReadOnlyCollection<Pw184GLAccount> GLAccounts { get; private set; }

    public virtual IReadOnlyCollection<Pw184Committee> Committees { get; private set; }

    public virtual IReadOnlyCollection<Pw184Acceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<Pw184Attachments> Attachments { get; private set; }

    public static Pw184 Create(Pw184Number pw184Number)
    {
        var pw184 = new Pw184
        {
            Id = Pw184Id.New(),
            Pw184Number = pw184Number,
            IsActive = true,
            Status = Pw184Status.Draft,
            CurrentCommitteeSequence = 0,
            Acceptors = [],
            Vendors = [],
            GLAccounts = [],
            Committees = [],
            Attachments = [],
        };

        pw184.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"สร้างข้อมูลใหม่ {nameof(Pw184)}",
            nameof(Pw184Status.Draft)));

        return pw184;
    }

    public Pw184 SetStatus(Pw184Status status)
    {
        this.Status = status;
        return this;
    }

    public Pw184 SetCurrentCommitteeSequence(int sequence)
    {
        this.CurrentCommitteeSequence = sequence;
        return this;
    }

    public Pw184 SetPw184Date(DateTimeOffset pw184Date)
    {
        this.Pw184Date = pw184Date;
        return this;
    }

    public Pw184 SetDocumentDate(DateTimeOffset documentDate)
    {
        this.DocumentDate = documentDate;
        return this;
    }

    public Pw184 SetDepartmentId(BusinessUnitId departmentId)
    {
        this.DepartmentId = departmentId;
        return this;
    }

    public Pw184 SetBudgetYear(int budgetYear)
    {
        this.BudgetYear = budgetYear;
        return this;
    }

    public Pw184 SetSupplyMethod(ParameterCode supplyMethodCode, ParameterCode? supplyMethodSpecialTypeCode)
    {
        this.SupplyMethodCode = supplyMethodCode;
        this.SupplyMethodSpecialTypeCode = supplyMethodSpecialTypeCode;
        return this;
    }

    public Pw184 SetSubject(string subject)
    {
        this.Subject = subject;
        return this;
    }

    public Pw184 SetSource(string source)
    {
        this.Source = source;
        return this;
    }

    public Pw184 SetReason(string? reason)
    {
        this.Reason = reason;
        return this;
    }

    public Pw184 SetBudget(decimal budget)
    {
        this.Budget = budget;
        return this;
    }

    public Pw184 SetIsAdvance(bool isAdvance)
    {
        this.IsAdvance = isAdvance;
        return this;
    }

    public Pw184 SetIsActive(bool isActive)
    {
        this.IsActive = isActive;
        return this;
    }

    public Pw184 SetAdvanceName(string? advanceName)
    {
        this.AdvanceName = advanceName;
        return this;
    }

    public Pw184 SetAdvancePayment(ParameterCode? advancePaymentMethodCode, DateTimeOffset? advancePaymentDate)
    {
        this.AdvancePaymentMethodCode = advancePaymentMethodCode;
        this.AdvancePaymentDate = advancePaymentDate;
        return this;
    }

    public Pw184 SetAdvanceBank(
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

    public Pw184 SetAdvanceDetail(string? advanceDetail)
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

    public Pw184 SetDisbursement(DateTimeOffset? disbursementDate, decimal? disbursementAmount, string? disbursementDescription)
    {
        this.DisbursementDate = disbursementDate;
        this.DisbursementAmount = disbursementAmount;
        this.DisbursementDescription = disbursementDescription;
        return this;
    }

    public Unit AddAcceptor(Pw184Acceptor acceptor)
    {
        if (acceptor == null) throw new ArgumentNullException(nameof(acceptor));
        var acceptors = this.Acceptors.ToHashSet();
        acceptors.Add(acceptor);
        this.Acceptors = acceptors;
        return unit;
    }

    public Unit AddAcceptors(IEnumerable<Pw184Acceptor> acceptors)
    {
        this.Acceptors = acceptors.ToList();
        return unit;
    }

    public Pw184 RemoveAcceptor(Pw184Acceptor acceptor)
    {
        if (acceptor == null) throw new ArgumentNullException(nameof(acceptor));
        var acceptors = this.Acceptors.ToHashSet();
        acceptors.RemoveWhere(a => a.Id == acceptor.Id);
        this.Acceptors = acceptors;
        return this;
    }

    public Unit AddVendors(IEnumerable<Pw184Vendor> vendors)
    {
        this.Vendors = vendors.ToList();
        return unit;
    }

    public Unit AddVendor(Pw184Vendor vendor)
    {
        if (vendor == null) throw new ArgumentNullException(nameof(vendor));
        var vendors = this.Vendors.ToHashSet();
        vendors.Add(vendor);
        this.Vendors = vendors;
        return unit;
    }

    public Unit AddGLAccounts(IEnumerable<Pw184GLAccount> glAccounts)
    {
        this.GLAccounts = glAccounts.ToList();
        return unit;
    }

    public Unit AddGLAccount(Pw184GLAccount glAccount)
    {
        if (glAccount == null) throw new ArgumentNullException(nameof(glAccount));
        var glAccounts = this.GLAccounts.ToHashSet();
        glAccounts.Add(glAccount);
        this.GLAccounts = glAccounts;
        return unit;
    }

    public Unit AddCommittees(IEnumerable<Pw184Committee> committees)
    {
        this.Committees = committees.ToList();
        return unit;
    }

    public Unit AddCommittee(Pw184Committee committee)
    {
        if (committee == null) throw new ArgumentNullException(nameof(committee));
        var committees = this.Committees.ToHashSet();
        committees.Add(committee);
        this.Committees = committees;
        return unit;
    }

    public Unit AddAttachment(Pw184Attachments attachment)
    {
        if (attachment == null) throw new ArgumentNullException(nameof(attachment));
        var attachments = this.Attachments.ToHashSet();
        attachments.Add(attachment);
        this.Attachments = attachments;
        return unit;
    }

    public Pw184 RemoveAttachment(Pw184Attachments attachment)
    {
        var list = (System.Collections.Generic.HashSet<Pw184Attachments>)this.Attachments;
        list.Remove(attachment);
        return this;
    }
}

public class Pw184Attachments : AuditableEntity<FileId>
{
    public ParameterCode DocumentTypeCode { get; set; }

    public override FileId Id { get; init; }

    public string FileName { get; set; }

    public bool IsPublic { get; set; }

    public int Sequence { get; set; }

    public virtual Pw184 Pw184 { get; init; }

    public virtual SuParameter DocumentType { get; init; }

    public static Pw184Attachments Create(
        ParameterCode documentTypeCode,
        FileId fileId,
        string fileName,
        int sequence,
        bool isPublic)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

        return new Pw184Attachments
        {
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            Id = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public Pw184Attachments SetIsPublic(bool isPublic)
    {
        this.IsPublic = isPublic;
        return this;
    }

    public Pw184Attachments SetSequence(int sequence)
    {
        this.Sequence = sequence;
        return this;
    }

    public Unit SetDocumentType(ParameterCode documentTypeCode)
    {
        this.DocumentTypeCode = documentTypeCode;
        return unit;
    }
}
