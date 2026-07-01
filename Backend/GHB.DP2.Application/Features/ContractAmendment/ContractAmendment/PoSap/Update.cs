namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoSap;

using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoSap.Abstract;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoSap;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdatePoSapRequest(
    Guid ContractAmendmentId,
    Guid Id,
    string? PoSapNumber,
    CamContractAmendmentPoSapStatus Status,
    IEnumerable<AcceptorRequest> Acceptors);

public class UpdatePoSapEndpoint : PoSapEndpointBase<UpdatePoSapRequest, Results<Ok, NotFound<string>>>
{
    public UpdatePoSapEndpoint(ILogger<UpdatePoSapEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Put("contract-amendment/{ContractAmendmentId:guid}/po-sap/{Id:guid}");
        this.Description(b => b
                              .WithTags("ContractAmendment/PoSap")
                              .WithName("UpdatePoSap")
                              .Produces(StatusCodes.Status204NoContent)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpdatePoSapRequest req, CancellationToken ct)
    {
        var poSap =
            await this.DbContext.CamContractAmendmentPoSaps
                      .FirstOrDefaultAsync(
                          c =>
                              c.Id == CamContractAmendmentPoSapId.From(req.Id)
                              && c.CamContractAmendmentId == CamContractAmendmentId.From(req.ContractAmendmentId),
                          ct);

        if (poSap is null)
        {
            return TypedResults.NotFound("ไม่พบ PO SAP ที่ระบุ");
        }

        poSap.SetPoSapNumber(req.PoSapNumber);

        await this.UpsertAcceptorsAsync(poSap, [.. req.Acceptors], ct);

        switch (req.Status)
        {
            case CamContractAmendmentPoSapStatus.Draft:
                _ = poSap.SetDraft();

                break;

            case CamContractAmendmentPoSapStatus.WaitingApproval:
                _ = poSap.SetWaitingApproval();

                break;

            case CamContractAmendmentPoSapStatus.Edit:
                _ = poSap.SetEdit();

                break;
        }

        this.DbContext.CamContractAmendmentPoSaps.Update(poSap);
        await this.DbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}