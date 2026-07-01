namespace GHB.DP2.Application.Features.Procurement.MedianPrice;

using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.MedianPrice.Abstract;
using GHB.DP2.Application.Features.Procurement.MedianPrice.Dto;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpsertAssigneeRequest(
    Guid ProcurementId,
    Guid MedianPriceId,
    MedianPriceAssigneeInfo[] Assignees);

public class UpsertAssigneeEndpoint : MedianPriceEndpointBase<UpsertAssigneeRequest, Results<Ok<Guid>, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpsertAssigneeEndpoint(
        ILogger<UpsertAssigneeEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        Dp2DbContext dbContext)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/median-price/{MedianPriceId:guid}/assignee");
        this.Options(b =>
            b.WithTags(nameof(MedianPrice))
             .WithName("UpsertAssignee")
             .Produces<Ok<Guid>>()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpsertAssigneeRequest req, CancellationToken ct)
    {
        var medianPrice =
            await this.dbContext.PpMedianPrices
                      .Include(mp => mp.Assignees)
                      .SingleOrDefaultAsync(
                          mp => mp.Id == MedianPriceId.From(req.MedianPriceId) &&
                                mp.ProcurementId == ProcurementId.From(req.ProcurementId),
                          ct);

        if (medianPrice is null)
        {
            return TypedResults.NotFound("ไม่พบราคากลางที่ระบุ");
        }

        if (medianPrice.Status is not MedianPriceStatus.WaitingAssign)
        {
            return TypedResults.BadRequest("ไม่สามารถแก้ไขผู้มอบหมายได้ในสถานะปัจจุบัน");
        }

        await this.UpsertAssignee(medianPrice, req.Assignees, cancellationToken: CancellationToken.None);

        // Save changes to the database
        this.dbContext.PpMedianPrices.Update(medianPrice);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(medianPrice.Id.Value);
    }
}