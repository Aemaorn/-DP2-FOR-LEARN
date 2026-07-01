namespace GHB.DP2.Infrastructure.EntityConfiguration.Report.RpAuditAndRevenue;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Report.RpAuditAndRevenue;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RpAuditAndRevenueDetailConfiguration : EntityTypeConfigurationBase<RpAuditAndRevenueDetail, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<RpAuditAndRevenueDetail> builder)
    {
        builder.ToTable(nameof(RpAuditAndRevenueDetail), nameof(Report));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.Property(p => p.Description)
               .IsRequired(false);

        builder.OwnsAuditInfo();
    }
}