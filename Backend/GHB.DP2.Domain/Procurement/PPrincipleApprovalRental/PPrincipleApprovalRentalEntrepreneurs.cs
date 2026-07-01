namespace GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRentalEntrepreneursId
{
    public static PPrincipleApprovalRentalEntrepreneursId New() => From(Guid.CreateVersion7());
}

public partial class PPrincipleApprovalRentalEntrepreneurs : AuditableEntity<PPrincipleApprovalRentalEntrepreneursId>, IHasSoftDelete
{
    public override PPrincipleApprovalRentalEntrepreneursId Id { get; init; }

    public bool EmailSend { get; private set; }

    public int Sequence { get; private set; }

    public bool WatchlistResult { get; private set; }

    public string? WatchlistResultRemark { get; private set; }

    public DateTimeOffset? WatchlistResultAt { get; private set; }

    public bool CoiResult { get; private set; }

    public string? CoiResultRemark { get; private set; }

    public DateTimeOffset? CoiResultAt { get; private set; }

    public bool EgpResult { get; private set; }

    public string? EgpResultRemark { get; private set; }

    public DateTimeOffset? EgpResultAt { get; private set; }

    public virtual PPrincipleApprovalRental PPrincipleApprovalRental { get; init; }

    public virtual SuVendor Vendor { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalEntrepreneursShareholders> EntrepreneursShareholders { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalEntrepreneursPriceDetails> EntrepreneursPriceDetails { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalEntrepreneursChecker> Checkers { get; private set; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalEntrepreneursAttachments> Attachments { get; private set; }

    public PPrincipleApprovalRentalEntrepreneurs SetWatchlist(bool result, string? remark, DateTimeOffset? at)
    {
        this.WatchlistResult = result;
        this.WatchlistResultRemark = remark;
        this.WatchlistResultAt = at;

        return this;
    }

    public PPrincipleApprovalRentalEntrepreneurs SetCoi(bool result, string? remark, DateTimeOffset? at)
    {
        this.CoiResult = result;
        this.CoiResultRemark = remark;
        this.CoiResultAt = at;

        return this;
    }

    public PPrincipleApprovalRentalEntrepreneurs SetEgp(bool result, string? remark, DateTimeOffset? at)
    {
        this.EgpResult = result;
        this.EgpResultRemark = remark;
        this.EgpResultAt = at;

        return this;
    }

    public static PPrincipleApprovalRentalEntrepreneurs Create(
        SuVendor vendor,
        int sequence,
        bool emailSend)
    {
        return new PPrincipleApprovalRentalEntrepreneurs
        {
            Id = PPrincipleApprovalRentalEntrepreneursId.New(),
            Vendor = vendor,
            Sequence = sequence,
            EmailSend = emailSend,
            Checkers = [],
        };
    }

    public PPrincipleApprovalRentalEntrepreneurs Update(
        int sequence,
        bool emailSend)
    {
        this.Sequence = sequence;
        this.EmailSend = emailSend;

        return this;
    }

    public PPrincipleApprovalRentalEntrepreneurs SetSequence(
        int sequence)
    {
        this.Sequence = sequence;

        return this;
    }

    public PPrincipleApprovalRentalEntrepreneurs AddShareholder(PPrincipleApprovalRentalEntrepreneursShareholders shareholder)
    {
        var shareholders = this.EntrepreneursShareholders?.ToList() ?? new List<PPrincipleApprovalRentalEntrepreneursShareholders>();
        shareholders.Add(shareholder);
        this.EntrepreneursShareholders = shareholders;

        return this;
    }

    public PPrincipleApprovalRentalEntrepreneurs RemoveShareholder(PPrincipleApprovalRentalEntrepreneursShareholders shareholder)
    {
        var list = this.EntrepreneursShareholders?.ToList() ?? new List<PPrincipleApprovalRentalEntrepreneursShareholders>();
        list.Remove(shareholder);
        this.EntrepreneursShareholders = list;

        return this;
    }

    public PPrincipleApprovalRentalEntrepreneurs AddPriceDetail(PPrincipleApprovalRentalEntrepreneursPriceDetails priceDetail)
    {
        var shareholders = this.EntrepreneursPriceDetails?.ToList() ?? new List<PPrincipleApprovalRentalEntrepreneursPriceDetails>();
        shareholders.Add(priceDetail);
        this.EntrepreneursPriceDetails = shareholders;

        return this;
    }

    public PPrincipleApprovalRentalEntrepreneurs RemovePriceDetail(PPrincipleApprovalRentalEntrepreneursPriceDetails priceDetail)
    {
        var list = this.EntrepreneursPriceDetails?.ToList() ?? new List<PPrincipleApprovalRentalEntrepreneursPriceDetails>();
        list.Remove(priceDetail);
        this.EntrepreneursPriceDetails = list;

        return this;
    }

    public PPrincipleApprovalRentalEntrepreneurs AddChecker(
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
            (PPrincipleApprovalRentalEntrepreneursChecker)PPrincipleApprovalRentalEntrepreneursChecker
                .Create(
                    checkType,
                    result,
                    resultAt,
                    remark);

        checkerList.Add(checker);

        this.Checkers = checkerList;

        return this;
    }

    public PPrincipleApprovalRentalEntrepreneurs RemoveAttachment(PPrincipleApprovalRentalEntrepreneursAttachments attachment)
    {
        var list = this.Attachments.ToHashSet();
        list.Remove(attachment);
        this.Attachments = list;

        return this;
    }

    public PPrincipleApprovalRentalEntrepreneurs AddAttachment(PPrincipleApprovalRentalEntrepreneursAttachments attachment)
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
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRentalEntrepreneursAttachmentId
{
    public static PPrincipleApprovalRentalEntrepreneursAttachmentId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalRentalEntrepreneursAttachments : AuditableEntity<PPrincipleApprovalRentalEntrepreneursAttachmentId>
{
    public ParameterCode DocumentTypeCode { get; private set; }

    public override PPrincipleApprovalRentalEntrepreneursAttachmentId Id { get; init; }

    public EntrepreneurAttachmentType Type { get; init; }

    public FileId FileId { get; private init; }

    public string FileName { get; private init; }

    public bool IsPublic { get; private set; }

    public int Sequence { get; private set; }

    public virtual SuParameter DocumentType { get; init; }

    public static PPrincipleApprovalRentalEntrepreneursAttachments Create(
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

        return new PPrincipleApprovalRentalEntrepreneursAttachments
        {
            Id = PPrincipleApprovalRentalEntrepreneursAttachmentId.New(),
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            Type = type,
            FileId = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public PPrincipleApprovalRentalEntrepreneursAttachments SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public PPrincipleApprovalRentalEntrepreneursAttachments SetIsPublic(bool isPublic)
    {
        this.IsPublic = isPublic;

        return this;
    }

    public Unit SetDocumentType(ParameterCode documentTypeCode)
    {
        this.DocumentTypeCode = documentTypeCode;

        return unit;
    }

    public PPrincipleApprovalRentalEntrepreneursAttachments Clone()
    {
        return new PPrincipleApprovalRentalEntrepreneursAttachments
        {
            Id = PPrincipleApprovalRentalEntrepreneursAttachmentId.New(),
            Sequence = this.Sequence,
            DocumentTypeCode = this.DocumentTypeCode,
            FileId = this.FileId,
            FileName = this.FileName,
            IsPublic = this.IsPublic,
        };
    }
}