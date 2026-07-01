namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApproval;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPrincipleApprovalConsoPerfSupportDataConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalConsoPerfSupportData, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPrincipleApprovalConsoPerfSupportData> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalConsoPerfSupportData), nameof(Procurement));

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