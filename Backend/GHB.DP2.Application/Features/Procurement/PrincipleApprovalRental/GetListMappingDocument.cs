namespace GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental;

using System.ComponentModel;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.PrincipleApproval;
using GHB.DP2.Application.Features.Procurement.Procurement.Abstact;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record CreatorReplace(
    [property: Description("รหัสผู้ใช้งาน")] Guid UserId,
    [property: Description("ลายเซ็นต์")] string Signature,
    [property: Description("ชื่อ-สกุล")] string FullName,
    [property: Description("ตำแหน่ง")] string PositionName,
    [property: Description("ตำแหน่งในคณะกรรมการ หรือผู้จัดทำ")]
    string? PositionOnBoard);

public record AcceptorReplace(
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("การดำเนินการ")]
    string Action,
    [property: Description("เห็นชอบหรืออนุมัติ")]
    string? Signature,
    [property: Description("ชื่อคณะกรรมการหรือผู้จัดทำ")]
    string FullName,
    [property: Description("ตำแหน่งผู้ผู้เห็นชอบ หรืออนุมัติ")]
    string? PositionName,
    [property: Description("ตำแหน่งผู้ปฏิบัติหน้าที่แทน เห็นชอบ หรืออนุมัติ")]
    string? Delegate,
    [property: Description("หมายเหตุ")]
    string? Remark,
    [property: Description("สถานะ")]
    AcceptorStatus Status);

public record PublisherDto(
    [property: Description("ลายเซ็นต์ผู้ประกาศผู้ชนะ")]
    string? Signature,
    [property: Description("ชื่อผู้ประกาศผู้ชนะ")]
    string? FullName,
    [property: Description("ตำแหน่งผู้ประกาศผู้ชนะ")]
    string? PositionName,
    [property: Description("ตำแหน่งในคณะกรรมการ หรือผู้จัดทำ")]
    string? ActingPosition,
    [property: Description("ตำแหน่งผู้ปฏิบัติหน้าที่แทน")]
    string? DelegatePosition,
    [property: Description("วันที่ประกาศผู้ชนะ")]
    string PublishedDate);

public record PrincipleApprovalRentalReplaceDto(
    [property: Description("ข้อมูลขออนุมัติหลักการเช่า")]
    PrincipleApprovalReplaceDto Principle,
    [property: Description("รหัสขออนุมัติหลักการเช่า")]
    Guid? Id,
    [property: Description("รหัสการจัดซื้อจัดจ้าง")]
    Guid? ProcurementId,
    [property: Description("ข้อมูลการจัดซื้อจัดจ้าง")]
    ProcurementReplaceDto? Procurement,
    [property: Description("รหัสเอกสารเช่า")]
    Guid? DocumentId,
    [property: Description("เอกสารเช่า เปลี่ยนแปลง")]
    bool? IsDocumentReplace,
    [property: Description("รหัสเอกสารผู้ชนะ")]
    Guid? WinnerDocumentId,
    [property: Description("เอกสารผู้ชนะ เปลี่ยนแปลง")]
    bool? IsWinnerDocumentReplace,
    [property: Description("ประเภทการใช้สัญญา")]
    UseContractType UseContract,
    [property: Description("ราคาอ้างอิง")] decimal? ReferencePriceAmount,
    [property: Description("มูลค่าปัจจุบันสุทธิ (NPV)")]
    decimal? AnalysisSummaryNpv,
    [property: Description("ระยะเวลาคืนทุน (Payback Period)")]
    decimal? AnalysisSummaryPaybackYearPeriod,
    [property: Description("ระยะเวลาคืนทุนส่วนลด (Discounted Payback Period)")]
    decimal? AnalysisSummaryDiscountedPaybackYearPeriod,
    [property: Description("เบอร์โทรศัพท์")]
    string Telephone,
    [property: Description("สถานะ")] PPrincipleApprovalRentalStatus Status,
    [property: Description("ผู้อนุมัติ")] AcceptorReplace[] Acceptors,
    [property: Description("ผู้รับมอบหมาย")]
    IEnumerable<PrincipleApprovalRentalAssigneeResponseDto> Assignees,
    [property: Description("ข้อมูลการสนับสนุนผลการดำเนินงาน")]
    PrincipleApprovalRentalPerfSupportDataResponseDto? PerfSupportData,
    [property: Description("รายละเอียดการสนับสนุนผลการดำเนินงาน")]
    IEnumerable<PrincipleApprovalRentalPerfSupportDataDetailResponseDto> PerfSupportDataDetails,
    [property: Description("ข้อมูลสรุปผลตอบแทนจากสินเชื่อและเงินฝาก")]
    IEnumerable<PrincipleApprovalRentalRoiLoanAndDepositSummaryResponseDto>? RoiLoanAndDepositSummaries,
    [property: Description("ผลการดำเนินงานตามผลตอบแทน")]
    IEnumerable<PrincipleApprovalRentalRoiPerfResultResponseDto>? RoiPerfResults,
    [property: Description("ข้อมูลงบประมาณ")]
    IEnumerable<PrincipleApprovalRentalBudgetDto>? Budgets,
    [property: Description("การวิเคราะห์การเช่า")]
    IEnumerable<PrincipleApprovalRentalRentalAnalysisDto>? RentalAnalyses,
    [property: Description("ข้อมูลผู้ประกอบการ")]
    IEnumerable<PrincipleApprovalRentalEntrepreneursResponseDto> Entrepreneurs,
    [property: Description("รายชื่อข้อมูลผู้ประกอบการ")]
    string? EntrepreneurName,
    [property: Description("ข้อมูลผู้จัดทำเอกสาร")]
    CreatorReplace? Creator,
    [property: Description("ข้อมูลผู้ประกาศผู้ชนะ")]
    PublisherDto? Publisher,
    [property: Description("ผู้รับมอบหมาย")]
    JorPorCommentReplace? JorPorComment,
    string? AcceptorDate);

public class GetListMappingPrincipleApprovalRentalDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingPrincipleApprovalRentalDocumentEndpoint(ILogger<GetListMappingPrincipleApprovalRentalDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/PrincipleApprovalRental"));
        this.Get("procurement/principle-approval-rental/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(PrincipleApprovalRentalReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}