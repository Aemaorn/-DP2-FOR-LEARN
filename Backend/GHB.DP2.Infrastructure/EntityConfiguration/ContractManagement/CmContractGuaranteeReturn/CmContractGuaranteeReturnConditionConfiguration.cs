namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CmContractGuaranteeReturn;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using Microsoft.EntityFrameworkCore;

public class CmContractGuaranteeReturnConditionConfiguration : EntityTypeConfigurationBase<Domain.ContractManagement.CmContractGuaranteeReturn.CmContractGuaranteeReturnCondition, Dp2DbContext>
{
    protected override void EntityConfigure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<CmContractGuaranteeReturnCondition> builder)
    {
        builder.ToTable(nameof(CmContractGuaranteeReturnCondition), nameof(ContractManagement));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasVogenConversion();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.Property(p => p.Description)
               .IsRequired();

        builder.Property(p => p.IsSatisfied)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}