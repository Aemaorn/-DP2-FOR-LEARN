namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAmendment.CamContractAmendment.CamContractAmendmentExtendChange;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CamContractAmendmentExtendChangeAssigneeConfiguration : EntityTypeConfigurationBase<CamContractAmendmentExtendChangeAssignee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CamContractAmendmentExtendChangeAssignee> builder)
    {
        builder.ToTable(nameof(CamContractAmendmentExtendChangeAssigneeConfiguration), nameof(ContractAmendment));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
               .HasVogenConversion();

        builder.OwnsAuditInfo();
        builder.AssigneeInfo();
    }
}