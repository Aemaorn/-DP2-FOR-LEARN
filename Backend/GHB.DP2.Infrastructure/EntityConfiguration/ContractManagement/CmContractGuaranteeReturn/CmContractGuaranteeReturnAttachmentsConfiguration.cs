namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CmContractGuaranteeReturn;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CmContractGuaranteeReturnAttachmentsConfiguration : EntityTypeConfigurationBase<CmContractGuaranteeReturnAttachments>
{
    protected override void EntityConfigure(EntityTypeBuilder<CmContractGuaranteeReturnAttachments> builder)
    {
        builder.ToTable(nameof(CmContractGuaranteeReturnAttachments), nameof(ContractManagement));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(a => a.FileId)
               .IsRequired();

        builder.Property(a => a.FileName)
               .IsRequired();

        builder.Property(a => a.IsPublic)
               .IsRequired();

        builder.Property(a => a.Sequence)
               .HasDefaultValue(1)
               .IsRequired();

        builder.HasOne(a => a.DocumentType)
               .WithMany()
               .HasForeignKey(a => a.DocumentTypeCode)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.CmContractGuaranteeReturn)
               .WithMany(p => p.Attachments);

        builder.OwnsAuditInfo();
    }
}