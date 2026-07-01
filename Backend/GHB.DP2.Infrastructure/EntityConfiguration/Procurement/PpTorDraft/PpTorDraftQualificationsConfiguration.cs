namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpTorDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpTorDraftQualificationsConfiguration : EntityTypeConfigurationBase<PpTorDraftQualifications, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpTorDraftQualifications> builder)
    {
        builder.ToTable(nameof(PpTorDraftQualifications), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasVogenConversion();

        builder.Property(p => p.Sequence);

        builder.Property(p => p.Description);

        builder.HasOne(p => p.PpTorDraft)
            .WithMany(p => p.PpTorDraftQualifications)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
    }
}