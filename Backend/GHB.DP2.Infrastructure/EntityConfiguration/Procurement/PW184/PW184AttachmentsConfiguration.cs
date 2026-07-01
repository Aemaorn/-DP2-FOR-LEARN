namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.Pw184;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.Pw184;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class Pw184AttachmentsConfiguration : EntityTypeConfigurationBase<Pw184Attachments>
{
    protected override void EntityConfigure(EntityTypeBuilder<Pw184Attachments> builder)
    {
        builder.ToTable(nameof(Pw184Attachments), nameof(Domain.Procurement.Procurement));

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

        builder.HasOne(p => p.Pw184)
               .WithMany(p => p.Attachments);

        builder.HasOne(a => a.DocumentType)
               .WithMany()
               .HasForeignKey(a => a.DocumentTypeCode)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsAuditInfo();
    }
}
