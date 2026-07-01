namespace GHB.DP2.Domain.Procurement.PPettyCash;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PettyCashVendorId
{
    public static PettyCashVendorId New() => From(Guid.CreateVersion7());
}

public class PPettyCashVendor : AuditableEntity<PettyCashVendorId>
{
    public override PettyCashVendorId Id { get; init; }

    public PettyCashId PettyCashId { get; set; }

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

    public virtual IReadOnlyCollection<PPettyCashVendorParcel> VendorParcels { get; private set; }

    public virtual PPettyCash PettyCash { get; init; }

    public virtual SuParameter VatIncludeType { get; init; }

    public virtual SuParameter BillType { get; init; }

    public static PPettyCashVendor Create(
            PettyCashId pettyCashId)
    {
        var PettyCashVendor = new PPettyCashVendor
        {
            Id = PettyCashVendorId.New(),
            PettyCashId = pettyCashId,
            VendorParcels = [],
        };

        return PettyCashVendor;
    }

    public PPettyCashVendor SetSequence(
        int sequence)
    {
        this.Sequence = sequence;

        return this;
    }

    public PPettyCashVendor SetVendorType(
        string vendorType)
    {
        this.VendorType = vendorType;

        return this;
    }

    public PPettyCashVendor SetVendor(
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

    public PPettyCashVendor SetBill(
        ParameterCode? vatIncludeTypeCode,
        ParameterCode billTypeCode,
        string? billTypeOther,
        string billBookNo,
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

    public Unit AddVendorParcels(PPettyCashVendorParcel parcel)
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

    public Unit UpdateVendorsParcels(IEnumerable<PPettyCashVendorParcel> parcels)
    {
        this.VendorParcels = parcels.ToList();

        return unit;
    }
}