namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPettyCash;

using GHB.DP2.Domain.Procurement.PPettyCash;
using Codehard.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPettyCashAcceptorConfiguration : EntityTypeConfigurationBase<PPettyCashAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPettyCashAcceptor> builder)
    {
        builder.ToTable(nameof(PPettyCashAcceptor), nameof(Domain.Procurement.Procurement));

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}