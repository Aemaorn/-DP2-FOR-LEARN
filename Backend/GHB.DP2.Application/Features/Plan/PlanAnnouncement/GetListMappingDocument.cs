namespace GHB.DP2.Application.Features.Plan.PlanAnnouncement;

using System.ComponentModel;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Plan.Dto;
using GHB.DP2.Domain.Plan;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record PlanAnnouncementReplate(
    [property: Description("เลขที่ประกาศแผน")]
    string PlanAnnouncementNumber,
    [property: Description("เลขกลุ่ม eGP")]
    string? GroupEgpNumber,
    [property: Description("ปีงบประมาณ")] int BudgetYear,
    [property: Description("ฝ่ายงบประมาณ")]
    string Department,
    [property: Description("หมายเลขโทรศัพท์")]
    string? Telephone,
    [property: Description("วิธีการจัดหา")]
    string SupplyMethod,
    [property: Description("งบประมาณ")] string? Budget,
    [property: Description("งบประมาณ(ตัวอักษร)")]
    string BudgetText,
    [property: Description("หมายเหตุ")] string? Remark,
    [property: Description("หัวข้อประกาศ")]
    string? AnnouncementTitle,
    [property: Description("วันที่ประกาศ")]
    string? AnnouncementDate,
    [property: Description("จำนวนแผน")] int PlanCount,
    [property: Description("วันที่อนุมัติ")]
    string? ApproverDate,
    string? PublicDate,
    [property: Description("วันที่ประกาศ")]
    string? Additional,
    [property: Description("ผู้จัดทำ")] CreatePlanReplace? Creator,
    [property: Description("ข้อมูลแผน")] IEnumerable<PlanSelectedReplate> PlanSelected,
    [property: Description("ผู้ประกาศเผยแผน")]
    PublicPlanReplace? Publisher,
    [property: Description("ผู้เห็นชอบ/อนุมัติ")]
    IEnumerable<AcceptorReplace> Acceptor);

public class GetListMappingPlanAnnouncementDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingPlanAnnouncementDocumentEndpoint(ILogger<GetListMappingPlanAnnouncementDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags(nameof(PlanAnnouncement)));
        this.Get("plan/announcement/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(PlanAnnouncementReplate);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}