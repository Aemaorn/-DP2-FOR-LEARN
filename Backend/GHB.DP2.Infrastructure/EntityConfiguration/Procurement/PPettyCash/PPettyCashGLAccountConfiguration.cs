namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPettyCash;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPettyCash;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPettyCashGLAccountConfiguration : EntityTypeConfigurationBase<PPettyCashGLAccount, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPettyCashGLAccount> builder)
    {
        builder.ToTable(nameof(PPettyCashGLAccount), nameof(Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.Property(p => p.PettyCashId)
               .HasConversion<PettyCashId.EfCoreValueConverter, PettyCashId.EfCoreValueComparer>()
               .IsRequired();

        builder.HasOne(p => p.PettyCash)
               .WithMany(p => p.GLAccounts)
               .HasForeignKey(p => p.PettyCashId)
               .OnDelete(DeleteBehavior.Cascade);

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

        builder.OwnsAuditInfo();
    }
}