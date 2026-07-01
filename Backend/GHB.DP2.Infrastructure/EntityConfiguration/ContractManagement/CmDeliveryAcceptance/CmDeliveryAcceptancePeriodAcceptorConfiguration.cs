namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CmDeliveryAcceptance;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CmDeliveryAcceptancePeriodAcceptorConfiguration : EntityTypeConfigurationBase<CmDeliveryAcceptancePeriodAcceptor, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CmDeliveryAcceptancePeriodAcceptor> builder)
    {
        builder.ToTable(nameof(CmDeliveryAcceptancePeriodAcceptor), nameof(ContractManagement));

        builder.Property(c => c.IsUnableToPerformDuties)
               .IsRequired();

        builder.Property(c => c.DeliveryAcceptancePeriodId)
               .HasVogenConversion()
               .IsRequired();

        builder.HasOne(m => m.CommitteePosition)
               .WithMany()
               .HasForeignKey(m => m.CommitteePositionsCode)
               .HasPrincipalKey(m => m.Code)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.DeliveryAcceptancePeriod)
               .WithMany(c => c.Acceptors)
               .HasForeignKey(c => c.DeliveryAcceptancePeriodId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.AcceptorInfo();
        builder.HasSoftDelete();
        builder.OwnsAuditInfo();
    }
}