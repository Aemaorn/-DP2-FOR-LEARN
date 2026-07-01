namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CaContractDraftVendorShareholderId
{
    public static CaContractDraftVendorShareholderId New() => From(Guid.CreateVersion7());
}

public partial class CaContractDraftVendorShareholders :
    AuditableEntity<CaContractDraftVendorShareholderId>,
    IHasSoftDelete
{
    public override CaContractDraftVendorShareholderId Id { get; init; }

    public int? Sequence { get; protected set; }

    public string? TaxId { get; private set; }

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public bool? IsDirector { get; private set; }

    public bool? IsShareholder { get; private set; }

    public bool? IsJuristic { get; private set; }

    public string? CheckType { get; private set; }

    public bool? WatchlistResult { get; private set; }

    public string? WatchlistResultRemark { get; private set; }

    public DateTimeOffset? WatchlistResultAt { get; private set; }

    public bool? CoiResult { get; private set; }

    public string? CoiResultRemark { get; private set; }

    public DateTimeOffset? CoiResultAt { get; private set; }

    public bool? EgpResult { get; private set; }

    public string? EgpRemark { get; private set; }

    public DateTimeOffset? EgpResultAt { get; private set; }

    public virtual CaContractDraftVendor ContractDraftVendor { get; init; }

    public virtual IReadOnlyCollection<CaContractDraftVendorShareholderChecker> VendorShareholderCheckers { get; private set; }

    public static CaContractDraftVendorShareholders Create(
        int sequence,
        string? taxId,
        string? firstName,
        string? lastName,
        bool? isDirector,
        bool? isShareholder,
        bool? isJuristic = null)
    {
        return new CaContractDraftVendorShareholders
        {
            Id = CaContractDraftVendorShareholderId.New(),
            Sequence = sequence,
            TaxId = taxId,
            FirstName = firstName,
            LastName = lastName,
            IsDirector = isDirector,
            IsShareholder = isShareholder,
            IsJuristic = isJuristic,
            VendorShareholderCheckers = [],
        };
    }

    public CaContractDraftVendorShareholders Update(
        int sequence,
        string? taxId,
        string? firstName,
        string? lastName,
        bool? isDirector,
        bool? isShareholder,
        bool? isJuristic = null)
    {
        this.Sequence = sequence;
        this.TaxId = taxId;
        this.FirstName = firstName;
        this.LastName = lastName;
        this.IsDirector = isDirector;
        this.IsShareholder = isShareholder;
        this.IsJuristic = isJuristic;

        return this;
    }

    public CaContractDraftVendorShareholders SetCheckType(string? checkType)
    {
        this.CheckType = checkType;
        return this;
    }

    public CaContractDraftVendorShareholders SetWatchlist(bool? result, string? remark, DateTimeOffset? at)
    {
        this.WatchlistResult = result;
        this.WatchlistResultRemark = remark;
        this.WatchlistResultAt = at;

        return this;
    }

    public CaContractDraftVendorShareholders SetCoi(bool? result, string? remark, DateTimeOffset? at)
    {
        this.CoiResult = result;
        this.CoiResultRemark = remark;
        this.CoiResultAt = at;

        return this;
    }

    public CaContractDraftVendorShareholders SetEgp(bool? result, string? remark, DateTimeOffset? at)
    {
        this.EgpResult = result;
        this.EgpResultAt = at;
        this.EgpRemark = remark;

        return this;
    }

    public CaContractDraftVendorShareholders AddChecker(
        QualificationType checkType,
        QualificationResult result,
        DateTimeOffset resultAt,
        string? remark)
    {
        var checkerList = this.VendorShareholderCheckers?.ToList() ?? [];

        var checkerExists =
            resultAt == checkerList.Where(x => x.CheckType == checkType).MaxBy(x => x.ResultAt)?.ResultAt;

        if (checkerExists)
        {
            return this;
        }

        var checker =
            (CaContractDraftVendorShareholderChecker)CaContractDraftVendorShareholderChecker
                .Create(
                    checkType,
                    result,
                    resultAt,
                    remark);

        checkerList.Add(checker);

        this.VendorShareholderCheckers = checkerList;

        return this;
    }
}