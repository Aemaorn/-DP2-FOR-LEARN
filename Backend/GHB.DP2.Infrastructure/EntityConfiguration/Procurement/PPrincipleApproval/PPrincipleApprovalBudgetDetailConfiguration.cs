namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApproval;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPrincipleApprovalBudgetDetailConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalBudgetDetail, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPrincipleApprovalBudgetDetail> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalBudgetDetail), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PrincipleApprovalBudgetId)
               .HasConversion<PPrincipleApprovalBudgetId.EfCoreValueConverter, PPrincipleApprovalBudgetId.EfCoreValueComparer>()
               .IsRequired();

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