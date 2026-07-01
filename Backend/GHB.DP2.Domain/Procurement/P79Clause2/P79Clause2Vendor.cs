namespace GHB.DP2.Domain.Procurement.P79Clause2;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct P79Clause2VendorId
{
    public static P79Clause2VendorId New() => From(Guid.CreateVersion7());
}

public class P79Clause2Vendor : AuditableEntity<P79Clause2VendorId>
{
    public override P79Clause2VendorId Id { get; init; }

    public P79Clause2Id P79Clause2Id { get; set; }

    public string VendorType { get; private set; }

    public SuVendorId? SuVendorId { get; private set; }

    public string VendorName { get; private set; }

    public int Sequence { get; private set; }

    public string? TaxNumber { get; private set; }

    public string? VendorBranchNumber { get; private set; }

    public ParameterCode? VatIncludeTypeCode { get; private set; }

    public ParameterCode BillTypeCode { get; private set; }

    public string? BillTypeOther { get; private set; }

    public string? BillBookNo { get; private set; }

    public DateTimeOffset? BillDate { get; private set; }

    public string? BillDetail { get; private set; }

    public virtual IReadOnlyCollection<P79Clause2VendorParcel> VendorParcels { get; private set; }

    public virtual P79Clause2 P79Clause2 { get; init; }

    public virtual SuParameter VatIncludeType { get; init; }

    public virtual SuParameter BillType { get; init; }

    public static P79Clause2Vendor Create(
            P79Clause2Id p79Clause2Id)
    {
        var P79Clause2Vendor = new P79Clause2Vendor
        {
            Id = P79Clause2VendorId.New(),
            P79Clause2Id = p79Clause2Id,
            VendorParcels = [],
        };

        return P79Clause2Vendor;
    }

    public P79Clause2Vendor SetSequence(
        int sequence)
    {
        this.Sequence = sequence;

        return this;
    }

    public P79Clause2Vendor SetVendorType(
        string vendorType)
    {
        this.VendorType = vendorType;

        return this;
    }

    public P79Clause2Vendor SetVendor(
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

    public P79Clause2Vendor SetBill(
        ParameterCode? vatIncludeTypeCode,
        ParameterCode billTypeCode,
        string? billTypeOther,
        string? billBookNo,
        DateTimeOffset? billDate,
        string? billDetail)
    {
        this.VatIncludeTypeCode = vatIncludeTypeCode;
        this.BillTypeCode = billTypeCode;
        this.BillTypeOther = billTypeOther;
        this.BillBookNo = billBookNo;
        this.BillDate = billDate;
        this.BillDetail = billDetail;

        return this;
    }

    public Unit AddVendorParcels(P79Clause2VendorParcel parcel)
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

    public Unit UpdateVendorsParcels(IEnumerable<P79Clause2VendorParcel> parcels)
    {
        this.VendorParcels = parcels.ToList();

        return unit;
    }
}