namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CmContractTermination;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CmContractTerminationAssigneeConfiguration : EntityTypeConfigurationBase<CmContractTerminationAssignee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CmContractTerminationAssignee> builder)
    {
        builder.ToTable(nameof(CmContractTerminationAssignee), nameof(ContractManagement));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion();

        builder.AssigneeInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}