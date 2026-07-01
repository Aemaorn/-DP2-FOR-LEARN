namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpMedianPrice;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PpMedianPriceStaffDetailConfiguration : EntityTypeConfigurationBase<PpMedianPriceStaffDetail, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpMedianPriceStaffDetail> builder)
    {
        builder.ToTable(nameof(PpMedianPriceStaffDetail), nameof(Procurement));

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(m => m.Type)
               .HasConversion(new EnumToStringConverter<MedianPriceStaffType>())
               .IsRequired();

        builder.Property(m => m.Sequence)
               .IsRequired();

        builder.Property(m => m.Description)
               .IsRequired();

        builder.HasDiscriminator(s => s.Type)
               .HasValue<PpMedianPriceStaffPersonal>(MedianPriceStaffType.Personal)
               .HasValue<PpMedianPriceStaffConsultantTypes>(MedianPriceStaffType.ConsultantTypes)
               .HasValue<PpMedianPriceStaffConsultantQualifications>(MedianPriceStaffType.ConsultantQualifications);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}