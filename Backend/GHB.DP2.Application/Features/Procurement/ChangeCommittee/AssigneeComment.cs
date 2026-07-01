namespace GHB.DP2.Application.Features.Procurement.ChangeCommittee;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.ChangeCommittee.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.ChangeCommittee;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record AssigneeCommentChangeCommitteeRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)] Guid UserId,
    Guid ChangeCommitteeId,
    string? Reason);

public class AssigneeCommentChangeCommitteeEndpoint : ChangeCommitteeEndpointBase<AssigneeCommentChangeCommitteeRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public AssigneeCommentChangeCommitteeEndpoint(
        Dp2DbContext dbContext,
        ILogger<AssigneeCommentChangeCommitteeEndpoint> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("ChangeCommittee"));
        this.Post("change-committee/{changeCommitteeId:guid}/assignee-comment");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(AssigneeCommentChangeCommitteeRequest req, CancellationToken ct)
    {
        var changeCommitteeId = CommitteeChangeId.From(req.ChangeCommitteeId);

        var changeCommittee = await this.dbContext.CommitteeChanges
                                        .Include(c => c.Assignees)
                                        .FirstOrDefaultAsync(x => x.Id == changeCommitteeId, ct);

        if (changeCommittee == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการเปลี่ยนแปลงคณะกรรมการ");
        }

        var assignee =
            changeCommittee.Assignees
                           .Select(DelegatorExtensions.DelegatorToAssignee)
                           .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                    ? a.UserId == UserId.From(req.UserId)
                                                    : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (assignee is null)
        {
            return TypedResults.BadRequest("ไม่พบข้อมูลผู้รับผิดชอบในรายการนี้");
        }

        var currentAssignee = changeCommittee.Assignees.FirstOrDefault(a => a.Id == assignee.Id);

        if (currentAssignee is null)
        {
            return TypedResults.BadRequest("ไม่พบข้อมูลผู้รับผิดชอบ");
        }

        currentAssignee
            .SetDelegatee(assignee.DelegateeId)
            .SetRemark(req.Reason);

        changeCommittee.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Comment,
            "เจ้าหน้าที่พัสดุให้ความเห็น",
            changeCommittee.Status.ToString(),
            req.Reason));

        this.dbContext.CommitteeChanges.Update(changeCommittee);
        await this.dbContext.SaveChangesAsync(ct);

        await this.ReplaceDocumentsAsync(changeCommittee, false, ct);

        return TypedResults.Ok();
    }
}
