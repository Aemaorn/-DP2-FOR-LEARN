namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PExpenseDisbursement;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PExpenseDisbursement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PExpenseDisbursementAttachmentConfiguration : EntityTypeConfigurationBase<PExpenseDisbursementAttachment>
{
    protected override void EntityConfigure(EntityTypeBuilder<PExpenseDisbursementAttachment> builder)
    {
        builder.ToTable(nameof(PExpenseDisbursementAttachment), nameof(Procurement));

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

        builder.Property(a => a.IsExpenseAttachment)
               .HasDefaultValue(false)
               .IsRequired();

        builder.HasOne(a => a.DocumentType)
               .WithMany()
               .HasForeignKey(a => a.DocumentTypeCode)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsAuditInfo();
    }
}