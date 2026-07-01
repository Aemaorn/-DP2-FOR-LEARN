namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPettyCash;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPettyCash;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPettyCashVendorConfiguration : EntityTypeConfigurationBase<PPettyCashVendor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPettyCashVendor> builder)
    {
        builder.ToTable(nameof(PPettyCashVendor), nameof(Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
                .HasVogenConversion()
                .ValueGeneratedNever();

        builder.Property(p => p.PettyCashId)
               .HasConversion<PettyCashId.EfCoreValueConverter, PettyCashId.EfCoreValueComparer>()
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

        builder.Property(p => p.BillTypeOther);

        builder.Property(p => p.BillBookNo);

        builder.Property(p => p.BillDate);

        builder.Property(p => p.BillDetail);

        builder.HasMany(p => p.VendorParcels)
               .WithOne(p => p.PettyCashVendor)
               .HasForeignKey(p => p.PettyCashVendorId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.PettyCash)
               .WithMany(p => p.Vendors)
               .HasForeignKey(p => p.PettyCashId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
    }
}