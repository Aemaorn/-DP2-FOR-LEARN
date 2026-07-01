namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpPurchaseRequisition;

using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PpPurchaseRequisitionFineRateConfiguration : IEntityTypeConfiguration<PpPurchaseRequisitionFineRate>
{
    public void Configure(EntityTypeBuilder<PpPurchaseRequisitionFineRate> builder)
    {
        builder.ToTable(nameof(PpPurchaseRequisitionFineRate), nameof(Procurement));

        builder.HasKey(x => x.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(p => p.PpPurchaseRequisitionId)
               .HasConversion<PpPurchaseRequisitionId.EfCoreValueConverter, PpPurchaseRequisitionId.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(x => x.Sequence)
               .IsRequired();

        builder.Property(x => x.Rate)
               .IsRequired();

        builder.Property(p => p.PeriodTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(p => p.ConditionCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
               .IsRequired();

        builder.Property(x => x.ConditionOther);

        builder.HasOne(x => x.PeriodType)
               .WithMany()
               .HasForeignKey(x => x.PeriodTypeCode)
               .HasPrincipalKey(x => x.Code);

        builder.HasOne(x => x.Condition)
               .WithMany()
               .HasForeignKey(x => x.ConditionCode)
               .HasPrincipalKey(x => x.Code);

        builder.OwnsAuditInfo();
    }
}