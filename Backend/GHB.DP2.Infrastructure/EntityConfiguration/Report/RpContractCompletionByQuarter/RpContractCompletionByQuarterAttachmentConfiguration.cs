namespace GHB.DP2.Infrastructure.EntityConfiguration.Report.RpContractCompletionByQuarter;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RpContractCompletionByQuarterAttachmentConfiguration : EntityTypeConfigurationBase<RpContractCompletionByQuarterAttachment>
{
    protected override void EntityConfigure(EntityTypeBuilder<RpContractCompletionByQuarterAttachment> builder)
    {
        builder.ToTable(nameof(RpContractCompletionByQuarterAttachment), nameof(Report));

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