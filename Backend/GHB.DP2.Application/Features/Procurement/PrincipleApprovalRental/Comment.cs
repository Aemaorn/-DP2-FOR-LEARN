namespace GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Abstact;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CommentPrincipleApprovalRentalRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)] Guid UserId,
    Guid ProcurementId,
    Guid Id,
    string Remark
);

public class CommentPrincipleApprovalRentalEndpoint : PrincipleApprovalRentalEndpointBase<CommentPrincipleApprovalRentalRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CommentPrincipleApprovalRentalEndpoint(
        Dp2DbContext dbContext,
        ILogger<CommentPrincipleApprovalRentalRequest> logger,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/PrincipleApprovalRental")
             .WithName("CommentPrincipleApprovalRental")
             .Accepts<CommentPrincipleApprovalRentalRequest>("application/json"));
        this.Post("procurement/{procurementId:guid}/principle-approval-rental/{id:guid}/comment");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(CommentPrincipleApprovalRentalRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PPrincipleApprovalRentals
                               .Include(x => x.Acceptors)
                               .Include(pPrincipleApprovalRental => pPrincipleApprovalRental.Assignees)
                               .Include(pp => pp.Procurement)
                               .ThenInclude(p => p.PrincipleApprovals)
                               .SingleOrDefaultAsync(
                                   x =>
                                       x.Id == PPrincipleApprovalRentalId.From(req.Id) &&
                                       x.ProcurementId == ProcurementId.From(req.ProcurementId),
                                   ct);

        if (entity == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลข้อมูลอนุมัติหลักการ");
        }

        var assignee =
            entity.Assignees
                  .Select(DelegatorExtensions.DelegatorToAssignee)
                  .FirstOrDefault(a => a.Group == AssigneeGroup.JorPor
                                    && a.Delegatee?.SuUserId == null
                                        ? a.UserId == UserId.From(req.UserId)
                                        : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (assignee is null)
        {
            return TypedResults.BadRequest("ไม่พบข้อมูลผู้รับผิดชอบในรายการนี้");
        }

        var currentUser =
            entity.Assignees
              .FirstOrDefault(a => a.Id == assignee.Id);

        if (currentUser == null)
        {
            this.ThrowError(
                    "ไม่พบผู้อนุมัติที่ใช้งานได้",
                    StatusCodes.Status400BadRequest);
        }

        currentUser.SetDelegatee(assignee.DelegateeId)
                   .SetRemark(req.Remark);

        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Comment,
            ActivityLogActionTypeConstant.Comment,
            entity.Status.ToString(),
            req.Remark));

        this.dbContext.PPrincipleApprovalRentals.Update(entity);

        await this.ReplaceDocumentsAsync(
            entity,
            entity.Procurement.PrincipleApprovals.FirstOrDefault()!,
            ct,
            false);

        await this.dbContext.SaveChangesAsync(ct);
        return TypedResults.Ok();
    }
}