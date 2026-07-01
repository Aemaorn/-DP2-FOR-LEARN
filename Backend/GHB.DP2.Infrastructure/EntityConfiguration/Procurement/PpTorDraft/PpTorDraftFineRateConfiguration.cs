namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpTorDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpTorDraftFineRateConfiguration : EntityTypeConfigurationBase<PpTorDraftFineRate, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpTorDraftFineRate> builder)
    {
        builder.ToTable(nameof(PpTorDraftFineRate), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasVogenConversion();

        builder.Property(p => p.Sequence);

        builder.Property(p => p.Description);

        builder.Property(p => p.Rate);

        builder.Property(p => p.ConditionOther);

        builder.HasOne(p => p.PpTorDraft)
            .WithMany(p => p.PpTorDraftFineRates)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.PeriodType)
               .WithMany()
               .HasForeignKey(p => p.PeriodTypeCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired(false);

        builder.HasOne(p => p.Condition)
               .WithMany()
               .HasForeignKey(p => p.ConditionCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired(false);

        builder.OwnsAuditInfo();
    }
}