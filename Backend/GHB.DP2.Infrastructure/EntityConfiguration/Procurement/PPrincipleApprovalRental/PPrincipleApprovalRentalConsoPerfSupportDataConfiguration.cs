namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApprovalRental;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPrincipleApprovalRentalConsoPerfSupportDataConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalRentalConsoPerfSupportData, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPrincipleApprovalRentalConsoPerfSupportData> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalRentalConsoPerfSupportData), nameof(Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.TransactionVolume);

        builder.Property(p => p.ActivityDescription);

        builder.Property(p => p.PeriodYear);

        builder.Property(p => p.StartMonth);

        builder.Property(p => p.EndMonth);

        builder.OwnsAuditInfo();
    }
}