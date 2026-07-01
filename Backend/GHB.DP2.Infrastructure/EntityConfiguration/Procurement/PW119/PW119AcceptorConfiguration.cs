namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.Pw119;

using GHB.DP2.Domain.Procurement.Pw119;
using Codehard.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class Pw119AcceptorConfiguration : EntityTypeConfigurationBase<Pw119Acceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<Pw119Acceptor> builder)
    {
        builder.ToTable(nameof(Pw119Acceptor), nameof(Domain.Procurement.Procurement));

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}