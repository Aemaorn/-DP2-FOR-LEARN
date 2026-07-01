namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpTorDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpTorDraftAssigneeConfiguration : EntityTypeConfigurationBase<PpTorDraftAssignee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpTorDraftAssignee> builder)
    {
        builder.ToTable(nameof(PpTorDraftAssignee), nameof(Procurement));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion();

        builder.HasOne(a => a.PpTorDraft)
               .WithMany(m => m.Assignees)
               .OnDelete(DeleteBehavior.Cascade);

        builder.AssigneeInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}