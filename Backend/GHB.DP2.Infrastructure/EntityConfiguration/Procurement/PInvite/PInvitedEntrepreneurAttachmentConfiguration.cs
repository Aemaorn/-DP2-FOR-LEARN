namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PInvite;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PInvite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PJp006EntrepreneurAttachmentsConfiguration : EntityTypeConfigurationBase<PInvitedEntrepreneursAttachments>
{
    protected override void EntityConfigure(EntityTypeBuilder<PInvitedEntrepreneursAttachments> builder)
    {
        builder.ToTable(nameof(Domain.Procurement.PInvite.PInvitedEntrepreneursAttachments), nameof(Domain.Procurement));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(a => a.FileId)
               .IsRequired();

        builder.Property(a => a.FileName)
               .IsRequired();

        builder.Property(a => a.Sequence)
               .HasDefaultValue(1)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}