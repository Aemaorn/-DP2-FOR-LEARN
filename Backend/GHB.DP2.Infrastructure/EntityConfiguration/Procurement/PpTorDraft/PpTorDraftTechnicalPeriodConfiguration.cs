namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpTorDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpTorDraftTechnicalPeriodConfiguration : EntityTypeConfigurationBase<PpTorDraftTechnicalPeriod, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpTorDraftTechnicalPeriod> builder)
    {
        builder.ToTable(nameof(PpTorDraftTechnicalPeriod), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasVogenConversion();

        builder.Property(p => p.Period);

        builder.Property(p => p.StartDate);

        builder.Property(p => p.EndDate);

        builder.Property(p => p.DeliveryConditionCode);

        builder.Property(p => p.DeliveryDate);

        builder.HasOne(p => p.PpTorDraft)
            .WithMany(p => p.PpTorDraftTechnicalPeriods)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.PeriodType)
               .WithMany()
               .HasForeignKey(p => p.PeriodTypeCode)
               .HasPrincipalKey(p => p.Code);

        builder.HasOne(p => p.PeriodCondition)
               .WithMany()
               .HasForeignKey(p => p.PeriodConditionCode)
               .HasPrincipalKey(p => p.Code);

        builder.HasOne(p => p.DeliveryCondition)
               .WithMany()
               .HasForeignKey(p => p.DeliveryConditionCode)
               .HasPrincipalKey(p => p.Code);

        builder.OwnsAuditInfo();
    }
}