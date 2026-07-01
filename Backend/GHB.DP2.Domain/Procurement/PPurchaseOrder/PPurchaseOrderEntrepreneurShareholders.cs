namespace GHB.DP2.Domain.Procurement.PPurchaseOrder;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPurchaseOrderEntrepreneurShareholdersId
{
    public static PPurchaseOrderEntrepreneurShareholdersId New() => From(Guid.CreateVersion7());
}

public partial class PPurchaseOrderEntrepreneurShareholders :
    AuditableEntity<PPurchaseOrderEntrepreneurShareholdersId>,
    IHasSoftDelete
{
    public override PPurchaseOrderEntrepreneurShareholdersId Id { get; init; }

    public int Sequence { get; private set; }

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

    public virtual PPurchaseOrderEntrepreneur PurchaseOrderEntrepreneur { get; init; }

    public virtual IReadOnlyCollection<PPurchaseOrderEntrepreneurShareholderChecker> PurchaseOrderEntrepreneurShareholderCheckers { get; private set; }

    public static PPurchaseOrderEntrepreneurShareholders Create(
        int sequence,
        string? taxId,
        string? firstName,
        string? lastName,
        bool? isDirector,
        bool? isShareholder,
        bool? isJuristic)
    {
        return new PPurchaseOrderEntrepreneurShareholders
        {
            Id = PPurchaseOrderEntrepreneurShareholdersId.New(),
            Sequence = sequence,
            TaxId = taxId,
            FirstName = firstName,
            LastName = lastName,
            IsDirector = isDirector,
            IsShareholder = isShareholder,
            IsJuristic = isJuristic,
            PurchaseOrderEntrepreneurShareholderCheckers = [],
        };
    }

    public PPurchaseOrderEntrepreneurShareholders Update(
        int sequence,
        string? taxId,
        string? firstName,
        string? lastName,
        bool? isDirector,
        bool? isShareholder,
        bool? isJuristic)
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

    public PPurchaseOrderEntrepreneurShareholders SetWatchlist(bool? result, string? remark, DateTimeOffset? at)
    {
        this.WatchlistResult = result;
        this.WatchlistResultRemark = remark;
        this.WatchlistResultAt = at;

        return this;
    }

    public PPurchaseOrderEntrepreneurShareholders SetCoi(bool? result, string? remark, DateTimeOffset? at)
    {
        this.CoiResult = result;
        this.CoiResultRemark = remark;
        this.CoiResultAt = at;

        return this;
    }

    public PPurchaseOrderEntrepreneurShareholders SetEgp(bool? result, string? remark, DateTimeOffset? at)
    {
        this.EgpResult = result;
        this.EgpRemark = remark;
        this.EgpResultAt = at;

        return this;
    }

    public PPurchaseOrderEntrepreneurShareholders SetCheckType(string? checkType)
    {
        this.CheckType = checkType;
        return this;
    }

    public PPurchaseOrderEntrepreneurShareholders AddChecker(
        QualificationType checkType,
        QualificationResult result,
        DateTimeOffset resultAt,
        string? remark)
    {
        var checkerList = this.PurchaseOrderEntrepreneurShareholderCheckers?.ToList() ?? [];

        var checkerExists =
            resultAt == checkerList.Where(x => x.CheckType == checkType).MaxBy(x => x.ResultAt)?.ResultAt;

        if (checkerExists)
        {
            return this;
        }

        var checker =
            (PPurchaseOrderEntrepreneurShareholderChecker)PPurchaseOrderEntrepreneurShareholderChecker
                .Create(
                    checkType,
                    result,
                    resultAt,
                    remark);

        checkerList.Add(checker);

        this.PurchaseOrderEntrepreneurShareholderCheckers = checkerList;

        return this;
    }
}