namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpTorDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpTorDraftTechnicalPeriodDetailConfiguration : EntityTypeConfigurationBase<PpTorDraftTechnicalPeriodDetail, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpTorDraftTechnicalPeriodDetail> builder)
    {
        builder.ToTable(nameof(PpTorDraftTechnicalPeriodDetail), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasVogenConversion();

        builder.Property(p => p.PpTorDraftTechnicalPeriodId)
            .HasConversion<PpTorDraftTechnicalPeriodId.EfCoreValueConverter, PpTorDraftTechnicalPeriodId.EfCoreValueComparer>()
            .IsRequired();

        builder.Property(p => p.Branch)
            .HasMaxLength(2000);

        builder.Property(p => p.PersonalCount);

        builder.Property(p => p.StartDate);

        builder.Property(p => p.EndDate);

        builder.OwnsAuditInfo();
    }
}