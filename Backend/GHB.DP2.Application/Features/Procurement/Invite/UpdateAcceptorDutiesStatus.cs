namespace GHB.DP2.Application.Features.Procurement.Invite;

using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateAcceptorDutiesStatusRequest(
    Guid ProcurementId,
    Guid InviteId,
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

        this.RuleFor(x => x.InviteId)
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
        this.Put("procurement/{ProcurementId:guid}/invite/{InviteId:guid}/acceptor/{AcceptorId:guid}/duties-status");
        this.Options(b =>
            b.WithTags("Procurement/Invite")
             .WithName("InviteUpdateAcceptorDutiesStatus")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdateAcceptorDutiesStatusRequest req, CancellationToken ct)
    {
        var inviteData = await this.dbContext.PInvites
                                   .Include(p => p.Procurement)
                                   .Include(i => i.Acceptors)
                                   .SingleOrDefaultAsync(
                                       w => w.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                                            w.Id == PInviteId.From(req.InviteId),
                                       ct);

        if (inviteData is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลหนังสือเชิญชวนผู้ประกอบการ");
        }

        var acceptor = inviteData.Acceptors.FirstOrDefault(a => a.Id == req.AcceptorId);

        if (acceptor == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้าง");
        }

        var isSixtyOver100K = inviteData.Procurement.Budget > 100000 && inviteData.Procurement.SupplyMethodCode == SupplyMethodConstant.Sixty;

        acceptor.SetIsUnableToPerformDuties(req.IsUnableToPerformDuties);

        if (req.IsUnableToPerformDuties)
        {
            acceptor.UnableToPerformDuties(req.Remark);
        }
        else
        {
            acceptor.Pending();
        }

        if (inviteData.HasMajorityRejection())
        {
            inviteData.SetRejected(string.Empty, isSixtyOver100K);
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}