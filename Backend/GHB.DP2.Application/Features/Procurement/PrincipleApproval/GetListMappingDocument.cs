namespace GHB.DP2.Application.Features.Procurement.PrincipleApproval;

using System.ComponentModel;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.Procurement.Abstact;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record CreatorReplaceDto(
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("ผู้จัดทำ")] string Signature,
    [property: Description("ชื่อผู้จัดทำ")]
    string FullName,
    [property: Description("ตำแหน่งผู้จัดทำ")]
    string PositionName,
    [property: Description("ตำแหน่งผู้จัดทำในคณะกรรมการ")]
    string? PositionOnBoard);

public record PrincipleApprovalAcceptorReplaceDto(
    [property: Description("รหัสผู้อนุมัติ")]
    Guid Id,
    [property: Description("การดำเนินการ")]
    string Action,
    [property: Description("ประเภทผู้อนุมัติ")]
    AcceptorType AcceptorType,
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("รหัสพนักงาน")] string EmployeeCode,
    [property: Description("ชื่อ-สกุล")] string FullName,
    [property: Description("ตำแหน่ง")] string PositionName,
    [property: Description("ชื่อหน่วยงาน")]
    string DepartmentName,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("สถานะการอนุมัติ")]
    AcceptorStatus Status,
    [property: Description("หมายเหตุ")] string? Remark,
    string? Delegate,
    [property: Description("วันที่ดำเนินการ")]
    string? ActionAt = default,
    [property: Description("เป็นผู้อนุมัติปัจจุบัน")]
    bool IsCurrent = false
);

public record PrincipleApprovalReplaceDto(
    [property: Description("รหัสขออนุมัติหลักการ")]
    Guid Id,
    [property: Description("วันที่ขออนุมัติหลักการ")]
    string? AcceptorDate,
    [property: Description("ตำแหน่งผู้มีอำนาจอนุมัติ")]
    IEnumerable<SectionApprove>? SectionApproveName,
    [property: Description("รหัสการจัดซื้อจัดจ้าง")]
    Guid ProcurementId,
    [property: Description("ข้อมูลการจัดซื้อจัดจ้าง")]
    ProcurementReplaceDto Procurement,
    [property: Description("สถานที่ตั้งสาขา")]
    string BranchLocation,
    [property: Description("รหัสเทมเพลตเอกสาร")]
    Guid? DocumentTemplateId,
    [property: Description("รหัสประเภทการเช่า")]
    string RentTypeCode,
    [property: Description("วันที่เริ่มเช่า")]
    string RentalStartDate,
    [property: Description("วันที่สิ้นสุดการเช่า")]
    string RentalEndDate,
    [property: Description("ระยะเวลาการเช่า (ปี)")]
    string? RentalDurationYear,
    [property: Description("ระยะเวลาการเช่า (เดือน)")]
    string? RentalDurationMonth,
    [property: Description("ระยะเวลาการเช่า (วัน)")]
    string? RentalDurationDay,
    [property: Description("ค่าเช่าสูงสุดต่อเดือน")]
    string MaxMonthlyRent,
    [property: Description("ค่าเช่าสูงสุดต่อเดือน")]
    string MaxMonthlyRentText,
    [property: Description("ค่าเช่ารวมทั้งสิ้น")]
    string TotalRentalAmount,
    [property: Description("ค่าเช่ารวมทั้งสิ้น")]
    string TotalRentalAmountText,
    [property: Description("วันที่คาดว่าจะทำสัญญา")]
    string ExpectedContractDate,
    [property: Description("รายละเอียดสถานที่เช่า")]
    string RentalLocationDetails,
    [property: Description("หมายเลขโทรศัพท์")]
    string Telephone,
    [property: Description("รหัสตำบล")] string SubDistrictCode,
    [property: Description("ชื่อตำบล")] string SubDistrictName,
    [property: Description("รหัสอำเภอ")] string DistrictCode,
    [property: Description("ชื่ออำเภอ")] string DistrictName,
    [property: Description("รหัสจังหวัด")] string ProvinceCode,
    [property: Description("ชื่อจังหวัด")] string ProvinceName,
    [property: Description("ราคาอ้างอิง")] string? ReferencePriceAmount,
    [property: Description("ราคาอ้างอิง")] string? ReferencePriceAmountText,
    [property: Description("มูลค่าปัจจุบันสุทธิ (NPV)")]
    string? AnalysisSummaryNpv,
    [property: Description("มูลค่าปัจจุบันสุทธิ (NPV)")]
    string? AnalysisSummaryNpvText,
    [property: Description("ระยะเวลาคืนทุน (Payback Period)")]
    decimal? AnalysisSummaryPaybackYearPeriod,
    [property: Description("ระยะเวลาคืนทุนส่วนลด (Discounted Payback Period)")]
    decimal? AnalysisSummaryDiscountedPaybackYearPeriod,
    [property: Description("สถานะ")] PPrincipleApprovalStatus Status,
    [property: Description("สายงานเห็นชอบ")]
    IEnumerable<PrincipleApprovalAcceptorReplaceDto> DepartmentAcceptors,
    [property: Description("ผู้มีอำนาตเห็นชอบ/อนมุัติ")]
    IEnumerable<PrincipleApprovalAcceptorReplaceDto> Acceptors,
    [property: Description("ผู้รับมอบหมาย")]
    IEnumerable<PrincipleApprovalAssigneeResponseDto> Assignees,
    [property: Description("คณะกรรมการจัดเช่า")]
    IEnumerable<PrincipleApprovalCommitteeResponseDto> RentCommittees,
    [property: Description("เป็นคณะกรรมการตรวจรับ")]
    IEnumerable<PrincipleApprovalCommitteeResponseDto> AcceptanceCommittees,
    [property: Description("ข้อมูลการสนับสนุนผลการดำเนินงาน")]
    PrincipleApprovalPerfSupportDataResponseDto? PerfSupportData,
    [property: Description("รายละเอียดการสนับสนุนผลการดำเนินงาน")]
    IEnumerable<PrincipleApprovalPerfSupportDataDetailResponseDto> PerfSupportDataDetails,
    [property: Description("ข้อมูลสรุปผลตอบแทนจากสินเชื่อและเงินฝาก")]
    IEnumerable<PrincipleApprovalRoiLoanAndDepositSummaryResponseDto> RoiLoanAndDepositSummaries,
    [property: Description("ผลการดำเนินงานตามผลตอบแทน")]
    IEnumerable<PrincipleApprovalRoiPerfResultResponseDto> RoiPerfResults,
    [property: Description("ข้อมูลงบประมาณ")]
    IEnumerable<PrincipleApprovalBudgetDto> Budgets,
    [property: Description("ข้อมูลงบประมาณ")]
    string? BudgetsAccountNo,
    [property: Description("การวิเคราะห์การเช่า")]
    IEnumerable<PrincipleApprovalRentalAnalysisDto>? RentalAnalyses,
    [property: Description("เป็นคณะกรรมการจัดเช่า")]
    bool IsRentCommittee,
    [property: Description("เป็นคณะกรรมการตรวจรับ")]
    bool IsAcceptanceCommittee,
    [property: Description("ข้อมูลผู้จัดทำเอกสาร")]
    CreatorReplaceDto? Creator,
    [property: Description("อำนาจอนุมัติ")]
    string? CommandText,
    [property: Description("ผู้รับมอบหมาย")]
    JorPorCommentReplace? JorPorComment);

public class GetListMappingPrincipleApprovalDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingPrincipleApprovalDocumentEndpoint(ILogger<GetListMappingPrincipleApprovalDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/PrincipleApproval"));
        this.Get("procurement/principle-approval/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(PrincipleApprovalReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}