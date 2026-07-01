namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpMedianPrice;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpMedianPriceStaffConfiguration : EntityTypeConfigurationBase<PpMedianPriceStaff, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpMedianPriceStaff> builder)
    {
        builder.ToTable(nameof(PpMedianPriceStaff), nameof(Procurement));

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(m => m.PersonnelCompensation)
               .IsRequired();

        builder.Property(m => m.PersonnelCount)
               .IsRequired();

        builder.HasMany(m => m.Details)
               .WithOne(d => d.MedianPriceStaff)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}