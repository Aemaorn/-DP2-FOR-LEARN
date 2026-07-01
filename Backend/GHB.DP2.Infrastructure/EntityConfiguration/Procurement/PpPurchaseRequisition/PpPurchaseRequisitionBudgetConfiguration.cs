namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpPurchaseRequisition;

using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpPurchaseRequisitionBudgetConfiguration : IEntityTypeConfiguration<PpPurchaseRequisitionBudget>
{
    public void Configure(EntityTypeBuilder<PpPurchaseRequisitionBudget> builder)
    {
        builder.ToTable(nameof(PpPurchaseRequisitionBudget), nameof(Procurement));

        builder.HasKey(x => x.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PpPurchaseRequisitionId)
               .HasConversion<PpPurchaseRequisitionId.EfCoreValueConverter, PpPurchaseRequisitionId.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(x => x.Description)
            .IsRequired();

        builder.Property(x => x.BudgetAmount)
            .IsRequired();

        builder.Property(x => x.Sequence)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}