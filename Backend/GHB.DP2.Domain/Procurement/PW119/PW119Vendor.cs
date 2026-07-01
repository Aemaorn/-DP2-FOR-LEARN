namespace GHB.DP2.Domain.Procurement.Pw119;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct Pw119VendorId
{
    public static Pw119VendorId New() => From(Guid.CreateVersion7());
}

public class Pw119Vendor : AuditableEntity<Pw119VendorId>
{
    public override Pw119VendorId Id { get; init; }

    public Pw119Id Pw119Id { get; set; }

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

    public virtual IReadOnlyCollection<Pw119VendorParcel> VendorParcels { get; private set; }

    public virtual SuParameter VatIncludeType { get; init; }

    public virtual SuParameter BillType { get; init; }

    public virtual Pw119 Pw119 { get; init; }

    public static Pw119Vendor Create(
            Pw119Id pw119Id)
    {
        var pw119Vendor = new Pw119Vendor
        {
            Id = Pw119VendorId.New(),
            Pw119Id = pw119Id,
            VendorParcels = [],
        };

        return pw119Vendor;
    }

    public Pw119Vendor SetSequence(
        int sequence)
    {
        this.Sequence = sequence;

        return this;
    }

    public Pw119Vendor SetVendorType(
        string vendorType)
    {
        this.VendorType = vendorType;

        return this;
    }

    public Pw119Vendor SetVendor(
        SuVendorId? suVendorId,
        string? taxNumber,
        string vendorName,
        string? vendorBranchNumber)
    {
        this.SuVendorId = suVendorId;
        this.TaxNumber = taxNumber;
        this.VendorName = vendorName;
        this.VendorBranchNumber = vendorBranchNumber;

        return this;
    }

    public Pw119Vendor SetBill(
        ParameterCode? vatIncludeTypeCode,
        ParameterCode billTypeCode,
        string? billTypeOther,
        string billBookNo,
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

    public Unit AddVendorParcels(Pw119VendorParcel parcel)
    {
        if (parcel == null)
        {
            throw new ArgumentNullException(nameof(parcel), "ไม่มีค่ารายการพัสดุ");
        }

        var parcels = this.VendorParcels.ToHashSet();

        parcels.Add(parcel);

        this.VendorParcels = parcels;

        return unit;
    }

    public Unit UpdateVendorsParcels(IEnumerable<Pw119VendorParcel> parcels)
    {
        this.VendorParcels = parcels.ToList();

        return unit;
    }
}