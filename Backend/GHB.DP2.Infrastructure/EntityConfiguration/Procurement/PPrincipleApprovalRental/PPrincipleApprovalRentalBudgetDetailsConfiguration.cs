namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApprovalRental;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPrincipleApprovalRentalBudgetDetailsConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalRentalBudgetDetail, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPrincipleApprovalRentalBudgetDetail> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalRentalBudgetDetail), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.Property(p => p.Department)
               .HasMaxLength(1000)
               .IsRequired();

        builder.Property(p => p.BudgetType)
               .HasMaxLength(1000)
               .IsRequired();

        builder.Property(p => p.ProjectCode)
               .HasMaxLength(1000);

        builder.Property(p => p.AccountNo)
               .HasMaxLength(1000)
               .IsRequired();

        builder.Property(p => p.Budget)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}