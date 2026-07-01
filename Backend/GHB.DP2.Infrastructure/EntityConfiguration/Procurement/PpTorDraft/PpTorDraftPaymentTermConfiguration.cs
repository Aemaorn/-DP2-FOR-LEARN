namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpTorDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpTorDraftPaymentTermConfiguration : EntityTypeConfigurationBase<PpTorDraftPaymentTerm, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpTorDraftPaymentTerm> builder)
    {
        builder.ToTable(nameof(PpTorDraftPaymentTerm), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasVogenConversion();

        builder.Property(p => p.PaymentPercent);

        builder.Property(p => p.Description);

        builder.Property(p => p.Period);

        builder.Property(p => p.IsMA);

        builder.HasOne(p => p.PpTorDraft)
            .WithMany(t => t.PpTorDraftPaymentTerms)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PpTorDraftPaymentTermDetails)
            .WithOne(p => p.PpTorDraftPaymentTerm)
            .HasForeignKey("PpTorDraftPaymentTermId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.ProRateType)
               .WithMany()
               .HasForeignKey(p => p.ProRateTypeCode)
               .HasPrincipalKey(p => p.Code);

        builder.HasOne(p => p.PeriodType)
               .WithMany()
               .HasForeignKey(p => p.PeriodTypeCode)
               .HasPrincipalKey(p => p.Code);

        builder.HasOne(p => p.TotalPeriodType)
               .WithMany()
               .HasForeignKey(p => p.TotalPeriodTypeCode)
               .HasPrincipalKey(p => p.Code);

        builder.OwnsAuditInfo();
    }
}