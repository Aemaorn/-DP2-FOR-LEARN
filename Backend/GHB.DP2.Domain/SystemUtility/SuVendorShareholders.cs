namespace GHB.DP2.Domain.SystemUtility;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct SuVendorShareholdersId
{
    public static SuVendorShareholdersId New() => From(Guid.CreateVersion7());
}

public partial class SuVendorShareholders : AuditableEntity<SuVendorShareholdersId>, IHasSoftDelete
{
    public override SuVendorShareholdersId Id { get; init; }

    public SuVendorId? VendorId { get; init; }

    public int? Sequence { get; private set; }

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public bool? IsDirector { get; private set; }

    public bool? IsShareholder { get; private set; }

    public bool? IsJuristic { get; private set; }

    public virtual SuVendor Vendor { get; init; }

    public static SuVendorShareholders Create(
        SuVendorId? vendorId,
        int sequence,
        string? firstName,
        string? lastName,
        bool? isDirector,
        bool? isShareholder,
        bool? isJuristic)
    {
        return new SuVendorShareholders
        {
            Id = SuVendorShareholdersId.New(),
            VendorId = vendorId,
            Sequence = sequence,
            FirstName = firstName,
            LastName = lastName,
            IsDirector = isDirector,
            IsShareholder = isShareholder,
            IsJuristic = isJuristic,
        };
    }

    public SuVendorShareholders Update(
        int sequence,
        string? firstName,
        string? lastName,
        bool? isDirector,
        bool? isShareholder,
        bool? isJuristic)
    {
        this.Sequence = sequence;
        this.FirstName = firstName;
        this.LastName = lastName;
        this.IsDirector = isDirector;
        this.IsShareholder = isShareholder;
        this.IsJuristic = isJuristic;

        return this;
    }
}
