namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractAmendment.CamContractAmendment.CamContractAmendmentPoAddendum;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using Microsoft.EntityFrameworkCore;

public class CamContractAmendmentPoAddendumPaymentTermConfiguration : EntityTypeConfigurationBase<CamContractAmendmentPoAddendumPaymentTerm, Dp2DbContext>
{
    protected override void EntityConfigure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<CamContractAmendmentPoAddendumPaymentTerm> builder)
    {
        builder.ToTable(nameof(CamContractAmendmentPoAddendumPaymentTerm), nameof(Domain.ContractAmendment.ContractAmendment));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasVogenConversion();

        builder.Property(p => p.Title);

        builder.Property(p => p.PaymentTermNo);

        builder.Property(p => p.LeadTime);

        builder.Property(p => p.DeliveryDate)
               .IsRequired();

        builder.Property(p => p.InstallmentPercentage)
               .IsRequired();

        builder.Property(p => p.Amount)
               .IsRequired();

        builder.Property(p => p.AdvanceDeductionAmount)
               .IsRequired();

        builder.Property(p => p.PerformanceDeductionAmount)
               .IsRequired();

        builder.Property(p => p.Description)
               .IsRequired();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}