namespace GHB.DP2.Infrastructure.EntityConfiguration.Plan;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Plan;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PlanAnnouncementSelectedConfiguration : EntityTypeConfigurationBase<PlanAnnouncementSelected, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PlanAnnouncementSelected> builder)
    {
        builder.ToTable(nameof(PlanAnnouncementSelected), nameof(Plan));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.HasOne(pas => pas.Plan)
               .WithMany(p => p.AnnouncementSelectedInformation)
               .HasForeignKey(pas => pas.PlanId)
               .IsRequired();

        builder.HasOne(pas => pas.PlanAnnouncement)
               .WithMany(p => p.AnnouncementSelectedInformations)
               .HasForeignKey(pas => pas.PlanAnnouncementId)
               .IsRequired();

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}