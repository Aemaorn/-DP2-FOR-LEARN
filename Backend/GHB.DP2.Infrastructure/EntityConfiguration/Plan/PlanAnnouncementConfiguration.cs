namespace GHB.DP2.Infrastructure.EntityConfiguration.Plan;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Plan;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PlanAnnouncementConfiguration : EntityTypeConfigurationBase<PlanAnnouncement, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PlanAnnouncement> builder)
    {
        builder.ToTable(nameof(PlanAnnouncement), nameof(Plan));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PlanAnnouncementNumber)
               .HasVogenConversion()
               .IsRequired();

        builder.Property(p => p.GroupEgpNumber)
               .HasMaxLength(500);

        builder.Property(p => p.Year)
               .IsRequired();

        builder.Property(p => p.SupplyMethodCode)
               .IsRequired();

        builder.Property(p => p.Remark);

        builder.Property(p => p.AnnouncementTitle)
               .HasMaxLength(2000);

        builder.Property(p => p.AnnouncementDate);

        builder.Property(p => p.Telephone)
               .IsRequired(false);

        builder.Property(p => p.DocumentDate);

        builder.Property(p => p.Status)
               .HasConversion(new EnumToStringConverter<PlanAnnouncementStatus>());

        builder.HasOne(p => p.SupplyMethodInfo)
               .WithMany()
               .HasForeignKey(p => p.SupplyMethodCode)
               .HasPrincipalKey(su => su.Code)
               .IsRequired();

        builder.HasMany(p => p.Acceptors)
               .WithOne(a => a.PlanAnnouncement)
               .HasForeignKey(p => p.PlanAnnouncementId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Assignees)
               .WithOne(p => p.PlanAnnouncement)
               .HasForeignKey(p => p.PlanAnnouncementId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Attachments)
               .WithOne();

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(PlanAnnouncementDocumentHistory), nameof(Plan));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.DocumentType)
                           .HasConversion(new EnumToStringConverter<PlanAnnouncementDocumentType>());

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<PlanAnnouncementStatus>());
        });

        builder.HasActivityInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}