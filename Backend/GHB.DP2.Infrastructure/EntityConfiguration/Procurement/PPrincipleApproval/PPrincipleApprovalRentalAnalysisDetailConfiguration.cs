namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApproval;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPrincipleApprovalRentalAnalysisDetailConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalRentalAnalysisDetail, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPrincipleApprovalRentalAnalysisDetail> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalRentalAnalysisDetail), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PPrincipleApprovalRentalAnalysisId)
               .HasConversion<PPrincipleApprovalRentalAnalysisId.EfCoreValueConverter, PPrincipleApprovalRentalAnalysisId.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(p => p.Year)
               .IsRequired();

        builder.Property(p => p.Amount)
               .IsRequired();

        builder.OwnsAuditInfo();
    }
}