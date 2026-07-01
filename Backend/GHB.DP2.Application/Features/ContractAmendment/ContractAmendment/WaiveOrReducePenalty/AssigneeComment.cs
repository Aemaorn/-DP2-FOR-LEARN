namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record AssigneeCommentWaiveOrReducePenaltyRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid CamContractAmendmentId,
    Guid WaiveOrReducePenaltyId,
    string? Remark);

public class AssigneeCommentWaiveOrReducePenaltyEndpoint : WaiveOrReducePenaltyEndpointBase<AssigneeCommentWaiveOrReducePenaltyRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    public AssigneeCommentWaiveOrReducePenaltyEndpoint(
        ILogger<AssigneeCommentWaiveOrReducePenaltyEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Put("contract-amendment/{CamContractAmendmentId:guid}/waive-or-reduce-penalty/{WaiveOrReducePenaltyId:guid}/assignee-comment");
        this.Options(b =>
            b.WithTags("ContractAmendment/WaiveOrReducePenalty")
             .WithName("UpdateWaiveOrReducePenaltyAssigneeComment")
             .Accepts<AssigneeCommentWaiveOrReducePenaltyRequest>("application/json"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(AssigneeCommentWaiveOrReducePenaltyRequest req, CancellationToken ct)
    {
        var entity =
            await this.DbContext.CamContractAmendmentWaiveOrReducePenalties
                      .Include(camContractAmendmentWaiveOrReducePenalty => camContractAmendmentWaiveOrReducePenalty.Assignees)
                      .FirstOrDefaultAsync(
                          w =>
                              w.Id == WaiveOrReducePenaltyId.From(req.WaiveOrReducePenaltyId) &&
                              w.CamContractAmendmentId == CamContractAmendmentId.From(req.CamContractAmendmentId),
                          ct);

        if (entity == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการแก้ไขสัญญา");
        }

        var assignee =
            entity.Assignees
                  .FirstOrDefault(a =>
                      a.UserId == UserId.From(req.UserId) && a.Group == AssigneeGroup.Contract);

        if (assignee is null)
        {
            return TypedResults.BadRequest("ไม่พบข้อมูลผู้รับผิดชอบในรายการนี้");
        }

        assignee.SetRemark(req.Remark);

        entity.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Comment,
                "เจ้าหน้าที่ให้ความเห็น",
                entity.Status.ToString(),
                req.Remark));

        this.DbContext.CamContractAmendmentWaiveOrReducePenalties.Update(entity);
        await this.DbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}