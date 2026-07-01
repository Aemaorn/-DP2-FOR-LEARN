namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpPurchaseRequisition;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpPurchaseRequisitionAcceptorsConfiguration : EntityTypeConfigurationBase<Domain.Procurement.PpPurchaseRequisition.PpPurchaseRequisitionAcceptors, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpPurchaseRequisitionAcceptors> builder)
    {
        builder.ToTable(nameof(PpPurchaseRequisitionAcceptors), nameof(Procurement));

        builder.Property(m => m.PpPurchaseRequisitionId)
               .HasVogenConversion()
               .IsRequired();

        builder.HasOne(m => m.PpPurchaseRequisition)
               .WithMany(m => m.Acceptors)
               .HasForeignKey(m => m.PpPurchaseRequisitionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}