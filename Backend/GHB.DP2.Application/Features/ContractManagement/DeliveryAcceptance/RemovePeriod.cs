namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptance;

using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record RemovePeriodRequest(
    Guid DeliveryAcceptanceId,
    Guid PeriodId);

public class RemovePeriodHandler : EndpointBase<RemovePeriodRequest, NoContent>
{
    private readonly Dp2DbContext dbContext;

    public RemovePeriodHandler(
        Dp2DbContext dbContext,
        ILogger<RemovePeriodHandler> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/DeliveryAcceptance")
             .Produces<NoContent>());
        this.Delete("delivery-acceptance/{DeliveryAcceptanceId:guid}/period/{PeriodId:guid}");
    }

    protected override async ValueTask<NoContent> HandleRequestAsync(RemovePeriodRequest req, CancellationToken ct)
    {
        var deliveryAcceptance = await this.dbContext.CmDeliveryAcceptances
                                           .Include(da => da.Periods)
                                           .FirstOrDefaultAsync(a => a.Id == CmDeliveryAcceptanceId.From(req.DeliveryAcceptanceId), ct);

        if (deliveryAcceptance == null)
        {
            this.ThrowError("ไม่พบข้อมูลการรับมอบงาน/พัสดุ", StatusCodes.Status404NotFound);
        }

        var period = deliveryAcceptance.Periods
                                       .FirstOrDefault(p => p.Id == CmDeliveryAcceptancePeriodId.From(req.PeriodId));

        if (period == null)
        {
            this.ThrowError("ไม่พบข้อมูลระยะเวลาการรับมอบงาน/พัสดุ", StatusCodes.Status404NotFound);
        }

        deliveryAcceptance.RemovePeriod(period);

        this.dbContext.CmDeliveryAcceptances.Update(deliveryAcceptance);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}