namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.Pw184;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.Pw184;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class Pw184VendorConfiguration : EntityTypeConfigurationBase<Pw184Vendor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<Pw184Vendor> builder)
    {
        builder.ToTable(nameof(Pw184Vendor), nameof(Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(p => p.Pw184Id)
               .HasConversion<Pw184Id.EfCoreValueConverter, Pw184Id.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(p => p.VendorType)
               .IsRequired();

        builder.Property(p => p.SuVendorId)
               .HasConversion<SuVendorId.EfCoreValueConverter, SuVendorId.EfCoreValueComparer>();

        builder.Property(p => p.VendorName)
               .IsRequired();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.Property(p => p.TaxNumber);
        builder.Property(p => p.VendorBranchNumber);

        builder.Property(p => p.VatIncludeTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.HasOne(p => p.VatIncludeType)
               .WithMany()
               .HasForeignKey(p => p.VatIncludeTypeCode)
               .HasPrincipalKey(p => p.Code);

        builder.Property(p => p.BillTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
               .IsRequired();

        builder.HasOne(p => p.BillType)
               .WithMany()
               .HasForeignKey(p => p.BillTypeCode)
               .HasPrincipalKey(p => p.Code);

        builder.Property(p => p.BillBookNo);
        builder.Property(p => p.BillTypeOther);
        builder.Property(p => p.BillDate);
        builder.Property(p => p.BillDetail);

        builder.HasMany(p => p.VendorParcels)
               .WithOne(p => p.Pw184Vendor)
               .HasForeignKey(p => p.Pw184VendorId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Pw184)
               .WithMany(p => p.Vendors)
               .HasForeignKey(p => p.Pw184Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
    }
}
