namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpMedianPrice;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PpMedianPriceBudgetAllocationsDetailConfiguration : EntityTypeConfigurationBase<PpMedianPriceBudgetAllocationsDetail, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpMedianPriceBudgetAllocationsDetail> builder)
    {
        builder.ToTable(nameof(PpMedianPriceBudgetAllocationsDetail), nameof(Procurement));

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(m => m.Type)
               .HasConversion(new EnumToStringConverter<BudgetAllocationsDetailType>())
               .IsRequired();

        builder.Property(m => m.Sequence)
               .IsRequired();

        builder.Property(m => m.Source)
               .IsRequired();

        builder.HasDiscriminator(m => m.Type)
               .HasValue<PpMedianPriceBudgetAllocationsWithDetail>(BudgetAllocationsDetailType.With)
               .HasValue<PpMedianPriceBudgetAllocationsWithoutDetail>(BudgetAllocationsDetailType.Without);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}