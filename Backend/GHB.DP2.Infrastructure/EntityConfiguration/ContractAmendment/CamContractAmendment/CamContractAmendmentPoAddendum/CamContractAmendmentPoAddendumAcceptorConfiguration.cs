namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAmendment.CamContractAmendment.CamContractAmendmentPoAddendum;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoAddendum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CamContractAmendmentPoAddendumAcceptorConfiguration : EntityTypeConfigurationBase<CamContractAmendmentPoAddendumAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CamContractAmendmentPoAddendumAcceptor> builder)
    {
        builder.ToTable(nameof(CamContractAmendmentPoAddendumAcceptor), nameof(Domain.ContractAmendment.ContractAmendment));

        builder.Property(m => m.IsUnableToPerformDuties)
               .IsRequired();

        builder.HasOne(m => m.CommitteePosition)
               .WithMany()
               .HasForeignKey(m => m.CommitteePositionsCode)
               .HasPrincipalKey(m => m.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.AcceptorInfo();
        builder.OwnsAuditInfo();
    }
}