namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.Pw184;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.Pw184;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class Pw184GLAccountConfiguration : EntityTypeConfigurationBase<Pw184GLAccount, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<Pw184GLAccount> builder)
    {
        builder.ToTable(nameof(Pw184GLAccount), nameof(Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(p => p.Pw184Id)
               .HasConversion<Pw184Id.EfCoreValueConverter, Pw184Id.EfCoreValueComparer>()
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

        builder.HasOne(p => p.Pw184)
               .WithMany(p => p.GLAccounts)
               .HasForeignKey(p => p.Pw184Id)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
    }
}
