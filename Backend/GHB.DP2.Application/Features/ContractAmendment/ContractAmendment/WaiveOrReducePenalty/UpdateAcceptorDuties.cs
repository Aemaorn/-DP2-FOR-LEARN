namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty;

using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty.Abstract;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateAcceptorDutiesWaiveOrReducePenaltyRequest(
    Guid CamContractAmendmentId,
    Guid WaiveOrReducePenaltyId,
    Guid AcceptorId,
    bool IsUnableToPerformDuties,
    string? Remark);

public class UpdateAcceptorDutiesWaiveOrReducePenaltyEndpoint : WaiveOrReducePenaltyEndpointBase<UpdateAcceptorDutiesWaiveOrReducePenaltyRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    public UpdateAcceptorDutiesWaiveOrReducePenaltyEndpoint(
        ILogger<UpdateAcceptorDutiesWaiveOrReducePenaltyEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Put("contract-amendment/{CamContractAmendmentId:guid}/waive-or-reduce-penalty/{WaiveOrReducePenaltyId:guid}/acceptor/{AcceptorId:guid}/duties-status");
        this.Options(b =>
            b.WithTags("ContractAmendment/WaiveOrReducePenalty")
             .WithName("UpdateWaiveOrReducePenaltyAcceptorDutiesStatus")
             .Accepts<UpdateAcceptorDutiesWaiveOrReducePenaltyRequest>("application/json"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdateAcceptorDutiesWaiveOrReducePenaltyRequest req, CancellationToken ct)
    {
        var entity =
            await this.DbContext.CamContractAmendmentWaiveOrReducePenalties
                      .Include(c => c.Acceptors)
                      .FirstOrDefaultAsync(
                          mp =>
                              mp.Id == WaiveOrReducePenaltyId.From(req.WaiveOrReducePenaltyId) &&
                              mp.CamContractAmendmentId == CamContractAmendmentId.From(req.CamContractAmendmentId),
                          ct);

        if (entity == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการแก้ไขสัญญาที่ระบุ");
        }

        var acceptor = entity.Acceptors.FirstOrDefault(a => a.Id == req.AcceptorId);

        if (acceptor == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้รับผิดชอบ");
        }

        _ = req.IsUnableToPerformDuties switch
        {
            true => acceptor.SetIsUnableToPerformDuties(true)
                            .UnableToPerformDuties(req.Remark),
            false => acceptor.SetIsUnableToPerformDuties(false)
                             .Pending(),
        };

        if (entity.HasMajorityRejection())
        {
            entity.SetStatus(CamContractAmendmentWaiveOrReducePenaltyStatus.Rejected);
        }

        this.DbContext.CamContractAmendmentWaiveOrReducePenalties.Update(entity);
        await this.DbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}