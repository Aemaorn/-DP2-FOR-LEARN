namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAmendment.CamContractAmendment.CamContractAmendmentWaiveOrReducePenalty;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CamContractAmendmentWaiveOrReducePenaltyAssigneeConfiguration : EntityTypeConfigurationBase<CamContractAmendmentWaiveOrReducePenaltyAssignee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CamContractAmendmentWaiveOrReducePenaltyAssignee> builder)
    {
        builder.ToTable(nameof(CamContractAmendmentWaiveOrReducePenaltyAssignee), nameof(ContractAmendment));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
               .HasVogenConversion();

        builder.OwnsAuditInfo();
        builder.AssigneeInfo();
    }
}