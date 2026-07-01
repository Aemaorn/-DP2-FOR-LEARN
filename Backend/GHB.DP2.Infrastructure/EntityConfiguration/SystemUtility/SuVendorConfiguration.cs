namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class SuVendorConfiguration : EntityTypeConfigurationBase<SuVendor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuVendor> builder)
    {
        builder.ToTable(nameof(SuVendor), nameof(SystemUtility));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasVogenConversion();

        builder.Property(e => e.Nationality)
               .HasConversion(new EnumToStringConverter<SuVendorNationality>())
               .IsRequired();

        builder.Property(e => e.Type)
               .HasConversion(new EnumToStringConverter<SuVendorType>())
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(e => e.EntrepreneurType)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(e => e.TaxpayerIdentificationNo)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(e => e.EstablishmentName)
               .HasMaxLength(1000)
               .IsRequired();

        builder.Property(e => e.PlaceName)
               .IsRequired();

        builder.Property(e => e.HouseNumber)
               .HasMaxLength(100)
               .IsRequired(false);

        builder.Property(e => e.RoomNumber)
               .HasMaxLength(100)
               .IsRequired(false);

        builder.Property(e => e.Floor)
               .HasMaxLength(50)
               .IsRequired(false);

        builder.Property(e => e.VillageName)
               .HasMaxLength(1000)
               .IsRequired(false);

        builder.Property(e => e.Moo)
               .HasMaxLength(1000)
               .IsRequired(false);

        builder.Property(e => e.Allay)
               .HasMaxLength(1000)
               .IsRequired(false);

        builder.Property(e => e.Road)
               .HasMaxLength(1000)
               .IsRequired(false);

        builder.Property(e => e.RawProvinceCode)
               .HasMaxLength(50)
               .IsRequired(false);

        builder.Property(e => e.RawDistrictCode)
               .HasMaxLength(50)
               .IsRequired(false);

        builder.Property(e => e.RawSubDistrictCode)
               .HasMaxLength(50)
               .IsRequired(false);

        builder.Property(e => e.PostalCode)
               .HasMaxLength(50)
               .IsRequired(false);

        builder.Property(e => e.Tel)
               .HasMaxLength(100)
               .IsRequired(false);

        builder.Property(e => e.Fax)
               .HasMaxLength(100)
               .IsRequired(false);

        builder.Property(e => e.SapVendorNumber)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(e => e.SapBranchNumber)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(e => e.Email)
               .HasMaxLength(1000)
               .IsRequired();

        builder.HasOne(o => o.EntrepreneurTypeInfo)
               .WithMany()
               .HasForeignKey(o => o.EntrepreneurType)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}