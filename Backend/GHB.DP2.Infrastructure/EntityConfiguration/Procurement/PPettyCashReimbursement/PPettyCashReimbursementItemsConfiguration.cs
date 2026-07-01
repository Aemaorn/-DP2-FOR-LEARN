namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPettyCashReimbursement;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPettyCashReimbursement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPettyCashReimbursementItemsConfiguration : EntityTypeConfigurationBase<PPettyCashReimbursementItems, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPettyCashReimbursementItems> builder)
    {
        builder.ToTable(nameof(PPettyCashReimbursementItems), nameof(Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}