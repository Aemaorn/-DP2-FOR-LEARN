namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPettyCashReimbursement;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPettyCashReimbursement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPettyCashReimbursementAttachmentsConfiguration : EntityTypeConfigurationBase<PPettyCashReimbursementAttachments>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPettyCashReimbursementAttachments> builder)
    {
        builder.ToTable(nameof(PPettyCashReimbursementAttachments), nameof(Procurement));

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

        builder.HasOne(p => p.PPettyCashReimbursement)
               .WithMany(p => p.Attachments);

        builder.OwnsAuditInfo();
    }
}