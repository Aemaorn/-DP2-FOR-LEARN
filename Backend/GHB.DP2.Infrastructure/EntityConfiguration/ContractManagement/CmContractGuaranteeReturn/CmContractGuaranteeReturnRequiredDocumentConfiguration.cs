namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CmContractGuaranteeReturn;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using Microsoft.EntityFrameworkCore;

public class CmContractGuaranteeReturnRequiredDocumentConfiguration : EntityTypeConfigurationBase<Domain.ContractManagement.CmContractGuaranteeReturn.CmContractGuaranteeReturnRequiredDocument, Dp2DbContext>
{
    protected override void EntityConfigure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<CmContractGuaranteeReturnRequiredDocument> builder)
    {
        builder.ToTable(nameof(CmContractGuaranteeReturnRequiredDocument), nameof(ContractManagement));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasVogenConversion();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.Property(p => p.DocumentName)
               .HasMaxLength(2000)
               .IsRequired();

        builder.Property(p => p.IsSubmitted)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}