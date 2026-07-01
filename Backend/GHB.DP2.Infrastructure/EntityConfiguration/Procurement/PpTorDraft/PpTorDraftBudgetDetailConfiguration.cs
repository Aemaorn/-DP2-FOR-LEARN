namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpTorDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpTorDraftBudgetDetailConfiguration : EntityTypeConfigurationBase<PpTorDraftBudgetDetail, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpTorDraftBudgetDetail> builder)
    {
        builder.ToTable(nameof(PpTorDraftBudgetDetail), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasVogenConversion();

        builder.Property(p => p.PpTorDraftBudgetId)
            .HasConversion<PpTorDraftBudgetId.EfCoreValueConverter, PpTorDraftBudgetId.EfCoreValueComparer>()
            .IsRequired();

        builder.Property(p => p.Sequence);

        builder.Property(p => p.Department)
            .HasMaxLength(1000);

        builder.Property(p => p.BudgetType)
            .HasMaxLength(1000);

        builder.Property(p => p.ProjectCode)
            .HasMaxLength(1000);

        builder.Property(p => p.AccountNo)
            .HasMaxLength(1000);

        builder.Property(p => p.Budget);

        builder.OwnsAuditInfo();
    }
}