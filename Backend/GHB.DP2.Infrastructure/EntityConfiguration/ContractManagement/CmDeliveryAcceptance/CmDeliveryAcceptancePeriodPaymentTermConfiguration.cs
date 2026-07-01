namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CmDeliveryAcceptance;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CmDeliveryAcceptancePeriodPaymentTermConfiguration : EntityTypeConfigurationBase<CmDeliveryAcceptancePeriodPaymentTerm, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CmDeliveryAcceptancePeriodPaymentTerm> builder)
    {
        builder.ToTable(nameof(CmDeliveryAcceptancePeriodPaymentTerm), nameof(ContractManagement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.DeliveryAcceptancePeriodId)
               .HasConversion<CmDeliveryAcceptancePeriodId.EfCoreValueConverter, CmDeliveryAcceptancePeriodId.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(p => p.DeliveryAcceptancePeriodId)
               .IsRequired();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.Property(p => p.Description)
               .IsRequired();

        builder.Property(p => p.Amount)
               .IsRequired();

        builder.Property(p => p.PaymentTerm)
               .IsRequired();

        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}