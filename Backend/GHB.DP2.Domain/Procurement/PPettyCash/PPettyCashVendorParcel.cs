namespace GHB.DP2.Domain.Procurement.PPettyCash;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PettyCashVendorParcelId
{
    public static PettyCashVendorParcelId New() => From(Guid.CreateVersion7());
}

public class PPettyCashVendorParcel : AuditableEntity<PettyCashVendorParcelId>
{
    public override PettyCashVendorParcelId Id { get; init; }

    public PettyCashVendorId PettyCashVendorId { get; set; }

    public int Sequence { get; set; }

    public string Item { get; set; }

    public string? ItemDetail { get; set; }

    public int Quantity { get; set; }

    public virtual ParameterCode UnitCode { get; private set; }

    public virtual SuParameter Unit { get; init; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal TotalPriceVat { get; set; }

    public virtual PPettyCashVendor PettyCashVendor { get; init; }

    public static PPettyCashVendorParcel Create(
          PettyCashVendorId pettyCashVendorId)
    {
        var PettyCashVendorParcel = new PPettyCashVendorParcel
        {
            Id = PettyCashVendorParcelId.New(),
            PettyCashVendorId = pettyCashVendorId,
        };

        return PettyCashVendorParcel;
    }

    public PPettyCashVendorParcel SetSequence(
        int sequence)
    {
        this.Sequence = sequence;

        return this;
    }

    public PPettyCashVendorParcel SetItem(
        string item,
        string? itemDetail)
    {
        this.Item = item;
        this.ItemDetail = itemDetail;

        return this;
    }

    public PPettyCashVendorParcel SetPrice(
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
}