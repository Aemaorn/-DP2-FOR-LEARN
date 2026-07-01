namespace GHB.DP2.Application.Features.Procurement.PrincipleApproval;

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using GHB.DP2.Application.Extensions;
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

public record RejectPrincipleApprovalRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid Id,
    string? Remark
);

public class RejectPrincipleApprovalEndpoint : EndpointBase<RejectPrincipleApprovalRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectPrincipleApprovalEndpoint(Dp2DbContext dbContext, ILogger<RejectPrincipleApprovalRequest> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/PrincipleApproval")
             .WithName("RejectPrincipleApproval")
             .AllowAnonymous()
             .Accepts<RejectPrincipleApprovalRequest>("application/json"));
        this.Post("procurement/{procurementId:guid}/principle-approval/{id:guid}/reject");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RejectPrincipleApprovalRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PPrincipleApprovals
                               .Include(x => x.PrincipleApprovalAcceptors)
                               .Include(x => x.DocumentHistories)
                               .SingleOrDefaultAsync(
                                   x =>
                                       x.Id == PPrincipleApprovalId.From(req.Id) &&
                                       x.ProcurementId == ProcurementId.From(req.ProcurementId),
                                   ct);

        if (entity == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลข้อมูลอนุมัติหลักการ");
        }

        if (entity.Status is
            PPrincipleApprovalStatus.Draft or
            PPrincipleApprovalStatus.Rejected or
            PPrincipleApprovalStatus.Approved)
        {
            return TypedResults.BadRequest("ไม่สามารถปฏิเสธสถานะนี้ได้");
        }

        switch (entity.Status)
        {
            case PPrincipleApprovalStatus.WaitingUnitApproval:
                this.DepartmentDirectorReject(entity, req);

                break;

            case PPrincipleApprovalStatus.WaitingAssign:
                this.AssigneeReject(entity, req);

                break;

            case PPrincipleApprovalStatus.WaitingAcceptance:
                this.ApproverReject(entity, req);

                break;

            default:
                return TypedResults.BadRequest("ไม่สามารถปฏิเสธเอกสารที่อยู่ในสถานะนี้ได้");
        }

        var replaceTemplate = entity.LastedNotReplacedDocument;
        if (replaceTemplate is not null)
        {
            entity.AddDocumentHistory(replaceTemplate.FileId, false);
        }

        this.dbContext.PPrincipleApprovals.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private void DepartmentDirectorReject(PPrincipleApproval entity, RejectPrincipleApprovalRequest req)
    {
        var departmentDirectorAcceptor =
            entity.PrincipleApprovalAcceptors
                  .Where(a => a is
                  {
                      Type: AcceptorType.DepartmentDirectorAgree,
                      IsActive: true,
                      Status: AcceptorStatus.Pending
                  })
                  .ToArray();

        var acceptor =
            departmentDirectorAcceptor.Select(DelegatorExtensions.DelegatorToAcceptor)
                                      .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                            ? a.UserId == UserId.From(req.UserId)
                                                            : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้ส่งกลับแก้ไขที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        var currentUser = entity.PrincipleApprovalAcceptors.FirstOrDefault(a => a.Id == acceptor.Id);

        if (currentUser == null)
        {
            this.ThrowError(
                    "ไม่พบผู้ส่งกลับแก้ไขที่ใช้งานได้",
                    StatusCodes.Status400BadRequest);
        }

        currentUser
            .SetDelegatee(acceptor.DelegateeId)
            .Reject(remark: req.Remark);

        entity.SetRejected(req.Remark);
    }

    private void AssigneeReject(PPrincipleApproval entity, RejectPrincipleApprovalRequest req)
    {
        var departmentDirectorAcceptor =
            entity.PrincipleApprovalAssignees
                  .ToArray();

        var assignee =
            departmentDirectorAcceptor.Select(DelegatorExtensions.DelegatorToAssignee)
                                      .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                            ? a.UserId == UserId.From(req.UserId)
                                                            : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (assignee is null)
        {
            this.ThrowError(
                "ไม่พบผู้ส่งกลับแก้ไขที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        var currentUser = entity.PrincipleApprovalAssignees.FirstOrDefault(a => a.Id == assignee.Id);

        if (currentUser == null)
        {
            this.ThrowError(
                    "ไม่พบผู้ส่งกลับแก้ไขที่ใช้งานได้",
                    StatusCodes.Status400BadRequest);
        }

        currentUser
            .SetDelegatee(assignee.DelegateeId)
            .Reject(remark: req.Remark);

        entity.SetRejected(req.Remark);
    }

    private void ApproverReject(PPrincipleApproval entity, RejectPrincipleApprovalRequest req)
    {
        var approverAcceptor =
            entity.PrincipleApprovalAcceptors
                  .Where(a => a is
                  {
                      Type: AcceptorType.Approver,
                      IsActive: true,
                      Status: AcceptorStatus.Pending
                  })
                  .ToArray();

        var acceptor =
            approverAcceptor.Select(DelegatorExtensions.DelegatorToAcceptor)
                                      .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                            ? a.UserId == UserId.From(req.UserId)
                                                            : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้ส่งกลับแก้ไขที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        var currentUser = entity.PrincipleApprovalAcceptors.FirstOrDefault(a => a.Id == acceptor.Id);

        if (currentUser == null)
        {
            this.ThrowError(
                    "ไม่พบผู้ส่งกลับแก้ไขที่ใช้งานได้",
                    StatusCodes.Status400BadRequest);
        }

        currentUser
            .SetDelegatee(acceptor.DelegateeId)
            .Reject(remark: req.Remark);

        entity.SetRejectToAssignee();
    }
}