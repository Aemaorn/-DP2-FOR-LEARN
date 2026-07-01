namespace GHB.DP2.Application.Features.Report.ContractCompletionByQuarter;

using System.ComponentModel;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record CreatorReplace(
    [property: Description("รหัสผู้ใช้งาน")] Guid UserId,
    [property: Description("ลายเซ็นต์")] string Signature,
    [property: Description("ชื่อ-สกุล")] string FullName,
    [property: Description("ตำแหน่ง")] string FullPositionName,
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
    string Signature,
    [property: Description("ชื่อคณะกรรมการหรือผู้จัดทำ")]
    string FullName,
    [property: Description("ตำแหน่งผู้ผู้เห็นชอบ หรืออนุมัติ")]
    string? FullPositionName,
    [property: Description("ตำแหน่งผู้ปฏิบัติหน้าที่แทน เห็นชอบ หรืออนุมัติ")]
    string? Delegate,
    [property: Description("หมายเหตุ")]
    string? Remark,
    [property: Description("สถานะ")]
    AcceptorStatus Status);

public record ReportContractCompletionByQuarterDetailReplaceDto(
    [property: Description("รหัสรายละเอียดรายงาน")] Guid Id,
    [property: Description("รหัสผู้ค้าในสัญญา")] Guid CaContractDraftVendorId,
    [property: Description("รหัสประเภทสัญญา")] string ContractTypeCode,
    [property: Description("ชื่อประเภทสัญญา")] string ContractTypeName,
    [property: Description("เลขที่สัญญา")] string ContractNumber,
    [property: Description("ชื่อสัญญา")] string ContractName,
    [property: Description("วันที่ลงนามสัญญา")] string ContractSignedDate,
    [property: Description("ชื่อผู้ประกอบการ")] string EntrepreneurName,
    [property: Description("งบประมาณ (ตัวเลข)")] string Budget,
    [property: Description("งบประมาณ (ข้อความ)")] string BudgetText,
    [property: Description("รายละเอียดเพิ่มเติมของสัญญา")] string? Description,
    [property: Description("สถานะเกินกำหนดหรือไม่")] bool Overdue);

public record ContractDetailReplaceDto(
    ContractTypeDetailReplaceDto Summary,
    IEnumerable<ContractTypeDetailReplaceDto> Details);

public record ContractTypeDetailReplaceDto(
    [property: Description("ชื่อประเภทสัญญา")] string Name,
    [property: Description("จำนวนสัญญา")] int Count,
    [property: Description("เปอร์เซ็นต์")] string Percent);

public record ContractTypeSummaryRowReplaceDto(
    [property: Description("ชื่อประเภทสัญญา")] string Name,
    [property: Description("จำนวนสัญญาไตรมาส 1")] int Q1Count,
    [property: Description("เปอร์เซ็นต์ไตรมาส 1")] string Q1Percent,
    [property: Description("จำนวนสัญญาไตรมาส 2")] int Q2Count,
    [property: Description("เปอร์เซ็นต์ไตรมาส 2")] string Q2Percent,
    [property: Description("จำนวนสัญญาไตรมาส 3")] int Q3Count,
    [property: Description("เปอร์เซ็นต์ไตรมาส 3")] string Q3Percent,
    [property: Description("จำนวนสัญญาไตรมาส 4")] int Q4Count,
    [property: Description("เปอร์เซ็นต์ไตรมาส 4")] string Q4Percent,
    [property: Description("จำนวนสัญญาทั้งปี")] int TotalCount,
    [property: Description("เปอร์เซ็นต์ทั้งปี")] string TotalPercent);

public record ReportContractCompletionByQuarterReplaceDto(
    [property: Description("รหัสรายงาน")] Guid Id,
    [property: Description("เลขที่เอกสารรายงาน")] string DocumentNumber,
    [property: Description("วันที่จัดทำรายงาน")] string DocumentDate,
    [property: Description("วันที่ส่งอนุมัติ")] string? AcceptorDate,
    [property: Description("ปีงบประมาณ")] int Year,
    [property: Description("ไตรมาส")] int Quarter,
    [property: Description("วันเริ่มต้นลงนามสัญญา")] string? SignStartDate,
    [property: Description("วันสิ้นสุดลงนามสัญญา")] string? SignEndDate,
    [property: Description("สถานะรายงานการส่งมอบสัญญา")] RpContractCompletionByQuarterStatus Status,
    [property: Description("รายละเอียดสัญญาในรายงาน")] IEnumerable<ReportContractCompletionByQuarterDetailReplaceDto>? Detail,
    [property: Description("ผู้เห็นชอบหรืออนุมัติ")] IEnumerable<AcceptorReplace>? Acceptors,
    [property: Description("ข้อมูลผู้สร้างรายงาน")] CreatorReplace? Creator,
    [property: Description("สัญญาแล้วเสร็จ ไตรมาส 1")] ContractDetailReplaceDto? CompletionContractQ1,
    [property: Description("สัญญาแล้วเสร็จ ไตรมาส 2")] ContractDetailReplaceDto? CompletionContractQ2,
    [property: Description("สัญญาแล้วเสร็จ ไตรมาส 3")] ContractDetailReplaceDto? CompletionContractQ3,
    [property: Description("สัญญาแล้วเสร็จ ไตรมาส 4")] ContractDetailReplaceDto? CompletionContractQ4,
    [property: Description("สัญญาแล้วเสร็จ ปี")] ContractDetailReplaceDto? CompletionContractYear,
    [property: Description("ตารางสรุปประเภทสัญญาทุกไตรมาส")] IEnumerable<ContractTypeSummaryRowReplaceDto> ContractTypeSummaryRows);

public class GetListMappingPw119DocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingPw119DocumentEndpoint(ILogger<GetListMappingPw119DocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Report/ContractCompletionByQuarter"));
        this.Get("report/contract-completion-by-quarter/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(ReportContractCompletionByQuarterReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}