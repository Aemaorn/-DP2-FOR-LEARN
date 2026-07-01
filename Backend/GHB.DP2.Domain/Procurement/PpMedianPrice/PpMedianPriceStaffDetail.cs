namespace GHB.DP2.Domain.Procurement.PpMedianPrice;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct MedianPriceStaffDetailId
{
    public static MedianPriceStaffDetailId New() => From(Guid.CreateVersion7());
}

public enum MedianPriceStaffType
{
    /// <summary>
    /// บุคลากรทั่วไปที่เกี่ยวข้องกับราคากลาง
    /// </summary>
    Personal,

    /// <summary>
    /// ประเภทของที่ปรึกษาที่เกี่ยวข้องกับราคากลาง
    /// </summary>
    ConsultantTypes,

    /// <summary>
    /// คุณสมบัติของที่ปรึกษาที่เกี่ยวข้องกับราคากลาง
    /// </summary>
    ConsultantQualifications,
}

public abstract partial class PpMedianPriceStaffDetail : AuditableEntity<MedianPriceStaffDetailId>, IHasSoftDelete
{
    public override MedianPriceStaffDetailId Id { get; init; }

    public MedianPriceStaffType Type { get; init; }

    public int Sequence { get; protected set; }

    public string Description { get; protected set; }

    public abstract PpMedianPriceStaffDetail Clone();

    public PpMedianPriceStaffDetail SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be non-negative.");
        }

        this.Sequence = sequence;

        return this;
    }

    public PpMedianPriceStaffDetail SetDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description cannot be null or empty.", nameof(description));
        }

        this.Description = description;

        return this;
    }

    public virtual PpMedianPriceStaff MedianPriceStaff { get; init; }
}

/// <summary>
/// รายละเอียดบุคลากรที่เกี่ยวข้องกับราคากลาง
/// </summary>
public partial class PpMedianPriceStaffPersonal : PpMedianPriceStaffDetail
{
    public int PersonalCount { get; private set; }

    public PpMedianPriceStaffPersonal SetPersonalCount(int personalCount)
    {
        if (personalCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(personalCount), "Personal count cannot be negative.");
        }

        this.PersonalCount = personalCount;

        return this;
    }

    public static PpMedianPriceStaffPersonal Create(
        int sequence,
        string description,
        int personalCount)
    {
        return new PpMedianPriceStaffPersonal
        {
            Id = MedianPriceStaffDetailId.New(),
            Type = MedianPriceStaffType.Personal,
            Sequence = sequence,
            Description = description,
            PersonalCount = personalCount,
        };
    }

    public static PpMedianPriceStaffPersonal Create(
        MedianPriceStaffDetailId id,
        int sequence,
        string description,
        int personalCount)
    {
        return new PpMedianPriceStaffPersonal
        {
            Id = id,
            Type = MedianPriceStaffType.Personal,
            Sequence = sequence,
            Description = description,
            PersonalCount = personalCount,
        };
    }

    public override PpMedianPriceStaffDetail Clone()
    {
        return new PpMedianPriceStaffPersonal
        {
            Id = MedianPriceStaffDetailId.New(),
            Sequence = this.Sequence,
            Description = this.Description,
            PersonalCount = this.PersonalCount,
            Type = MedianPriceStaffType.Personal,
        };
    }
}

/// <summary>
/// รายละเอียดประเภทของที่ปรึกษาที่เกี่ยวข้องกับราคากลาง
/// </summary>
public partial class PpMedianPriceStaffConsultantTypes : PpMedianPriceStaffDetail
{
    public static PpMedianPriceStaffConsultantTypes Create(
        int sequence,
        string description)
    {
        return new PpMedianPriceStaffConsultantTypes
        {
            Id = MedianPriceStaffDetailId.New(),
            Type = MedianPriceStaffType.ConsultantTypes,
            Sequence = sequence,
            Description = description,
        };
    }

    public static PpMedianPriceStaffConsultantTypes Create(
        MedianPriceStaffDetailId id,
        int sequence,
        string description)
    {
        return new PpMedianPriceStaffConsultantTypes
        {
            Id = id,
            Type = MedianPriceStaffType.ConsultantTypes,
            Sequence = sequence,
            Description = description,
        };
    }

    public override PpMedianPriceStaffDetail Clone()
    {
        return new PpMedianPriceStaffConsultantTypes
        {
            Id = MedianPriceStaffDetailId.New(),
            Sequence = this.Sequence,
            Description = this.Description,
            Type = MedianPriceStaffType.ConsultantTypes,
        };
    }
}

/// <summary>
/// รายละเอียดคุณสมบัติของที่ปรึกษาที่เกี่ยวข้องกับราคากลาง
/// </summary>
public partial class PpMedianPriceStaffConsultantQualifications : PpMedianPriceStaffDetail
{
    public static PpMedianPriceStaffConsultantQualifications Create(
        int sequence,
        string description)
    {
        return new PpMedianPriceStaffConsultantQualifications
        {
            Id = MedianPriceStaffDetailId.New(),
            Type = MedianPriceStaffType.ConsultantQualifications,
            Sequence = sequence,
            Description = description,
        };
    }

    public static PpMedianPriceStaffConsultantQualifications Create(
        MedianPriceStaffDetailId id,
        int sequence,
        string description)
    {
        return new PpMedianPriceStaffConsultantQualifications
        {
            Id = id,
            Type = MedianPriceStaffType.ConsultantQualifications,
            Sequence = sequence,
            Description = description,
        };
    }

    public override PpMedianPriceStaffDetail Clone()
    {
        return new PpMedianPriceStaffConsultantQualifications
        {
            Id = MedianPriceStaffDetailId.New(),
            Sequence = this.Sequence,
            Description = this.Description,
            Type = MedianPriceStaffType.ConsultantQualifications,
        };
    }
}