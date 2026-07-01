namespace GHB.DP2.Domain.Procurement.P79Clause2;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct P79Clause2VendorParcelId
{
    public static P79Clause2VendorParcelId New() => From(Guid.CreateVersion7());
}

public class P79Clause2VendorParcel : AuditableEntity<P79Clause2VendorParcelId>
{
    public override P79Clause2VendorParcelId Id { get; init; }

    public P79Clause2VendorId P79Clause2VendorId { get; set; }

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

    public virtual P79Clause2Vendor P79Clause2Vendor { get; init; }

    public virtual SuParameter VatIncludeType { get; init; }

    public static P79Clause2VendorParcel Create(
        P79Clause2VendorId p79Clause2VendorId)
    {
        var P79Clause2VendorParcel = new P79Clause2VendorParcel
        {
            Id = P79Clause2VendorParcelId.New(),
            P79Clause2VendorId = p79Clause2VendorId,
        };

        return P79Clause2VendorParcel;
    }

    public P79Clause2VendorParcel SetSequence(
        int sequence)
    {
        this.Sequence = sequence;

        return this;
    }

    public P79Clause2VendorParcel SetItem(
        string item,
        string? itemDetail)
    {
        this.Item = item;
        this.ItemDetail = itemDetail;

        return this;
    }

    public P79Clause2VendorParcel SetPrice(
        int quantity,
        ParameterCode unitCode,
        decimal unitPrice,
        decimal totalPrice,
        decimal totalPriceVat,
        ParameterCode? vatIncludeTypeCode)
    {
        this.Quantity = quantity;
        this.UnitCode = unitCode;
        this.UnitPrice = unitPrice;
        this.TotalPrice = totalPrice;
        this.TotalPriceVat = totalPriceVat;
        this.VatIncludeTypeCode = vatIncludeTypeCode;

        return this;
    }
}