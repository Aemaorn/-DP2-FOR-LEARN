namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpMedianPrice;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpMedianPriceBudgetAllocationsConfiguration : EntityTypeConfigurationBase<PpMedianPriceBudgetAllocations, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpMedianPriceBudgetAllocations> builder)
    {
        builder.ToTable(nameof(PpMedianPriceBudgetAllocations), nameof(Procurement));

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(m => m.ReferenceDate);

        builder.Property(m => m.Budget)
               .IsRequired();

        builder.Property(m => m.ReferenceMedianPrice)
               .IsRequired();

        builder.HasMany(m => m.Details)
               .WithOne(d => d.BudgetAllocations)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}