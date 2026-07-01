namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAmendment.CamContractAmendment.CamContractAmendmentWaiveOrReducePenalty;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CamContractAmendmentWaiveOrReducePenaltyAcceptorConfiguration : EntityTypeConfigurationBase<CamContractAmendmentWaiveOrReducePenaltyAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CamContractAmendmentWaiveOrReducePenaltyAcceptor> builder)
    {
        builder.ToTable(nameof(CamContractAmendmentWaiveOrReducePenaltyAcceptor), nameof(ContractAmendment));

        builder.Property(m => m.IsUnableToPerformDuties)
               .IsRequired();

        builder.HasOne(m => m.CommitteePosition)
               .WithMany()
               .HasForeignKey(m => m.CommitteePositionsCode)
               .HasPrincipalKey(m => m.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsAuditInfo();
        builder.AcceptorInfo();
    }
}