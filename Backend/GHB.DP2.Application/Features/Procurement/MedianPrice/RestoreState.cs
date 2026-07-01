namespace GHB.DP2.Application.Features.Procurement.MedianPrice;

using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.MedianPrice.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record RestoreStateRequest(
    Guid Id,
    string Reason
);

public class RestoreStateEndPoint : MedianPriceEndpointBase<RestoreStateRequest, Results<Ok<MedianPriceId>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RestoreStateEndPoint(
        ILogger<UpdateMedianPriceEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        Dp2DbContext dbContext)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/MedianPrice"));
        this.Post("median-price/{id}/restore-state");
    }

    protected override async ValueTask<Results<Ok<MedianPriceId>, NotFound<string>>> HandleRequestAsync(RestoreStateRequest req, CancellationToken ct)
    {
        var medianPriceData = await this.dbContext.PpMedianPrices.Include(ppMedianPrice => ppMedianPrice.Procurement)
                                .FirstOrDefaultAsync(a => a.Id == MedianPriceId.From(req.Id), ct);

        if (medianPriceData == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลราคากลาง");
        }

        var returnMedianPriceData = await this.dbContext.PpMedianPrices
                                      .Where(a => a.IsActive == false)
                                      .FirstOrDefaultAsync(a => a.Id == medianPriceData.ReferenceId, ct);

        if (returnMedianPriceData == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลราคากลางอ้างอิง");
        }

        medianPriceData.SetActive(false);

        medianPriceData.Procurement.SetProcessType(ProcessType.PurchaseRequisition);

        medianPriceData.Procurement.SetActiveProcurement();

        medianPriceData.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.RestoreState,
                medianPriceData.IsChange ? "ยกเลิกคำขอเปลี่ยนแปลง" : "ยกเลิกคำขอยกเลิก",
                medianPriceData.Status.ToString(),
                req.Reason));

        returnMedianPriceData.SetActive(true);

        returnMedianPriceData.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.RestoreState,
                medianPriceData.IsChange ? "ยกเลิกคำขอเปลี่ยนแปลง" : "ยกเลิกคำขอยกเลิก",
                medianPriceData.Status.ToString(),
                req.Reason));

        this.dbContext.PpMedianPrices.UpdateRange(medianPriceData, returnMedianPriceData);
        this.dbContext.PpMedianPrices.Remove(medianPriceData);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(returnMedianPriceData.Id);
    }
}
