namespace GHB.DP2.Infrastructure.EntityConfiguration.Plan;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Plan;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PlanAnnouncementAssigneeConfiguration : EntityTypeConfigurationBase<PlanAnnouncementAssignee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PlanAnnouncementAssignee> builder)
    {
        builder.ToTable(nameof(PlanAnnouncementAssignee), nameof(Plan));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.AssigneeInfo();
        builder.OwnsAuditInfo();

        builder.Property(p => p.PlanAnnouncementId)
               .HasVogenConversion();
    }
}