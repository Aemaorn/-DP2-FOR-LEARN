namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPettyCashReimbursement;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPettyCashReimbursement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPettyCashReimbursementAcceptorConfiguration : EntityTypeConfigurationBase<PPettyCashReimbursementAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPettyCashReimbursementAcceptor> builder)
    {
        builder.ToTable(nameof(PPettyCashReimbursementAcceptor), nameof(Procurement));

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}