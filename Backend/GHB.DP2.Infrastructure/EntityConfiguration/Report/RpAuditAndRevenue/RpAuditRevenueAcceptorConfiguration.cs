namespace GHB.DP2.Infrastructure.EntityConfiguration.Report.RpAuditAndRevenue;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Report.RpAuditAndRevenue;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RpAuditRevenueAcceptorConfiguration : EntityTypeConfigurationBase<RpAuditRevenueAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<RpAuditRevenueAcceptor> builder)
    {
        builder.ToTable(nameof(RpAuditRevenueAcceptor), nameof(Report));

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}