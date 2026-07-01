namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpPurchaseRequisition;

using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpPurchaseRequisitionBudgetDetailConfiguration : IEntityTypeConfiguration<PpPurchaseRequisitionBudgetDetail>
{
    public void Configure(EntityTypeBuilder<PpPurchaseRequisitionBudgetDetail> builder)
    {
        builder.ToTable(nameof(PpPurchaseRequisitionBudgetDetail), nameof(Procurement));

        builder.HasKey(x => x.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PpPurchaseRequisitionBudgetId)
               .HasConversion<PpPurchaseRequisitionBudgetId.EfCoreValueConverter, PpPurchaseRequisitionBudgetId.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(x => x.Sequence)
               .IsRequired();

        builder.Property(x => x.Department)
               .HasMaxLength(500)
               .IsRequired();

        builder.Property(p => p.BudgetTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(x => x.ProjectCode)
               .HasMaxLength(50);

        builder.Property(x => x.AccountNoCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(x => x.Budget)
               .IsRequired();

        builder.HasOne(x => x.BudgetType)
               .WithMany()
               .HasForeignKey(x => x.BudgetTypeCode)
               .HasPrincipalKey(x => x.Code);

        builder.HasOne(x => x.AccountNo)
               .WithMany()
               .HasForeignKey(x => x.AccountNoCode)
               .HasPrincipalKey(x => x.Code);

        builder.OwnsAuditInfo();
    }
}