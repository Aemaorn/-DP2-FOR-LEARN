namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpTorDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpTorPaymentTermPeriodConfiguration : EntityTypeConfigurationBase<PpTorPaymentTermPeriod, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpTorPaymentTermPeriod> builder)
    {
        builder.ToTable(nameof(PpTorPaymentTermPeriod), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasVogenConversion();

        builder.Property(p => p.Sequence);

        builder.Property(p => p.Description);

        builder.Property(p => p.Quantity);

        builder.Property(p => p.PeriodTypeCode);

        builder.Property(p => p.TotalQuantity);

        builder.Property(p => p.TotalPeriodTypeCode);

        builder.HasOne(p => p.PpTorDraft)
            .WithMany(t => t.PpTorPaymentTermPeriods)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.PeriodType)
               .WithMany()
               .HasForeignKey(p => p.PeriodTypeCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired(false);

        builder.HasOne(p => p.TotalPeriodType)
               .WithMany()
               .HasForeignKey(p => p.TotalPeriodTypeCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired(false);

        builder.OwnsAuditInfo();
    }
}