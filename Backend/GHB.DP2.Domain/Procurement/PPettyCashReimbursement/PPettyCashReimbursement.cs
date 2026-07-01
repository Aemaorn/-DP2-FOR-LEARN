namespace GHB.DP2.Domain.Procurement.PPettyCashReimbursement;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

public enum PPettyCashReimbursementStatus
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
    /// มอบหมาย
    /// </summary>
    Approved,

    /// <summary>
    /// ส่งกลับแก้ไข
    /// </summary>
    Rejected,

    /// <summary>
    /// ยกเลิกรายการ
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
public partial struct PPettyCashReimbursementId
{
    public static PPettyCashReimbursementId New() => From(Guid.CreateVersion7());
}

public partial class PPettyCashReimbursement : AuditableEntity<PPettyCashReimbursementId>, IHasSoftDelete, IHasActivityInfo
{
    public override PPettyCashReimbursementId Id { get; init; }

    public string Number { get; private set; }

    public PPettyCashReimbursementStatus Status { get; private set; }

    public DateTimeOffset ReimbursementDate { get; private set; }

    public BusinessUnitId? DepartmentId { get; private set; }

    public string Subject { get; private set; }

    public string? Description { get; private set; }

    public string? ReferredTo { get; private set; }

    public string BankAccountName { get; private set; }

    public string BankAccountNumber { get; private set; }

    public DateTimeOffset? DisbursementDate { get; private set; }

    public decimal? DisbursementAmount { get; private set; }

    public string? DisbursementDescription { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public virtual RawBusinessUnit Department { get; init; }

    public virtual IReadOnlyCollection<PPettyCashReimbursementAcceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<PPettyCashReimbursementItems>? Items { get; private set; }

    public virtual IReadOnlyCollection<PPettyCashReimbursementAttachments> Attachments { get; private set; }

    public static PPettyCashReimbursement Create()
    {
        var newData = new PPettyCashReimbursement
        {
            Id = PPettyCashReimbursementId.New(),
            Acceptors = [],
            Items = [],
            Attachments = [],
        };

        newData.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Create,
                string.Empty,
                newData.Status.ToString()));

        return newData;
    }

    public PPettyCashReimbursement SetValues(
        string number,
        PPettyCashReimbursementStatus status,
        DateTimeOffset reimbursementDate,
        string subject,
        string? description,
        string bankAccountName,
        string bankAccountNumber,
        BusinessUnitId departmentId)
    {
        this.Number = number;
        this.Status = status;
        this.ReimbursementDate = reimbursementDate;
        this.Subject = subject;
        this.Description = description;
        this.BankAccountName = bankAccountName;
        this.BankAccountNumber = bankAccountNumber;
        this.DepartmentId = departmentId;

        if (status == PPettyCashReimbursementStatus.WaitingApproval)
        {
            this.SetInitialApproverCurrent();
        }

        return this;
    }

    public PPettyCashReimbursement SetDocumentDate(DateTimeOffset documentDate)
    {
        this.DocumentDate = documentDate;
        return this;
    }

    public PPettyCashReimbursement SetDisbursement(
       DateTimeOffset? disbursementDate,
       decimal? disbursementAmount,
       string? disbursementDescription)
    {
        this.DisbursementDate = disbursementDate;
        this.DisbursementAmount = disbursementAmount;
        this.DisbursementDescription = disbursementDescription;

        return this;
    }

    public PPettyCashReimbursement AddAcceptor(PPettyCashReimbursementAcceptor acceptor)
    {
        var acceptors = this.Acceptors?.ToList() ?? new List<PPettyCashReimbursementAcceptor>();
        acceptors.Add(acceptor);
        this.Acceptors = acceptors;

        return this;
    }

    public PPettyCashReimbursement RemoveAcceptor(PPettyCashReimbursementAcceptor acceptor)
    {
        var list = this.Acceptors?.ToList() ?? new List<PPettyCashReimbursementAcceptor>();
        list.Remove(acceptor);
        this.Acceptors = list;

        return this;
    }

    public PPettyCashReimbursement AddDetail(PPettyCashReimbursementItems item)
    {
        var list = this.Items?.ToList() ?? new List<PPettyCashReimbursementItems>();
        list.Add(item);
        this.Items = list;

        return this;
    }

    public PPettyCashReimbursement RemoveDetail(PPettyCashReimbursementItems item)
    {
        var list = this.Items?.ToList() ?? new List<PPettyCashReimbursementItems>();
        list.Remove(item);
        this.Items = list;

        return this;
    }

    private void SetInitialApproverCurrent()
    {
        if (this.Status != PPettyCashReimbursementStatus.WaitingApproval)
        {
            return;
        }

        this.Acceptors.Iter(x => x.Pending());

        var approvers = this.Acceptors?
                            .Where(a => a.Type == AcceptorType.Approver && a.IsActive && a.Status == AcceptorStatus.Pending)
                            .OrderBy(a => a.Sequence)
                            .ToList();

        if (approvers is null || approvers.Count == 0)
        {
            return;
        }

        // reset all current
        foreach (var a in approvers)
        {
            a.SetCurrent(false);
        }

        var firstSeq = approvers[0].Sequence;

        foreach (var a in approvers.Where(a => a.Sequence == firstSeq))
        {
            a.SetCurrent(true);
        }
    }

    public PPettyCashReimbursement EvaluateAcceptorApproval()
    {
        var approvers = this.Acceptors?
                            .Where(a => a.Type == AcceptorType.Approver && a.IsActive)
                            .ToList() ?? new List<PPettyCashReimbursementAcceptor>();

        var accountingAcceptors = this.Acceptors?
                            .Where(a => (a.Type == AcceptorType.AccountingApprover || a.Type == AcceptorType.AccountingOperator) && a.IsActive)
                            .ToList() ?? new List<PPettyCashReimbursementAcceptor>();

        if (approvers.Any() && approvers.All(a => a.Status == AcceptorStatus.Approved) &&
            this.Status == PPettyCashReimbursementStatus.WaitingApproval)
        {
            this.Status = PPettyCashReimbursementStatus.WaitingAccountingApproval;
        }
        else if (accountingAcceptors.Any() && accountingAcceptors.All(a => a.Status == AcceptorStatus.Approved) &&
                 this.Status == PPettyCashReimbursementStatus.WaitingAccountingApproval)
        {
            this.Status = PPettyCashReimbursementStatus.WaitingDisbursementDate;
        }

        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Update,
            "เห็นชอบ/อนุมัติ",
            this.Status.ToString()));

        return this;
    }

    public PPettyCashReimbursement SetRejected(string? remark)
    {
        if (this.Status is not (PPettyCashReimbursementStatus.WaitingApproval or PPettyCashReimbursementStatus.Edit or PPettyCashReimbursementStatus.WaitingAccountingApproval))
        {
            return this;
        }

        this.Status = PPettyCashReimbursementStatus.Rejected;
        this.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            "ตีกลับแก้ไข",
            this.Status.ToString(),
            remark));

        return this;
    }

    public PPettyCashReimbursement AddAttachment(PPettyCashReimbursementAttachments attachment)
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

    public PPettyCashReimbursement RemoveAttachment(PPettyCashReimbursementAttachments attachment)
    {
        var list = this.Attachments.ToHashSet();
        list.Remove(attachment);
        this.Attachments = list;

        return this;
    }
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPettyCashReimbursementAttachmentsId
{
    public static PPettyCashReimbursementAttachmentsId New() => From(Guid.CreateVersion7());
}

public class PPettyCashReimbursementAttachments : AuditableEntity<PPettyCashReimbursementAttachmentsId>
{
    public override PPettyCashReimbursementAttachmentsId Id { get; init; }

    public ParameterCode DocumentTypeCode { get; private set; }

    public FileId FileId { get; private init; }

    public string FileName { get; private init; }

    public bool IsPublic { get; private set; }

    public int Sequence { get; private set; }

    public virtual PPettyCashReimbursement PPettyCashReimbursement { get; init; }

    public virtual SuParameter DocumentType { get; init; }

    public static PPettyCashReimbursementAttachments Create(
        ParameterCode documentTypeCode,
        FileId fileId,
        string fileName,
        int sequence,
        bool isPublic)
    {
        return new PPettyCashReimbursementAttachments
        {
            Id = PPettyCashReimbursementAttachmentsId.New(),
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            FileId = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public PPettyCashReimbursementAttachments SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public PPettyCashReimbursementAttachments SetIsPublic(bool isPublic)
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