namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.P79Clause2;

using GHB.DP2.Domain.Procurement.P79Clause2;
using Codehard.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class P79Clause2AttachmentsConfiguration : EntityTypeConfigurationBase<P79Clause2Attachments>
{
    protected override void EntityConfigure(EntityTypeBuilder<P79Clause2Attachments> builder)
    {
        builder.ToTable(nameof(P79Clause2Attachments), nameof(Domain.Procurement.Procurement));

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

        builder.HasOne(p => p.P79Clause2)
               .WithMany(p => p.Attachments);

        builder.HasOne(a => a.DocumentType)
               .WithMany()
               .HasForeignKey(a => a.DocumentTypeCode)
               .HasPrincipalKey(p => p.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsAuditInfo();
    }
}