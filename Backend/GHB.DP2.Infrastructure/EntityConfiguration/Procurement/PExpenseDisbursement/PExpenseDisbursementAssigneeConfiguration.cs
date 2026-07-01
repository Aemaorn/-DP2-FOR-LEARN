namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PExpenseDisbursement;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PExpenseDisbursement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PExpenseDisbursementAssigneeConfiguration : EntityTypeConfigurationBase<PExpenseDisbursementAssignee, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PExpenseDisbursementAssignee> builder)
    {
        builder.ToTable(nameof(PExpenseDisbursementAssignee), nameof(Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PExpenseDisbursementId)
               .HasVogenConversion();

        builder.AssigneeInfo();
        builder.OwnsAuditInfo();
    }
}