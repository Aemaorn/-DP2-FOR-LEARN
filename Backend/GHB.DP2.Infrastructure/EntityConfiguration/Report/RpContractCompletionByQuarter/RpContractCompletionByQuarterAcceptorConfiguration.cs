namespace GHB.DP2.Infrastructure.EntityConfiguration.Report.RpContractCompletionByQuarter;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RpContractCompletionByQuarterAcceptorConfiguration : EntityTypeConfigurationBase<RpContractCompletionByQuarterAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<RpContractCompletionByQuarterAcceptor> builder)
    {
        builder.ToTable(nameof(RpContractCompletionByQuarterAcceptor), nameof(Report));

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}