namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod;

using FluentValidation;
using GHB.DP2.Application.Dtos;
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
using Microsoft.IdentityModel.JsonWebTokens;

public record UpsertAssigneeRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid DeliveryAcceptanceId,
    Guid Id,
    IEnumerable<AssigneeRequest> Assignees);

public class UpsertAssigneeRequestValidator : Validator<UpsertAssigneeRequest>
{
    public UpsertAssigneeRequestValidator()
    {
        this.RuleFor(x => x.DeliveryAcceptanceId)
            .NotEmpty()
            .WithMessage("ต้องมีข้อมูลการส่งมอบตรวจรับ");

        this.RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ต้องมีข้อมูลงวดการส่งมอบตรวจรับ");

        this.RuleFor(x => x.Assignees)
            .NotNull()
            .WithMessage("ผู้รับผิดชอบไม่สามารถเป็นค่าว่างได้")
            .Must(items => items.Any())
            .WithMessage("ต้องมีอย่างน้อย 1 ผู้รับผิดชอบ");
    }
}

public class UpsertAssigneeEndpoint : DeliveryAcceptancePeriodEndpointBase<
    UpsertAssigneeRequest,
    Results<Ok<Guid>, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpsertAssigneeEndpoint(
        ILogger<UpsertAssigneeEndpoint> logger,
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("delivery-acceptance/{DeliveryAcceptanceId:guid?}/period/{Id:guid}/assignee");
        this.Options(b =>
            b.WithTags("ContractManagement/DeliveryAcceptance/Period")
             .WithName("UpsertDeliveryAcceptanceAssignee")
             .Produces<Ok<Guid>>()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>, BadRequest<string>>>
        HandleRequestAsync(UpsertAssigneeRequest req, CancellationToken ct)
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

        var hasPermission =
            periodExisting.Assignees.Any(a =>
                a.UserId == req.UserId &&
                a is
                {
                    IsDeleted: false,
                    Type: AssigneeType.Director,
                });

        if (!hasPermission)
        {
            return TypedResults.BadRequest("ผู้ใช้งานไม่มีสิทธิ์ในการแก้ไขเจ้าหน้าที่พัสดุให้ความเห็น");
        }

        if (periodExisting.Status is not CmDeliveryAcceptancePeriodStatus.WaitingAssign)
        {
            return TypedResults.BadRequest("ไม่สามารถแก้ไขเจ้าหน้าที่พัสดุให้ความเห็นได้ในสถานะปัจจุบัน");
        }

        await this.UpsertAssigneeAsync(
            periodExisting,
            [.. req.Assignees],
            ct);

        this.dbContext.CmDeliveryAcceptancePeriods.Update(periodExisting);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(periodExisting.Id.Value);
    }
}