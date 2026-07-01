namespace GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

public enum CmContractGuaranteeReturnStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    /// <summary>
    /// รอเห็นชอบโดย บุคคล/คณะกรรมการตรวจรับพัสดุ
    /// </summary>
    WaitingCommitteeApproval,

    /// <summary>
    /// รอ ผอ. จพ. มอบหมาย (มอบหมายให้เจ้าหน้าที่ทำ Module Procurement)
    /// </summary>
    WaitingAssigned,

    /// <summary>
    /// จพ. มอบหมายงาน
    /// </summary>
    Assigned,

    /// <summary>
    /// รอเห็นชอบโดย ผู้มีอำนาจเห็นชอบ/อนุมัติ
    /// </summary>
    WaitingAcceptance,

    /// <summary>
    /// อนุมัติแล้ว
    /// </summary>
    Approved,

    /// <summary>
    /// ปฏิเสธ
    /// </summary>
    Rejected,

    WaitingAccountingApproval,

    WaitingDisbursementDate,

    Paid,
}

[ValueObject<string>(Conversions.EfCoreValueConverter)]
public partial struct GuaranteeReturnNumber
{
    public static GuaranteeReturnNumber New(int year)
    {
        if (year <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be greater than 0.");
        }

        // Generate a new plan number in the format "PYY00001"
        // where YY is the last two digits of the year and 00001 is the initial number
        var yearString = (year % 100).ToString("D2"); // Get last two digits of year and ensure it's two digits

        var guaranteeReturnNumber = $"{RunningPrefixConstant.GuranteeReturn}{yearString}00001";

        return From(guaranteeReturnNumber);
    }

    public GuaranteeReturnNumber Next()
    {
        if (string.IsNullOrWhiteSpace(this.Value))
        {
            throw new InvalidOperationException("Plan number cannot be null or empty.");
        }

        // Assuming the plan number is in the format "DPYYXXXXX"
        if (this.Value.Length < 4 || (!this.Value.StartsWith(RunningPrefixConstant.GuranteeReturn) && !this.Value.StartsWith("BP") && !this.Value.StartsWith("P") && !this.Value.StartsWith("B")))
        {
            throw new FormatException("Invalid plan number format.");
        }

        // NOTE: Add Condition For Support Both Prefix "DP" and ("P" or "BP" or "B")
        string yearPart = this.Value.StartsWith(RunningPrefixConstant.GuranteeReturn) ? this.Value.Substring(2, 2) : this.Value.Substring(1, 2);
        string numberPart = this.Value.Substring(4);

        if (!int.TryParse(yearPart, out var year) || !int.TryParse(numberPart, out var number))
        {
            throw new FormatException("Invalid plan number format.");
        }

        // Increment the number part
        number++;

        // Format the new plan number
        var newGuaranteeReturnNumber = $"{RunningPrefixConstant.GuranteeReturn}{year}{number:D5}";

        return From(newGuaranteeReturnNumber);
    }
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CmContractGuaranteeReturnId
{
    public static CmContractGuaranteeReturnId New => From(Guid.CreateVersion7());
}

public partial class CmContractGuaranteeReturn : AuditableEntity<CmContractGuaranteeReturnId>, IHasSoftDelete, IHasActivityInfo
{
    public override CmContractGuaranteeReturnId Id { get; init; }

    public ContractDraftVendorId ContractDraftVendorId { get; init; }

    public DateTimeOffset GuaranteeReturnDate { get; private set; }

    public decimal ReturnAmount { get; private set; }

    public bool IsDeducted { get; private set; }

    public decimal? DeductedAmount { get; private set; }

    public decimal NetReturnAmount { get; private set; }

    public string? AdditionalComment { get; private set; }

    public CmContractGuaranteeReturnStatus Status { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public string? ContractDescription { get; private set; }

    public string? ProofOfPaymentDescription { get; private set; }

    public string? GuranteeDescription { get; private set; }

    public GuaranteeReturnNumber GuaranteeNumber { get; private set; }

    public DateTimeOffset? DisbursementDate { get; private set; }

    public decimal? DisbursementAmount { get; private set; }

    public string? DisbursementRemark { get; private set; }

    public bool IsSendMail { get; private set; }

    public string? EmailSend { get; private set; }

    public string? EmailTemplate { get; private set; }

    public virtual CaContractDraftVendor CaContractDraftVendor { get; init; }

    public virtual IReadOnlyCollection<CmContractGuaranteeReturnAcceptor> Acceptors { get; private set; }

    public virtual IReadOnlyCollection<CmContractGuaranteeReturnAssignee> Assignees { get; private set; }

    public virtual IReadOnlyCollection<CmContractGuaranteeReturnCondition> Conditions { get; private set; }

    public virtual IReadOnlyCollection<CmContractGuaranteeReturnRequiredDocument> RequiredDocuments { get; private set; }

    public virtual IReadOnlyCollection<CmContractGuaranteeReturnDocumentHistory> DocumentHistories { get; private set; }

    public virtual IReadOnlyCollection<CmContractGuaranteeReturnAttachments> Attachments { get; private set; }

    public virtual IReadOnlyCollection<CmContractGuaranteeReturnEmailAttachments> EmailAttachments { get; private set; }

    public CmContractGuaranteeReturnDocumentHistory? LastedApprovalGuaranteeReturnDraftDocument =>
        this.DocumentHistories
            .Where(x => x.DocumentType == CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn)
            .Where(x => x.StatusState == CmContractGuaranteeReturnStatus.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public CmContractGuaranteeReturnDocumentHistory? LastedApprovalGuaranteeReturnWaitingCommitteeApprovalDocument =>
        this.DocumentHistories
            .Where(x => x.DocumentType == CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn)
            .Where(x => x.StatusState == CmContractGuaranteeReturnStatus.WaitingCommitteeApproval)
            .Where(x => !x.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public CmContractGuaranteeReturnDocumentHistory? LastedContractGuaranteeReturnResultDraftDocument =>
        this.DocumentHistories
            .Where(x => x.DocumentType == CmContractGuaranteeReturnDocumentType.CmContractGuaranteeReturnResule)
            .Where(x => x.StatusState == CmContractGuaranteeReturnStatus.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public CmContractGuaranteeReturnDocumentHistory? LastedContractGuaranteeReturnResultWaitingCommitteeApprovalDocument =>
        this.DocumentHistories
            .Where(x => x.DocumentType == CmContractGuaranteeReturnDocumentType.CmContractGuaranteeReturnResule)
            .Where(x => x.StatusState == CmContractGuaranteeReturnStatus.WaitingCommitteeApproval)
            .Where(x => !x.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public CmContractGuaranteeReturnDocumentHistory? GetApprovalDocumentForStatus(
        CmContractGuaranteeReturnStatus statusContext) =>
        statusContext switch
        {
            CmContractGuaranteeReturnStatus.Draft or
                CmContractGuaranteeReturnStatus.Rejected =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn)
                    .Where(dh =>
                        dh.StatusState == CmContractGuaranteeReturnStatus.Draft ||
                        dh.StatusState == CmContractGuaranteeReturnStatus.Rejected)
                    .OrderVersions()
                    .FirstOrDefault(),

            CmContractGuaranteeReturnStatus.WaitingCommitteeApproval =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn)
                    .Where(dh =>
                        dh.StatusState == CmContractGuaranteeReturnStatus.Draft ||
                        dh.StatusState == CmContractGuaranteeReturnStatus.Rejected ||
                        (dh.StatusState == CmContractGuaranteeReturnStatus.WaitingCommitteeApproval && dh.IsReplaced))
                    .OrderVersions()
                    .FirstOrDefault(),

            CmContractGuaranteeReturnStatus.WaitingAssigned =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn)
                    .Where(dh =>
                        dh.StatusState == CmContractGuaranteeReturnStatus.Draft ||
                        dh.StatusState == CmContractGuaranteeReturnStatus.Rejected ||
                        dh.StatusState == CmContractGuaranteeReturnStatus.WaitingCommitteeApproval ||
                        dh.StatusState == CmContractGuaranteeReturnStatus.WaitingAssigned)
                    .OrderVersions()
                    .FirstOrDefault(),

            CmContractGuaranteeReturnStatus.Assigned =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn)
                    .Where(dh =>
                        dh.StatusState == CmContractGuaranteeReturnStatus.WaitingCommitteeApproval ||
                        dh.StatusState == CmContractGuaranteeReturnStatus.WaitingAssigned ||
                        dh.StatusState == CmContractGuaranteeReturnStatus.Assigned)
                    .OrderVersions()
                    .FirstOrDefault(),

            CmContractGuaranteeReturnStatus.WaitingAcceptance =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn)
                    .Where(dh =>
                        dh.StatusState == CmContractGuaranteeReturnStatus.Assigned ||
                        (dh.StatusState == CmContractGuaranteeReturnStatus.WaitingAcceptance && dh.IsReplaced))
                    .OrderVersions()
                    .FirstOrDefault(),

            CmContractGuaranteeReturnStatus.Approved =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn)
                    .Where(dh => dh.IsReplaced &&
                                 ((dh.StatusState == CmContractGuaranteeReturnStatus.WaitingAcceptance && dh.IsReplaced) ||
                                  dh.StatusState == CmContractGuaranteeReturnStatus.Approved))
                    .OrderVersions()
                    .FirstOrDefault(),

            _ => this.DocumentHistories
                     .Where(dh => dh.DocumentType == CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn)
                     .OrderVersions()
                     .FirstOrDefault(),
        };

    public CmContractGuaranteeReturnDocumentHistory? GetResultDocumentForStatus(
        CmContractGuaranteeReturnStatus statusContext) =>
        statusContext switch
        {
            CmContractGuaranteeReturnStatus.Draft or
                CmContractGuaranteeReturnStatus.Rejected =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == CmContractGuaranteeReturnDocumentType.CmContractGuaranteeReturnResule)
                    .Where(dh =>
                        dh.StatusState == CmContractGuaranteeReturnStatus.Draft ||
                        dh.StatusState == CmContractGuaranteeReturnStatus.Rejected)
                    .OrderVersions()
                    .FirstOrDefault(),

            CmContractGuaranteeReturnStatus.WaitingCommitteeApproval =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == CmContractGuaranteeReturnDocumentType.CmContractGuaranteeReturnResule)
                    .Where(dh =>
                        dh.StatusState == CmContractGuaranteeReturnStatus.Draft ||
                        dh.StatusState == CmContractGuaranteeReturnStatus.Rejected ||
                        (dh.StatusState == CmContractGuaranteeReturnStatus.WaitingCommitteeApproval && dh.IsReplaced))
                    .OrderVersions()
                    .FirstOrDefault(),

            CmContractGuaranteeReturnStatus.WaitingAssigned =>
                this.DocumentHistories
                    .Where(dh => dh.DocumentType == CmContractGuaranteeReturnDocumentType.CmContractGuaranteeReturnResule)
                    .Where(dh =>
                        dh.StatusState == CmContractGuaranteeReturnStatus.Draft ||
                        dh.StatusState == CmContractGuaranteeReturnStatus.Rejected ||
                        (dh.StatusState == CmContractGuaranteeReturnStatus.WaitingCommitteeApproval) ||
                        dh.StatusState == CmContractGuaranteeReturnStatus.WaitingAssigned)
                    .OrderVersions()
                    .FirstOrDefault(),

            _ => this.DocumentHistories
                     .Where(dh => dh.DocumentType == CmContractGuaranteeReturnDocumentType.CmContractGuaranteeReturnResule)
                     .OrderVersions()
                     .FirstOrDefault(),
        };

    public Unit AddDocumentHistory(
        CmContractGuaranteeReturnDocumentType documentType,
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
            CmContractGuaranteeReturnDocumentHistory.Create(
                documentType,
                this.Status,
                version,
                fileId,
                isReplace));

        this.DocumentHistories = histories;

        return unit;
    }

    public CmContractGuaranteeReturn AddAttachment(CmContractGuaranteeReturnAttachments attachment)
    {
        if (attachment == null)
        {
            throw new ArgumentNullException(nameof(attachment), "Attachment cannot be null.");
        }

        if (this.Attachments.Contains(attachment))
        {
            throw new InvalidOperationException("Attachment already exists in the plan.");
        }

        var attachments = this.Attachments.ToHashSet() ?? [];

        attachments.Add(attachment);

        this.Attachments = attachments;

        return this;
    }

    public CmContractGuaranteeReturn RemoveAttachment(CmContractGuaranteeReturnAttachments attachment)
    {
        var list = this.Attachments.ToHashSet() ?? [];
        list.Remove(attachment);
        this.Attachments = list;

        return this;
    }

    public static CmContractGuaranteeReturn Create(
        ContractDraftVendorId contractDraftVendorId)
    {
        var entity = new CmContractGuaranteeReturn
        {
            Id = CmContractGuaranteeReturnId.New,
            ContractDraftVendorId = contractDraftVendorId,
            Acceptors = [],
            Assignees = [],
            Conditions = [],
            RequiredDocuments = [],
            DocumentHistories = [],
            Attachments = [],
            GuaranteeNumber = GuaranteeReturnNumber.New(DateTimeOffset.UtcNow.Year),
            EmailAttachments = [],
        };

        entity.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Create,
                ActivityLogActionTypeConstant.Create,
                nameof(CmContractGuaranteeReturnStatus.Draft)));

        return entity;
    }

    public CmContractGuaranteeReturn SetValues(
        DateTimeOffset guaranteeReturnDate,
        decimal returnAmount,
        bool isDeducted,
        decimal? deductedAmount,
        decimal netReturnAmount,
        string? additionalComment)
    {
        this.GuaranteeReturnDate = guaranteeReturnDate;
        this.ReturnAmount = returnAmount;
        this.IsDeducted = isDeducted;
        this.DeductedAmount = deductedAmount;
        this.NetReturnAmount = netReturnAmount;
        this.AdditionalComment = additionalComment;

        return this;
    }

    public CmContractGuaranteeReturn SetDescriptions(
        string? contractDescription,
        string? proofOfPaymentDescription,
        string? guranteeDescription)
    {
        this.ContractDescription = contractDescription;
        this.ProofOfPaymentDescription = proofOfPaymentDescription;
        this.GuranteeDescription = guranteeDescription;

        return this;
    }

    public CmContractGuaranteeReturn SetDocumentDate(DateTimeOffset? date = null)
    {
        this.DocumentDate = date ?? DateTimeOffset.Now;

        return this;
    }

    public CmContractGuaranteeReturn SetStatus(CmContractGuaranteeReturnStatus status)
    {
        this.Status = status;

        return this;
    }

    public CmContractGuaranteeReturn SetDisbursement(
        DateTimeOffset? disbursementDate,
        decimal? disbursementAmount,
        string? disbursementRemark)
    {
        this.DisbursementDate = disbursementDate;
        this.DisbursementAmount = disbursementAmount;
        this.DisbursementRemark = disbursementRemark;

        return this;
    }

    public CmContractGuaranteeReturn AddAcceptor(CmContractGuaranteeReturnAcceptor acceptor)
    {
        var acceptors = this.Acceptors?.ToList() ?? new List<CmContractGuaranteeReturnAcceptor>();
        acceptors.Add(acceptor);
        this.Acceptors = acceptors;

        return this;
    }

    public CmContractGuaranteeReturn RemoveAcceptor(CmContractGuaranteeReturnAcceptor acceptor)
    {
        var list = this.Acceptors?.ToList() ?? new List<CmContractGuaranteeReturnAcceptor>();
        list.Remove(acceptor);
        this.Acceptors = list;

        return this;
    }

    public CmContractGuaranteeReturn AddAssignee(CmContractGuaranteeReturnAssignee assignee)
    {
        var assignees = this.Assignees?.ToList() ?? new List<CmContractGuaranteeReturnAssignee>();
        assignees.Add(assignee);
        this.Assignees = assignees;

        return this;
    }

    public CmContractGuaranteeReturn RemoveAssignee(CmContractGuaranteeReturnAssignee assign)
    {
        var assignees = this.Assignees?.ToList() ?? new List<CmContractGuaranteeReturnAssignee>();
        assignees.Remove(assign);
        this.Assignees = assignees;

        return this;
    }

    public CmContractGuaranteeReturn AddConditions(CmContractGuaranteeReturnCondition condition)
    {
        var conditions = this.Conditions?.ToList() ?? new List<CmContractGuaranteeReturnCondition>();
        conditions.Add(condition);
        this.Conditions = conditions;

        return this;
    }

    public CmContractGuaranteeReturn RemoveConditions(CmContractGuaranteeReturnCondition condition)
    {
        var conditions = this.Conditions?.ToList() ?? new List<CmContractGuaranteeReturnCondition>();
        conditions.Remove(condition);
        this.Conditions = conditions;

        return this;
    }

    public CmContractGuaranteeReturn AddRequiredDocuments(CmContractGuaranteeReturnRequiredDocument requiredDocument)
    {
        var requiredDocuments = this.RequiredDocuments?.ToList() ?? new List<CmContractGuaranteeReturnRequiredDocument>();
        requiredDocuments.Add(requiredDocument);
        this.RequiredDocuments = requiredDocuments;

        return this;
    }

    public CmContractGuaranteeReturn RemoveRequiredDocuments(CmContractGuaranteeReturnRequiredDocument requiredDocument)
    {
        var requiredDocuments = this.RequiredDocuments?.ToList() ?? new List<CmContractGuaranteeReturnRequiredDocument>();
        requiredDocuments.Remove(requiredDocument);
        this.RequiredDocuments = requiredDocuments;

        return this;
    }

    public CmContractGuaranteeReturn SetStatusWaitingAssigned()
    {
        this.Status = CmContractGuaranteeReturnStatus.WaitingAssigned;

        return this;
    }

    public CmContractGuaranteeReturn SetStatusRejected()
    {
        this.Status = CmContractGuaranteeReturnStatus.Rejected;

        return this;
    }

    public CmContractGuaranteeReturn SetStatusApproved()
    {
        this.Status = CmContractGuaranteeReturnStatus.Approved;

        return this;
    }

    public bool HasMajorityRejection()
    {
        if (this.Status != CmContractGuaranteeReturnStatus.WaitingCommitteeApproval)
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

    public CmContractGuaranteeReturn RemoveAttachment(CmContractGuaranteeReturnEmailAttachments attachment)
    {
        var list = this.EmailAttachments.ToHashSet();
        list.Remove(attachment);
        this.EmailAttachments = list;

        return this;
    }

    public CmContractGuaranteeReturn AddAttachment(CmContractGuaranteeReturnEmailAttachments attachment)
    {
        if (attachment == null)
        {
            throw new ArgumentNullException(nameof(attachment), "Attachment cannot be null.");
        }

        if (this.EmailAttachments == null)
        {
            this.EmailAttachments = [];
        }

        if (this.EmailAttachments.Contains(attachment))
        {
            throw new InvalidOperationException("Attachment already exists in the plan.");
        }

        var attachments = this.EmailAttachments.ToHashSet();

        attachments.Add(attachment);

        this.EmailAttachments = attachments;

        return this;
    }

    public CmContractGuaranteeReturn SetSendMailInfo(string email, string emailTemplate)
    {
        this.IsSendMail = true;
        this.EmailSend = email;
        this.EmailTemplate = emailTemplate;

        return this;
    }
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CmContractGuaranteeReturnAttachmentId
{
    public static CmContractGuaranteeReturnAttachmentId New() => From(Guid.CreateVersion7());
}

public class CmContractGuaranteeReturnAttachments : AuditableEntity<CmContractGuaranteeReturnAttachmentId>
{
    public override CmContractGuaranteeReturnAttachmentId Id { get; init; }

    public ParameterCode DocumentTypeCode { get; private set; }

    public FileId FileId { get; private init; }

    public string FileName { get; private init; }

    public bool IsPublic { get; private set; }

    public int Sequence { get; private set; }

    public virtual CmContractGuaranteeReturn CmContractGuaranteeReturn { get; init; }

    public virtual SuParameter DocumentType { get; init; }

    public static CmContractGuaranteeReturnAttachments Create(
        ParameterCode documentTypeCode,
        FileId fileId,
        string fileName,
        int sequence,
        bool isPublic)
    {
        return new CmContractGuaranteeReturnAttachments
        {
            Id = CmContractGuaranteeReturnAttachmentId.New(),
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            FileId = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public CmContractGuaranteeReturnAttachments SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public CmContractGuaranteeReturnAttachments SetIsPublic(bool isPublic)
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