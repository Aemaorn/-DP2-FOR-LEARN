namespace GHB.DP2.Domain.ContractAgreement.CaContractInvitation;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using System.ComponentModel.DataAnnotations;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractInvitationVendorsId
{
    public static ContractInvitationVendorsId New() => From(Guid.CreateVersion7());
}

public partial class CaContractInvitationVendors : AuditableEntity<ContractInvitationVendorsId>, IHasSoftDelete
{
    public override ContractInvitationVendorsId Id { get; init; }

    public PurchaseOrderApprovalContractId PurchaseOrderApprovalContractId { get; init; }

    public FileId? DocumentId { get; private set; }

    [MaxLength(1000)]
    public string Email { get; private set; }

    [MaxLength(2000)]
    public string ContractName { get; private set; }

    [MaxLength(100)]
    public string PoNumber { get; private set; }

    [MaxLength(100)]
    public string ContractNumber { get; private set; }

    public decimal AgreedPrice { get; private set; }

    public bool HasContractGuarantee { get; private set; }

    public decimal? ContractGuaranteePercent { get; private set; }

    public decimal? GuaranteeAmount { get; private set; }

    [MaxLength(1000)]
    public string ContractOfficerName { get; private set; }

    [MaxLength(20)]
    public string ContractOfficerPhone { get; private set; }

    public string ContractOfficerEmail { get; private set; }

    public bool? EgpResult { get; private set; }

    public string? EgpRemark { get; private set; }

    public DateTimeOffset? EgpDate { get; private set; }

    public bool? CoiResult { get; private set; }

    public string? CoiRemark { get; private set; }

    public DateTimeOffset? CoiDate { get; private set; }

    public bool? WatchlistResult { get; private set; }

    public string? WatchlistRemark { get; private set; }

    public string? EmailSend { get; private set; }

    public string? EmailTemplate { get; private set; }

    public DateTimeOffset? WatchlistDate { get; private set; }

    public DateTimeOffset? DocumentDate { get; private set; }

    public ParameterCode? DocumentTemplateCode { get; private set; }

    public virtual SuParameter? DocumentTemplateType { get; init; }

    public virtual CaContractInvitation ContractInvitation { get; init; }

    public virtual PPurchaseOrderApprovalContract PurchaseOrderApprovalContract { get; init; }

    public virtual IReadOnlyCollection<CaContractInvitationVendorsDocumentHistory> DocumentHistories { get; private set; }

    public virtual IReadOnlyCollection<CaContractInvitationVendorShareholders> Shareholders { get; private set; }

    public virtual IReadOnlyCollection<CaContractInvitationVendorChecker> Checkers { get; private set; }

    public virtual IReadOnlyCollection<CaContractInvitationVendorAttachments> Attachments { get; private set; }

    public virtual IReadOnlyCollection<CaContractInvitationVendorEmailAttachments> EmailAttachments { get; private set; }

    public CaContractInvitationVendorsDocumentHistory? LastedDraftContractDraftDocument =>
        this.DocumentHistories
            .Where(x => x.DocumentType == CaContractInvitationDocumentType.ContractInvitation)
            .Where(x => x.StatusState == ContractInvitationStatus.Draft || x.StatusState == ContractInvitationStatus.Edit || x.StatusState == ContractInvitationStatus.Rejected)
            .OrderVersions()
            .FirstOrDefault();

    public CaContractInvitationVendorsDocumentHistory? LastedDocument =>
        this.DocumentHistories
            .Where(x => x.DocumentType == CaContractInvitationDocumentType.ContractInvitation)
            .Where(x => !x.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public Unit AddDocumentHistory(CaContractInvitationVendorsDocumentHistory vendorsDocumentHistory)
    {
        if (vendorsDocumentHistory == null)
        {
            throw new ArgumentNullException(nameof(vendorsDocumentHistory), "Document history cannot be null.");
        }

        if (this.DocumentHistories.Contains(vendorsDocumentHistory))
        {
            throw new InvalidOperationException("Document history already exists in the plan.");
        }

        var histories = this.DocumentHistories.ToHashSet();

        histories.Add(vendorsDocumentHistory);

        this.DocumentHistories = histories;

        return unit;
    }

    public record InvitationVendorInfo(
        PurchaseOrderApprovalContractId PurchaseOrderApprovalContractId,
        FileId? DocumentId,
        string Email,
        string ContractName,
        string PoNumber,
        string ContractNumber,
        decimal AgreedPrice,
        bool HasContractGuarantee,
        decimal? ContractGuaranteePercent,
        decimal? GuaranteeAmount,
        string ContractOfficerName,
        string ContractOfficerPhone,
        string ContractOfficerEmail,
        bool? EgpResult,
        string? EgpRemark,
        DateTimeOffset? EgpDate,
        bool? CoiResult,
        string? CoiRemark,
        DateTimeOffset? CoiDate,
        bool? WatchlistResult,
        string? WatchlistRemark,
        DateTimeOffset? WatchlistDate,
        ParameterCode? DocumentTemplateCode);

    public static CaContractInvitationVendors Create(InvitationVendorInfo vendorInfo)
    {
        return new CaContractInvitationVendors
        {
            Id = ContractInvitationVendorsId.New(),
            PurchaseOrderApprovalContractId = vendorInfo.PurchaseOrderApprovalContractId,
            DocumentId = vendorInfo.DocumentId,
            Email = vendorInfo.Email,
            ContractName = vendorInfo.ContractName,
            PoNumber = vendorInfo.PoNumber,
            ContractNumber = vendorInfo.ContractNumber,
            AgreedPrice = vendorInfo.AgreedPrice,
            HasContractGuarantee = vendorInfo.HasContractGuarantee,
            ContractGuaranteePercent = vendorInfo.ContractGuaranteePercent,
            GuaranteeAmount = vendorInfo.GuaranteeAmount,
            ContractOfficerName = vendorInfo.ContractOfficerName,
            ContractOfficerPhone = vendorInfo.ContractOfficerPhone,
            ContractOfficerEmail = vendorInfo.ContractOfficerEmail,
            EgpResult = vendorInfo.EgpResult,
            EgpRemark = vendorInfo.EgpRemark,
            EgpDate = vendorInfo.EgpDate,
            CoiResult = vendorInfo.CoiResult,
            CoiRemark = vendorInfo.CoiRemark,
            CoiDate = vendorInfo.CoiDate,
            WatchlistResult = vendorInfo.WatchlistResult,
            WatchlistRemark = vendorInfo.WatchlistRemark,
            WatchlistDate = vendorInfo.WatchlistDate,
            DocumentTemplateCode = vendorInfo.DocumentTemplateCode,
            DocumentHistories = [],
            Shareholders = [],
            Checkers = [],
            Attachments = [],
        };
    }

    public CaContractInvitationVendors Update(InvitationVendorInfo vendorInfo)
    {
        this.DocumentId = vendorInfo.DocumentId;
        this.Email = vendorInfo.Email;
        this.ContractName = vendorInfo.ContractName;
        this.PoNumber = vendorInfo.PoNumber;
        this.ContractNumber = vendorInfo.ContractNumber;
        this.HasContractGuarantee = vendorInfo.HasContractGuarantee;
        this.ContractGuaranteePercent = vendorInfo.ContractGuaranteePercent;
        this.GuaranteeAmount = vendorInfo.GuaranteeAmount;
        this.ContractOfficerName = vendorInfo.ContractOfficerName;
        this.ContractOfficerPhone = vendorInfo.ContractOfficerPhone;
        this.ContractOfficerEmail = vendorInfo.ContractOfficerEmail;
        this.EgpResult = vendorInfo.EgpResult;
        this.EgpRemark = vendorInfo.EgpRemark;
        this.EgpDate = vendorInfo.EgpDate;
        this.AgreedPrice = vendorInfo.AgreedPrice;
        this.CoiResult = vendorInfo.CoiResult;
        this.CoiRemark = vendorInfo.CoiRemark;
        this.CoiDate = vendorInfo.CoiDate;
        this.WatchlistResult = vendorInfo.WatchlistResult;
        this.WatchlistRemark = vendorInfo.WatchlistRemark;
        this.WatchlistDate = vendorInfo.WatchlistDate;
        this.DocumentTemplateCode = vendorInfo.DocumentTemplateCode;

        return this;
    }

    public CaContractInvitationVendors AddCaContractInvitationVendorShareholderList(List<CaContractInvitationVendorShareholders> shareholders)
    {
        this.Shareholders = shareholders;

        return this;
    }

    public CaContractInvitationVendors AddCaContractInvitationVendorShareholder(CaContractInvitationVendorShareholders shareholders)
    {
        var shareholdersList = this.Shareholders?.ToList() ?? new List<CaContractInvitationVendorShareholders>();
        shareholdersList.Add(shareholders);

        this.Shareholders = shareholdersList;

        return this;
    }

    public CaContractInvitationVendors UpdateCaContractInvitationVendorShareholder(CaContractInvitationVendorShareholders shareholders)
    {
        var shareholdersList = this.Shareholders?.ToList() ?? new List<CaContractInvitationVendorShareholders>();
        var idx = shareholdersList.FindIndex(a => a.Id == shareholders.Id);

        if (idx >= 0)
        {
            shareholdersList[idx] = shareholders;
            this.Shareholders = shareholdersList;
        }

        return this;
    }

    public CaContractInvitationVendors RemoveCaContractInvitationVendorShareholder(CaContractInvitationVendorShareholderId shareholdersId)
    {
        var entrepreneurShareholdersList = this.Shareholders?.ToList() ?? new List<CaContractInvitationVendorShareholders>();
        entrepreneurShareholdersList.RemoveAll(a => a.Id == shareholdersId);
        this.Shareholders = entrepreneurShareholdersList;

        return this;
    }

    public CaContractInvitationVendors AddChecker(
        QualificationType checkType,
        QualificationResult result,
        DateTimeOffset resultAt,
        string? remark)
    {
        var checkerList = this.Checkers?.ToList() ?? [];

        var checkerExists =
            resultAt == checkerList.Where(x => x.CheckType == checkType).MaxBy(x => x.ResultAt)?.ResultAt;

        if (checkerExists)
        {
            return this;
        }

        var checker =
            (CaContractInvitationVendorChecker)CaContractInvitationVendorChecker
                .Create(
                    checkType,
                    result,
                    resultAt,
                    remark);

        checkerList.Add(checker);

        this.Checkers = checkerList;

        return this;
    }

    public CaContractInvitationVendors RemoveAttachment(CaContractInvitationVendorAttachments attachment)
    {
        var list = this.Attachments.ToHashSet();
        list.Remove(attachment);
        this.Attachments = list;

        return this;
    }

    public CaContractInvitationVendors AddAttachment(CaContractInvitationVendorAttachments attachment)
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

    public CaContractInvitationVendors RemoveAttachment(CaContractInvitationVendorEmailAttachments attachment)
    {
        var list = this.EmailAttachments.ToHashSet();
        list.Remove(attachment);
        this.EmailAttachments = list;

        return this;
    }

    public CaContractInvitationVendors AddAttachment(CaContractInvitationVendorEmailAttachments attachment)
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

    public CaContractInvitationVendors SetSendMailInfo(string email, string emailTemplate)
    {
        this.EmailSend = email;
        this.EmailTemplate = emailTemplate;

        return this;
    }

    public CaContractInvitationVendors SetDocumentDate(DateTimeOffset? date = null)
    {
        this.DocumentDate = date ?? DateTimeOffset.Now;

        return this;
    }
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CaContractInvitationVendorAttachmentId
{
    public static CaContractInvitationVendorAttachmentId New() => From(Guid.CreateVersion7());
}

public class CaContractInvitationVendorAttachments : AuditableEntity<CaContractInvitationVendorAttachmentId>
{
    public ParameterCode DocumentTypeCode { get; private set; }

    public override CaContractInvitationVendorAttachmentId Id { get; init; }

    public EntrepreneurAttachmentType Type { get; init; }

    public FileId FileId { get; private init; }

    public string FileName { get; private init; }

    public bool IsPublic { get; private set; }

    public int Sequence { get; private set; }

    public virtual SuParameter DocumentType { get; init; }

    public static CaContractInvitationVendorAttachments Create(
        ParameterCode documentTypeCode,
        FileId fileId,
        string fileName,
        EntrepreneurAttachmentType type,
        int sequence,
        bool isPublic)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        return new CaContractInvitationVendorAttachments
        {
            Id = CaContractInvitationVendorAttachmentId.New(),
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            Type = type,
            FileId = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public CaContractInvitationVendorAttachments SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public CaContractInvitationVendorAttachments SetIsPublic(bool isPublic)
    {
        this.IsPublic = isPublic;

        return this;
    }

    public Unit SetDocumentType(ParameterCode documentTypeCode)
    {
        this.DocumentTypeCode = documentTypeCode;

        return unit;
    }

    public CaContractInvitationVendorAttachments Clone()
    {
        return new CaContractInvitationVendorAttachments
        {
            Id = CaContractInvitationVendorAttachmentId.New(),
            Sequence = this.Sequence,
            DocumentTypeCode = this.DocumentTypeCode,
            FileId = this.FileId,
            FileName = this.FileName,
            IsPublic = this.IsPublic,
        };
    }
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CaContractInvitationVendorEmailAttachmentsId
{
    public static CaContractInvitationVendorEmailAttachmentsId New() => From(Guid.CreateVersion7());
}

public class CaContractInvitationVendorEmailAttachments : AuditableEntity<CaContractInvitationVendorEmailAttachmentsId>
{
    public override CaContractInvitationVendorEmailAttachmentsId Id { get; init; }

    public FileId FileId { get; private init; }

    public string FileName { get; private init; }

    public int Sequence { get; private set; }

    public static CaContractInvitationVendorEmailAttachments Create(
        FileId fileId,
        string fileName,
        int sequence)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        return new CaContractInvitationVendorEmailAttachments
        {
            Id = CaContractInvitationVendorEmailAttachmentsId.New(),
            Sequence = sequence,
            FileId = fileId,
            FileName = fileName,
        };
    }

    public CaContractInvitationVendorEmailAttachments SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public CaContractInvitationVendorEmailAttachments Clone()
    {
        return new CaContractInvitationVendorEmailAttachments
        {
            Id = CaContractInvitationVendorEmailAttachmentsId.New(),
            Sequence = this.Sequence,
            FileId = this.FileId,
            FileName = this.FileName,
        };
    }
}