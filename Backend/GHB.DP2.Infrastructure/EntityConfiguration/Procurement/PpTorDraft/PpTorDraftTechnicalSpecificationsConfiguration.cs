namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpTorDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpTorDraftTechnicalSpecificationsConfiguration : EntityTypeConfigurationBase<PpTorDraftTechnicalSpecifications, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpTorDraftTechnicalSpecifications> builder)
    {
        builder.ToTable(nameof(PpTorDraftTechnicalSpecifications), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.Sequence);

        builder.Property(p => p.Name);

        builder.Property(p => p.Description);

        builder.Property(p => p.Quantity);

        builder.HasOne(p => p.PpTorDraft)
            .WithMany(p => p.PpTorDraftTechnicalSpecifications)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Unit)
               .WithMany()
               .HasForeignKey(p => p.UnitCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired(false);

        builder.OwnsAuditInfo();
    }
}