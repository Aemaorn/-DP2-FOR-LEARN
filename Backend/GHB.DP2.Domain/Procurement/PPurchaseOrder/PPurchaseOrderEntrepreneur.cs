namespace GHB.DP2.Domain.Procurement.PPurchaseOrder;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PurchaseOrderEntrepreneurId
{
    public static PurchaseOrderEntrepreneurId New() => From(Guid.CreateVersion7());
}

public partial class PPurchaseOrderEntrepreneur :
    AuditableEntity<PurchaseOrderEntrepreneurId>,
    IHasSoftDelete
{
    public override PurchaseOrderEntrepreneurId Id { get; init; }

    public PurchaseOrderId PurchaseOrderId { get; init; }

    public SuVendorId SuVendorId { get; init; }

    public bool EmailSended { get; private set; }

    public int Sequence { get; private set; }

    public bool? CoiResult { get; private set; }

    public string? CoiRemark { get; private set; }

    public DateTimeOffset? CoiDate { get; private set; }

    public bool? WatchlistResult { get; private set; }

    public string? WatchlistRemark { get; private set; }

    public DateTimeOffset? WatchlistDate { get; private set; }

    public bool? EgpResult { get; private set; }

    public string? EgpRemark { get; private set; }

    public DateTimeOffset? EgpDate { get; private set; }

    public bool IsWinner { get; private set; }

    public string? SelectionReasonCode { get; private set; }

    public string? Remark { get; private set; }

    public virtual PPurchaseOrder PPurchaseOrder { get; init; }

    public virtual SuVendor SuVendor { get; init; }

    public virtual IReadOnlyCollection<PPurchaseOrderPriceDetails> PJp006PriceDetails { get; private set; }

    public virtual IReadOnlyCollection<PPurchaseOrderEntrepreneurShareholders> PurchaseOrderShareholders { get; private set; }

    public virtual IReadOnlyCollection<PPurchaseOrderEntrepreneurChecker> PurchaseOrderEntrepreneurChecker { get; private set; }

    public virtual IReadOnlyCollection<PurchaseOrderEntrepreneurAttachments> Attachments { get; private set; }

    public static PPurchaseOrderEntrepreneur Create(
        PurchaseOrderId purchaseOrderId,
        SuVendorId suVendorId)
    {
        return new PPurchaseOrderEntrepreneur
        {
            Id = PurchaseOrderEntrepreneurId.New(),
            PurchaseOrderId = purchaseOrderId,
            SuVendorId = suVendorId,
            PJp006PriceDetails = [],
            PurchaseOrderShareholders = [],
            PurchaseOrderEntrepreneurChecker = [],
            Attachments = [],
        };
    }

    public static PPurchaseOrderEntrepreneur CreateWithId(
        PurchaseOrderEntrepreneurId id,
        PurchaseOrderId purchaseOrderId,
        SuVendorId suVendorId)
    {
        return new PPurchaseOrderEntrepreneur
        {
            Id = id,
            PurchaseOrderId = purchaseOrderId,
            SuVendorId = suVendorId,
            PJp006PriceDetails = [],
            PurchaseOrderShareholders = [],
        };
    }

    public PPurchaseOrderEntrepreneur AddPriceDetails(PPurchaseOrderPriceDetails priceDetails)
    {
        if (priceDetails == null)
        {
            throw new ArgumentNullException(nameof(priceDetails));
        }

        var priceDetailsList = this.PJp006PriceDetails.ToHashSet();

        if (priceDetailsList.Any(pd => pd.Sequence == priceDetails.Sequence))
        {
            throw new InvalidOperationException("Price details with the same sequence already exists.");
        }

        priceDetailsList.Add(priceDetails);
        this.PJp006PriceDetails = priceDetailsList;

        return this;
    }

    public PPurchaseOrderEntrepreneur RemovePriceDetails(PPurchaseOrderPriceDetails priceDetails)
    {
        if (priceDetails == null)
        {
            throw new ArgumentNullException(nameof(priceDetails));
        }

        var priceDetailsList = this.PJp006PriceDetails.ToHashSet();

        if (!priceDetailsList.Remove(priceDetails))
        {
            throw new InvalidOperationException("Price details not found.");
        }

        this.PJp006PriceDetails = priceDetailsList;

        return this;
    }

    public PPurchaseOrderEntrepreneur SetEmailSended(bool emailSended)
    {
        this.EmailSended = emailSended;

        return this;
    }

    public PPurchaseOrderEntrepreneur SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be non-negative.");
        }

        this.Sequence = sequence;

        return this;
    }

    public PPurchaseOrderEntrepreneur SetCoiResult(bool? coiResult = null, string? coiRemark = null, DateTimeOffset? coiDate = null)
    {
        this.CoiResult = coiResult;
        this.CoiRemark = coiRemark;
        this.CoiDate = coiDate;

        return this;
    }

    public PPurchaseOrderEntrepreneur SetWatchlistResult(bool? watchlistResult = null, string? watchlistRemark = null, DateTimeOffset? watchlistDate = null)
    {
        this.WatchlistResult = watchlistResult;
        this.WatchlistRemark = watchlistRemark;
        this.WatchlistDate = watchlistDate;

        return this;
    }

    public PPurchaseOrderEntrepreneur SetEgpResult(bool? egpResult = null, string? egpRemark = null, DateTimeOffset? egpDate = null)
    {
        this.EgpResult = egpResult;
        this.EgpRemark = egpRemark;
        this.EgpDate = egpDate;

        return this;
    }

    public PPurchaseOrderEntrepreneur SetIsWinner(bool isWinner)
    {
        this.IsWinner = isWinner;

        return this;
    }

    public PPurchaseOrderEntrepreneur SetSelectionReasonCode(string? selectionReasonCode)
    {
        this.SelectionReasonCode = selectionReasonCode;

        return this;
    }

    public PPurchaseOrderEntrepreneur SetRemark(string? remark)
    {
        this.Remark = remark;

        return this;
    }

    public PPurchaseOrderEntrepreneur AddPurchaseOrderEntrepreneurShareholderList(List<PPurchaseOrderEntrepreneurShareholders> shareholderList)
    {
        this.PurchaseOrderShareholders = shareholderList;

        return this;
    }

    public PPurchaseOrderEntrepreneur AddPurchaseOrderEntrepreneurShareholder(PPurchaseOrderEntrepreneurShareholders pInvitedEntrepreneurShareholder)
    {
        var shareholdersList = this.PurchaseOrderShareholders?.ToList() ?? new List<PPurchaseOrderEntrepreneurShareholders>();
        shareholdersList.Add(pInvitedEntrepreneurShareholder);

        this.PurchaseOrderShareholders = shareholdersList;

        return this;
    }

    public PPurchaseOrderEntrepreneur UpdatePurchaseOrderEntrepreneursShareholder(PPurchaseOrderEntrepreneurShareholders shareholders)
    {
        var shareholdersList = this.PurchaseOrderShareholders?.ToList() ?? new List<PPurchaseOrderEntrepreneurShareholders>();
        var idx = shareholdersList.FindIndex(a => a.Id == shareholders.Id);

        if (idx >= 0)
        {
            shareholdersList[idx] = shareholders;
            this.PurchaseOrderShareholders = shareholdersList;
        }

        return this;
    }

    public PPurchaseOrderEntrepreneur RemovePurchaseOrderEntrepreneursShareholder(PPurchaseOrderEntrepreneurShareholdersId shareholdersId)
    {
        var entrepreneurShareholdersList = this.PurchaseOrderShareholders?.ToList() ?? new List<PPurchaseOrderEntrepreneurShareholders>();
        entrepreneurShareholdersList.RemoveAll(a => a.Id == shareholdersId);
        this.PurchaseOrderShareholders = entrepreneurShareholdersList;

        return this;
    }

    public PPurchaseOrderEntrepreneur AddChecker(
        QualificationType checkType,
        QualificationResult result,
        DateTimeOffset resultAt,
        string? remark)
    {
        var checkerList = this.PurchaseOrderEntrepreneurChecker?.ToList() ?? [];

        var checkerExists =
            resultAt == checkerList.Where(x => x.CheckType == checkType).MaxBy(x => x.ResultAt)?.ResultAt;

        if (checkerExists)
        {
            return this;
        }

        var checker =
            (PPurchaseOrderEntrepreneurChecker)PPurchaseOrderEntrepreneurChecker
                .Create(
                    checkType,
                    result,
                    resultAt,
                    remark);

        checkerList.Add(checker);

        this.PurchaseOrderEntrepreneurChecker = checkerList;

        return this;
    }

    public PPurchaseOrderEntrepreneur RemoveAttachment(PurchaseOrderEntrepreneurAttachments attachment)
    {
        var list = this.Attachments.ToHashSet();
        list.Remove(attachment);
        this.Attachments = list;

        return this;
    }

    public PPurchaseOrderEntrepreneur AddAttachment(PurchaseOrderEntrepreneurAttachments attachment)
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
public partial struct PurchaseOrderEntrepreneurAttachmentId
{
    public static PurchaseOrderEntrepreneurAttachmentId New() => From(Guid.CreateVersion7());
}

public enum EntrepreneurAttachmentType
{
    Coi,
    Watchlist,
    Egp,
    General,
}

public class PurchaseOrderEntrepreneurAttachments : AuditableEntity<PurchaseOrderEntrepreneurAttachmentId>
{
    public ParameterCode DocumentTypeCode { get; private set; }

    public override PurchaseOrderEntrepreneurAttachmentId Id { get; init; }

    public EntrepreneurAttachmentType Type { get; init; }

    public FileId FileId { get; private init; }

    public string FileName { get; private init; }

    public bool IsPublic { get; private set; }

    public int Sequence { get; private set; }

    public virtual SuParameter DocumentType { get; init; }

    public static PurchaseOrderEntrepreneurAttachments Create(
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

        return new PurchaseOrderEntrepreneurAttachments
        {
            Id = PurchaseOrderEntrepreneurAttachmentId.New(),
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            Type = type,
            FileId = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public PurchaseOrderEntrepreneurAttachments SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public PurchaseOrderEntrepreneurAttachments SetIsPublic(bool isPublic)
    {
        this.IsPublic = isPublic;

        return this;
    }

    public Unit SetDocumentType(ParameterCode documentTypeCode)
    {
        this.DocumentTypeCode = documentTypeCode;

        return unit;
    }

    public PurchaseOrderEntrepreneurAttachments Clone()
    {
        return new PurchaseOrderEntrepreneurAttachments
        {
            Id = PurchaseOrderEntrepreneurAttachmentId.New(),
            Sequence = this.Sequence,
            DocumentTypeCode = this.DocumentTypeCode,
            FileId = this.FileId,
            FileName = this.FileName,
            IsPublic = this.IsPublic,
        };
    }
}