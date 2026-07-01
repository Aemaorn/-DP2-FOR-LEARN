namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpTorDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpTorDraftWarrantyConfiguration : EntityTypeConfigurationBase<PpTorDraftWarranty, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<PpTorDraftWarranty> builder)
    {
        builder.ToTable(nameof(PpTorDraftWarranty), nameof(GHB.DP2.Domain.Procurement.Procurement));
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasVogenConversion();

        builder.Property(p => p.HasWarranty);

        builder.Property(p => p.Period);

        builder.Property(p => p.PeriodTypeCode)
               .IsRequired(false);

        builder.Property(p => p.ConditionOther);

        builder.HasOne(p => p.PpTorDraft)
            .WithMany(t => t.PpTorDraftWarranties)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.PeriodType)
               .WithMany()
               .HasForeignKey(p => p.PeriodTypeCode)
               .HasPrincipalKey(p => p.Code)
               .IsRequired(false);

        builder.OwnsAuditInfo();
    }
}