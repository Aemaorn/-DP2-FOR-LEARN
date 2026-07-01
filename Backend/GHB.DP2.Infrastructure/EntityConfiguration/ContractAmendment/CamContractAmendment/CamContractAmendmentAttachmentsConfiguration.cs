namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAmendment.CamContractAmendment;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CamContractAmendmentAttachmentsConfiguration : EntityTypeConfigurationBase<CamContractAmendmentAttachment, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CamContractAmendmentAttachment> builder)
    {
        builder.ToTable(nameof(CamContractAmendmentAttachment), nameof(ContractAmendment));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasVogenConversion();

        builder.Property(e => e.DocumentTypeCode)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(e => e.FileId)
               .IsRequired();

        builder.Property(e => e.FileName)
               .IsRequired();

        builder.Property(e => e.IsPublic)
               .IsRequired();

        builder.Property(e => e.Sequence)
               .IsRequired();

        builder.HasOne(e => e.DocumentType)
               .WithMany()
               .HasForeignKey(e => e.DocumentTypeCode)
               .HasPrincipalKey(e => e.Code);

        builder.OwnsAuditInfo();
    }
}