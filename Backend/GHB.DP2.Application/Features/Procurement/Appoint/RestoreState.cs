namespace GHB.DP2.Application.Features.Procurement.Appoint;

using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Appoint.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record RestoreStateRequest(
    Guid Id,
    string Reason
);

public class RestoreStateEndPoint : AppointEndpointBase<RestoreStateRequest, Results<Ok<PpAppointId>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RestoreStateEndPoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<RequestActionAppointEndpoint> logger)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/Appoint"));
        this.Post("appointments/{id}/restore-state");
    }

    protected override async ValueTask<Results<Ok<PpAppointId>, NotFound<string>>> HandleRequestAsync(RestoreStateRequest req, CancellationToken ct)
    {
        var appointData = await this.dbContext.PpAppoints.Include(ppAppoint => ppAppoint.Procurement)
                                    .FirstOrDefaultAsync(a => a.Id == PpAppointId.From(req.Id), ct);

        if (appointData == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลขอแต่งตั้ง");
        }

        var returnAppointData = await this.dbContext.PpAppoints
                                          .Where(a => a.IsActive == false)
                                          .FirstOrDefaultAsync(a => a.Id == appointData.ReferenceId, ct);

        if (returnAppointData == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลขอแต่งตั้งอ้างอิง");
        }

        appointData.SetActive(false);

        appointData.Procurement.SetProcessType(ProcessType.TorDraft);

        appointData.Procurement.SetActiveProcurement();

        appointData.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.RestoreState,
                appointData.IsChange ? "ยกเลิกคำขอเปลี่ยนแปลง" : "ยกเลิกคำขอยกเลิก",
                appointData.Status.ToString(),
                req.Reason));

        returnAppointData.SetActive(true);

        returnAppointData.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.RestoreState,
                appointData.IsChange ? "ยกเลิกคำขอเปลี่ยนแปลง" : "ยกเลิกคำขอยกเลิก",
                appointData.Status.ToString(),
                req.Reason));

        this.dbContext.PpAppoints.UpdateRange(appointData, returnAppointData);
        this.dbContext.PpAppoints.Remove(appointData);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(returnAppointData.Id);
    }
}