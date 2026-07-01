namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CmContractTermination;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CmContractTerminationAcceptorConfiguration : EntityTypeConfigurationBase<CmContractTerminationAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CmContractTerminationAcceptor> builder)
    {
        builder.ToTable(nameof(CmContractTerminationAcceptor), nameof(ContractManagement));

        builder.Property(m => m.IsUnableToPerformDuties)
               .IsRequired();

        builder.HasOne(m => m.CommitteePosition)
               .WithMany()
               .HasForeignKey(m => m.CommitteePositionsCode)
               .HasPrincipalKey(m => m.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}