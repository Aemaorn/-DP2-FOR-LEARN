namespace GHB.DP2.Domain.Procurement.Pw184;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct Pw184VendorId
{
    public static Pw184VendorId New() => From(Guid.CreateVersion7());
}

public class Pw184Vendor : AuditableEntity<Pw184VendorId>
{
    public override Pw184VendorId Id { get; init; }

    public Pw184Id Pw184Id { get; set; }

    public string VendorType { get; private set; }

    public SuVendorId? SuVendorId { get; private set; }

    public string VendorName { get; private set; }

    public int Sequence { get; private set; }

    public string? TaxNumber { get; private set; }

    public string? VendorBranchNumber { get; private set; }

    public ParameterCode? VatIncludeTypeCode { get; private set; }

    public ParameterCode BillTypeCode { get; private set; }

    public string? BillBookNo { get; private set; }

    public string? BillTypeOther { get; private set; }

    public DateTimeOffset? BillDate { get; private set; }

    public string? BillDetail { get; private set; }

    public virtual IReadOnlyCollection<Pw184VendorParcel> VendorParcels { get; private set; }

    public virtual SuParameter VatIncludeType { get; init; }

    public virtual SuParameter BillType { get; init; }

    public virtual Pw184 Pw184 { get; init; }

    public static Pw184Vendor Create(Pw184Id pw184Id)
    {
        return new Pw184Vendor
        {
            Id = Pw184VendorId.New(),
            Pw184Id = pw184Id,
            VendorParcels = [],
        };
    }

    public Pw184Vendor SetSequence(int sequence)
    {
        this.Sequence = sequence;
        return this;
    }

    public Pw184Vendor SetVendorType(string vendorType)
    {
        this.VendorType = vendorType;
        return this;
    }

    public Pw184Vendor SetVendor(SuVendorId? suVendorId, string? taxNumber, string vendorName, string? vendorBranchNumber)
    {
        this.SuVendorId = suVendorId;
        this.TaxNumber = taxNumber;
        this.VendorName = vendorName;
        this.VendorBranchNumber = vendorBranchNumber;
        return this;
    }

    public Pw184Vendor SetBill(
        ParameterCode? vatIncludeTypeCode,
        ParameterCode billTypeCode,
        string? billTypeOther,
        string? billBookNo,
        DateTimeOffset? billDate,
        string? billDetail)
    {
        this.VatIncludeTypeCode = vatIncludeTypeCode;
        this.BillTypeCode = billTypeCode;
        this.BillBookNo = billBookNo;
        this.BillTypeOther = billTypeOther;
        this.BillDate = billDate;
        this.BillDetail = billDetail;
        return this;
    }

    public Unit AddVendorParcels(Pw184VendorParcel parcel)
    {
        if (parcel == null) throw new ArgumentNullException(nameof(parcel));
        var parcels = this.VendorParcels.ToHashSet();
        parcels.Add(parcel);
        this.VendorParcels = parcels;
        return unit;
    }

    public Unit UpdateVendorsParcels(IEnumerable<Pw184VendorParcel> parcels)
    {
        this.VendorParcels = parcels.ToList();
        return unit;
    }
}
