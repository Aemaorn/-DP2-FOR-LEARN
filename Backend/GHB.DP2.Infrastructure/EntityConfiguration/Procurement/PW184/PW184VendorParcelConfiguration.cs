namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.Pw184;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.Pw184;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class Pw184VendorParcelConfiguration : EntityTypeConfigurationBase<Pw184VendorParcel, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<Pw184VendorParcel> builder)
    {
        builder.ToTable(nameof(Pw184VendorParcel), nameof(Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(p => p.Pw184VendorId)
               .HasConversion<Pw184VendorId.EfCoreValueConverter, Pw184VendorId.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(p => p.Sequence).IsRequired();
        builder.Property(p => p.Item).IsRequired();
        builder.Property(p => p.ItemDetail);
        builder.Property(p => p.Quantity).IsRequired();

        builder.Property(p => p.UnitCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
               .IsRequired();

        builder.HasOne(p => p.Unit)
               .WithMany()
               .HasForeignKey(p => p.UnitCode)
               .HasPrincipalKey(p => p.Code);

        builder.Property(p => p.UnitPrice).IsRequired();
        builder.Property(p => p.TotalPrice).IsRequired();
        builder.Property(p => p.TotalPriceVat).IsRequired();

        builder.Property(p => p.VatIncludeTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.HasOne(p => p.VatIncludeType)
               .WithMany()
               .HasForeignKey(p => p.VatIncludeTypeCode)
               .HasPrincipalKey(p => p.Code);

        builder.HasOne(p => p.Pw184Vendor)
               .WithMany(p => p.VendorParcels)
               .HasForeignKey(p => p.Pw184VendorId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
    }
}
