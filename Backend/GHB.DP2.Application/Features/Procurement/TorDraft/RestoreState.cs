namespace GHB.DP2.Application.Features.Procurement.TorDraft;

using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.TorDraft.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record RestoreStateRequest(
    Guid Id,
    string Reason
);

public class RestoreStateEndPoint : TorDraftEndpointBase<RestoreStateRequest, Results<Ok<PpTorDraftId>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RestoreStateEndPoint(
        ILogger<UpdateTorDraftEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        Dp2DbContext dbContext)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/TorDraft"));
        this.Post("tordraft/{id}/restore-state");
    }

    protected override async ValueTask<Results<Ok<PpTorDraftId>, NotFound<string>>> HandleRequestAsync(RestoreStateRequest req, CancellationToken ct)
    {
        var torData = await this.dbContext.PpTorDrafts.Include(ppAppoint => ppAppoint.Procurement)
                                .FirstOrDefaultAsync(a => a.Id == PpTorDraftId.From(req.Id), ct);

        if (torData == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลขอแต่งตั้ง");
        }

        var returnTorData = await this.dbContext.PpTorDrafts
                                      .Where(a => a.IsActive == false)
                                      .FirstOrDefaultAsync(a => a.Id == torData.ReferenceId, ct);

        if (returnTorData == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลขอแต่งตั้งอ้างอิง");
        }

        torData.SetActive(false);

        var budget = torData.Procurement.Budget ?? 0;
        var activeMedianPrice = await this.dbContext.PpMedianPrices
                                          .Where(m => m.ProcurementId == torData.Procurement.Id && m.IsActive)
                                          .FirstOrDefaultAsync(ct);
        var isMedianPriceApproved = activeMedianPrice?.Status == MedianPriceStatus.Approved;
        var processType = budget > 100_000m && !isMedianPriceApproved
            ? ProcessType.MedianPrice
            : ProcessType.PurchaseRequisition;

        torData.Procurement.SetProcessType(processType);

        torData.Procurement.SetActiveProcurement();

        torData.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.RestoreState,
                torData.IsChange ? "ยกเลิกคำขอเปลี่ยนแปลง" : "ยกเลิกคำขอยกเลิก",
                torData.Status.ToString(),
                req.Reason));

        returnTorData.SetActive(true);

        returnTorData.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.RestoreState,
                torData.IsChange ? "ยกเลิกคำขอเปลี่ยนแปลง" : "ยกเลิกคำขอยกเลิก",
                torData.Status.ToString(),
                req.Reason));

        this.dbContext.PpTorDrafts.UpdateRange(torData, returnTorData);
        this.dbContext.PpTorDrafts.Remove(torData);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(returnTorData.Id);
    }
}