namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpMedianPrice;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpMedianPriceExpenseDescriptionConfiguration : EntityTypeConfigurationBase<PpMedianPriceExpenseDescription, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpMedianPriceExpenseDescription> builder)
    {
        builder.ToTable(nameof(PpMedianPriceExpenseDescription), nameof(Procurement));

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
               .HasVogenConversion()
               .IsRequired();
    }
}