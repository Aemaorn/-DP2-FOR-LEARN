namespace GHB.DP2.Infrastructure.EntityConfiguration.Procurement.PpPurchaseRequisition;

using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class PpPurchaseRequisitionConfiguration : IEntityTypeConfiguration<PpPurchaseRequisition>
{
    public void Configure(EntityTypeBuilder<PpPurchaseRequisition> builder)
    {
        builder.ToTable(nameof(Domain.Procurement.PpPurchaseRequisition.PpPurchaseRequisition), nameof(Domain.Procurement.Procurement));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .HasVogenConversion();

        builder.Property(x => x.ProcurementId)
               .HasConversion<ProcurementId.EfCoreValueConverter, ProcurementId.EfCoreValueComparer>();

        builder.Property(x => x.PurchaseRequisitionNumber)
               .HasVogenConversion()
               .HasMaxLength(50);

        builder.Property(x => x.EgpNumber)
               .HasMaxLength(50);

        builder.Property(x => x.PrNumber)
               .HasMaxLength(50);

        builder.Property(x => x.Description);

        builder.Property(x => x.PriceReasonablenessInfo);

        builder.Property(x => x.Telephone)
               .HasMaxLength(50);

        builder.Property(x => x.SendEditRemark);

        builder.Property(x => x.IsMigration)
               .HasDefaultValue(false);

        builder.Property(x => x.MedianPriceAmount);

        builder.Property(p => p.EvaluationCriteriaCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(x => x.DeliveryPeriod);

        builder.Property(p => p.DeliveryPeriodTypeCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(p => p.DeliveryConditionCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(x => x.HasFineRate);

        builder.Property(x => x.HasWarranty);

        builder.Property(x => x.WarrantyPeriod);

        builder.Property(p => p.WarrantyPeriodCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(p => p.WarrantyConditionCode)
               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

        builder.Property(x => x.HasContractGuarantee);

        builder.Property(x => x.HasInspectionCommittee);

        builder.Property(x => x.HasConstructionSupervisor);

        builder.Property(p => p.DeliveryDate);

        builder.Property(p => p.DocumentDate);

        builder.Property(p => p.PaymentTypeCode);

        builder.Property(x => x.Status)
               .HasConversion(new EnumToStringConverter<PurchaseRequisitionStatus>())
               .IsRequired();

        builder.HasOne(x => x.PaymentType)
         .WithMany()
         .HasForeignKey(x => x.PaymentTypeCode)
         .HasPrincipalKey(x => x.Code);

        builder.HasOne(x => x.Procurement)
               .WithMany(x => x.PurchaseRequisitions)
               .HasForeignKey(x => x.ProcurementId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TorDraft)
               .WithMany()
               .HasForeignKey(x => x.TorDraftId);

        builder.HasMany(x => x.Budgets)
               .WithOne(x => x.PpPurchaseRequisition)
               .HasForeignKey(x => x.PpPurchaseRequisitionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Warranties)
               .WithOne(x => x.PpPurchaseRequisition)
               .HasForeignKey(x => x.PpPurchaseRequisitionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.PaymentTerms)
               .WithOne(x => x.PpPurchaseRequisition)
               .HasForeignKey(x => x.PpPurchaseRequisitionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.FineRates)
               .WithOne(x => x.PpPurchaseRequisition)
               .HasForeignKey(x => x.PpPurchaseRequisitionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Committees)
               .WithOne(x => x.PpPurchaseRequisition)
               .HasForeignKey(x => x.PpPurchaseRequisitionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Acceptors)
               .WithOne(a => a.PpPurchaseRequisition)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.EvaluationCriteria)
               .WithMany()
               .HasForeignKey(x => x.EvaluationCriteriaCode)
               .HasPrincipalKey(x => x.Code);

        builder.HasOne(x => x.DeliveryPeriodType)
               .WithMany()
               .HasForeignKey(x => x.DeliveryPeriodTypeCode)
               .HasPrincipalKey(x => x.Code);

        builder.HasOne(x => x.DeliveryCondition)
               .WithMany()
               .HasForeignKey(x => x.DeliveryConditionCode)
               .HasPrincipalKey(x => x.Code);

        builder.HasOne(x => x.WarrantyPeriodType)
               .WithMany()
               .HasForeignKey(x => x.WarrantyPeriodCode)
               .HasPrincipalKey(x => x.Code);

        builder.HasOne(x => x.WarrantyCondition)
               .WithMany()
               .HasForeignKey(x => x.WarrantyConditionCode)
               .HasPrincipalKey(x => x.Code);

        builder.HasMany(p => p.TechnicalSpecifications)
               .WithOne(p => p.PurchaseRequisition)
               .HasForeignKey(p => p.PpPurchaseRequisitionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnDocumentHistory(p => p.DocumentHistories, documentBuilder =>
        {
            documentBuilder.ToTable(nameof(PurchaseRequisitionDocumentHistory), nameof(Procurement));

            documentBuilder.HasKey(d => d.Id);

            documentBuilder.Property(d => d.Id)
                           .HasVogenConversion();

            documentBuilder.Property(d => d.StatusState)
                           .HasConversion(new EnumToStringConverter<PurchaseRequisitionStatus>());
        });

        builder.HasActivityInfo();
        builder.OwnsAuditInfo();
        builder.HasSoftDelete();
    }
}