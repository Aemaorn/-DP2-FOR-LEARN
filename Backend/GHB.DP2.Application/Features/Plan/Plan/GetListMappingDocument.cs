namespace GHB.DP2.Application.Features.Plan.Plan;

using System.ComponentModel;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Plan.Dto;
using GHB.DP2.Domain.Plan;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record PlanReplaceDto(
    [property: Description("เลขที่แผน")] string PlanNumber,
    [property: Description("ฝ่ายงบประมาณ")]
    string Department,
    [property: Description("ประเภทแผน")] PlanType Type,
    [property: Description("วิธีการจัดหา")]
    string SupplyMethod,
    [property: Description("ประเภทวิธีการจัดหา")]
    string? SupplyMethodType,
    [property: Description("ประเภทพิเศษวิธีการจัดหา")]
    string? SupplyMethodSpecialType,
    [property: Description("ปีงบประมาณ")] int BudgetYear,
    [property: Description("ชื่อแผน")] string Name,
    [property: Description("งบประมาณ")] string? Budget,
    [property: Description("งบประมาณ(ตัวอักษร)")]
    string BudgetText,
    [property: Description("หมายเหตุ")] string Remark,
    [property: Description("หมายเลขโทรศัพท์")]
    string? Telephone,
    [property: Description("เป็นครุภัณฑ์")]
    bool IsStock,
    [property: Description("ส่วนราชการ")] string? AssignSegment,
    [property: Description("เลขกลุ่ม eGP")]
    string? GroupEgpNumber,
    [property: Description("เลข eGP")] string? EgpNumber,
    [property: Description("เป็นวัสดุพาณิชย์")]
    bool IsCommercialMaterial,
    [property: Description("ขอเปลี่ยนแปลง")]
    bool IsChange,
    [property: Description("ขอยกเลิก")] bool IsCancel,
    [property: Description("เหตุผลขอเปลี่ยนแปลง")]
    string? ChangeReason,
    [property: Description("เหตุผลขอยกเลิก")]
    string? CancelReason,
    [property: Description("จำนวนแผน")] int PlanCount,
    [property: Description("วันที่อนุมัติ")]
    string? AcceptorDate,
    [property: Description("วันที่ประกาศ")]
    string? AnnouncementDate,
    [property: Description("ข้อความเพิ่มเติม")]
    string? Additional,
    [property: Description("ผู้จัดทำ")] CreatePlanReplace? Creator,
    [property: Description("ข้อมูลแผน")] PlanSelectedReplate PlanSelected,
    [property: Description("การเปลี่ยนแปลงแผน")]
    ChangePlanReplace? Change,
    [property: Description("ผู้ประกาศเผยแผน")]
    PublicPlanReplace? Publisher,
    [property: Description("ผู้เห็นชอบ/อนุมัติ")]
    IEnumerable<AcceptorReplace> Acceptor);

public class GetListMappingPlanDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingPlanDocumentEndpoint(ILogger<GetListMappingPlanDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Plan"));
        this.Get("plan/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(PlanReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}