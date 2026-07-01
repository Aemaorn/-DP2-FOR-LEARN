namespace GHB.DP2.Domain.Procurement.Pw184;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct Pw184VendorParcelId
{
    public static Pw184VendorParcelId New() => From(Guid.CreateVersion7());
}

public class Pw184VendorParcel : AuditableEntity<Pw184VendorParcelId>
{
    public override Pw184VendorParcelId Id { get; init; }

    public Pw184VendorId Pw184VendorId { get; set; }

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

    public virtual Pw184Vendor Pw184Vendor { get; init; }

    public virtual SuParameter VatIncludeType { get; init; }

    public static Pw184VendorParcel Create(Pw184VendorId pw184VendorId)
    {
        return new Pw184VendorParcel
        {
            Id = Pw184VendorParcelId.New(),
            Pw184VendorId = pw184VendorId,
        };
    }

    public Pw184VendorParcel SetSequence(int sequence)
    {
        this.Sequence = sequence;
        return this;
    }

    public Pw184VendorParcel SetItem(string item, string? itemDetail)
    {
        this.Item = item;
        this.ItemDetail = itemDetail;
        return this;
    }

    public Pw184VendorParcel SetPrice(
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

    public Pw184VendorParcel SetVatIncludeType(ParameterCode? vatIncludeTypeCode)
    {
        this.VatIncludeTypeCode = vatIncludeTypeCode;
        return this;
    }
}
