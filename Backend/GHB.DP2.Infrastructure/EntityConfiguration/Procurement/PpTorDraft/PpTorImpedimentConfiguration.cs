namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpTorDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

public class PpTorImpedimentConfiguration : EntityTypeConfigurationBase<PpTorImpediment, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpTorImpediment> builder)
    {
        builder.ToTable(nameof(PpTorImpediment), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(a => a.Sequence);

        builder.Property(a => a.Description);
        builder.Property(a => a.ImpedimentValue);

        builder.HasOne(p => p.PpTorDraft)
               .WithMany(t => t.PpTorImpediments)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
    }
}
