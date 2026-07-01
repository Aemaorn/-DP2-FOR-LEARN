namespace GHB.DP2.Infrastructure.EntityConfiguration.Common;

using Codehard.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GHB.DP2.Domain.Common;

public class ActivityLogActivityConfiguration : EntityTypeConfigurationBase<ActivityLog, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable(nameof(ActivityLog), nameof(SystemUtility));

        builder.HasKey(al => al.Id);

        builder.Property(al => al.Key);

        builder.HasIndex(al => al.Key);

        builder.ComplexProperty(al => al.ActivityInfo, ai =>
        {
            ai.Property(a => a.Type).HasColumnName("ActivityType");
            ai.Property(a => a.Action).HasColumnName("ActivityAction");
            ai.Property(a => a.Status).HasColumnName("ActivityStatus");
            ai.Property(a => a.Remark).HasColumnName("ActivityRemark");
            ai.Property(a => a.ProgramCode).HasColumnName("ActivityProgramCode");
            ai.Property(a => a.Ip).HasColumnName("ActivityIp");
        });

        builder.ComplexProperty(al => al.AuditInfo, ai =>
        {
            ai.Property(a => a.CreatedAt).HasColumnName("CreatedAt");
            ai.Property(a => a.CreatedBy).HasColumnName("CreatedBy");
            ai.Property(a => a.CreatedByName).HasColumnName("CreatedByName");
            ai.Property(a => a.LastModifiedAt).HasColumnName("LastModifiedAt");
            ai.Property(a => a.LastModifiedBy).HasColumnName("LastModifiedBy");
            ai.Property(a => a.LastModifiedByName).HasColumnName("LastModifiedByName");
        });
    }
}