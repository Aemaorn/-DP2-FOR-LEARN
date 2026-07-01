namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApproval;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPrincipleApprovalAssigneeConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalAssignee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPrincipleApprovalAssignee> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalAssignee), nameof(Procurement));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion();

        builder.HasOne(a => a.PPrincipleApproval)
               .WithMany(m => m.PrincipleApprovalAssignees)
               .OnDelete(DeleteBehavior.Cascade);

        builder.AssigneeInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}