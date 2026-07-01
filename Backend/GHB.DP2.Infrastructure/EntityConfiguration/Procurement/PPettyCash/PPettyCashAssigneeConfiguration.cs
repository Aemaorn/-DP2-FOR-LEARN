namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPettyCash;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPettyCash;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPettyCashAssigneeConfiguration : EntityTypeConfigurationBase<PPettyCashAssignee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPettyCashAssignee> builder)
    {
        builder.ToTable(nameof(PPettyCashAssignee), nameof(Domain.Procurement.Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion()
               .ValueGeneratedNever();

        builder.AssigneeInfo();
        builder.OwnsAuditInfo();
    }
}