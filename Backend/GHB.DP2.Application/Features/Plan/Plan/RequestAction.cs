namespace GHB.DP2.Application.Features.Plan.Plan;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Features.Plan.Plan.Abstract;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record RequestActionPlanRequest(
    Guid PlanId,
    bool IsChange,
    string? Reason);

public class RequestActionPlanEndpoint : PlanEndpointBase<RequestActionPlanRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public RequestActionPlanEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<RequestActionPlanEndpoint> logger)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(Plan))
             .WithName("RequestActionPlan")
             .Accepts<RequestActionPlanRequest>("application/json"));
        this.Post("plan/{PlanId:guid}/request-action");
        this.AuditLog("รายการจัดซื้อจัดจ้าง", "ขออนุมัติเปลี่ยนแปลง/ยกเลิกแผนจัดซื้อจัดจ้าง");
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(RequestActionPlanRequest req, CancellationToken ct)
    {
        PlanStatus[] statuses = [PlanStatus.Announcement, PlanStatus.ApprovePlan];

        var plan = await this.dbContext.Plans
                             .SingleOrDefaultAsync(
                                 c => c.Id == PlanId.From(req.PlanId) && statuses.Contains(c.Status) && c.IsActive,
                                 ct);

        if (plan is null)
        {
            this.ThrowError("ไม่พบข้อมูลแผน", StatusCodes.Status404NotFound);
        }

        var alreadyUsedPlan = await this.dbContext.Plans
                                        .Where(w => w.ReferenceId == plan.Id && !statuses.Contains(w.Status))
                                        .AnyAsync(ct);

        if (alreadyUsedPlan)
        {
            this.ThrowError("แผนนี้มีการดำเนินการ \"ขอยกเลิก\" หรือ \"ขอเปลี่ยนแปลง\" อยู่แล้ว", StatusCodes.Status404NotFound);
        }

        var anyCanceled = await this.dbContext.Plans
                                    .Where(w => w.ReferenceId == plan.Id && w.Status == PlanStatus.CancelPlan)
                                    .AnyAsync(ct);

        if (anyCanceled)
        {
            this.ThrowError("แผนนี้ทำการ \"ขอยกเลิก\" แล้ว, ไม่สามารถดำเนินการได้", StatusCodes.Status404NotFound);
        }

        var newPlan = plan.Clone(req.IsChange, req.Reason);

        _ = req.IsChange switch
        {
            true => newPlan.SetChangeReason(req.Reason),
            false => newPlan.SetCancelReason(req.Reason),
        };

        this.dbContext.Plans.Update(plan);
        this.dbContext.Plans.Add(newPlan);

        await this.dbContext.SaveChangesAsync(ct);

        var creatorUserId = UserId.From(plan.AuditInfo.CreatedBy);

        _ = req.IsChange
            ? SendNotificationRequestChangeAsync(newPlan, creatorUserId, ct)
            : SendNotificationRequestCancelAsync(newPlan, creatorUserId, ct);

        return TypedResults.Created(string.Empty, newPlan.Id.Value);
    }

    private static async Task SendNotificationRequestCancelAsync(Plan plan, UserId user, CancellationToken ct)
    {
        await Notification
              .Crate(
                  user,
                  NotificationConstant.RequestCancelPlan.Title,
                  string.Format(NotificationConstant.RequestCancelPlan.Message, ProgramConstant.Plan.Name, plan.PlanNumber.Value),
                  NotificationProgram.Plan)
              .SetReferenceId(plan.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Plan.Url, plan.Id.Value), ProgramConstant.Plan.Button)
              .PublishAsync(ct);
    }

    private static async Task SendNotificationRequestChangeAsync(Plan plan, UserId user, CancellationToken ct)
    {
        await Notification
              .Crate(
                  user,
                  NotificationConstant.RequestChangePlan.Title,
                  string.Format(NotificationConstant.RequestChangePlan.Message, ProgramConstant.Plan.Name, plan.PlanNumber.Value),
                  NotificationProgram.Plan)
              .SetReferenceId(plan.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Plan.Url, plan.Id.Value), ProgramConstant.Plan.Button)
              .PublishAsync(ct);
    }
}