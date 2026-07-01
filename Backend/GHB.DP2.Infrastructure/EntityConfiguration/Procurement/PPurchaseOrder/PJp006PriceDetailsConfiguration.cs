namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPurchaseOrder;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PJp006PriceDetailsConfiguration : EntityTypeConfigurationBase<PPurchaseOrderPriceDetails, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPurchaseOrderPriceDetails> builder)
    {
        builder.ToTable(nameof(PPurchaseOrderPriceDetails), nameof(Domain.Procurement));

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasVogenConversion()
            .IsRequired();

        builder.Property(m => m.PurchaseOrderEntrepreneurId)
            .HasVogenConversion()
            .IsRequired();

        builder.Property(m => m.Sequence)
            .IsRequired();

        builder.Property(m => m.ParcelName)
            .IsRequired();

        builder.Property(m => m.ParcelQuantity)
            .IsRequired();

        builder.Property(m => m.ParcelUnitCode)
            .IsRequired();

        builder.Property(m => m.VatTypeCode);

        builder.Property(m => m.OfferedPrice)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(m => m.AgreedPrice)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(m => m.Description)
               .IsRequired();

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}