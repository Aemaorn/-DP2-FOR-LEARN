namespace GHB.DP2.Infrastructure.EntityConfiguration.SystemUtility;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SuDelegateeHistoriesConfiguration : EntityTypeConfigurationBase<SuDelegateeHistories, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<SuDelegateeHistories> builder)
    {
        builder.ToTable(nameof(SuDelegateeHistories), nameof(SystemUtility));

        builder.HasKey(dh => dh.Id);

        builder.Property(dh => dh.Id)
               .HasVogenConversion();

        builder.Property(dh => dh.SuDelegateeId)
               .HasVogenConversion();

        builder.Property(dh => dh.ProgramId)
               .HasVogenConversion();

        builder.Property(dh => dh.ActionStatus)
               .IsRequired();

        builder.Property(dh => dh.Remark)
               .IsRequired(false);

        builder.HasOne(dh => dh.SuProgram)
               .WithMany()
               .IsRequired();

        builder.ComplexProperty(dh => dh.ActivityInfo, ai =>
        {
            ai.Property(a => a.Type).HasColumnName("ActivityType");
            ai.Property(a => a.Action).HasColumnName("ActivityAction");
            ai.Property(a => a.Status).HasColumnName("ActivityStatus");
            ai.Property(a => a.Remark).HasColumnName("ActivityRemark");
            ai.Property(a => a.ProgramCode).HasColumnName("ActivityProgramCode");
            ai.Property(a => a.Ip).HasColumnName("ActivityIp");
        });

        builder.OwnsAuditInfo();
    }
}