namespace GHB.DP2.Infrastructure.EntityConfiguration.ContractManagement.CaContractDraftVendorEdit;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class CaContractDraftEditTermsConditionsConfiguration : EntityTypeConfigurationBase<CaContractDraftEditTermsConditions, Dp2DbContext>
{
    protected override void EntityConfigure(EntityTypeBuilder<CaContractDraftEditTermsConditions> builder)
    {
        builder.ToTable(nameof(CaContractDraftEditTermsConditions), nameof(ContractManagement));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasVogenConversion();

        builder.Property(c => c.ContractDraftVendorEditId)
               .HasVogenConversion();

        builder.HasOne(e => e.DefectWarrantyTypeNavigation)
               .WithMany()
               .HasForeignKey(e => e.DefectWarrantyTypeCode)
               .HasPrincipalKey(c => c.Code)
               .IsRequired(false);

        builder.OwnsOne(
            c => c.AdvancePayment,
            advancePaymentBuilder =>
            {
                advancePaymentBuilder.Property(c => c.IsIncluded)
                                     .HasColumnName("AdvancePaymentIsIncluded");

                advancePaymentBuilder.Property(c => c.Amount)
                                     .HasColumnName("AdvancePaymentAmount");

                advancePaymentBuilder.Property(c => c.Percentage)
                                     .HasColumnName("AdvancePaymentPercentage");

                advancePaymentBuilder.Property(c => c.DueDate)
                                     .HasColumnName("AdvancePaymentDueDate");

                advancePaymentBuilder.Property(c => c.ConditionCode)
                                     .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
                                     .HasColumnName("AdvancePaymentConditionCode");
            });

        builder.OwnsOne(
            c => c.RetentionPayment,
            retentionPaymentBuilder =>
            {
                retentionPaymentBuilder.Property(c => c.IsIncluded)
                                       .HasColumnName("RetentionIsIncluded");

                retentionPaymentBuilder.Property(c => c.Amount)
                                       .HasColumnName("RetentionPaymentAmount");

                retentionPaymentBuilder.Property(c => c.Percentage)
                                       .HasColumnName("RetentionPaymentPercentage");
            });

        builder.OwnsOne(
            c => c.Warranty,
            warrantyBuilder =>
            {
                warrantyBuilder.Property(c => c.HasWarranty)
                               .HasColumnName("HasWarranty");

                warrantyBuilder.OwnsOne(
                    c => c.WarrantyPeriod,
                    warrantyPeriodBuilder =>
                    {
                        warrantyPeriodBuilder.Property(w => w.Day)
                                             .HasColumnName("WarrantyPeriodDay");

                        warrantyPeriodBuilder.Property(w => w.Month)
                                             .HasColumnName("WarrantyPeriodMonth");

                        warrantyPeriodBuilder.Property(w => w.Year)
                                             .HasColumnName("WarrantyPeriodYear");
                    });

                warrantyBuilder.Property(c => c.WarrantyConditionCode)
                               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
                               .HasColumnName("WarrantyConditionCode");

                warrantyBuilder.HasOne(c => c.WarrantyCondition)
                               .WithMany()
                               .HasForeignKey(c => c.WarrantyConditionCode)
                               .HasPrincipalKey(p => p.Code)
                               .OnDelete(DeleteBehavior.Restrict);

                warrantyBuilder.OwnsOne(
                    c => c.FixingDeadlinePeriod,
                    fixingDeadlinePeriodBuilder =>
                    {
                        fixingDeadlinePeriodBuilder.Property(f => f.Day)
                                                   .HasColumnName("FixingDeadlinePeriodDay");

                        fixingDeadlinePeriodBuilder.Property(f => f.Month)
                                                   .HasColumnName("FixingDeadlinePeriodMonth");

                        fixingDeadlinePeriodBuilder.Property(f => f.Year)
                                                   .HasColumnName("FixingDeadlinePeriodYear");
                    });

                warrantyBuilder.Property(c => c.WarrantyMonthlyAllowedDowntimeHours)
                               .HasColumnName("WarrantyMonthlyAllowedDowntimeHours");

                warrantyBuilder.Property(c => c.WarrantyDowntimePercentPerMonth)
                               .HasColumnName("WarrantyDowntimePercentPerMonth");

                warrantyBuilder.Property(c => c.WarrantyPenaltyPerHour)
                               .HasColumnName("WarrantyPenaltyPerHour");

                warrantyBuilder.Property(c => c.DowntimeResolutionHours)
                               .HasColumnName("DowntimeResolutionHours");

                warrantyBuilder.Property(c => c.DowntimeResolutionDay)
                               .HasColumnName("DowntimeResolutionDay");

                warrantyBuilder.Property(c => c.RepairCompletionHours)
                               .HasColumnName("RepairCompletionHours");

                warrantyBuilder.Property(c => c.RepairCompletionDay)
                               .HasColumnName("RepairCompletionDay");

                warrantyBuilder.Property(c => c.RepairDelayPenaltyPercentPerHour)
                               .HasColumnName("RepairDelayPenaltyPercentPerHour");

                warrantyBuilder.Property(c => c.MaxMonthlyMalfunction);

                warrantyBuilder.Property(c => c.MaxMonthlyMalfunctionTypeCode)
                               .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>();

                warrantyBuilder.HasOne(c => c.MaxMonthlyMalfunctionTypeCodeNavigation)
                               .WithMany()
                               .HasForeignKey(c => c.MaxMonthlyMalfunctionTypeCode)
                               .HasPrincipalKey(p => p.Code)
                               .OnDelete(DeleteBehavior.Restrict);

                warrantyBuilder.Property(c => c.MaxMonthlyMalfunctionRate);

                warrantyBuilder.Property(c => c.MaxMonthlyMalfunctionPenaltyPercentageRate);

                warrantyBuilder.Property(c => c.MaxMonthlyMalfunctionPenaltyPerHour);

                warrantyBuilder.Property(c => c.MaxMonthlyMalfunctionPenaltyDueDays);

                warrantyBuilder.Property(c => c.WarrantyStartDate);

                warrantyBuilder.Property(c => c.WarrantyEndDate);

                warrantyBuilder.Property(c => c.WarrantyMaintenanceCount);

                warrantyBuilder.HasOne(c => c.WarrantyMaintenanceType)
                               .WithMany()
                               .HasForeignKey(c => c.WarrantyMaintenanceTypeCode)
                               .HasPrincipalKey(p => p.Code)
                               .OnDelete(DeleteBehavior.Restrict);
            });

        builder.OwnsOne(
            c => c.Penalty,
            penaltyBuilder =>
            {
                penaltyBuilder.Property(p => p.IsPenalty)
                              .HasColumnName("PenaltyIsPenalty");

                penaltyBuilder.Property(p => p.TypeCode)
                              .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
                              .HasColumnName("PenaltyTypeCode");

                penaltyBuilder.Property(p => p.Rate)
                              .HasColumnName("PenaltyRate");

                penaltyBuilder.Property(p => p.Amount)
                              .HasColumnName("PenaltyAmount");

                penaltyBuilder.Property(p => p.RateTypeCode)
                              .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
                              .HasColumnName("PenaltyRateTypeCode");

                penaltyBuilder.HasOne(p => p.Type)
                              .WithMany()
                              .HasForeignKey(p => p.TypeCode)
                              .HasPrincipalKey(p => p.Code)
                              .OnDelete(DeleteBehavior.Restrict);

                penaltyBuilder.HasOne(p => p.RateType)
                              .WithMany()
                              .HasForeignKey(p => p.RateTypeCode)
                              .HasPrincipalKey(p => p.Code)
                              .OnDelete(DeleteBehavior.Restrict);
            });

        builder.OwnsOne(
            c => c.Guarantee,
            guaranteeBuilder =>
            {
                guaranteeBuilder.Property(g => g.IsSubmitted)
                                .HasColumnName("IsGuaranteeSubmitted");

                guaranteeBuilder.Property(g => g.TypeCode)
                                .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
                                .HasColumnName("GuaranteeTypeCode");

                guaranteeBuilder.Property(g => g.Amount)
                                .HasColumnName("GuaranteeAmount");

                guaranteeBuilder.Property(g => g.Percentage)
                                .HasColumnName("GuaranteePercentage");

                guaranteeBuilder.HasOne(g => g.Type)
                                .WithMany()
                                .HasForeignKey(g => g.TypeCode)
                                .HasPrincipalKey(p => p.Code)
                                .OnDelete(DeleteBehavior.Restrict);

                guaranteeBuilder.Property(c => c.ReferenceNumber)
                                .HasColumnName("GuaranteeReferenceNumber");

                guaranteeBuilder.Property(c => c.GuaranteeDate)
                                .HasColumnName("GuaranteeDate");

                guaranteeBuilder.Property(c => c.BankCode)
                                .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
                                .HasColumnName("GuaranteeBank");

                guaranteeBuilder.HasOne(g => g.Bank)
                                .WithMany()
                                .HasForeignKey(g => g.BankCode)
                                .HasPrincipalKey(p => p.Code)
                                .OnDelete(DeleteBehavior.Restrict);

                guaranteeBuilder.Property(c => c.BankBranch)
                                .HasColumnName("GuaranteeBankBranch");

                guaranteeBuilder.Property(c => c.BankAccountNumber)
                                .HasColumnName("GuaranteeBankAccountNumber");

                guaranteeBuilder.Property(c => c.BankCollateralStartDate)
                                .HasColumnName("GuaranteeBankCollateralStartDate");

                guaranteeBuilder.Property(c => c.BankCollateralEndDate)
                                .HasColumnName("GuaranteeBankCollateralEndDate");

                guaranteeBuilder.Property(c => c.OtherDetails)
                                .HasColumnName("GuaranteeOtherDetails");
            });

        builder.OwnsOne(
            c => c.RedeliveryCorrection,
            redeliveryCorrectionBuilder =>
            {
                redeliveryCorrectionBuilder.Property(r => r.Type)
                                           .HasConversion(new EnumToStringConverter<RedeliveryType>())
                                           .HasColumnName("RedeliveryCorrectionType");

                redeliveryCorrectionBuilder.Property(r => r.Description)
                                           .HasColumnName("RedeliveryCorrectionDescription");

                redeliveryCorrectionBuilder.OwnsOne(
                    r => r.RentalDuration,
                    rentalDurationBuilder =>
                    {
                        rentalDurationBuilder.Property(rd => rd.Day)
                                             .HasColumnName("RedeliveryCorrectionRentalDurationDay");

                        rentalDurationBuilder.Property(rd => rd.Month)
                                             .HasColumnName("RedeliveryCorrectionRentalDurationMonth");

                        rentalDurationBuilder.Property(rd => rd.Year)
                                             .HasColumnName("RedeliveryCorrectionRentalDurationYear");
                    });

                redeliveryCorrectionBuilder.Property(r => r.RedeliveryDeadline)
                                           .HasColumnName("RedeliveryCorrectionDeadline");

                redeliveryCorrectionBuilder.Property(r => r.RedeliveryDeadlineTypeCode)
                                           .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
                                           .HasColumnName("RedeliveryCorrectionDeadlineTypeCode");

                redeliveryCorrectionBuilder.Property(r => r.CorrectionDue)
                                           .HasColumnName("RedeliveryCorrectionDue");

                redeliveryCorrectionBuilder.Property(r => r.CorrectionDueTypeCode)
                                           .HasConversion<ParameterCode.EfCoreValueConverter, ParameterCode.EfCoreValueComparer>()
                                           .HasColumnName("RedeliveryCorrectionDueTypeCode");

                redeliveryCorrectionBuilder.HasOne(r => r.RedeliveryDeadlineType)
                                           .WithMany()
                                           .HasForeignKey(r => r.RedeliveryDeadlineTypeCode)
                                           .HasPrincipalKey(p => p.Code)
                                           .OnDelete(DeleteBehavior.Restrict);

                redeliveryCorrectionBuilder.HasOne(r => r.CorrectionDueType)
                                           .WithMany()
                                           .HasForeignKey(r => r.CorrectionDueTypeCode)
                                           .HasPrincipalKey(p => p.Code)
                                           .OnDelete(DeleteBehavior.Restrict);
            });

        builder.OwnsAuditInfo();
    }
}
