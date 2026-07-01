namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPettyCash;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPettyCash;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPettyCashVendorParcelConfiguration : EntityTypeConfigurationBase<PPettyCashVendorParcel, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPettyCashVendorParcel> builder)
    {
        builder.ToTable(nameof(PPettyCashVendorParcel), nameof(Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(p => p.PettyCashVendorId)
               .HasConversion<PettyCashVendorId.EfCoreValueConverter, PettyCashVendorId.EfCoreValueComparer>()
               .IsRequired();

        builder.HasOne(p => p.PettyCashVendor)
               .WithMany(p => p.VendorParcels)
               .HasForeignKey(p => p.PettyCashVendorId)
               .OnDelete(DeleteBehavior.Cascade);

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

        builder.HasOne(p => p.Unit)
               .WithMany()
               .HasForeignKey(p => p.UnitCode)
               .HasPrincipalKey(p => p.Code);

        builder.Property(p => p.UnitPrice)
               .IsRequired();

        builder.Property(p => p.TotalPrice)
               .IsRequired();

        builder.Property(p => p.TotalPriceVat)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}