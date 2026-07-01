namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPurchaseOrder;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class Pjp006Configuration : EntityTypeConfigurationBase<Domain.Procurement.PPurchaseOrder.PPurchaseOrder, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<Domain.Procurement.PPurchaseOrder.PPurchaseOrder> builder)
    {
        builder.ToTable(nameof(Domain.Procurement.PPurchaseOrder.PPurchaseOrder), nameof(Domain.Procurement));

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(m => m.PurchaseOrderNumber)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(m => m.ProcurementId)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(m => m.Status)
               .HasConversion(new EnumToStringConverter<PurchaseOrderStatus>())
               .IsRequired();

        builder.Property(m => m.IsMigration)
               .HasDefaultValue(false);

        builder.Property(m => m.DocumentDate);

        builder.HasMany(m => m.Entrepreneurs)
               .WithOne(e => e.PPurchaseOrder)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Procurement)
               .WithMany(p => p.PurchaseOrder)
               .HasForeignKey(m => m.ProcurementId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Acceptors)
               .WithOne(a => a.PPurchaseOrder)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Assignees)
               .WithOne(a => a.PPurchaseOrder)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(PurchaseOrderDocumentHistory), nameof(Procurement));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<PurchaseOrderDocumentType>());

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<PurchaseOrderStatus>());
        });

        builder.HasActivityInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}