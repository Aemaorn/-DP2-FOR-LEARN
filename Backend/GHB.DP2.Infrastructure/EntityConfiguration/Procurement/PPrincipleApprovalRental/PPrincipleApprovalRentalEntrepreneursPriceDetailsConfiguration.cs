namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApprovalRental;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPrincipleApprovalRentalEntrepreneursPriceDetailsConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalRentalEntrepreneursPriceDetails, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPrincipleApprovalRentalEntrepreneursPriceDetails> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalRentalEntrepreneursPriceDetails), nameof(Domain.Procurement));

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(m => m.Sequence)
               .IsRequired();

        builder.Property(m => m.ParcelName)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(m => m.ParcelQuantity)
               .IsRequired();

        builder.Property(m => m.ParcelUnitCode)
               .IsRequired();

        builder.Property(m => m.VatTypeCode)
               .IsRequired();

        builder.Property(m => m.OfferedPrice)
               .HasPrecision(18, 2)
               .IsRequired();

        builder.Property(m => m.AgreedPrice)
               .HasPrecision(18, 2)
               .IsRequired();

        builder.Property(m => m.Description)
               .IsRequired();

        builder.HasOne(p => p.ParcelUnit)
               .WithMany()
               .HasForeignKey(p => p.ParcelUnitCode)
               .HasPrincipalKey(p => p.Code);

        builder.HasOne(p => p.VatType)
               .WithMany()
               .HasForeignKey(p => p.VatTypeCode)
               .HasPrincipalKey(p => p.Code);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}