namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoAddendum;

using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoAddendum;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateAcceptorDutiesStatusRequest(
    Guid CamContractAmendmentId,
    Guid Id,
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
        this.Put("contract-amendment/{CamContractAmendmentId:guid}/po-addendum/{Id:guid}/acceptor/{AcceptorId:guid}/duties-status");
        this.Options(b =>
            b.WithTags("ContractAmendment/PoAddendum")
             .WithName("UpdatePoAddendumAcceptorDutiesStatus")
             .Accepts<UpdateAcceptorDutiesStatusRequest>("application/json"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdateAcceptorDutiesStatusRequest req, CancellationToken ct)
    {
        // Fetch median price and acceptor data
        var poAddendum = await this.dbContext.CamContractAmendmentPoAddendums
                                   .Include(c => c.Acceptors)
                                    .SingleOrDefaultAsync(
                                        mp => mp.Id == CamContractAmendmentPoAddendumId.From(req.Id) &&
                                              mp.CamContractAmendmentId == CamContractAmendmentId.From(req.CamContractAmendmentId),
                                        ct);

        if (poAddendum == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลบันทึกต่อท้ายสัญญา");
        }

        var acceptor = poAddendum.Acceptors.FirstOrDefault(a => a.Id == req.AcceptorId);

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

        if (poAddendum.HasMajorityRejection())
        {
            poAddendum.SetStatus(CamContractAmendmentPoAddendumStatus.Rejected);
        }

        this.dbContext.CamContractAmendmentPoAddendums.Update(poAddendum);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}