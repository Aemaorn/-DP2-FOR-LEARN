namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApprovalRental;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPrincipleApprovalRentalRentalAnalysisDetailConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalRentalRentalAnalysisDetail, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPrincipleApprovalRentalRentalAnalysisDetail> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalRentalRentalAnalysisDetail), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.Year)
               .IsRequired();

        builder.Property(p => p.Amount)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}