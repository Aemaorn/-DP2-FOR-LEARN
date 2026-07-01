namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PExpenseDisbursement;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PExpenseDisbursement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PExpenseDisbursementAcceptorConfiguration : EntityTypeConfigurationBase<PExpenseDisbursementAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PExpenseDisbursementAcceptor> builder)
    {
        builder.ToTable(nameof(PExpenseDisbursementAcceptor), nameof(Procurement));

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}