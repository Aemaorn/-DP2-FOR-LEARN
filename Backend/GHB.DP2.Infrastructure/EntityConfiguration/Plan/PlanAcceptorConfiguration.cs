namespace GHB.DP2.Infrastructure.EntityConfiguration.Plan;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Plan;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PlanAcceptorConfiguration : EntityTypeConfigurationBase<PlanAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PlanAcceptor> builder)
    {
        builder.ToTable(nameof(PlanAcceptor), nameof(Plan));

        builder.HasKey(p => p.Id);

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
    }
}