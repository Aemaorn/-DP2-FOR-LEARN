namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PPrincipleApprovalRental;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PPrincipleApprovalRentalRentalAnalysisConfiguration : EntityTypeConfigurationBase<PPrincipleApprovalRentalRentalAnalysis, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PPrincipleApprovalRentalRentalAnalysis> builder)
    {
        builder.ToTable(nameof(PPrincipleApprovalRentalRentalAnalysis), nameof(Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.Sequence)
               .IsRequired();

        builder.Property(p => p.Type)
               .IsRequired();

        builder.Property(p => p.Description)
               .IsRequired();

        builder.HasMany(m => m.PrincipleApprovalRentalRentalAnalysisDetails)
               .WithOne(d => d.RentalAnalysis)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
    }
}