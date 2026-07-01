namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.ChangeCommittee;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.ChangeCommittee;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CommitteeChangeAttachmentInfoConfiguration : EntityTypeConfigurationBase<CommitteeChangeAttachmentInfo, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CommitteeChangeAttachmentInfo> builder)
    {
        builder.ToTable(nameof(CommitteeChangeAttachmentInfo), "Procurement");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion();

        builder.Property(a => a.CommitteeChangeAttachmentId)
               .HasVogenConversion();

        builder.Property(a => a.Sequence)
               .IsRequired()
               .HasDefaultValue(1);

        builder.Property(a => a.FileId)
               .IsRequired();

        builder.Property(a => a.FileName)
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(a => a.IsPublic)
               .IsRequired()
               .HasDefaultValue(false);

        builder.HasOne(a => a.CommitteeChangeAttachment)
               .WithMany(c => c.CommitteeChangeAttachmentInfos)
               .HasForeignKey(a => a.CommitteeChangeAttachmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}