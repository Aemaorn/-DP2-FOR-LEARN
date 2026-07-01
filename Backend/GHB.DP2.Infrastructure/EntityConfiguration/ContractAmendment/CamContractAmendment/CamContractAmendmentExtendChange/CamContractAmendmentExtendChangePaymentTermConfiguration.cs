namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAmendment.CamContractAmendment.CamContractAmendmentExtendChange;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CamContractAmendmentExtendChangePaymentTermConfiguration : EntityTypeConfigurationBase<CamContractAmendmentExtendChangePaymentTerm, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CamContractAmendmentExtendChangePaymentTerm> builder)
    {
        builder.ToTable(nameof(CamContractAmendmentExtendChangePaymentTerm), nameof(ContractAmendment));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
               .HasVogenConversion();

        builder.Property(e => e.PaymentTermNo)
               .IsRequired();

        builder.Property(e => e.LeadTime)
               .IsRequired();

        builder.Property(e => e.DeliveryDate)
               .IsRequired();

        builder.Property(e => e.InstallmentPercent)
               .IsRequired();

        builder.Property(e => e.Amount)
               .IsRequired();

        builder.Property(e => e.AdvanceDeductionAmount)
               .IsRequired();

        builder.Property(e => e.PerformanceDeductionAmount)
               .IsRequired();

        builder.Property(e => e.Description)
               .IsRequired();

        builder.Property(e => e.IsDelivery)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}