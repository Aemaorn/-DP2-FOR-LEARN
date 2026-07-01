namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpMedianPrice;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpMedianPriceAssigneeConfiguration : EntityTypeConfigurationBase<PpMedianPriceAssignee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpMedianPriceAssignee> builder)
    {
        builder.ToTable(nameof(PpMedianPriceAssignee), nameof(Procurement));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion();

        builder.HasOne(a => a.MedianPrice)
               .WithMany(m => m.Assignees)
               .OnDelete(DeleteBehavior.Cascade);

        builder.AssigneeInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}