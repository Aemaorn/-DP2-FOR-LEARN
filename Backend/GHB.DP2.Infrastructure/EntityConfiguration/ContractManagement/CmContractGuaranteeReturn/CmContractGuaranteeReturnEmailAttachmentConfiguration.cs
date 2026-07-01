namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CmContractGuaranteeReturn;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CmContractGuranteeReturnEmailAttachmentConfiguration : EntityTypeConfigurationBase<CmContractGuaranteeReturnEmailAttachments>
{
    protected override void EntityConfigure(EntityTypeBuilder<CmContractGuaranteeReturnEmailAttachments> builder)
    {
        builder.ToTable(nameof(CmContractGuaranteeReturnEmailAttachments), nameof(Domain.ContractManagement.CmContractGuaranteeReturn.CmContractGuaranteeReturn));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(a => a.FileId)
               .IsRequired();

        builder.Property(a => a.FileName)
               .IsRequired();

        builder.Property(a => a.Sequence)
               .HasDefaultValue(1)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}