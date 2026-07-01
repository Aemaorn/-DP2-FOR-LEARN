namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPurchaseOrder;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PJp006EntrepreneurConfiguration : EntityTypeConfigurationBase<PPurchaseOrderEntrepreneur, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPurchaseOrderEntrepreneur> builder)
    {
        builder.ToTable(nameof(PPurchaseOrderEntrepreneur), nameof(Domain.Procurement));

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(m => m.PurchaseOrderId)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(m => m.SuVendorId)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(m => m.EmailSended)
               .IsRequired();

        builder.Property(m => m.Sequence)
               .IsRequired();

        builder.Property(m => m.CoiResult)
               .IsRequired(false);

        builder.Property(m => m.CoiRemark)
               .IsRequired(false);

        builder.Property(m => m.CoiDate)
               .IsRequired(false);

        builder.Property(m => m.WatchlistResult)
               .IsRequired(false);

        builder.Property(m => m.WatchlistRemark)
               .IsRequired(false);

        builder.Property(m => m.WatchlistDate)
               .IsRequired(false);

        builder.Property(m => m.EgpResult)
               .IsRequired(false);

        builder.Property(m => m.EgpRemark)
               .IsRequired(false);

        builder.Property(m => m.EgpDate)
               .IsRequired(false);

        builder.Property(m => m.IsWinner)
               .IsRequired();

        builder.Property(m => m.SelectionReasonCode)
               .HasMaxLength(50)
               .IsRequired(false);

        builder.Property(m => m.Remark)
               .IsRequired(false);

        builder.HasMany(m => m.PJp006PriceDetails)
               .WithOne(pd => pd.PPurchaseOrderEntrepreneur)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.PurchaseOrderShareholders)
               .WithOne(pd => pd.PurchaseOrderEntrepreneur)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.PurchaseOrderEntrepreneurChecker)
               .WithOne(c => c.PurchaseOrderEntrepreneur)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Attachments)
               .WithOne()
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}