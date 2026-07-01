namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpPurchaseRequisition;

using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpPurchaseRequisitionWarrantyConfiguration : IEntityTypeConfiguration<PpPurchaseRequisitionWarranty>
{
    public void Configure(EntityTypeBuilder<PpPurchaseRequisitionWarranty> builder)
    {
        builder.ToTable(nameof(PpPurchaseRequisitionWarranty), nameof(Procurement));

        builder.HasKey(x => x.Id);
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PpPurchaseRequisitionId)
               .HasConversion<PpPurchaseRequisitionId.EfCoreValueConverter, PpPurchaseRequisitionId.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(p => p.HasWarranty)
               .IsRequired();

        builder.Property(p => p.Period)
               .IsRequired();

        builder.Property(p => p.ConditionOther);

        builder.Property(p => p.PeriodTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
               .IsRequired();

        builder.HasOne(p => p.PeriodType)
               .WithMany()
               .HasForeignKey(p => p.PeriodTypeCode)
               .HasPrincipalKey(x => x.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsAuditInfo();
    }
}