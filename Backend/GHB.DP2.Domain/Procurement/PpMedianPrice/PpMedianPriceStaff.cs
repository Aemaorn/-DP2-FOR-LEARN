namespace GHB.DP2.Domain.Procurement.PpMedianPrice;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct MedianPriceStaffId
{
    public static MedianPriceStaffId New() => From(Guid.CreateVersion7());
}

/// <summary>
/// รายละเอียดบุคลากรที่เกี่ยวข้องกับราคากลาง
/// </summary>
public partial class PpMedianPriceStaff : AuditableEntity<MedianPriceStaffId>, IHasSoftDelete
{
    public override MedianPriceStaffId Id { get; init; }

    public decimal PersonnelCompensation { get; private set; }

    public int PersonnelCount { get; private set; }

    public virtual PpMedianPrice MedianPrice { get; init; }

    public virtual IReadOnlyCollection<PpMedianPriceStaffDetail> Details { get; private set; }

    public PpMedianPriceStaff Clone()
    {
        return new PpMedianPriceStaff
        {
            Id = MedianPriceStaffId.New(),
            PersonnelCompensation = this.PersonnelCompensation,
            PersonnelCount = this.PersonnelCount,
            Details = this.Details.Map(t => t.Clone()).ToHashSet(),
        };
    }

    public PpMedianPriceStaff AddDetail(PpMedianPriceStaffDetail detail)
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

    public PpMedianPriceStaff RemoveDetail(PpMedianPriceStaffDetail detail)
    {
        if (detail == null)
        {
            throw new ArgumentNullException(nameof(detail));
        }

        var details = this.Details.ToHashSet();

        if (!details.Remove(detail))
        {
            throw new InvalidOperationException("Detail not found.");
        }

        this.Details = details;

        return this;
    }

    public PpMedianPriceStaff SetPersonnelCompensation(decimal personnelCompensation)
    {
        if (personnelCompensation < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(personnelCompensation), "Personnel compensation cannot be negative.");
        }

        this.PersonnelCompensation = personnelCompensation;

        return this;
    }

    public PpMedianPriceStaff SetPersonnelCount(int personnelCount)
    {
        if (personnelCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(personnelCount), "Personnel count cannot be negative.");
        }

        this.PersonnelCount = personnelCount;

        return this;
    }

    public static PpMedianPriceStaff Create(
        decimal personnelCompensation,
        int personnelCount)
    {
        return new PpMedianPriceStaff
        {
            Id = MedianPriceStaffId.New(),
            PersonnelCompensation = personnelCompensation,
            PersonnelCount = personnelCount,
            Details = [],
        };
    }
}