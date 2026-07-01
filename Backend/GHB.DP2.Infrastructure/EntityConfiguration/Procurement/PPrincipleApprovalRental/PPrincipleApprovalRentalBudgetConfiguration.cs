namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApprovalRental;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPrincipleApprovalRentalBudgetConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalRentalBudget, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPrincipleApprovalRentalBudget> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalRentalBudget), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.Property(p => p.Description)
               .IsRequired();

        builder.Property(p => p.BudgetAmount)
               .IsRequired();

        builder.HasMany(m => m.PrincipleApprovalRentalBudgetDetails)
               .WithOne(d => d.PrincipleApprovalRentalBudget)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
    }
}