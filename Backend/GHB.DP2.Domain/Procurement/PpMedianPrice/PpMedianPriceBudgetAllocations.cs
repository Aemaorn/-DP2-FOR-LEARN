namespace GHB.DP2.Domain.Procurement.PpMedianPrice;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct BudgetAllocationsId
{
    public static BudgetAllocationsId New() => From(Guid.CreateVersion7());
}

/// <summary>
/// ตารางแสดงวงเงินงบประมาณที่ได้รับการจัดสรร
/// </summary>
public partial class PpMedianPriceBudgetAllocations : AuditableEntity<BudgetAllocationsId>, IHasSoftDelete
{
    public override BudgetAllocationsId Id { get; init; }

    public DateTimeOffset? ReferenceDate { get; private set; }

    public decimal Budget { get; private set; }

    public decimal ReferenceMedianPrice { get; private set; }

    public virtual PpMedianPrice MedianPrice { get; init; }

    public virtual IReadOnlyCollection<PpMedianPriceBudgetAllocationsDetail> Details { get; private set; }

    public PpMedianPriceBudgetAllocations Clone()
    {
        return new PpMedianPriceBudgetAllocations
        {
            Id = BudgetAllocationsId.New(),
            ReferenceDate = this.ReferenceDate,
            Budget = this.Budget,
            ReferenceMedianPrice = this.ReferenceMedianPrice,
            Details = this.Details.Map(i => i.Clone()).ToHashSet(),
        };
    }

    public PpMedianPriceBudgetAllocations AddDetail(PpMedianPriceBudgetAllocationsDetail detail)
    {
        if (detail == null)
        {
            throw new ArgumentNullException(nameof(detail));
        }

        var details = this.Details.ToHashSet();

        if (details.Any(d => d.Id == detail.Id))
        {
            throw new InvalidOperationException("Detail with the same Id already exists.");
        }

        details.Add(detail);

        this.Details = details;

        return this;
    }

    public PpMedianPriceBudgetAllocations RemoveDetail(PpMedianPriceBudgetAllocationsDetail detail)
    {
        if (detail == null)
        {
            throw new ArgumentNullException(nameof(detail));
        }

        var details = this.Details.ToHashSet();

        if (!details.Remove(detail))
        {
            throw new InvalidOperationException("Detail with the specified Id does not exist.");
        }

        this.Details = details;

        return this;
    }

    public PpMedianPriceBudgetAllocations SetReferenceDate()
    {
        this.ReferenceDate = DateTimeOffset.UtcNow;

        return this;
    }

    public PpMedianPriceBudgetAllocations SetBudget(decimal budget)
    {
        if (budget <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(budget), "Budget must be greater than zero.");
        }

        this.Budget = budget;

        return this;
    }

    public PpMedianPriceBudgetAllocations SetReferenceMedianPrice(decimal referenceMedianPrice)
    {
        if (referenceMedianPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(referenceMedianPrice), "Reference median price must be greater than zero.");
        }

        this.ReferenceMedianPrice = referenceMedianPrice;

        return this;
    }

    public static PpMedianPriceBudgetAllocations Create(
        decimal budget,
        decimal referenceMedianPrice)
    {
        return new PpMedianPriceBudgetAllocations
        {
            Id = BudgetAllocationsId.New(),
            Budget = budget,
            ReferenceMedianPrice = referenceMedianPrice,
            Details = [],
        };
    }
}