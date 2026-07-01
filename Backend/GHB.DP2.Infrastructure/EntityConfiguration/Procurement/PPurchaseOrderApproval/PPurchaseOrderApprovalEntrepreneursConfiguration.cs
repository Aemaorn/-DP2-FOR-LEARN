namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPurchaseOrderApproval;

using Codehard.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;

public class PPurchaseOrderApprovalEntrepreneursConfiguration : EntityTypeConfigurationBase<PPurchaseOrderApprovalEntrepreneurs, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPurchaseOrderApprovalEntrepreneurs> builder)
    {
        builder.ToTable(nameof(PPurchaseOrderApprovalEntrepreneurs), nameof(Procurement));

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(i => i.Sequence)
               .IsRequired();

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}
