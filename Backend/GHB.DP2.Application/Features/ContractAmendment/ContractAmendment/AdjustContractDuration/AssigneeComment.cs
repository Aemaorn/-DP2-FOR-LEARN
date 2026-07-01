namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record AssigneeCommentAdjustContractDurationRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid CamContractAmendmentId,
    Guid Id,
    string? Remark);

public class AssigneeCommentAdjustContractDurationEndpoint : AdjustContractDurationEndpointBase<AssigneeCommentAdjustContractDurationRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    public AssigneeCommentAdjustContractDurationEndpoint(ILogger<AssigneeCommentAdjustContractDurationEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Put("contract-amendment/{CamContractAmendmentId:guid}/adjust-contract-duration/{Id:guid}/assignee-comment");
        this.Options(b =>
            b.WithTags("ContractAmendment/AdjustContractDuration")
             .WithName("UpdateAdjustContractDurationAssigneeComment")
             .Accepts<AssigneeCommentAdjustContractDurationRequest>("application/json"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(AssigneeCommentAdjustContractDurationRequest req, CancellationToken ct)
    {
        var entity =
            await this.DbContext.CamContractAmendmentExtendChanges.Include(camContractAmendmentExtendChange => camContractAmendmentExtendChange.Assignees)
                      .FirstOrDefaultAsync(
                          w =>
                              w.Id == ContractAmendmentExtendChangeId.From(req.Id) &&
                              w.CamContractAmendmentId == CamContractAmendmentId.From(req.CamContractAmendmentId),
                          ct);

        if (entity == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการแก้ไขสัญญา");
        }

        var assignee =
            entity.Assignees
                  .Select(DelegatorExtensions.DelegatorToAssignee)
                  .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                             ? a.UserId == UserId.From(req.UserId)
                                             : a.Delegatee?.SuUserId == UserId.From(req.UserId)
                                       && a.Group == AssigneeGroup.Contract);

        if (assignee is null)
        {
            return TypedResults.BadRequest("ไม่พบข้อมูลผู้รับผิดชอบในรายการนี้");
        }

        var currentUser = entity.Assignees.FirstOrDefault(a => a.Id == assignee.Id);

        if (currentUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentUser
            .SetDelegatee(assignee.DelegateeId)
            .SetRemark(remark: req.Remark);

        entity.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Comment,
                "เจ้าหน้าที่ให้ความเห็น",
                entity.Status.ToString(),
                req.Remark));

        this.DbContext.CamContractAmendmentExtendChanges.Update(entity);
        await this.DbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}