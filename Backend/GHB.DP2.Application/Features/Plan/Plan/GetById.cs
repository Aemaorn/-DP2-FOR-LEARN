namespace GHB.DP2.Application.Features.Plan.Plan;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Plan.Plan.Abstract;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

public record GetPlanDetailRequest(
    Guid Id);

public record PlanDocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);

public record GetPlanResponse(
    [property: Description("รหัสแผน")] PlanId Id,
    [property: Description("สถานะแผน")] PlanStatus Status,
    [property: Description("เลขที่แผน")] string PlanNumber,
    [property: Description("รหัสหน่วยงาน")]
    BusinessUnitId DepartmentCode,
    [property: Description("ประเภทแผน")] PlanType Type,
    [property: Description("รหัสวิธีการจัดหา")]
    ParameterCode SupplyMethodCode,
    [property: Description("รหัสประเภทวิธีการจัดหา")]
    ParameterCode? SupplyMethodTypeCode,
    [property: Description("รหัสประเภทพิเศษวิธีการจัดหา")]
    ParameterCode? SupplyMethodSpecialTypeCode,
    [property: Description("ปีงบประมาณ")] int BudgetYear,
    [property: Description("ชื่อแผน")] string Name,
    [property: Description("งบประมาณ")] decimal Budget,
    [property: Description("วันที่คาดว่าจะดำเนินการจัดซื้อจัดจ้าง")]
    DateTimeOffset ExpectingProcurementAt,
    [property: Description("วันที่เอกสาร")]
    DateTimeOffset? DocumentDate,
    [property: Description("หมายเหตุ")] string? Remark,
    [property: Description("หมายเลขโทรศัพท์")]
    string? Telephone,
    [property: Description("เป็นครุภัณฑ์")]
    bool IsStock,
    [property: Description("รหัสส่วนราชการ")]
    ParameterCode? AssignSegmentCode,
    [property: Description("เลขกลุ่ม eGP")]
    string? GroupEgpNumber,
    [property: Description("เลข eGP")] string? EgpNumber,
    [property: Description("เป็นวัสดุพาณิชย์")]
    bool? IsCommercialMaterial,
    [property: Description("รหัสเอกสารแผน")]
    Guid? PlanDocumentId,
    [property: Description("รหัสเอกสารแผน")]
    bool? IsPlanDocumentIdReplace,
    [property: Description("รหัสเอกสารประกาศแผน")]
    Guid? PlanAnnouncementDocumentId,
    [property: Description("รหัสเอกสารประกาศแผน")]
    bool? IsPlanAnnouncementDocumentIdReplace,
    [property: Description("ผู้อนุมัติ")] AcceptorResponse[] Acceptors,
    [property: Description("ผู้รับมอบหมาย")]
    AssigneeResponse[] Assignees,
    [property: Description("เอกสารแนบ")] AttachmentsDtoWithId[] Attachments,
    [property: Description("ผู้สร้าง")] Guid CreatedBy,
    [property: Description("ผู้รับมอบหมายประกาศ")]
    AssigneeResponse? AssigneeAnnouncement,
    [property: Description("ขอเปลี่ยนแปลง")]
    bool IsChange,
    [property: Description("ขอยกเลิก")]
    bool IsCancel,
    [property: Description("เหตุผลขอเปลี่ยนแปลง")]
    string? ChangeReason,
    [property: Description("เหตุผลขอยกเลิก")]
    string? CancelReason,
    [property: Description("หมายเหตุการปิดงาน")]
    string? RemarkClosed,
    bool IsCurrentCancelOrChange,
    bool IsProcurement,
    [property: Description("ประวัติเวอร์ชันเอกสารแผน")]
    PlanDocumentVersionResponse[] PlanDocumentVersions,
    [property: Description("ประวัติเวอร์ชันเอกสารประกาศแผน")]
    PlanDocumentVersionResponse[] PlanAnnouncementDocumentVersions,
    [property: Description("วันที่แก้ไขล่าสุด")]
    DateTimeOffset? LastModifiedAt);

public class GetPlanDetail : PlanEndpointBase<GetPlanDetailRequest, Results<Ok<GetPlanResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetPlanDetail(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<GetPlanDetail> logger)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Plan")
             .WithName("GetPlanDetail")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Get("plan/{Id:guid}");
    }

    protected override async ValueTask<Results<Ok<GetPlanResponse>, NotFound<string>>> HandleRequestAsync(GetPlanDetailRequest req, CancellationToken ct)
    {
        var data = await this.dbContext
                             .Plans
                             .AsNoTracking()
                             .Include(plan => plan.Acceptors)
                             .ThenInclude(a => a.User)
                             .ThenInclude(u => u.Employee)
                             .Include(plan => plan.Assignees)
                             .Include(plan => plan.Attachments)
                             .Include(auditableEntity => auditableEntity.AuditInfo)
                             .Include(plan => plan.DocumentHistories)
                             .SingleOrDefaultAsync(p => p.Id == PlanId.From(req.Id), ct);

        var isAnyCancelOrChangePlan = await this.dbContext.Plans
                                                .Where(w => w.ReferenceId == PlanId.From(req.Id))
                                                .AnyAsync(ct);

        var isProcurement = await this.dbContext.Procurements
                                                .Where(w => w.PlanId == PlanId.From(req.Id) && !w.IsCancelled && !w.IsDeleted && w.Status != ProcurementStatus.Cancelled)
                                                .AnyAsync(ct);

        if (data is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลแผน");
        }

        var response = MapToGetPlanResponse(data, isAnyCancelOrChangePlan, isProcurement);

        return TypedResults.Ok(response);
    }
}