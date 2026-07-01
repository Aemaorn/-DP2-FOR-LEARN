namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpMedianPrice;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PpMedianPriceConfiguration : EntityTypeConfigurationBase<PpMedianPrice, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpMedianPrice> builder)
    {
        builder.ToTable(nameof(PpMedianPrice), nameof(Procurement));

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(m => m.ReferenceId)
               .HasConversion<MedianPriceId.EfCoreValueConverter, MedianPriceId.EfCoreValueComparer>();

        builder.Property(m => m.ProcurementId)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(m => m.ReferenceNumber)
               .HasVogenConversion()
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(m => m.Object)
               .IsRequired();

        builder.Property(m => m.Reason)
               .IsRequired();

        builder.Property(m => m.SpecialDescription)
               .IsRequired();

        builder.Property(m => m.JobDescription);

        builder.Property(m => m.PriceReasonablenessInfo)
               .IsRequired();

        builder.Property(m => m.Status)
               .HasConversion(new EnumToStringConverter<MedianPriceStatus>())
               .IsRequired();

        builder.Property(m => m.IsChange)
               .IsRequired();

        builder.Property(m => m.IsCancel)
               .IsRequired();

        builder.Property(m => m.IsActive)
               .IsRequired();

        builder.Property(m => m.IsMigration)
               .HasDefaultValue(false);

        builder.Property(m => m.CancelReason);

        builder.Property(m => m.ChangeReason);

        builder.Property(m => m.Telephone);

        builder.Property(m => m.DocumentDate);

        builder.HasOne(m => m.DocumentTemplate)
               .WithMany()
               .HasForeignKey(m => m.DocumentTemplateId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.BudgetAllocations)
               .WithOne(b => b.MedianPrice)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Staff)
               .WithOne(s => s.MedianPrice)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Acceptors)
               .WithOne(a => a.MedianPrice)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.ExpenseDescription)
               .WithOne(e => e.MedianPrice)
               .HasForeignKey<PpMedianPriceExpenseDescription>(e => e.Id)
               .OnDelete(DeleteBehavior.Cascade)
               .IsRequired(false);

        builder.HasOne(m => m.Procurement)
               .WithMany(p => p.MedianPrices)
               .HasForeignKey(m => m.ProcurementId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(PpMedianPriceDocumentHistory), nameof(Procurement));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<MedianPriceStatus>());
        });

        builder.HasActivityInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}