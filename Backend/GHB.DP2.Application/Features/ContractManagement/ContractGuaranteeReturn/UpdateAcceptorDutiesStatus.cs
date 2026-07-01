namespace GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateAcceptorDutiesStatusRequest(
    Guid Id,
    Guid AcceptorId,
    bool IsUnableToPerformDuties,
    string? Remark);

public class UpdateAcceptorDutiesStatusEndpoint : EndpointBase<UpdateAcceptorDutiesStatusRequest, Results<Ok, NotFound<string>>>
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
        this.Put("contract/contract-guarantee-return/{Id:guid}/acceptor/{AcceptorId:guid}/set-duties");
        this.Options(b =>
            b.WithTags("ContractManagement/ContractGuaranteeReturn")
             .WithName("ContractGuaranteeReturnUpdateAcceptorDutiesStatus")
             .Produces<Ok>()
             .ProducesProblem(statusCode: StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(
        UpdateAcceptorDutiesStatusRequest req,
        CancellationToken ct)
    {
        var guaranteeReturn = await this.dbContext
                                        .CmContractGuaranteeReturns
                                        .Include(a => a.Acceptors)
                                        .SingleOrDefaultAsync(w => w.Id == CmContractGuaranteeReturnId.From(req.Id), ct);

        if (guaranteeReturn is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลคืนหลักประกันสัญญา");
        }

        var acceptor = guaranteeReturn.Acceptors.FirstOrDefault(f => f.Id == AcceptorId.From(req.AcceptorId));

        if (acceptor is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลคณะกรรมการตรวจรับ");
        }

        _ = req.IsUnableToPerformDuties switch
        {
            true => acceptor.SetIsUnableToPerformDuties(true)
                            .UnableToPerformDuties(req.Remark),
            false => acceptor.SetIsUnableToPerformDuties(false)
                             .Pending(),
        };

        if (guaranteeReturn.HasMajorityRejection())
        {
            guaranteeReturn.SetStatusRejected();
        }

        this.dbContext.CmContractGuaranteeReturns.Update(guaranteeReturn);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}