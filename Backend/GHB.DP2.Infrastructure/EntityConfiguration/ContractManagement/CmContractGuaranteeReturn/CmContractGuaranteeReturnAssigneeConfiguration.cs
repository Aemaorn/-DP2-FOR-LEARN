namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CmContractGuaranteeReturn;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CmContractGuaranteeReturnAssigneeConfiguration : EntityTypeConfigurationBase<CmContractGuaranteeReturnAssignee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CmContractGuaranteeReturnAssignee> builder)
    {
        builder.ToTable(nameof(CmContractGuaranteeReturnAssignee), nameof(ContractManagement));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion();

        builder.AssigneeInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}