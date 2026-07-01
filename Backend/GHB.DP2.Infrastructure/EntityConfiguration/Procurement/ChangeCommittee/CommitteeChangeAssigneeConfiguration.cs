namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.ChangeCommittee;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.ChangeCommittee;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CommitteeChangeAssigneeConfiguration : EntityTypeConfigurationBase<CommitteeChangeAssignee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CommitteeChangeAssignee> builder)
    {
        builder.ToTable(nameof(CommitteeChangeAssignee), "Procurement");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion();

        builder.Property(a => a.CommitteeChangeId)
               .HasVogenConversion();

        builder.HasOne(a => a.CommitteeChanges)
               .WithMany(c => c.Assignees)
               .HasForeignKey(a => a.CommitteeChangeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.AssigneeInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}
