namespace GHB.DP2.Application.Features.Procurement.PrincipleApproval.Constants;

using Codehard.Common.Extensions;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;

public static class PrincipleApprovalConstant
{
    public static IEnumerable<PrincipleApprovalPerfSupportDataDetailResponseDto>
        DefaultPerfSupportDataDetails() =>
    [
        new(null, 1, "ฝากเงิน", null, null, null, null),
        new(null, 2, "ถอนเงิน", null, null, null, null),
        new(null, 3, "เปิดบัญชีใหม่", null, null, null, null),
    ];

    public static IEnumerable<PrincipleApprovalRoiLoanAndDepositSummaryResponseDto>
        DefaultRoiLoanAndDepositSummaries() =>
    [
        new(null, 1, "ปริมาณสินเชื่อปล่อยใหม่ (ต่อปี)", null, null, null),
        new(null, 2, "ปริมาณเงินฝากสะสม", null, null, null),
    ];

    public static IEnumerable<int> DefaultAnalysisYears()
    {
        var currentYear = int.Parse(DateTimeOffset.UtcNow.ToThaiDateString("yyyy"));

        return Enumerable.Range(currentYear - 4, 5)
                         .ToArray();
    }

    public static IEnumerable<PrincipleApprovalRentalAnalysisDto>
        DefaultRentalAnalysis() =>
    [
        new(null, 1, RentalAnalysisType.General, "สินเชื่อปล่อยใหม่", DefaultAnalysisYears().Select(y => new PrincipleApprovalRentalAnalysisDetail(null, y, 0))),
        new(null, 2, RentalAnalysisType.General, "เงินฝาก", DefaultAnalysisYears().Select(y => new PrincipleApprovalRentalAnalysisDetail(null, y, 0))),
        new(null, 3, RentalAnalysisType.General, "NPL เข้าใหม่", DefaultAnalysisYears().Select(y => new PrincipleApprovalRentalAnalysisDetail(null, y, 0))),
        new(null, 1, RentalAnalysisType.ProfitAndLoss, "รายได้ดอกเบี้ย", DefaultAnalysisYears().Select(y => new PrincipleApprovalRentalAnalysisDetail(null, y, 0))),
        new(null, 2, RentalAnalysisType.ProfitAndLoss, "หัก ค่าใช้จ่ายดอกเบี้ย", DefaultAnalysisYears().Select(y => new PrincipleApprovalRentalAnalysisDetail(null, y, 0))),
        new(null, 3, RentalAnalysisType.ProfitAndLoss, "รายได้ดอกเบี้ยสุทธิ", DefaultAnalysisYears().Select(y => new PrincipleApprovalRentalAnalysisDetail(null, y, 0))),
        new(null, 4, RentalAnalysisType.ProfitAndLoss, "รายได้ดอกเบี้ยสุทธิสุทธิ", DefaultAnalysisYears().Select(y => new PrincipleApprovalRentalAnalysisDetail(null, y, 0))), // ถ้าไม่ซ้ำ ให้ลบ
        new(null, 5, RentalAnalysisType.ProfitAndLoss, "รายได้จากการดำเนินงาน", DefaultAnalysisYears().Select(y => new PrincipleApprovalRentalAnalysisDetail(null, y, 0))),
        new(null, 6, RentalAnalysisType.ProfitAndLoss, "หัก ค่าใช้จ่ายการดำเนินงานอื่น ๆ", DefaultAnalysisYears().Select(y => new PrincipleApprovalRentalAnalysisDetail(null, y, 0))),
    ];
}

public static class AnalysisSummary
{
    public const string Npv = "NPV";
    public const string PaybackPeriod = "Payback Period";
    public const string DiscountedPaybackPeriod = "Discounted Payback Period";
}

public static class ValueAnalysisSheet
{
    public const string General = "การวิเคราะห์ความคุ้มค่าของการเช่าอาคารสถานที่";
    public const string ProfitAndLoss = "งบกำไรขาดทุน";
    public const string Summary = "สรุปความคุ้มค่า";
}

public static class ConsiderationSheet
{
    public const string PerfSupportDataDetails = "ข้อมูลประกอบผลการดำเนินงาน";
    public const string RoiLoanAndDepositSummaries = "ปริมาณสินเชื่อปล่อยใหม่ และเงินฝากสะสม";
    public const string DepositRemaining = "ผลการดำเนินการ - เงินฝากคงเหลือ";
    public const string LoanExisting = "ผลการดำเนินการ - สินเชื่อคงเหลือ";
    public const string LoanNew = "ผลการดำเนินการ - สินเชื่อปล่อยใหม่";
}