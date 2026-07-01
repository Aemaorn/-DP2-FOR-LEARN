namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration;

using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration.Abstract;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration.Dto;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateAdjustContractDurationRequest(
    Guid CamContractAmendmentId,
    AdjustContractDurationInfo AdjustContractDurationOld,
    AdjustContractDurationInfo AdjustContractDurationNew,
    AcceptorRequest[]? Acceptors,
    AssigneeRequest[]? Assignees);

public class CreateAdjustContractDurationEndpoint : AdjustContractDurationEndpointBase<CreateAdjustContractDurationRequest, Results<Created<Guid>, NotFound<string>>>
{
    public CreateAdjustContractDurationEndpoint(ILogger<CreateAdjustContractDurationEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Post("contract-amendment/{CamContractAmendmentId:guid}/adjust-contract-duration");
        this.Description(b =>
            b.WithTags("ContractAmendment/AdjustContractDuration")
             .WithName("CreateAdjustContractDuration")
             .Produces<Created<Guid>>(StatusCodes.Status201Created)
             .Produces<NotFound<string>>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Created<Guid>, NotFound<string>>> HandleRequestAsync(CreateAdjustContractDurationRequest req, CancellationToken ct)
    {
        var contractAmendmentId = CamContractAmendmentId.From(req.CamContractAmendmentId);

        var contractAmendment =
            await this.DbContext.CamContractAmendments
                      .FirstOrDefaultAsync(
                          e => e.Id == contractAmendmentId,
                          ct);

        if (contractAmendment is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลสัญญาที่ระบุ");
        }

        var adjustContractDurationNew = req.AdjustContractDurationNew;

        var entity = CamContractAmendmentExtendChange.Create(
            contractAmendmentId,
            adjustContractDurationNew.ChangeType!.Value,
            adjustContractDurationNew.WorkStartDate!.Value,
            adjustContractDurationNew.NewEndDate!.Value);

        if (adjustContractDurationNew.PaymentTypeCode is not null)
        {
            entity.SetPaymentType(adjustContractDurationNew.PaymentTypeCode);
        }

        this.UpsertPaymentTerms(entity, adjustContractDurationNew.PaymentTerms);

        if (req.Acceptors is not null)
        {
            await this.UpsertAcceptors(
                entity,
                req.Acceptors,
                entity.Status,
                ct);
        }

        if (req.Assignees is not null)
        {
            await this.UpsertAssignee(
                entity,
                req.Assignees,
                ct);
        }

        contractAmendment.SetStatus(CamContractAmendmentStatus.InProgress);

        await this.SetDefaultDocumentTemplate(entity, ct);
        this.DbContext.CamContractAmendments.Update(contractAmendment);
        this.DbContext.CamContractAmendmentExtendChanges.Add(entity);
        await this.DbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, entity.Id.Value);
    }
}