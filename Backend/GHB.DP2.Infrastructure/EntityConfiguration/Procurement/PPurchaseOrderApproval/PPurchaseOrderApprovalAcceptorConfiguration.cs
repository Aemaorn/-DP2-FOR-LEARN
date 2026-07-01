namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPurchaseOrderApproval;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPurchaseOrderApprovalAcceptorConfiguration : EntityTypeConfigurationBase<PPurchaseOrderApprovalAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPurchaseOrderApprovalAcceptor> builder)
    {
        builder.ToTable(nameof(PPurchaseOrderApprovalAcceptor), nameof(Procurement));

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}