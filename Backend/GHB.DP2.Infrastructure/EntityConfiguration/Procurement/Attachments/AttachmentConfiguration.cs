namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.Attachments;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ProcurementAttachmentConfiguration : EntityTypeConfigurationBase<ProcurementAttachment, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<ProcurementAttachment> builder)
    {
        builder.ToTable(nameof(ProcurementAttachment), nameof(Procurement));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion();

        builder.Property(a => a.ProcurementId)
               .HasVogenConversion();

        builder.Property(a => a.Sequence)
               .IsRequired()
               .HasDefaultValue(1);

        builder.Property(a => a.TypeCode)
               .HasVogenConversion();

        builder.Property(a => a.Remark)
               .IsRequired(false);

        builder.HasOne(a => a.Procurement)
               .WithMany(p => p.Attachments)
               .HasForeignKey(a => a.ProcurementId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.ProcurementAttachmentInfos)
               .WithOne(i => i.ProcurementAttachment)
               .HasForeignKey(i => i.ProcurementAttachmentId)
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