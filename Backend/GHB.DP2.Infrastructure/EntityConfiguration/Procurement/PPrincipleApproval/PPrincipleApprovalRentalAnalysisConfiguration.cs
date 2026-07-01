namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApproval;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPrincipleApprovalRentalAnalysisConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalRentalAnalysis, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPrincipleApprovalRentalAnalysis> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalRentalAnalysis), nameof(Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.Property(p => p.Type)
               .IsRequired();

        builder.Property(p => p.Description)
               .IsRequired();

        builder.OwnsAuditInfo();

        builder.HasOne(b => b.PPrincipleApproval)
               .WithMany(t => t.PrincipleApprovalRentalAnalyses)
               .OnDelete(DeleteBehavior.Cascade);
    }
}