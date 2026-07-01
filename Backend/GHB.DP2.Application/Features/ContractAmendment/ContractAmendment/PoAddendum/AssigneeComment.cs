namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoAddendum;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoAddendum;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record PoAddendumAssigneeCommentRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)] Guid UserId,
    Guid CamContractAmendmentId,
    Guid Id,
    string? Remark);

public class PoAddendumAssigneeCommentEndpoint : EndpointBase<PoAddendumAssigneeCommentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public PoAddendumAssigneeCommentEndpoint(
        Dp2DbContext dbContext,
        ILogger<PoAddendumAssigneeCommentEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("contract-amendment/{CamContractAmendmentId:guid}/po-addendum/{Id:guid}/assignee-comment");
        this.Options(b =>
            b.WithTags("ContractAmendment/PoAddendum")
             .WithName("UpdatePoAddendumAssigneeComment")
             .Accepts<PoAddendumAssigneeCommentRequest>("application/json"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(PoAddendumAssigneeCommentRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.CamContractAmendmentPoAddendums
                               .Include(a => a.Assignees)
                               .SingleOrDefaultAsync(w => w.Id == CamContractAmendmentPoAddendumId.From(req.Id) && w.CamContractAmendmentId == CamContractAmendmentId.From(req.CamContractAmendmentId), ct);

        if (entity == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลบันทึกต่อท้ายสัญญา");
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

        var currentAcceptorUser = entity.Assignees.FirstOrDefault(a => a.Id == assignee.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(assignee.DelegateeId)
            .SetRemark(remark: req.Remark);

        entity.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Comment,
                "เจ้าหน้าที่ให้ความเห็น",
                entity.Status.ToString(),
                req.Remark));

        this.dbContext.CamContractAmendmentPoAddendums.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);
        return TypedResults.Ok();
    }
}