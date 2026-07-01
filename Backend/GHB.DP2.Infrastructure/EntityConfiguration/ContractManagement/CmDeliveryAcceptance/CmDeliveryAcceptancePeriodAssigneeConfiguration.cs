namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CmDeliveryAcceptance;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PJp006AssigneeConfiguration : EntityTypeConfigurationBase<CmDeliveryAcceptancePeriodAssignee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CmDeliveryAcceptancePeriodAssignee> builder)
    {
        builder.ToTable(nameof(CmDeliveryAcceptancePeriodAssignee), nameof(ContractManagement));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasVogenConversion();

        builder.Property(c => c.DeliveryAcceptancePeriodId)
               .HasVogenConversion()
               .IsRequired();

        builder.HasOne(c => c.DeliveryAcceptancePeriod)
               .WithMany(c => c.Assignees)
               .HasForeignKey(c => c.DeliveryAcceptancePeriodId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.AssigneeInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}