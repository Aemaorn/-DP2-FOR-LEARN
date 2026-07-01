namespace GHB.DP2.Infrastructure.EntityConfiguration.Plan;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Plan;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PlanAssigneeConfiguration : EntityTypeConfigurationBase<PlanAssignee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PlanAssignee> builder)
    {
        builder.ToTable(nameof(PlanAssignee), nameof(Plan));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.AssigneeInfo();
        builder.OwnsAuditInfo();

        builder.Property(p => p.PlanId)
               .HasVogenConversion();
    }
}