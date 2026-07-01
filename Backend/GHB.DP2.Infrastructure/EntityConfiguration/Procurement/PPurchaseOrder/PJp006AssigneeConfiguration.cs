namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPurchaseOrder;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PJp006AssigneeConfiguration : EntityTypeConfigurationBase<PPurchaseOrderAssignee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPurchaseOrderAssignee> builder)
    {
        builder.ToTable(nameof(PPurchaseOrderAssignee), nameof(Procurement));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasVogenConversion();

        builder.AssigneeInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}