namespace GHB.DP2.Application.Features.Procurement.PrincipleApproval;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.PrincipleApproval.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CommentPrincipleApprovalRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid Id,
    string Remark
);

public class CommentPrincipleApprovalEndpoint : PrincipleApprovalEndpointBase<CommentPrincipleApprovalRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CommentPrincipleApprovalEndpoint(Dp2DbContext dbContext, ILogger<CommentPrincipleApprovalRequest> logger)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/PrincipleApproval")
             .WithName("CommentPrincipleApproval")
             .AllowAnonymous()
             .Accepts<CommentPrincipleApprovalRequest>("application/json"));
        this.Post("procurement/{procurementId:guid}/principle-approval/{id:guid}/comment");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(CommentPrincipleApprovalRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PPrincipleApprovals
                               .Include(x => x.PrincipleApprovalAssignees)
                               .SingleOrDefaultAsync(
                                   x =>
                                       x.Id == PPrincipleApprovalId.From(req.Id) &&
                                       x.ProcurementId == ProcurementId.From(req.ProcurementId),
                                   ct);

        if (entity == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลข้อมูลอนุมัติหลักการ");
        }

        var assignee =
            entity.PrincipleApprovalAssignees
                  .Select(DelegatorExtensions.DelegatorToAssignee)
                  .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                       ? a.UserId == req.UserId
                                       : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (assignee is null)
        {
            return TypedResults.BadRequest("ไม่พบข้อมูลผู้รับผิดชอบในรายการนี้");
        }

        entity.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Comment,
                "เจ้าหน้าที่พัสดุให้ความเห็น",
                entity.Status.ToString(),
                req.Remark));

        var currentUser =
                entity.PrincipleApprovalAssignees
                  .FirstOrDefault(a => a.Id == assignee.Id);

        if (currentUser == null)
        {
            this.ThrowError(
                    "ไม่พบผู้อนุมัติที่ใช้งานได้",
                    StatusCodes.Status400BadRequest);
        }

        currentUser
            .SetDelegatee(assignee.DelegateeId)
            .SetRemark(remark: req.Remark);

        await this.ReplaceDocumentsAsync(entity, false, false, ct);

        this.dbContext.PPrincipleApprovals.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}