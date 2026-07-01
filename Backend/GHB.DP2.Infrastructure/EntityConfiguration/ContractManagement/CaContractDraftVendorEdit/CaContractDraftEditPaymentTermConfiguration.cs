namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CaContractDraftVendorEdit;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CaContractDraftEditPaymentTermConfiguration : EntityTypeConfigurationBase<CaContractDraftEditPaymentTerm, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractDraftEditPaymentTerm> builder)
    {
        builder.ToTable(nameof(CaContractDraftEditPaymentTerm), nameof(ContractManagement));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasVogenConversion();

        builder.Property(c => c.PaymentTermNo);

        builder.Property(c => c.LeadTime);

        builder.Property(c => c.DeliveryDate);

        builder.Property(c => c.InstallmentPercentage);

        builder.Property(c => c.Amount);

        builder.Property(c => c.AdvanceDeductionAmount);

        builder.Property(c => c.PerformanceDeductionAmount);

        builder.Property(c => c.Description);

        builder.Property(c => c.Sequence);

        builder.Property(c => c.PeriodTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
               .IsRequired(false);

        builder.HasOne(c => c.CaContractDraftVendorEdit)
               .WithMany(c => c.PaymentTerms)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PeriodType)
               .WithMany()
               .HasForeignKey(x => x.PeriodTypeCode)
               .HasPrincipalKey(x => x.Code);

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}
