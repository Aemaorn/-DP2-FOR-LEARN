namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.P79Clause2;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.P79Clause2;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class P79Clause2VendorParcelConfiguration : EntityTypeConfigurationBase<P79Clause2VendorParcel, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<P79Clause2VendorParcel> builder)
    {
        builder.ToTable(nameof(P79Clause2VendorParcel), nameof(Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(p => p.P79Clause2VendorId)
               .HasConversion<P79Clause2VendorId.EfCoreValueConverter, P79Clause2VendorId.EfCoreValueComparer>()
               .IsRequired();

        builder.HasOne(p => p.VatIncludeType)
               .WithMany()
               .HasForeignKey(p => p.VatIncludeTypeCode)
               .HasPrincipalKey(p => p.Code);

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.Property(p => p.Item)
               .IsRequired();

        builder.Property(p => p.ItemDetail);

        builder.Property(p => p.Quantity)
               .IsRequired();

        builder.Property(p => p.UnitCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(p => p.UnitPrice)
               .IsRequired();

        builder.Property(p => p.TotalPrice)
               .IsRequired();

        builder.Property(p => p.TotalPriceVat)
               .IsRequired();

        builder.HasOne(p => p.P79Clause2Vendor)
               .WithMany(p => p.VendorParcels)
               .HasForeignKey(p => p.P79Clause2VendorId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Unit)
               .WithMany()
               .HasForeignKey(p => p.UnitCode)
               .HasPrincipalKey(p => p.Code);

        builder.OwnsAuditInfo();
    }
}