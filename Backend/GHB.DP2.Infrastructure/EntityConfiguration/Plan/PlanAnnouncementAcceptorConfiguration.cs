namespace GHB.DP2.Infrastructure.EntityConfiguration.Plan;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Plan;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PlanAnnouncementAcceptorConfiguration : EntityTypeConfigurationBase<PlanAnnouncementAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PlanAnnouncementAcceptor> builder)
    {
        builder.ToTable(nameof(PlanAnnouncementAcceptor), nameof(Plan));

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();

        builder.Property(p => p.PlanAnnouncementId)
               .HasVogenConversion();
    }
}