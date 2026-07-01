namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpPurchaseRequisition;

using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpPurchaseRequisitionPaymentTermConfiguration : IEntityTypeConfiguration<PpPurchaseRequisitionPaymentTerm>
{
    public void Configure(EntityTypeBuilder<PpPurchaseRequisitionPaymentTerm> builder)
    {
        builder.ToTable(nameof(PpPurchaseRequisitionPaymentTerm), nameof(Procurement));

        builder.HasKey(x => x.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PpPurchaseRequisitionId)
               .HasConversion<PpPurchaseRequisitionId.EfCoreValueConverter, PpPurchaseRequisitionId.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(x => x.TermNumber);

        builder.Property(x => x.Percent);

        builder.Property(x => x.Period);

        builder.Property(x => x.Description);

        builder.Property(x => x.IsMA);

        builder.Property(x => x.PaymentTypeCode);

        builder.Property(x => x.PeriodTypeCode);

        builder.Property(x => x.TotalPeriod);

        builder.Property(x => x.TotalPeriodTypeCode);

        builder.HasOne(x => x.PaymentType)
               .WithMany()
               .HasForeignKey(x => x.PaymentTypeCode)
               .HasPrincipalKey(x => x.Code);

        builder.HasOne(x => x.PeriodType)
              .WithMany()
              .HasForeignKey(x => x.PeriodTypeCode)
              .HasPrincipalKey(x => x.Code);

        builder.HasOne(x => x.TotalPeriodType)
              .WithMany()
              .HasForeignKey(x => x.TotalPeriodTypeCode)
              .HasPrincipalKey(x => x.Code);

        builder.OwnsAuditInfo();
    }
}