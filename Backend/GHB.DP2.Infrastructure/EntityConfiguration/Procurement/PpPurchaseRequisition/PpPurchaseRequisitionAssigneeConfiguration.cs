namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpPurchaseRequisition;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PlanAssigneeConfiguration : EntityTypeConfigurationBase<PpPurchaseRequisitionAssignee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpPurchaseRequisitionAssignee> builder)
    {
        builder.ToTable(nameof(PpPurchaseRequisitionAssignee), nameof(Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PpPurchaseRequisitionId)
               .HasVogenConversion();

        builder.AssigneeInfo();
        builder.OwnsAuditInfo();
    }
}