namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration;

using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration.Abstract;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateAcceptorDutiesAdjustContractDurationRequest(
    Guid CamContractAmendmentId,
    Guid Id,
    Guid AcceptorId,
    bool IsUnableToPerformDuties,
    string? Remark);

public class UpdateAcceptorDutiesAdjustContractDurationEndpoint : AdjustContractDurationEndpointBase<UpdateAcceptorDutiesAdjustContractDurationRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    public UpdateAcceptorDutiesAdjustContractDurationEndpoint(
        ILogger<UpdateAcceptorDutiesAdjustContractDurationEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Put("contract-amendment/{CamContractAmendmentId:guid}/adjust-contract-duration/{Id:guid}/acceptor/{AcceptorId:guid}/duties-status");
        this.Options(b =>
            b.WithTags("ContractAmendment/AdjustContractDuration")
             .WithName("UpdateAdjustContractDurationAcceptorDutiesStatus")
             .Accepts<UpdateAcceptorDutiesAdjustContractDurationRequest>("application/json"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdateAcceptorDutiesAdjustContractDurationRequest req, CancellationToken ct)
    {
        var entity =
            await this.DbContext.CamContractAmendmentExtendChanges
                      .Include(c => c.Acceptors)
                      .FirstOrDefaultAsync(
                          mp =>
                              mp.Id == ContractAmendmentExtendChangeId.From(req.Id) &&
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

        this.DbContext.CamContractAmendmentExtendChanges.Update(entity);
        await this.DbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}