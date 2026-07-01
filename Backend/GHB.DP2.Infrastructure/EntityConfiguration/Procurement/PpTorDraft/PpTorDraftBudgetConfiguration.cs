namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpTorDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpTorDraftBudgetConfiguration : EntityTypeConfigurationBase<PpTorDraftBudget, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpTorDraftBudget> builder)
    {
        builder.ToTable(nameof(PpTorDraftBudget), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasVogenConversion();

        builder.Property(p => p.Sequence);

        builder.Property(p => p.Description);

        builder.Property(p => p.BudgetAmount);

        builder.HasOne(b => b.PpTorDraft)
               .WithMany(t => t.PpTorDraftBudgets)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
    }
}