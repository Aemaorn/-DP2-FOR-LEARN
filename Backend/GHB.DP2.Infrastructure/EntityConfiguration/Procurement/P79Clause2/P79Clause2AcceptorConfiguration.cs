namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.P79Clause2;

using GHB.DP2.Domain.Procurement.P79Clause2;
using Codehard.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class P79Clause2AcceptorConfiguration : EntityTypeConfigurationBase<P79Clause2Acceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<P79Clause2Acceptor> builder)
    {
        builder.ToTable(nameof(P79Clause2Acceptor), nameof(Domain.Procurement.Procurement));

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}