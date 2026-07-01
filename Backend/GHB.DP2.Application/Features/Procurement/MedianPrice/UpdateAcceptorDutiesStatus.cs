namespace GHB.DP2.Application.Features.Procurement.MedianPrice;

using FluentValidation;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateAcceptorDutiesStatusRequest(
    Guid ProcurementId,
    Guid MedianPriceId,
    Guid AcceptorId,
    bool IsUnableToPerformDuties,
    string? Remark);

public class UpdateAcceptorDutiesStatusValidator : Validator<UpdateAcceptorDutiesStatusRequest>
{
    public UpdateAcceptorDutiesStatusValidator()
    {
        this.RuleFor(x => x.ProcurementId)
            .NotNull()
            .NotEmpty()
            .WithMessage("ProcurementId is required.");

        this.RuleFor(x => x.MedianPriceId)
            .NotNull()
            .NotEmpty()
            .WithMessage("MedianPriceId is required.");

        this.RuleFor(x => x.AcceptorId)
            .NotNull()
            .NotEmpty()
            .WithMessage("AcceptorId is required.");

        this.RuleFor(x => x.IsUnableToPerformDuties)
            .NotNull()
            .WithMessage("IsUnableToPerformDuties is required.");
    }
}

public class UpdateAcceptorDutiesStatusEndpoint : EndpointBase<UpdateAcceptorDutiesStatusRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateAcceptorDutiesStatusEndpoint(
        ILogger<UpdateAcceptorDutiesStatusEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/median-price/{MedianPriceId:guid}/acceptor/{AcceptorId:guid}/duties-status");
        this.Options(b =>
            b.WithTags(nameof(MedianPrice))
             .WithName("MedianPriceUpdateAcceptorDutiesStatus")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdateAcceptorDutiesStatusRequest req, CancellationToken ct)
    {
        // Fetch median price and acceptor data
        var medianPrice = await this.dbContext.PpMedianPrices
                                    .Include(mp => mp.Acceptors)
                                    .SingleOrDefaultAsync(
                                        mp => mp.Id == MedianPriceId.From(req.MedianPriceId) &&
                                              mp.ProcurementId == ProcurementId.From(req.ProcurementId),
                                        ct);

        if (medianPrice == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลราคากลาง");
        }

        var acceptor = medianPrice.Acceptors.FirstOrDefault(a => a.Id == req.AcceptorId);

        if (acceptor == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้รับผิดชอบ");
        }

        // Update the acceptor's status
        acceptor.SetIsUnableToPerformDuties(req.IsUnableToPerformDuties);

        if (req.IsUnableToPerformDuties)
        {
            acceptor.UnableToPerformDuties(req.Remark);
        }
        else
        {
            acceptor.Pending();
        }

        if (medianPrice.HasMajorityRejection())
        {
            medianPrice.SetRejected(null);
        }

        // Save changes to the database
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}