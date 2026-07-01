namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPurchaseOrderApproval;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPurchaseOrderApprovalAssigneeConfiguration : EntityTypeConfigurationBase<PPurchaseOrderApprovalAssignee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPurchaseOrderApprovalAssignee> builder)
    {
        builder.ToTable(nameof(PPurchaseOrderApprovalAssignee), nameof(Procurement));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion();

        builder.AssigneeInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}