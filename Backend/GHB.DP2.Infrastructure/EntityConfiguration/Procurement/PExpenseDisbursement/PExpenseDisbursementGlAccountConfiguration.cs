namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PExpenseDisbursement;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PExpenseDisbursement;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PExpenseDisbursementGlAccountConfiguration : EntityTypeConfigurationBase<PExpenseDisbursementGlAccount, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PExpenseDisbursementGlAccount> builder)
    {
        builder.ToTable(nameof(PExpenseDisbursementGlAccount), nameof(Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.Property(p => p.SoId)
               .IsRequired();

        builder.Property(p => p.BudgetTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
               .IsRequired();

        builder.HasOne(p => p.BudgetType)
               .WithMany()
               .HasForeignKey(p => p.BudgetTypeCode)
               .HasPrincipalKey(p => p.Code);

        builder.Property(p => p.ProjectNumber);

        builder.Property(p => p.Amount)
               .IsRequired();

        builder.Property(p => p.GlAccountCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
               .IsRequired();

        builder.HasOne(p => p.GlAccount)
               .WithMany()
               .HasForeignKey(p => p.GlAccountCode)
               .HasPrincipalKey(p => p.Code);

        builder.OwnsAuditInfo();
    }
}