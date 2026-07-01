namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.Attachments;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ProcurementAttachmentInfoConfiguration : EntityTypeConfigurationBase<ProcurementAttachmentInfo, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<ProcurementAttachmentInfo> builder)
    {
        builder.ToTable(nameof(ProcurementAttachmentInfo), nameof(Procurement));

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
               .HasVogenConversion();

        builder.Property(i => i.ProcurementAttachmentId)
               .HasVogenConversion();

        builder.Property(i => i.FileName)
               .IsRequired()
               .HasMaxLength(255);

        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}