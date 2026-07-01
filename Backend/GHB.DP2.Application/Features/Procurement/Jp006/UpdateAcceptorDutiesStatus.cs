namespace GHB.DP2.Application.Features.Procurement.Jp006;

using FluentValidation;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateAcceptorDutiesStatusRequest(
    Guid ProcurementId,
    Guid Jp006Id,
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

        this.RuleFor(x => x.Jp006Id)
            .NotNull()
            .NotEmpty()
            .WithMessage("Jp006Id is required.");

        this.RuleFor(x => x.AcceptorId)
            .NotNull()
            .NotEmpty()
            .WithMessage("AcceptorId is required.");

        this.RuleFor(x => x.IsUnableToPerformDuties)
            .NotNull()
            .NotEmpty()
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
        this.Put("procurement/{ProcurementId:guid}/jp006/{Jp006Id:guid}/acceptor/{AcceptorId:guid}/duties-status");
        this.Options(b =>
            b.WithTags(nameof(Jp006))
             .WithName("UpdatePjp006AcceptorDutiesStatus")
             .Produces<Ok<Guid>>()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdateAcceptorDutiesStatusRequest req, CancellationToken ct)
    {
        // Fetch Jp006 data
        var jp006 = await this.dbContext.PJp006S
                              .Include(x => x.Acceptors)
                              .SingleOrDefaultAsync(x => x.ProcurementId == ProcurementId.From(req.ProcurementId) && x.Id == PurchaseOrderId.From(req.Jp006Id), ct);

        if (jp006 == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการจัดซื้อจัดจ้าง");
        }

        // Update acceptor duties status
        var acceptor = jp006.Acceptors.FirstOrDefault(x => x.Id == AcceptorId.From(req.AcceptorId));

        if (acceptor == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้รับผิดชอบ");
        }

        acceptor.SetIsUnableToPerformDuties(req.IsUnableToPerformDuties);

        if (req.IsUnableToPerformDuties)
        {
            acceptor.UnableToPerformDuties(req.Remark);
        }
        else
        {
            acceptor.Pending();
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}