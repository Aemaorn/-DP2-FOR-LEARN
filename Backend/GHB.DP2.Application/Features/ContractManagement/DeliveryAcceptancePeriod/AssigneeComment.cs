namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod;

using System.IdentityModel.Tokens.Jwt;
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

public record AssigneeCommentRequest(
    Guid DeliveryAcceptanceId,
    Guid Id,
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    string Remark);

public class AssigneeCommentRequestValidator : Validator<AssigneeCommentRequest>
{
    public AssigneeCommentRequestValidator()
    {
        this.RuleFor(x => x.DeliveryAcceptanceId)
            .NotEmpty()
            .WithMessage("ต้องมีข้อมูลการส่งมอบตรวจรับ");

        this.RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ต้องมีข้อมูลงวดการส่งมอบตรวจรับ");

        this.RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("ต้องมีข้อมูลผู้ใช้งาน");

        this.RuleFor(x => x.Remark)
            .NotEmpty()
            .WithMessage("กรุณาระบุความคิดเห็น");
    }
}

public class AssigneeCommentDeliveryAcceptancePeriodEndpoint : DeliveryAcceptancePeriodEndpointBase<
    AssigneeCommentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public AssigneeCommentDeliveryAcceptancePeriodEndpoint(
        ILogger<AssigneeCommentDeliveryAcceptancePeriodEndpoint> logger,
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("delivery-acceptance/{DeliveryAcceptanceId:guid?}/period/{Id:guid}/assignee-comment");
        this.Options(b =>
            b.WithTags("ContractManagement/DeliveryAcceptance/Period")
             .WithName("AssigneeCommentDeliveryAcceptancePeriod")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>>
        HandleRequestAsync(AssigneeCommentRequest req, CancellationToken ct)
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

        if (periodExisting.Status != CmDeliveryAcceptancePeriodStatus.WaitingComment)
        {
            return TypedResults.BadRequest("ผู้รับผิดชอบงวดส่งมอบและตรวจรับงานนี้ไม่ได้อยู่ในสถานะที่สามารถเพิ่มความคิดเห็นได้");
        }

        var assignee =
            periodExisting.Assignees
                          .FirstOrDefault(a => a.UserId == UserId.From(req.UserId));

        if (assignee is null)
        {
            return TypedResults.BadRequest("ผู้ใช้ไม่อยู่ในรายชื่อผู้รับผิดชอบราคากลางนี้");
        }

        periodExisting.AddActivity(new ActivityInfo(
            "เจ้าหน้าที่พัสดุให้ความเห็น",
            $"เจ้าหน้าที่พัสดุให้ความเห็น ข้อมูลขออนุุมัติสั่งซื้อ/สั่งจ้าง(จพ.006)",
            periodExisting.Status.ToString(),
            req.Remark));

        assignee.SetRemark(req.Remark);

        await this.ReplaceDocumentsForStatusAsync(
            periodExisting,
            req.UserId,
            CmDeliveryAcceptancePeriodStatus.WaitingComment,
            ct);

        this.dbContext.CmDeliveryAcceptancePeriods.Update(periodExisting);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}