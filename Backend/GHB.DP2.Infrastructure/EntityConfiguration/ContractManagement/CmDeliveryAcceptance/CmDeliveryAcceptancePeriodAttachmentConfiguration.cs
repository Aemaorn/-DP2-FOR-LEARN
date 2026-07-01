namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CmDeliveryAcceptance;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CmDeliveryAcceptancePeriodAttachmentConfiguration : EntityTypeConfigurationBase<CmDeliveryAcceptancePeriodAttachment>
{
    protected override void EntityConfigure(EntityTypeBuilder<CmDeliveryAcceptancePeriodAttachment> builder)
    {
        builder.ToTable(nameof(CmDeliveryAcceptancePeriodAttachment), nameof(ContractManagement));

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

        builder.HasOne(p => p.CmDeliveryAcceptancePeriod)
               .WithMany(p => p.Attachments)
               .HasForeignKey(p => p.CmDeliveryAcceptancePeriodId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
    }
}
