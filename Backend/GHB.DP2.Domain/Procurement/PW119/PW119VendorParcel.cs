namespace GHB.DP2.Domain.Procurement.Pw119;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct Pw119VendorParcelId
{
    public static Pw119VendorParcelId New() => From(Guid.CreateVersion7());
}

public class Pw119VendorParcel : AuditableEntity<Pw119VendorParcelId>
{
    public override Pw119VendorParcelId Id { get; init; }

    public Pw119VendorId Pw119VendorId { get; set; }

    public int Sequence { get; set; }

    public string Item { get; set; }

    public string? ItemDetail { get; set; }

    public int Quantity { get; set; }

    public virtual ParameterCode UnitCode { get; private set; }

    public virtual SuParameter Unit { get; init; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal TotalPriceVat { get; set; }

    public ParameterCode? VatIncludeTypeCode { get; private set; }

    public virtual Pw119Vendor Pw119Vendor { get; init; }

    public virtual SuParameter VatIncludeType { get; init; }

    public static Pw119VendorParcel Create(
        Pw119VendorId pw119VendorId)
    {
        var pw119VendorParcel = new Pw119VendorParcel
        {
            Id = Pw119VendorParcelId.New(),
            Pw119VendorId = pw119VendorId,
        };

        return pw119VendorParcel;
    }

    public Pw119VendorParcel SetSequence(
        int sequence)
    {
        this.Sequence = sequence;

        return this;
    }

    public Pw119VendorParcel SetItem(
        string item,
        string? itemDetail)
    {
        this.Item = item;
        this.ItemDetail = itemDetail;

        return this;
    }

    public Pw119VendorParcel SetPrice(
        int quantity,
        ParameterCode unitCode,
        decimal unitPrice,
        decimal totalPrice,
        decimal totalPriceVat)
    {
        this.Quantity = quantity;
        this.UnitCode = unitCode;
        this.UnitPrice = unitPrice;
        this.TotalPrice = totalPrice;
        this.TotalPriceVat = totalPriceVat;

        return this;
    }

    public Pw119VendorParcel SetVatIncludeType(
        ParameterCode? vatIncludeTypeCode)
    {
        this.VatIncludeTypeCode = vatIncludeTypeCode;

        return this;
    }
}