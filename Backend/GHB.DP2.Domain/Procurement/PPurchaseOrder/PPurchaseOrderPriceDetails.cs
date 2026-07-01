namespace GHB.DP2.Domain.Procurement.PPurchaseOrder;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPurchaseOrderPriceDetailsId
{
    public static PPurchaseOrderPriceDetailsId New() => From(Guid.CreateVersion7());
}

public partial class PPurchaseOrderPriceDetails : AuditableEntity<PPurchaseOrderPriceDetailsId>, IHasSoftDelete
{
    public override PPurchaseOrderPriceDetailsId Id { get; init; }

    public PurchaseOrderEntrepreneurId PurchaseOrderEntrepreneurId { get; init; }

    public int Sequence { get; private set; }

    public string ParcelName { get; private set; }

    public int ParcelQuantity { get; private set; }

    public string ParcelUnitCode { get; private set; }

    public string? VatTypeCode { get; private set; }

    public decimal OfferedPrice { get; private set; }

    public decimal AgreedPrice { get; private set; }

    public string Description { get; private set; }

    public virtual PPurchaseOrderEntrepreneur PPurchaseOrderEntrepreneur { get; init; }

    public static PPurchaseOrderPriceDetails Create(PurchaseOrderEntrepreneurId purchaseOrderEntrepreneurId)
    {
        return new PPurchaseOrderPriceDetails
        {
            Id = PPurchaseOrderPriceDetailsId.New(),
            PurchaseOrderEntrepreneurId = purchaseOrderEntrepreneurId,
        };
    }

    public static PPurchaseOrderPriceDetails Create(
        PPurchaseOrderPriceDetailsId id,
        PurchaseOrderEntrepreneurId purchaseOrderEntrepreneurId)
    {
        return new PPurchaseOrderPriceDetails
        {
            Id = id,
            PurchaseOrderEntrepreneurId = purchaseOrderEntrepreneurId,
        };
    }

    public record PriceDetailsInfo(
        int Sequence,
        string ParcelName,
        int ParcelQuantity,
        string ParcelUnitCode,
        string? VatTypeCode,
        decimal OfferedPrice,
        decimal AgreedPrice,
        string Description);

    public void SetDetails(PriceDetailsInfo detailsInfo)
    {
        this.Sequence = detailsInfo.Sequence;
        this.ParcelName = detailsInfo.ParcelName;
        this.ParcelQuantity = detailsInfo.ParcelQuantity;
        this.ParcelUnitCode = detailsInfo.ParcelUnitCode;
        this.VatTypeCode = detailsInfo.VatTypeCode;
        this.OfferedPrice = detailsInfo.OfferedPrice;
        this.AgreedPrice = detailsInfo.AgreedPrice;
        this.Description = detailsInfo.Description;
    }
}