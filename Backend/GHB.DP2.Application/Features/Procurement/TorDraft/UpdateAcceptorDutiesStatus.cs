namespace GHB.DP2.Application.Features.Procurement.TorDraft;

using FluentValidation;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateAcceptorDutiesStatusRequest(
    Guid ProcurementId,
    Guid Id,
    Guid AcceptorId,
    bool IsUnableToPerformDuties,
    string? Remark);

public class UpdateAcceptorDutiesStatusValidator : Validator<UpdateAcceptorDutiesStatusRequest>
{
    public UpdateAcceptorDutiesStatusValidator()
    {
        this.RuleFor(x => x.Id)
            .NotNull()
            .NotEmpty()
            .WithMessage("TorDraftId is required.");

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
        this.Options(b =>
            b.WithTags("Procurement/TorDraft")
             .WithName("TorDraftUpdateAcceptorDutiesStatus")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<UpdateAcceptorDutiesStatusRequest>("application/json"));
        this.Put("procurement/{ProcurementId:guid}/tordraft/{Id:guid}/acceptor/{AcceptorId:guid}/duties-status");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdateAcceptorDutiesStatusRequest req, CancellationToken ct)
    {
        var torDraft = await this.dbContext.PpTorDrafts
                                 .Include(t => t.PpTorDraftAcceptors)
                                 .FirstOrDefaultAsync(t => t.Id == PpTorDraftId.From(req.Id) && t.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        if (torDraft is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        var acceptor = torDraft.PpTorDraftAcceptors
                               .FirstOrDefault(a => a.Id == req.AcceptorId);

        if (acceptor is null)
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

        if (torDraft.HasMajorityRejection())
        {
            torDraft.SetRejected(null);
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}