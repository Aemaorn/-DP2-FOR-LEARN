namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpPurchaseRequisition;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpPurchaseRequisitionTechnicalSpecificationConfiguration : EntityTypeConfigurationBase<PpPurchaseRequisitionTechnicalSpecifications>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpPurchaseRequisitionTechnicalSpecifications> builder)
    {
        builder.ToTable(nameof(PpPurchaseRequisitionTechnicalSpecifications), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.Property(p => p.Name)
               .IsRequired();

        builder.Property(p => p.Description)
               .IsRequired();

        builder.Property(p => p.Quantity)
               .IsRequired();

        builder.HasOne(p => p.PurchaseRequisition)
               .WithMany(p => p.TechnicalSpecifications)
               .HasForeignKey(p => p.PpPurchaseRequisitionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.UnitCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.HasOne(p => p.Unit)
               .WithMany()
               .HasForeignKey(p => p.UnitCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired(false);

        builder.OwnsAuditInfo();
    }
}