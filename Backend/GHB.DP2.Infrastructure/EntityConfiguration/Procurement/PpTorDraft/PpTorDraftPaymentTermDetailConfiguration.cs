namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpTorDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpTorDraftPaymentTermDetailConfiguration : EntityTypeConfigurationBase<PpTorDraftPaymentTermDetail, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpTorDraftPaymentTermDetail> builder)
    {
        builder.ToTable(nameof(PpTorDraftPaymentTermDetail), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PpTorDraftPaymentTermId)
               .HasConversion<PpTorDraftPaymentTermId.EfCoreValueConverter, PpTorDraftPaymentTermId.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(p => p.TermNumber);

        builder.Property(p => p.Percent);

        builder.Property(p => p.Description);

        builder.HasOne(p => p.PpTorDraftPaymentTerm)
               .WithMany(p => p.PpTorDraftPaymentTermDetails)
               .HasForeignKey(p => p.PpTorDraftPaymentTermId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsAuditInfo();
    }
}