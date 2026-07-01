namespace GHB.DP2.Infrastructure.EntityConfiguration.Report.RpAuditAndRevenue;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Report.RpAuditAndRevenue;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RpAuditAndRevenueAttachmentConfiguration : EntityTypeConfigurationBase<RpAuditAndRevenueAttachment>
{
    protected override void EntityConfigure(EntityTypeBuilder<RpAuditAndRevenueAttachment> builder)
    {
        builder.ToTable(nameof(RpAuditAndRevenueAttachment), nameof(Report));

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

        builder.OwnsAuditInfo();
    }
}