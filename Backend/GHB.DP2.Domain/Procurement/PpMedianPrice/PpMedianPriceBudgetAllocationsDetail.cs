namespace GHB.DP2.Domain.Procurement.PpMedianPrice;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct BudgetAllocationsDetailId
{
    public static BudgetAllocationsDetailId New() => From(Guid.CreateVersion7());
}

public enum BudgetAllocationsDetailType
{
    /// <summary>
    /// รายละเอียดการจัดสรรงบประมาณ
    /// </summary>
    With,

    /// <summary>
    /// รายละเอียดการจัดสรรงบประมาณที่ไม่มี
    /// </summary>
    Without,
}

public abstract partial class PpMedianPriceBudgetAllocationsDetail : AuditableEntity<BudgetAllocationsDetailId>, IHasSoftDelete
{
    public override BudgetAllocationsDetailId Id { get; init; }

    public BudgetAllocationsDetailType Type { get; init; }

    public int Sequence { get; protected set; }

    public string Source { get; protected set; }

    public virtual PpMedianPriceBudgetAllocations BudgetAllocations { get; init; }

    public abstract PpMedianPriceBudgetAllocationsDetail Clone();

    public PpMedianPriceBudgetAllocationsDetail SetSequence(int sequence)
    {
        this.Sequence = sequence;

        return this;
    }

    public PpMedianPriceBudgetAllocationsDetail SetSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source cannot be null or empty.", nameof(source));
        }

        this.Source = source;

        return this;
    }
}

/// <summary>
/// ประเภทของการจัดสรรงบประมาณที่มีรายละเอียดราคาอ้างอิง
/// </summary>
public partial class PpMedianPriceBudgetAllocationsWithDetail : PpMedianPriceBudgetAllocationsDetail
{
    public decimal ReferenceBudge { get; private set; }

    public PpMedianPriceBudgetAllocationsWithDetail SetReferenceBudge(decimal referenceBudge)
    {
        if (referenceBudge < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(referenceBudge), "Reference budget cannot be negative.");
        }

        this.ReferenceBudge = referenceBudge;

        return this;
    }

    public static PpMedianPriceBudgetAllocationsDetail CreateWithDetail(
        int sequence,
        string source,
        decimal referenceBudge)
    {
        return new PpMedianPriceBudgetAllocationsWithDetail
        {
            Id = BudgetAllocationsDetailId.New(),
            Sequence = sequence,
            Source = source,
            ReferenceBudge = referenceBudge,
            Type = BudgetAllocationsDetailType.With,
        };
    }

    public static PpMedianPriceBudgetAllocationsDetail CreateWithDetail(
        BudgetAllocationsDetailId id,
        int sequence,
        string source,
        decimal referenceBudge)
    {
        return new PpMedianPriceBudgetAllocationsWithDetail
        {
            Id = id,
            Sequence = sequence,
            Source = source,
            ReferenceBudge = referenceBudge,
            Type = BudgetAllocationsDetailType.With,
        };
    }

    public override PpMedianPriceBudgetAllocationsWithDetail Clone()
    {
        return new PpMedianPriceBudgetAllocationsWithDetail
        {
            Id = BudgetAllocationsDetailId.New(),
            Sequence = this.Sequence,
            Source = this.Source,
            ReferenceBudge = this.ReferenceBudge,
            Type = BudgetAllocationsDetailType.With,
        };
    }
}

/// <summary>
/// ประเภทของการจัดสรรงบประมาณที่ไม่มีรายละเอียดราคาอ้างอิง
/// </summary>
public partial class PpMedianPriceBudgetAllocationsWithoutDetail : PpMedianPriceBudgetAllocationsDetail
{
    public static PpMedianPriceBudgetAllocationsDetail CreateWithoutDetail(
        int sequence,
        string source)
    {
        return new PpMedianPriceBudgetAllocationsWithoutDetail
        {
            Id = BudgetAllocationsDetailId.New(),
            Sequence = sequence,
            Source = source,
            Type = BudgetAllocationsDetailType.Without,
        };
    }

    public static PpMedianPriceBudgetAllocationsDetail CreateWithoutDetail(
        BudgetAllocationsDetailId id,
        int sequence,
        string source)
    {
        return new PpMedianPriceBudgetAllocationsWithoutDetail
        {
            Id = id,
            Sequence = sequence,
            Source = source,
            Type = BudgetAllocationsDetailType.Without,
        };
    }

    public override PpMedianPriceBudgetAllocationsDetail Clone()
    {
        return new PpMedianPriceBudgetAllocationsWithoutDetail
        {
            Id = BudgetAllocationsDetailId.New(),
            Sequence = this.Sequence,
            Source = this.Source,
            Type = BudgetAllocationsDetailType.Without,
        };
    }
}