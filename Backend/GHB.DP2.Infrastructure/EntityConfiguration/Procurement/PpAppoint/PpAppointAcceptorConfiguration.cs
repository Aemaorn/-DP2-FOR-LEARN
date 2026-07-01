namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpAppoint;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpAppoint;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpAppointAcceptorConfiguration : EntityTypeConfigurationBase<PpAppointAcceptors, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpAppointAcceptors> builder)
    {
        builder.ToTable(nameof(PpAppointAcceptors), nameof(Procurement));

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
    }
}