namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration.Dto;

using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;
using GreatFriends.ThaiBahtText;

public record AdjustContractDurationInfo(
    Guid? AdjustContractDurationId,
    ContractAmendmentExtendChangeType? ChangeType,
    DateTimeOffset? WorkStartDate,
    string? WorkStartDateText,
    DateTimeOffset? NewEndDate,
    string? NewEndDateText,
    string? PaymentTypeCode,
    IEnumerable<AdjustContractDurationPaymentTermInfo> PaymentTerms)
{
    public static AdjustContractDurationInfo Map(CaContractDraftVendor vendor)
    {
        return new AdjustContractDurationInfo(
            null,
            null,
            null,
            null,
            null,
            null,
            vendor.Payment?.TypeCode?.Value,
            vendor.PaymentTerms.Map(AdjustContractDurationPaymentTermInfo.Map));
    }

    public static AdjustContractDurationInfo Map(CamContractAmendmentExtendChange entity)
    {
        return new AdjustContractDurationInfo(
            entity.Id.Value,
            entity.ChangeType,
            entity.WorkStartDate,
            entity.WorkStartDate.ToThaiDateString(includeBuddhistEra: false),
            entity.NewEndDate,
            entity.NewEndDate.ToThaiDateString(includeBuddhistEra: false),
            entity.PaymentTypeCode?.Value,
            entity.PaymentTerms.Map(AdjustContractDurationPaymentTermInfo.Map));
    }
}

public record AdjustContractDurationPaymentTermInfo(
    Guid? PaymentTermId,
    int PaymentTermNo,
    int LeadTime,
    DateTimeOffset? DeliveryDate,
    string? DeliveryDateText,
    decimal InstallmentPercent,
    decimal Amount,
    string? AmountFormat,
    string? AmountText,
    decimal AdvanceDeductionAmount,
    string? AdvanceDeductionAmountFormat,
    string? AdvanceDeductionAmountText,
    decimal PerformanceDeductionAmount,
    string? PerformanceDeductionAmountFormat,
    string? PerformanceDeductionAmountText,
    string Description,
    bool IsDelivery)
{
    public CamContractAmendmentExtendChangePaymentTerm MapToEntity()
    {
        if (this.PaymentTermId.HasValue)
        {
            return CamContractAmendmentExtendChangePaymentTerm
                   .Create(this.PaymentTermId.Value)
                   .SetPaymentTermNo(this.PaymentTermNo)
                   .SetLeadTime(this.LeadTime)
                   .SetDeliveryDate(this.DeliveryDate)
                   .SetInstallmentPercent(this.InstallmentPercent)
                   .SetAmount(this.Amount)
                   .SetAmount(this.AdvanceDeductionAmount)
                   .SetPerformanceDeductionAmount(this.PerformanceDeductionAmount)
                   .SetDescription(this.Description);
        }

        return CamContractAmendmentExtendChangePaymentTerm
               .Create()
               .SetLeadTime(this.LeadTime)
               .SetPaymentTermNo(this.PaymentTermNo)
               .SetDeliveryDate(this.DeliveryDate)
               .SetInstallmentPercent(this.InstallmentPercent)
               .SetAmount(this.Amount)
               .SetAmount(this.AdvanceDeductionAmount)
               .SetPerformanceDeductionAmount(this.PerformanceDeductionAmount)
               .SetDescription(this.Description);
    }

    public static AdjustContractDurationPaymentTermInfo Map(CaContractDraftPaymentTerm entity)
    {
        return new AdjustContractDurationPaymentTermInfo(
            null,
            entity.PaymentTermNo ?? 0,
            entity.LeadTime ?? 0,
            entity.DeliveryDate,
            entity.DeliveryDate.ToThaiDateString(includeBuddhistEra: false),
            entity.InstallmentPercentage ?? 0,
            entity.Amount ?? 0,
            entity.Amount.ToCurrencyStringWithComma(),
            entity.Amount.ThaiBahtText(),
            entity.AdvanceDeductionAmount ?? 0,
            entity.AdvanceDeductionAmount.ToCurrencyStringWithComma(),
            entity.AdvanceDeductionAmount.ThaiBahtText(),
            entity.PerformanceDeductionAmount ?? 0,
            entity.PerformanceDeductionAmount.ToCurrencyStringWithComma(),
            entity.PerformanceDeductionAmount.ThaiBahtText(),
            entity.Description ?? string.Empty,
            entity.DeliveryAcceptancePeriods.Any());
    }

    public static AdjustContractDurationPaymentTermInfo Map(CamContractAmendmentExtendChangePaymentTerm entity)
    {
        return new AdjustContractDurationPaymentTermInfo(
            entity.Id.Value,
            entity.PaymentTermNo,
            entity.LeadTime,
            entity.DeliveryDate,
            entity.DeliveryDate.ToThaiDateString(includeBuddhistEra: false),
            entity.InstallmentPercent,
            entity.Amount,
            entity.Amount.ToCurrencyStringWithComma(),
            entity.Amount.ThaiBahtText(),
            entity.AdvanceDeductionAmount,
            entity.AdvanceDeductionAmount.ToCurrencyStringWithComma(),
            entity.AdvanceDeductionAmount.ThaiBahtText(),
            entity.PerformanceDeductionAmount,
            entity.PerformanceDeductionAmount.ToCurrencyStringWithComma(),
            entity.PerformanceDeductionAmount.ThaiBahtText(),
            entity.Description,
            entity.IsDelivery);
    }
}