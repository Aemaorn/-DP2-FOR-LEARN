namespace GHB.DP2.Application.Features.Plan.PlanAnnouncement;

using System.ComponentModel;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Plan.PlanAnnouncement.Abstract;
using GHB.DP2.Application.Features.Plan.PlanAnnouncement.DTO;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetPlanAnnouncementByIdRequest(Guid Id);

public record PlanAnnouncementDocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);

public record GetPlanAnnouncementByIdResponse(
    [property: Description("รหัสประกาศแผน")]
    Guid PlanAnnouncementId,
    [property: Description("เลขที่ประกาศแผน")]
    string PlanAnnouncementNumber,
    [property: Description("เลขกลุ่ม eGP")]
    string? GroupEgpNumber,
    [property: Description("หมายเลขโทรศัพท์")]
    string? Telephone,
    [property: Description("ปี")] int Year,
    [property: Description("รหัสวิธีการจัดหา")]
    string SupplyMethodCode,
    [property: Description("หมายเหตุ")] string? Remark,
    [property: Description("หัวข้อประกาศ")]
    string? AnnouncementTitle,
    [property: Description("วันที่ประกาศ")]
    DateTimeOffset? AnnouncementDate,
    [property: Description("วันที่เอกสาร")]
    DateTimeOffset? DocumentDate,
    [property: Description("สถานะประกาศแผน")]
    PlanAnnouncementStatus Status,
    [property: Description("รหัสเอกสารประกาศ")]
    Guid? AnnouncementDocumentId,
    [property: Description("รหัสเอกสารอนุมัติ")]
    Guid? ApproveDocumentId,
    [property: Description("แผนที่เลือก")] PlanAnnouncementSelectedDto[] PlanSelected,
    [property: Description("เอกสารแนบ")] AttachmentsDto[] Attachments,
    [property: Description("ผู้รับมอบหมาย")]
    AssigneeResponse[] Assignees,
    [property: Description("ผู้อนุมัติ")] AcceptorResponse[] Acceptors,
    [property: Description("ผู้รับมอบหมายประกาศ")]
    AssigneeResponse? AssigneeAnnouncement,
    bool IsApproveDocumentIdReplace,
    bool IsAnnouncementDocumentIdReplace,
    [property: Description("ประวัติเวอร์ชันเอกสารอนุมัติ")]
    PlanAnnouncementDocumentVersionResponse[] ApproveDocumentVersions,
    [property: Description("ประวัติเวอร์ชันเอกสารประกาศ")]
    PlanAnnouncementDocumentVersionResponse[] AnnouncementDocumentVersions,
    [property: Description("วันที่แก้ไขล่าสุด")]
    DateTimeOffset? LastModifiedAt);

public class GetPlanAnnouncementById : PlanAnnouncementEndpointBase<GetPlanAnnouncementByIdRequest, Results<Ok<GetPlanAnnouncementByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetPlanAnnouncementById(
        Dp2DbContext dbContext,
        ILogger<GetPlanAnnouncementById> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(PlanAnnouncement))
             .WithName("GetPlanAnnouncementById"));
        this.Get("plan/announcement/{Id:guid}");
    }

    protected override async ValueTask<Results<Ok<GetPlanAnnouncementByIdResponse>, NotFound<string>>> HandleRequestAsync(
        GetPlanAnnouncementByIdRequest req,
        CancellationToken ct)
    {
        var data =
            await this.dbContext.PlanAnnouncements
                      .AsNoTracking()
                      .Include(i => i.AnnouncementSelectedInformations)
                      .ThenInclude(planAnnouncementSelected => planAnnouncementSelected.Plan)
                      .ThenInclude(plan => plan.Department)
                      .Include(i => i.Attachments)
                      .Include(i => i.Assignees)
                      .ThenInclude(a => a.User)
                      .ThenInclude(u => u.Employee)
                      .Include(i => i.Acceptors)
                      .ThenInclude(a => a.User)
                      .ThenInclude(u => u.Employee)
                      .Include(planAnnouncement => planAnnouncement.AnnouncementSelectedInformations)
                      .ThenInclude(planAnnouncementSelected => planAnnouncementSelected.Plan)
                      .ThenInclude(plan => plan.SupplyMethod)
                      .Include(planAnnouncement => planAnnouncement.AnnouncementSelectedInformations)
                      .ThenInclude(planAnnouncementSelected => planAnnouncementSelected.Plan)
                      .ThenInclude(plan => plan.SupplyMethodType)
                      .Include(planAnnouncement => planAnnouncement.DocumentHistories)
                      .Include(planAnnouncement => planAnnouncement.AuditInfo)
                      .SingleOrDefaultAsync(p => p.Id == PlanAnnouncementId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound($"Plan Announcement Id {req.Id} not found");
        }

        var result = await this.MapToGetPlanAnnouncementResponse(data, ct);

        return TypedResults.Ok(result);
    }
}