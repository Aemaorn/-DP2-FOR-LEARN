namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SuVendorAttachmentConfiguration : EntityTypeConfigurationBase<SuVendorAttachment, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuVendorAttachment> builder)
    {
        builder.ToTable(nameof(SuVendorAttachment), nameof(SystemUtility));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasVogenConversion();

        builder.Property(e => e.FileName)
               .IsRequired();

        builder.Property(e => e.IsPrivate)
               .IsRequired();

        builder.Property(e => e.Sequence)
               .IsRequired();

        builder.Property(e => e.FileId)
               .IsRequired();

        builder.HasOne(e => e.Vendor)
               .WithMany(e => e.Attachments)
               .HasForeignKey(e => e.VendorId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
    }
}