namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpTorDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

public class PpTorTrainingItemConfiguration : EntityTypeConfigurationBase<PpTorTrainingItem, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpTorTrainingItem> builder)
    {
        builder.ToTable(nameof(PpTorTrainingItem), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(a => a.Sequence);

        builder.Property(a => a.CourseName);

        builder.Property(a => a.PeriodDay);

        builder.Property(a => a.Place);

        builder.Property(a => a.TrainingCount);

        builder.Property(a => a.TotalPersonPerTime);

        builder.HasOne(p => p.PpTorDraft)
               .WithMany(t => t.PpTorTrainingItems)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
    }
}
