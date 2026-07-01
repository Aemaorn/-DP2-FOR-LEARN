namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty;

using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty.Abstract;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateWaiveOrReducePenaltyRequest(
    Guid CamContractAmendmentId,
    bool WaiveAll,
    PenaltyInfo? PenaltyOld,
    PenaltyInfo? PenaltyNew,
    IEnumerable<AcceptorRequest>? Acceptors,
    IEnumerable<AssigneeRequest>? Assignees);

public class CreateWaiveOrReducePenaltyEndpoint : WaiveOrReducePenaltyEndpointBase<CreateWaiveOrReducePenaltyRequest, Results<Created<Guid>, NotFound<string>, BadRequest<string
>>>
{
    public CreateWaiveOrReducePenaltyEndpoint(ILogger<CreateWaiveOrReducePenaltyEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Post("contract-amendment/{CamContractAmendmentId:guid}/waive-or-reduce-penalty");
        this.Description(b =>
            b.WithTags("ContractAmendment/WaiveOrReducePenalty")
             .WithName("CreateWaiveOrReducePenalty")
             .Produces<Created<Guid>>(StatusCodes.Status201Created)
             .Produces<NotFound<string>>(StatusCodes.Status404NotFound)
             .Produces<BadRequest<Error>>(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Created<Guid>, NotFound<string>, BadRequest<string>>> HandleRequestAsync(CreateWaiveOrReducePenaltyRequest req, CancellationToken ct)
    {
        var contractAmendmentId = CamContractAmendmentId.From(req.CamContractAmendmentId);
        var contractAmendment =
            await this.DbContext.CamContractAmendments
                      .SingleOrDefaultAsync(
                          c => c.Id == contractAmendmentId,
                          ct);

        if (contractAmendment is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการแก้ไขสัญญาหรือคู่ค้าสัญญาที่เกี่ยวข้อง");
        }

        if (contractAmendment.Type != CmContractAmendmentType.WaiveOrReducePenalty)
        {
            return TypedResults.BadRequest("ไม่สามารถเพิ่มรายการนี้ได้ เนื่องจากประเภทการแก้ไขสัญญาไม่ถูกต้อง");
        }

        var waiveOrReducePenalty =
            CamContractAmendmentWaiveOrReducePenalty
                .Create(contractAmendmentId, req.WaiveAll);

        if (req.PenaltyNew is not null)
        {
            _ = waiveOrReducePenalty.SetPenaltyNew(
                req.PenaltyNew.PenaltyTypeCode is not null ? ParameterCode.From(req.PenaltyNew.PenaltyTypeCode) : null,
                req.PenaltyNew.Rate,
                req.PenaltyNew.Amount,
                req.PenaltyNew.RateTypeCode is not null ? ParameterCode.From(req.PenaltyNew.RateTypeCode) : null);
        }

        if (req.Acceptors is not null)
        {
            await this.UpsertAcceptors(
                waiveOrReducePenalty,
                [.. req.Acceptors],
                waiveOrReducePenalty.Status,
                ct);
        }

        if (req.Assignees is not null)
        {
            await this.UpsertAssignee(waiveOrReducePenalty, [.. req.Assignees], ct);
        }

        contractAmendment.SetStatus(CamContractAmendmentStatus.InProgress);
        await this.SetDefaultDocumentTemplate(waiveOrReducePenalty, ct);
        this.DbContext.CamContractAmendments.Update(contractAmendment);
        this.DbContext.CamContractAmendmentWaiveOrReducePenalties.Add(waiveOrReducePenalty);
        await this.DbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, waiveOrReducePenalty.Id.Value);
    }
}