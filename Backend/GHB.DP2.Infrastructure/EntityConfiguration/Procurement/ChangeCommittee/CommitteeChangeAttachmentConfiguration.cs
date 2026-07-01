namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.ChangeCommittee;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.ChangeCommittee;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CommitteeChangeAttachmentConfiguration : EntityTypeConfigurationBase<CommitteeChangeAttachment, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CommitteeChangeAttachment> builder)
    {
        builder.ToTable(nameof(CommitteeChangeAttachment), "Procurement");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion();

        builder.Property(a => a.CommitteeChangeId)
               .HasVogenConversion();

        builder.Property(a => a.Sequence)
               .IsRequired()
               .HasDefaultValue(1);

        builder.Property(a => a.TypeCode)
               .HasVogenConversion();

        builder.Property(a => a.Remark)
               .IsRequired(false);

        builder.HasOne(a => a.CommitteeChanges)
               .WithMany(c => c.Attachments)
               .HasForeignKey(a => a.CommitteeChangeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.CommitteeChangeAttachmentInfos)
               .WithOne(i => i.CommitteeChangeAttachment)
               .HasForeignKey(i => i.CommitteeChangeAttachmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.SuParameter)
               .WithMany()
               .HasForeignKey(a => a.TypeCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired();

        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}