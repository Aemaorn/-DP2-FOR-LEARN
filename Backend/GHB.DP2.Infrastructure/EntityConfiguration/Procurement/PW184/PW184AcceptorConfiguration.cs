namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.Pw184;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.Pw184;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class Pw184AcceptorConfiguration : EntityTypeConfigurationBase<Pw184Acceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<Pw184Acceptor> builder)
    {
        builder.ToTable(nameof(Pw184Acceptor), nameof(Domain.Procurement.Procurement));

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}
