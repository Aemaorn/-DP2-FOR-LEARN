namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod;

using FluentValidation;
using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record UpdatePeriodAcceptorDutiesStatusRequest(
    Guid DeliveryAcceptanceId,
    Guid Id,
    Guid AcceptorId,
    bool IsUnableToPerformDuties,
    string? Remark);

public class UpdateAcceptorDutiesStatusValidator : Validator<UpdatePeriodAcceptorDutiesStatusRequest>
{
    public UpdateAcceptorDutiesStatusValidator()
    {
        this.RuleFor(x => x.DeliveryAcceptanceId)
            .NotNull()
            .NotEmpty()
            .WithMessage("ต้องระบุการส่งมอบและตรวจรับงาน ");

        this.RuleFor(x => x.Id)
            .NotNull()
            .NotEmpty()
            .WithMessage("กรุณาระบุงวดส่งมอบและตรวจรับงาน");

        this.RuleFor(x => x.AcceptorId)
            .NotNull()
            .NotEmpty()
            .WithMessage("กรุณาระบุบุคคล/คณะกรรมการตรวจรับพัสดุ");

        this.RuleFor(x => x.IsUnableToPerformDuties)
            .NotNull()
            .WithMessage("กรุณาระบุสถานะการไม่สามารถปฏิบัติหน้าที่ได้");
    }
}

public class UpdatePeriodAcceptorDutiesStatusEndpoint :
    DeliveryAcceptancePeriodEndpointBase<
        UpdatePeriodAcceptorDutiesStatusRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdatePeriodAcceptorDutiesStatusEndpoint(
        ILogger<UpdatePeriodAcceptorDutiesStatusEndpoint> logger,
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/DeliveryAcceptance/Period")
             .WithName("DeliveryAcceptancePeriodUpdateAcceptorDutiesStatus")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<UpdatePeriodAcceptorDutiesStatusRequest>("application/json"));
        this.Put("delivery-acceptance/{DeliveryAcceptanceId:guid?}/period/{Id:guid}/acceptor/{AcceptorId:guid}/duties-status");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdatePeriodAcceptorDutiesStatusRequest req, CancellationToken ct)
    {
        var periodExisting =
            await this.GetById(
                CmDeliveryAcceptanceId.From(req.DeliveryAcceptanceId),
                CmDeliveryAcceptancePeriodId.From(req.Id),
                ct);

        if (periodExisting == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลงวดส่งมอบและตรวจรับงาน");
        }

        var acceptor =
            periodExisting.Acceptors
                          .FirstOrDefault(a =>
                              a.Id == req.AcceptorId &&
                              a.Type == AcceptorType.AcceptanceCommittee);

        if (acceptor is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลบุคคล/คณะกรรมการตรวจรับพัสดุ");
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

        if (periodExisting.HasMajorityRejection())
        {
            periodExisting.SetRejected();
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}