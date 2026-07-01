namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApprovalRental;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPrincipleApprovalRentalAssigneeConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalRentalAssignee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPrincipleApprovalRentalAssignee> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalRentalAssignee), nameof(Procurement));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
               .HasVogenConversion();

        builder.AssigneeInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}