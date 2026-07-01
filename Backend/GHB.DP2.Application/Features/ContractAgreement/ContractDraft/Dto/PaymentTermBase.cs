namespace GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Dto;

using System.Linq;
using System.Text.Json.Serialization;
using Codehard.Common.Extensions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.SystemUtility;
using GreatFriends.ThaiBahtText;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Contract), nameof(Contract))]
[JsonDerivedType(typeof(Term), nameof(Term))]
[JsonDerivedType(typeof(TremNotType), nameof(TremNotType))]
public abstract class PaymentBase
{
    public abstract CaContractDraftVendor MapToEntity(CaContractDraftVendor vendor);

    public static PaymentBase? MapToModel(CaContractDraftVendor vendor, PpTorDraft? tor)
    {
        if (vendor.PaymentTerms.Any())
        {
            return Term.FromEntity(vendor);
        }

        var details = tor?.PpTorDraftPaymentTerms?
                         .FirstOrDefault()?
                         .PpTorDraftPaymentTermDetails?
                         .Where(x => x != null)
                         .Select((x, index) => PaymentTermDetail.FromEntityTorDraftPaymentTermDetail(x, vendor.Budget, index))
                         .ToArray()
                      ?? [];

        return new Term
        {
            DueDay = 0,
            RedeliveryTypeCode = vendor.Payment.RedeliveryDateCode?.Value ?? string.Empty,
            PaymentTypeCode = vendor.Payment?.TypeCode?.Value ?? string.Empty,
            Details = details,
        };
    }
}

public class Contract : PaymentBase
{
    public int DueDay { get; init; }

    public string RedeliveryTypeCode { get; init; }

    public override CaContractDraftVendor MapToEntity(CaContractDraftVendor vendor)
    {
        return
            vendor.SetPayment(
                new Payment(
                    null,
                    this.DueDay,
                    ParameterCode.From(this.RedeliveryTypeCode)));
    }

    public static Contract FromEntity(Payment entity)
    {
        return new Contract
        {
            DueDay = entity.DueDays ?? 0,
            RedeliveryTypeCode = entity.RedeliveryDateCode?.Value ?? string.Empty,
        };
    }
}

public class Term : PaymentBase
{
    public string? PaymentTypeCode { get; init; }

    public PaymentTermDetail[] Details { get; init; }

    public int? DueDay { get; init; }

    public string? RedeliveryTypeCode { get; init; }

    public override CaContractDraftVendor MapToEntity(CaContractDraftVendor vendor)
    {
        var draftVendor =
            vendor.SetPayment(
                new Payment(
                    !string.IsNullOrWhiteSpace(this.PaymentTypeCode) ? ParameterCode.From(this.PaymentTypeCode) : null,
                    this.DueDay,
                    !string.IsNullOrWhiteSpace(this.RedeliveryTypeCode) ? ParameterCode.From(this.RedeliveryTypeCode) : null));

        _ = vendor.SetPaymentTerm(
            this.Details.Map(d =>
                d.MapToEntity()));

        return draftVendor;
    }

    public static Term FromEntity(CaContractDraftVendor vendor, IEnumerable<PpPurchaseRequisitionPaymentTerm>? prPaymentTerms = null)
    {
        var isCanEditLeadTime = true;

        if (prPaymentTerms != null && prPaymentTerms.Any())
        {
            var vendorPaymentTypeCode = vendor.Payment?.TypeCode?.Value;

            var vendorIsMa = prPaymentTerms
                .FirstOrDefault(pt => pt.PaymentTypeCode?.Value == vendorPaymentTypeCode)?.IsMA;

            var hasSplitPayment002InGroup = prPaymentTerms
                .Any(pt => pt.IsMA == vendorIsMa && pt.PaymentTypeCode?.Value == "SplitPayment002");

            isCanEditLeadTime = !hasSplitPayment002InGroup;
        }

        return new Term
        {
            DueDay = vendor.Payment?.DueDays ?? 0,
            RedeliveryTypeCode = vendor.Payment?.RedeliveryDateCode?.Value ?? null,
            PaymentTypeCode = vendor.Payment?.TypeCode?.Value ?? null,
            Details =
            [
                .. vendor.PaymentTerms
                         .OrderBy(x => x.Sequence)
                         .Select(x => PaymentTermDetail.FromEntity(x, isCanEditLeadTime: isCanEditLeadTime))
            ],
        };
    }
}

public class TremNotType : PaymentBase
{
    public PaymentTermDetail[] Details { get; init; }

    public override CaContractDraftVendor MapToEntity(CaContractDraftVendor vendor)
    {
        var draftVendor =
            vendor.SetPayment(
                new Payment(
                    null,
                    null,
                    null));

        _ = vendor.SetPaymentTerm(
            this.Details.Map(d =>
                d.MapToEntity()));

        return draftVendor;
    }

    public static TremNotType FromEntity(CaContractDraftVendor vendor)
    {
        return new TremNotType
        {
            Details =
            [
                .. vendor.PaymentTerms
                         .OrderBy(x => x.Sequence)
                         .Select(x => PaymentTermDetail.FromEntity(x))
            ],
        };
    }
}

public class PaymentTermDetail
{
    /// <summary>
    /// รหัส (ถ้าเป็นการแก้ไขจะต้องมีรหัส)
    /// </summary>
    public Guid? Id { get; init; }

    /// <summary>
    /// งวดที่
    /// </summary>
    public int? No { get; init; }

    /// <summary>
    /// ระยะเวลา(วัน)
    /// </summary>
    public int? LeadTime { get; init; }

    /// <summary>
    /// วันที่ส่งมอบ
    /// </summary>
    public DateTimeOffset? DeliveryDate { get; init; }

    public string? DeliveryDateFormat { get; init; }

    /// <summary>
    /// ร้อยละ
    /// </summary>
    public decimal? InstallmentPercentage { get; init; }

    /// <summary>
    /// จำนวนเงิน
    /// </summary>
    public decimal? Amount { get; init; }

    public string? AmountFormat { get; init; }

    public string? AmountText { get; init; }

    /// <summary>
    /// จำนวนเงินหักล่วงหน้า
    /// </summary>
    public decimal? AdvanceDeductionAmount { get; init; }

    public string? AdvanceDeductionAmountFormat { get; init; }

    public string? AdvanceDeductionAmountText { get; init; }

    /// <summary>
    /// จำนวนเงินหักประกันผลงาน
    /// </summary>
    public decimal? PerformanceDeductionAmount { get; init; }

    public string? PerformanceDeductionAmountFormat { get; init; }

    public string? PerformanceDeductionAmountText { get; init; }

    /// <summary>
    /// รายละเอียด
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// ลำดับ
    /// </summary>
    public int Sequence { get; init; }

    public ParameterCode PeriodTypeCode { get; init; }

    public bool IsCanEditLeadTime { get; init; }

    private string? workDescription;

    public string WorkDescription
    {
        get => this.workDescription ?? $"งวดที่ {this.No} เป็นจำนวนเงิน {this.Amount.ToCurrencyStringWithComma()} บาท ({this.Amount.ThaiBahtText()})";
        set => this.workDescription = value;
    }

    public static string GenerateWorkDescription(PaymentTermDetail detail, string? templateCode, bool isLastTerm)
    {
        var no = detail.No;
        var amount = detail.Amount.ToCurrencyStringWithComma();
        var amountText = detail.Amount.ThaiBahtText();
        var leadTime = detail.LeadTime;
        var leadTimeText = detail.LeadTime is not null ? detail.LeadTime.Value.ToThaiNumberText() : TemplatePlaceholders.MissingValue;
        var periodTypeName = GetPeriodTypeName(detail.PeriodTypeCode);
        var percentage = detail.InstallmentPercentage;
        var description = !string.IsNullOrWhiteSpace(detail.Description) ? detail.Description : TemplatePlaceholders.MissingValue;

        return templateCode switch
        {
            ContractFormatConstant.CFormat016 or ContractFormatConstant.CFormat013 => isLastTerm
                ? $"งวดสุดท้าย เป็นจำนวนเงิน {amount} บาท ({amountText}) เมื่อผู้รับจ้างได้ปฏิบัติงานทั้งหมดให้แล้วเสร็จเรียบร้อยตาม"
                : $"งวดที่ {no} เป็นจำนวนเงิน {amount} บาท ({amountText}) เมื่อผู้รับจ้างได้ปฏิบัติงาน{TemplatePlaceholders.MissingValue}ให้แล้วเสร็จภายใน{TemplatePlaceholders.MissingValue}",

            ContractFormatConstant.CFormat007 => $"งวดที่ {no} เป็นเงิน {amount} บาท ({amountText}) จะจ่ายเมื่อผู้รับจ้าง ได้ดำเนินการบำรุงรักษาและซ่อมแซมแก้ไขคอมพิวเตอร์เป็นเวลา {leadTime ?? 0} ({leadTimeText}) {periodTypeName} และผู้ว่าจ้างได้ตรวจรับมอบงานตามสัญญาแล้ว",

            ContractFormatConstant.CFormat014 => isLastTerm
                ? $"งวดสุดท้าย เป็นจำนวนเงิน {amount} บาท ({amountText}) จะจ่ายให้เมื่อผู้ว่าจ้างได้รับมอบงานออกแบบจากผู้ให้บริการครบบริบูรณ์และคณะกรรมการตรวจรับพัสดุได้พิจารณาแล้ว เห็นว่าครบถ้วนถูกต้องและตรวจรับเรียบร้อยตามสัญญาแล้ว"
                : $"งวดที่ {no} จำนวนร้อยละ {percentage?.ToString("N0") ?? TemplatePlaceholders.MissingValue} ({(percentage is not null ? ((int)percentage).ToThaiNumberText() : TemplatePlaceholders.MissingValue)}) ของค่าจ้างงานออกแบบตามข้อ ๓.๑ เป็นเงิน {amount} บาท ({amountText}) จะจ่ายให้เมื่อ{description}และคณะกรรมการตรวจรับพัสดุได้พิจารณาแล้วเห็นว่าครบถ้วนถูกต้องและตรวจรับเรียบร้อยแล้ว",

            ContractFormatConstant.CFormat005 => isLastTerm
                ? $"งวดสุดท้ายเป็นเงิน {amount} บาท ({amountText}) จะจ่ายให้เมื่อครบกำหนดระยะเวลาอนุญาตให้ใช้สิทธิในโปรแกรมคอมพิวเตอร์ดังกล่าว"
                : $"งวดที่ {no} เป็นเงิน {amount} บาท ({amountText}) จะจ่ายให้เมื่อผู้ซื้อได้ใช้โปรแกรมคอมพิวเตอร์เป็นเวลา {leadTime ?? 0} ({leadTimeText}) {periodTypeName}",

            _ => $"งวดที่ {no} เป็นจำนวนเงิน {amount} บาท ({amountText})",
        };
    }

    public static string? GenerateSingleWorkDescription(
        string? templateCode,
        PaymentTermDetail[] details,
        decimal? totalAmount = null,
        string? vatRateTypeCode = null)
    {
        if (details.Length == 0)
        {
            return null;
        }

        var totalAmountFormat = totalAmount.ToCurrencyStringWithComma();
        var totalAmountText = totalAmount.ThaiBahtText();
        var vatText = vatRateTypeCode == VatTypeConstant.IncluedVat
            ? " ซึ่งได้รวมภาษีมูลค่าเพิ่มแล้ว ตลอดจนค่าแรงงาน ค่าสิ่งของตลอดอายุสัญญา ภาษีอากรอื่น และค่าใช้จ่ายทั้งปวงไว้ด้วยแล้ว"
            : string.Empty;
        var termCount = details.Length;
        var termCountText = termCount.ToThaiNumberText();

        var normalAmount = details[0].Amount.ToCurrencyStringWithComma();
        var normalAmountText = details[0].Amount.ThaiBahtText();

        return templateCode switch
        {
            ContractFormatConstant.CFormat009 => details.All(d => d.Amount == details[0].Amount)
                ? $"ผู้ว่าจ้างตกลงชำระค่าจ้างเป็นรายงวด งวดละหนึ่งเดือน รวมทั้งหมด {termCount} ({termCountText}) งวด ในอัตรางวดละ {normalAmount} บาท ({normalAmountText})"
                : $"ผู้ว่าจ้างตกลงชำระค่าจ้างเป็นรายงวด งวดละหนึ่งเดือน รวมทั้งหมด {termCount} ({termCountText}) งวด ในอัตรางวดละ {normalAmount} บาท ({normalAmountText}) และงวดสุดท้าย {details[^1].Amount.ToCurrencyStringWithComma()} บาท ({details[^1].Amount.ThaiBahtText()})",

            ContractFormatConstant.CFormat007 =>
                $"ผู้ว่าจ้างตกลงชำระค่าจ้างบริการบำรุงรักษาและซ่อมแซมแก้ไขคอมพิวเตอร์เป็นเงินทั้งสิ้น {totalAmountFormat} บาท ({totalAmountText}){vatText} โดยผู้ว่าจ้างจะแบ่งจ่ายให้แก่ผู้รับจ้างเป็นงวดๆ รวม {termCount} ({termCountText}) งวด ดังนี้",

            ContractFormatConstant.CFormat001 =>
                $"ผู้ว่าจ้างตกลงจ่ายและผู้รับจ้างตกลงรับเงินค่าจ้างเป็นจำนวนเงิน {totalAmountFormat} บาท ({totalAmountText}){vatText}",

            ContractFormatConstant.CFormat015 =>
                $"ผู้ว่าจ้างและที่ปรึกษาได้ตกลงราคาค่าจ้างตามสัญญานี้ เป็นจำนวนเงินทั้งสิ้น {totalAmountFormat} บาท ({totalAmountText}){vatText}",

            ContractFormatConstant.CFormat010 =>
                $"ผู้ว่าจ้างตกลงจ่ายค่าจ้างตามข้อ 1 ให้แก่ผู้รับจ้างเป็นรายเดือนในอัตราเดือนละ {normalAmount} บาท ({normalAmountText}){vatText}",

            _ => null,
        };
    }

    private static string GetPeriodTypeName(ParameterCode? periodTypeCode) =>
        periodTypeCode?.Value switch
        {
            PeriodTypeConstant.PeriodType001 => "วัน",
            PeriodTypeConstant.PeriodType002 => "เดือน",
            PeriodTypeConstant.PeriodType003 => "ปี",
            _ => TemplatePlaceholders.MissingValue,
        };

    public CaContractDraftPaymentTerm MapToEntity()
    {
        return
            Optional(this.Id)
                .Map(CaContractDraftPaymentTerm.Create)
                .IfNone(CaContractDraftPaymentTerm.Create())
                .SetPaymentTermNo(this.No)
                .SetLeadTime(this.LeadTime)
                .SetDeliveryDate(this.DeliveryDate)
                .SetInstallmentPercentage(this.InstallmentPercentage)
                .SetAmount(this.Amount)
                .SetAdvanceDeductionAmount(this.AdvanceDeductionAmount ?? 0)
                .SetPerformanceDeductionAmount(this.PerformanceDeductionAmount ?? 0)
                .SetDescription(this.Description)
                .SetSequence(this.Sequence)
                .SetPeriodType(this.PeriodTypeCode);
    }

    public static PaymentTermDetail FromEntity(CaContractDraftPaymentTerm entity, string? paymentTypeCode = null, bool isCanEditLeadTime = true)
    {
        var detail = new PaymentTermDetail
        {
            Id = entity.Id.Value,
            No = entity.PaymentTermNo ?? null,
            LeadTime = entity.LeadTime ?? null,
            DeliveryDate = entity.DeliveryDate != default ? entity.DeliveryDate : null,
            DeliveryDateFormat = entity.DeliveryDate is not null && entity.DeliveryDate != default ? entity.DeliveryDate.ToThaiDateString() : null,
            InstallmentPercentage = entity.InstallmentPercentage ?? null,
            Amount = entity.Amount ?? null,
            AmountFormat = entity.Amount.ToCurrencyStringWithComma(),
            AmountText = entity.Amount.ThaiBahtText(),
            AdvanceDeductionAmount = entity.AdvanceDeductionAmount,
            AdvanceDeductionAmountFormat = entity.AdvanceDeductionAmount.ToCurrencyStringWithComma(),
            AdvanceDeductionAmountText = entity.AdvanceDeductionAmount.ThaiBahtText(),
            PerformanceDeductionAmount = entity.PerformanceDeductionAmount,
            PerformanceDeductionAmountFormat = entity.PerformanceDeductionAmount.ToCurrencyStringWithComma(),
            PerformanceDeductionAmountText = entity.PerformanceDeductionAmount.ThaiBahtText(),
            Description = entity.Description ?? string.Empty,
            Sequence = entity.Sequence,
            PeriodTypeCode = entity.PeriodTypeCode,
            IsCanEditLeadTime = isCanEditLeadTime,
        };

        if (paymentTypeCode == "PayType002")
        {
            detail.WorkDescription = string.Empty;
        }

        return detail;
    }

    public static PaymentTermDetail FromEntity(CaContractDraftEditPaymentTerm entity, string? paymentTypeCode = null, bool isCanEditLeadTime = true)
    {
        var detail = new PaymentTermDetail
        {
            Id = entity.Id.Value,
            No = entity.PaymentTermNo ?? null,
            LeadTime = entity.LeadTime ?? null,
            DeliveryDate = entity.DeliveryDate != default ? entity.DeliveryDate : null,
            DeliveryDateFormat = entity.DeliveryDate is not null && entity.DeliveryDate != default ? entity.DeliveryDate.ToThaiDateString() : null,
            InstallmentPercentage = entity.InstallmentPercentage ?? null,
            Amount = entity.Amount ?? null,
            AmountFormat = entity.Amount.ToCurrencyStringWithComma(),
            AmountText = entity.Amount.ThaiBahtText(),
            AdvanceDeductionAmount = entity.AdvanceDeductionAmount,
            AdvanceDeductionAmountFormat = entity.AdvanceDeductionAmount.ToCurrencyStringWithComma(),
            AdvanceDeductionAmountText = entity.AdvanceDeductionAmount.ThaiBahtText(),
            PerformanceDeductionAmount = entity.PerformanceDeductionAmount,
            PerformanceDeductionAmountFormat = entity.PerformanceDeductionAmount.ToCurrencyStringWithComma(),
            PerformanceDeductionAmountText = entity.PerformanceDeductionAmount.ThaiBahtText(),
            Description = entity.Description ?? string.Empty,
            Sequence = entity.Sequence,
            PeriodTypeCode = entity.PeriodTypeCode ?? ParameterCode.From("PeriodType001"),
            IsCanEditLeadTime = isCanEditLeadTime,
        };

        if (paymentTypeCode == "PayType002")
        {
            detail.WorkDescription = string.Empty;
        }

        return detail;
    }

    public static PaymentTermDetail FromEntityTorDraftPaymentTermDetail(PpTorDraftPaymentTermDetail? entity, decimal budget, int index)
    {
        if (entity != null)
        {
            var amount = entity.Percent > 0 ? (budget / 100) * entity.Percent : 0;

            return new PaymentTermDetail
            {
                No = entity.TermNumber,
                InstallmentPercentage = entity.Percent,
                LeadTime = entity.Period,
                Description = entity.Description,
                Amount = amount,
                AmountFormat = amount.ToCurrencyStringWithComma(),
                AmountText = amount.ThaiBahtText(),
                DeliveryDate = null,
                Sequence = index + 1,
                PeriodTypeCode = entity.PpTorDraftPaymentTerm.PeriodTypeCode ?? ParameterCode.From("PeriodType001"),
                IsCanEditLeadTime = true,
            };
        }

        return new PaymentTermDetail { };
    }
}