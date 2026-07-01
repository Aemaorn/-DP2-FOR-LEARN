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

public record CreatePoSapRequest(
    Guid ContractAmendmentId,
    string? PoSapNumber,
    CamContractAmendmentPoSapStatus Status,
    IEnumerable<AcceptorRequest> Acceptors);

public class CreatePoSapEndpoint : PoSapEndpointBase<CreatePoSapRequest, Results<Created<Guid>, NotFound<string>, BadRequest>>
{
    public CreatePoSapEndpoint(ILogger<CreatePoSapEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Post("contract-amendment/{ContractAmendmentId:guid}/po-sap");
        this.Description(b => b
                              .WithTags("ContractAmendment/PoSap")
                              .WithName("CreatePoSap")
                              .Produces(StatusCodes.Status201Created)
                              .Produces<string>(StatusCodes.Status404NotFound)
                              .Produces(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Created<Guid>, NotFound<string>, BadRequest>> HandleRequestAsync(CreatePoSapRequest req, CancellationToken ct)
    {
        var poAddendum =
            await this.DbContext.CamContractAmendments
                      .FirstOrDefaultAsync(
                          c => c.Id == CamContractAmendmentId.From(req.ContractAmendmentId),
                          ct);

        if (poAddendum is null)
        {
            return TypedResults.NotFound("ไม่พบสัญญาที่ระบุ");
        }

        var poSap = CamContractAmendmentPoSap.Create(poAddendum.Id);

        await this.UpsertAcceptorsAsync(poSap, [.. req.Acceptors], ct);

        if (!string.IsNullOrWhiteSpace(req.PoSapNumber))
        {
            _ = poSap.SetPoSapNumber(req.PoSapNumber);
        }

        switch (req.Status)
        {
            case CamContractAmendmentPoSapStatus.Edit:
                _ = poSap.SetEdit();

                break;

            case CamContractAmendmentPoSapStatus.WaitingApproval:
                _ = poSap.SetWaitingApproval();

                break;

            case CamContractAmendmentPoSapStatus.Draft:
                _ = poSap.SetDraft();

                break;

            case CamContractAmendmentPoSapStatus.Approved:
            case CamContractAmendmentPoSapStatus.Rejected:
            default:
                this.ThrowError(r => r.Status, "สถานะไม่นี้รองรับการทำรายการ", StatusCodes.Status400BadRequest);

                break;
        }

        this.DbContext.CamContractAmendmentPoSaps.Add(poSap);
        await this.DbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, poSap.Id.Value);
    }
}