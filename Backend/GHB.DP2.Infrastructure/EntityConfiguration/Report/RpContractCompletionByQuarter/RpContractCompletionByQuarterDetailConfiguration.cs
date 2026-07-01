namespace GHB.DP2.Infrastructure.EntityConfiguration.Report.RpContractCompletionByQuarter;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RpContractCompletionByQuarterDetailConfiguration : EntityTypeConfigurationBase<RpContractCompletionByQuarterDetail, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<RpContractCompletionByQuarterDetail> builder)
    {
        builder.ToTable(nameof(RpContractCompletionByQuarterDetail), nameof(Report));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.Property(p => p.Description);

        builder.OwnsAuditInfo();
    }
}