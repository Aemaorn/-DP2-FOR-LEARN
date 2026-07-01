namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpTorDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

public class PpTorTemplateComputerConfiguration : EntityTypeConfigurationBase<PpTorTemplateComputer, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpTorTemplateComputer> builder)
    {
        builder.ToTable(nameof(PpTorTemplateComputer), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasVogenConversion();

        builder.Property(t => t.EvidenceDescription);

        builder.Property(t => t.EvidenceNumber);

        builder.Property(t => t.DocumentDescription);

        builder.Property(t => t.CriteriaConsiderDescription);

        builder.Property(t => t.ManuelDescription);

        builder.HasOne(c => c.PpTorDraft)
               .WithOne(d => d.PpTorTemplateComputer)
               .HasForeignKey<PpTorTemplateComputer>(c => c.PpTorDraftId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(t => t.Evidence, e =>
        {
            e.HasOne(p => p.SuPresentCountUnit)
              .WithMany()
              .HasForeignKey(p => p.PresentCountUnit)
              .HasPrincipalKey(p => p.Code)
              .IsRequired(false);

            e.HasOne(p => p.SuPresentCountType)
              .WithMany()
              .HasForeignKey(p => p.PresentCountType)
              .HasPrincipalKey(p => p.Code)
              .IsRequired(false);
        });

        builder.OwnsOne(t => t.PreventiveMaintenance, pm =>
        {
            pm.HasOne(p => p.PmUnitType)
              .WithMany()
              .HasForeignKey(p => p.PmUnit)
              .HasPrincipalKey(p => p.Code)
              .IsRequired(false);

            pm.HasOne(p => p.DisruptedCountUnitType)
              .WithMany()
              .HasForeignKey(p => p.DisruptedCountUnit)
              .HasPrincipalKey(p => p.Code)
              .IsRequired(false);

            pm.HasOne(p => p.PmFinePctUnitType)
              .WithMany()
              .HasForeignKey(p => p.PmFinePctUnit)
              .HasPrincipalKey(p => p.Code)
              .IsRequired(false);
        });

        builder.OwnsOne(t => t.CorrectiveMaintenance, cm =>
        {
            cm.HasOne(p => p.CmUnitType)
              .WithMany()
              .HasForeignKey(p => p.CmUnit)
              .HasPrincipalKey(p => p.Code)
              .IsRequired(false);

            cm.HasOne(p => p.CmCompleteUnitType)
              .WithMany()
              .HasForeignKey(p => p.CmCompleteUnit)
              .HasPrincipalKey(p => p.Code)
              .IsRequired(false);

            cm.HasOne(p => p.DayStartType)
              .WithMany()
              .HasForeignKey(p => p.DayStart)
              .HasPrincipalKey(p => p.Code)
              .IsRequired(false);

            cm.HasOne(p => p.DayEndType)
              .WithMany()
              .HasForeignKey(p => p.DayEnd)
              .HasPrincipalKey(p => p.Code)
              .IsRequired(false);

            cm.HasOne(p => p.CmFinePercentUnitType)
              .WithMany()
              .HasForeignKey(p => p.CmFinePercentUnit)
              .HasPrincipalKey(p => p.Code)
              .IsRequired(false);
        });

        builder.OwnsOne(t => t.Training, tr =>
        {
            tr.HasOne(p => p.TrainingCountUnitType)
              .WithMany()
              .HasForeignKey(p => p.TrainingCountUnit)
              .HasPrincipalKey(p => p.Code)
              .IsRequired(false);

            tr.HasOne(p => p.TrainingUnitType)
              .WithMany()
              .HasForeignKey(p => p.TrainingUnitId)
              .HasPrincipalKey(p => p.Code)
              .IsRequired(false);
        });

        builder.OwnsAuditInfo();
    }
}
