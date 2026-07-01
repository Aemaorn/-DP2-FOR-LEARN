namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class SuVendorCheckHistoryConfiguration : EntityTypeConfigurationBase<SuVendorCheckHistory, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuVendorCheckHistory> builder)
    {
        builder.ToTable(nameof(SuVendorCheckHistory), nameof(SystemUtility));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasVogenConversion();

        builder.Property(e => e.CheckType)
               .HasConversion(new EnumToStringConverter<CheckType>())
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(e => e.Result)
               .IsRequired();

        builder.Property(e => e.Remark);

        builder.HasOne(e => e.Vendor)
               .WithMany(e => e.VendorCheck)
               .HasForeignKey(e => e.VendorId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
    }
}