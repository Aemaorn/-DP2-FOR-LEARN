namespace GHB.DP2.Application.Features.Procurement.ChangeCommittee;

using FluentValidation;
using GHB.DP2.Domain.Procurement.ChangeCommittee;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateAcceptorDutiesStatusRequest(
    Guid ChangeCommitteeId,
    Guid AcceptorId,
    bool IsUnableToPerformDuties,
    string? Remark);

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
        this.Put("change-committee/{ChangeCommitteeId:guid}/acceptor/{AcceptorId:guid}/duties-status");
        this.Options(b =>
            b.WithTags(nameof(ChangeCommittee))
             .WithName("ChangeCommitteeUpdateAcceptorDutiesStatus")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdateAcceptorDutiesStatusRequest req, CancellationToken ct)
    {
        var committeeChange = await this.dbContext.CommitteeChanges
                                    .Include(mp => mp.Acceptors)
                                    .FirstOrDefaultAsync(
                                        mp => mp.Id == CommitteeChangeId.From(req.ChangeCommitteeId),
                                        ct);

        if (committeeChange == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        var acceptor = committeeChange.Acceptors.FirstOrDefault(a => a.Id == req.AcceptorId);

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

        if (committeeChange.HasMajorityRejection())
        {
            committeeChange.SetRejected(null);
        }

        // Save changes to the database
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}