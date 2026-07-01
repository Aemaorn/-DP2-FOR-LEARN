namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod;

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record AssigneeRejectDeliveryAcceptancePeriodRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid DeliveryAcceptanceId,
    Guid Id,
    string? Remark
);

public class AssigneeRejectDeliveryAcceptancePeriodValidator : Validator<AssigneeRejectDeliveryAcceptancePeriodRequest>
{
    public AssigneeRejectDeliveryAcceptancePeriodValidator()
    {
        this.RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ต้องระบุผู้ใช้งาน");

        this.RuleFor(x => x.DeliveryAcceptanceId)
            .NotEmpty().WithMessage("ต้องระบุการส่งมอบและตรวจรับงาน");

        this.RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ต้องระบุงวดส่งมอบและตรวจรับงาน");
    }
}

public class AssigneeRejectDeliveryAcceptancePeriodEndpoint :
    DeliveryAcceptancePeriodEndpointBase<
        AssigneeRejectDeliveryAcceptancePeriodRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public AssigneeRejectDeliveryAcceptancePeriodEndpoint(
        Dp2DbContext dbContext,
        ILogger<AssigneeRejectDeliveryAcceptancePeriodEndpoint> logger,
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
             .WithName("AssigneeRejectDeliveryAcceptancePeriod")
             .AllowAnonymous()
             .Accepts<AssigneeRejectDeliveryAcceptancePeriodRequest>("application/json"));
        this.Put("delivery-acceptance/{DeliveryAcceptanceId:guid?}/period/{Id:guid}/assignee-reject");
    }

    protected override async ValueTask
        <Results<Ok, NotFound<string>, BadRequest<string>>>
        HandleRequestAsync(
            AssigneeRejectDeliveryAcceptancePeriodRequest req,
            CancellationToken ct)
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

        if (periodExisting.Status != CmDeliveryAcceptancePeriodStatus.WaitingComment && periodExisting.Status != CmDeliveryAcceptancePeriodStatus.RejectToAssignee && periodExisting.Status != CmDeliveryAcceptancePeriodStatus.WaitingAssign)
        {
            return TypedResults.BadRequest("ไม่สามารถปฏิเสธเอกสารที่อยู่ในสถานะนี้ได้");
        }

        this.AssigneeReject(periodExisting, req);

        this.dbContext.CmDeliveryAcceptancePeriods.Update(periodExisting);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private void AssigneeReject(
        CmDeliveryAcceptancePeriod periodExisting,
        AssigneeRejectDeliveryAcceptancePeriodRequest req)
    {
        var currentAssignee =
            periodExisting.Assignees
                          .FirstOrDefault(a =>
                              a.UserId == UserId.From(req.UserId) &&
                              a.Status is AssigneeStatus.Pending);

        if (currentAssignee is null)
        {
            this.ThrowError(
                "ไม่พบเจ้าหน้าที่พัสดุที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAssignee.Reject(req.Remark);

        periodExisting.SetRejected(req.Remark);
    }
}