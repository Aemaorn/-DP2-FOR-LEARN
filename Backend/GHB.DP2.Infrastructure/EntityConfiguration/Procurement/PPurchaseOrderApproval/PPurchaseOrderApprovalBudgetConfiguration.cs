namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPurchaseOrderApproval;

using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPurchaseOrderApprovalBudgetConfiguration : IEntityTypeConfiguration<PPurchaseOrderApprovalBudget>
{
    public void Configure(EntityTypeBuilder<PPurchaseOrderApprovalBudget> builder)
    {
        builder.ToTable(nameof(PPurchaseOrderApprovalBudget), nameof(Procurement));

        builder.HasKey(x => x.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(x => x.Description)
               .IsRequired();

        builder.Property(x => x.BudgetAmount)
               .IsRequired();

        builder.Property(x => x.Sequence)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}
