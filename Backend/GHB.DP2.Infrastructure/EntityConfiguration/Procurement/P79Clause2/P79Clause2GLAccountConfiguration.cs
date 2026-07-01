namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.P79Clause2;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.P79Clause2;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class P79Clause2GLAccountConfiguration : EntityTypeConfigurationBase<P79Clause2GLAccount, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<P79Clause2GLAccount> builder)
    {
        builder.ToTable(nameof(P79Clause2GLAccount), nameof(Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(p => p.P79Clause2Id)
               .HasConversion<P79Clause2Id.EfCoreValueConverter, P79Clause2Id.EfCoreValueComparer>()
               .IsRequired();

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

        builder.Property(p => p.GLAccountCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
               .IsRequired();

        builder.HasOne(p => p.GLAccount)
               .WithMany()
               .HasForeignKey(p => p.GLAccountCode)
               .HasPrincipalKey(p => p.Code);

        builder.Property(p => p.Amount)
               .IsRequired();

        builder.HasOne(p => p.P79Clause2)
               .WithMany(p => p.GLAccounts)
               .HasForeignKey(p => p.P79Clause2Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
    }
}