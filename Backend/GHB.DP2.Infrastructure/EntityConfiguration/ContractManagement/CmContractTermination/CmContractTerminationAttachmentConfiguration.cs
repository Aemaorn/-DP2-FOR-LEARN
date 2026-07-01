namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CmContractTermination;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CmContractTerminationAttachmentConfiguration : EntityTypeConfigurationBase<CmContractTerminationAttachment>
{
    protected override void EntityConfigure(EntityTypeBuilder<CmContractTerminationAttachment> builder)
    {
        builder.ToTable(nameof(CmContractTerminationAttachment), nameof(ContractManagement));

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

        builder.HasOne(a => a.DocumentTypeCodeNavigation)
               .WithMany()
               .HasForeignKey(a => a.DocumentTypeCode)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.CmContractTermination)
               .WithMany(p => p.Attachments)
               .HasForeignKey(p => p.CmContractTerminationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
    }
}