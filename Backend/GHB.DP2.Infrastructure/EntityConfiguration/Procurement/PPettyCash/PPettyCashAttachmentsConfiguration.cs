namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPettyCash;

using GHB.DP2.Domain.Procurement.PPettyCash;
using Codehard.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPettyCashAttachmentsConfiguration : EntityTypeConfigurationBase<PPettyCashAttachments>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPettyCashAttachments> builder)
    {
        builder.ToTable(nameof(PPettyCashAttachments), nameof(Domain.Procurement.Procurement));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .ValueGeneratedNever();

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