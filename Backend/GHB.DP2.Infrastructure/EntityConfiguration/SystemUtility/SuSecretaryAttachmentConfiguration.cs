namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SuSecretaryAttachmentConfiguration : EntityTypeConfigurationBase<SuSecretaryAttachment, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuSecretaryAttachment> builder)
    {
        builder.ToTable(nameof(SuSecretaryAttachment), nameof(SystemUtility));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasVogenConversion();

        builder.Property(e => e.SecretaryOwnerId)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(e => e.FileId)
               .IsRequired();

        builder.Property(e => e.FileName)
               .IsRequired();

        builder.Property(e => e.Sequence)
               .IsRequired();

        builder.HasOne(e => e.SecretaryOwner)
               .WithMany(o => o.Attachments)
               .HasForeignKey(e => e.SecretaryOwnerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
    }
}
