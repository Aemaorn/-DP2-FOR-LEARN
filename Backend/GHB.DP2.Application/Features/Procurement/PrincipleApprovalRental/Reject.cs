namespace GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental;

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using GHB.DP2.Application.Extensions;
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

public record RejectPrincipleApprovalRentalRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid PrincipleApprovalRentalId,
    string? Remark);

public class RejectPrincipleApprovalRentalEndpoint : EndpointBase<RejectPrincipleApprovalRentalRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectPrincipleApprovalRentalEndpoint(
        Dp2DbContext dbContext,
        ILogger<RejectPrincipleApprovalRentalEndpoint> logger)
        : base(logger) => this.dbContext = dbContext;

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/PrincipleApprovalRental")
             .WithName("RejectPrincipleApprovalRental")
             .Accepts<RejectPrincipleApprovalRentalEndpoint>("application/json"));

        this.Post("procurement/{procurementId:guid}/principle-approval-rental/{principleApprovalRentalId:guid}/reject");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        RejectPrincipleApprovalRentalRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PPrincipleApprovalRentals
                               .Include(x => x.Acceptors)
                               .Include(x => x.DocumentHistories)
                               .FirstOrDefaultAsync(
                                   x => x.Id == PPrincipleApprovalRentalId.From(req.PrincipleApprovalRentalId)
                                        && x.ProcurementId == ProcurementId.From(req.ProcurementId),
                                   ct);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการอนุมัติหลักการเช่า");
        }

        var replaceApprovalTemplate = entity.LastedNotReplacedDocument(PPrincipleApprovalRentalDocumentType.Approval);
        var replaceWinnerTemplate = entity.LastedNotReplacedDocument(PPrincipleApprovalRentalDocumentType.Winner);

        switch (entity.Status)
        {
            case PPrincipleApprovalRentalStatus.WaitingCommitteeApproval:
                this.CommitteeRejected(entity, req);

                break;

            case PPrincipleApprovalRentalStatus.WaitingUnitApproval:
                this.DepartmentDirectorReject(entity, req);

                break;

            case PPrincipleApprovalRentalStatus.WaitingAssign or PPrincipleApprovalRentalStatus.RejectToAssignee:
                this.AssigneeReject(entity, req);

                break;

            case PPrincipleApprovalRentalStatus.WaitingAcceptance:
                this.ApproverReject(entity, req);

                replaceApprovalTemplate = entity.LastedWaitingCommentNotReplacedDocument(PPrincipleApprovalRentalDocumentType.Approval);
                replaceWinnerTemplate = entity.LastedWaitingCommentNotReplacedDocument(PPrincipleApprovalRentalDocumentType.Winner);

                break;

            default:
                return TypedResults.BadRequest("ไม่สามารถปฏิเสธเอกสารที่อยู่ในสถานะนี้ได้");
        }

        if (replaceApprovalTemplate is not null)
        {
            entity.AddDocumentHistory(PPrincipleApprovalRentalDocumentType.Approval, replaceApprovalTemplate.FileId, false);
        }

        if (replaceWinnerTemplate is not null)
        {
            entity.AddDocumentHistory(PPrincipleApprovalRentalDocumentType.Winner, replaceWinnerTemplate.FileId, false);
        }

        this.dbContext.PPrincipleApprovalRentals.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private void CommitteeRejected(PPrincipleApprovalRental entity, RejectPrincipleApprovalRentalRequest req)
    {
        var acceptors = entity.Acceptors
                              .Where(a => a is { Type: AcceptorType.RentCommittee, IsActive: true })
                              .OrderBy(a => a.Sequence)
                              .ToList();

        var current = acceptors.FirstOrDefault(a => a.UserId == req.UserId);

        if (current is null)
        {
            this.ThrowError("ไม่พบผู้ส่งกลับแก้ไขที่ใช้งานได้", StatusCodes.Status400BadRequest);
        }

        current.Reject(req.Remark);

        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.CommitteeReject,
            ActivityLogActionTypeConstant.CommitteeReject,
            entity.Status.ToString(),
            req.Remark));

        if (entity.HasMajorityRejection() || current.IsBoardChairman())
        {
            entity.SetRejected();
        }
    }

    private void DepartmentDirectorReject(PPrincipleApprovalRental entity, RejectPrincipleApprovalRentalRequest req)
    {
        var departmentDirectorAcceptor =
            entity.Acceptors
                  .Where(a => a is
                  {
                      Type: AcceptorType.DepartmentDirectorAgree,
                      IsActive: true,
                      Status: AcceptorStatus.Pending
                  })
                  .Select(DelegatorExtensions.DelegatorToAcceptor)
                  .ToArray();

        var acceptor =
            departmentDirectorAcceptor.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                        ? a.UserId == UserId.From(req.UserId)
                                        : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้ส่งกลับแก้ไขที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        var currentUser = entity.Acceptors.FirstOrDefault(a => a.Id == acceptor.Id);

        if (currentUser == null)
        {
            this.ThrowError(
                    "ไม่พบผู้ส่งกลับแก้ไขที่ใช้งานได้",
                    StatusCodes.Status400BadRequest);
        }

        currentUser.SetDelegatee(acceptor.DelegateeId)
                   .Reject(req.Remark);

        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.DepartmentReject,
            ActivityLogActionTypeConstant.DepartmentReject,
            entity.Status.ToString(),
            req.Remark));

        entity.SetRejected();
    }

    private void AssigneeReject(PPrincipleApprovalRental entity, RejectPrincipleApprovalRentalRequest req)
    {
        var departmentDirectorAcceptor =
            entity.Assignees
                  .Select(DelegatorExtensions.DelegatorToAssignee)
                  .ToArray();

        var assignee =
            departmentDirectorAcceptor.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                        ? a.UserId == UserId.From(req.UserId)
                                        : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (assignee is null)
        {
            this.ThrowError(
                "ไม่พบผู้ส่งกลับแก้ไขที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        var currentUser = entity.Assignees.FirstOrDefault(a => a.Id == assignee.Id);

        if (currentUser == null)
        {
            this.ThrowError(
                    "ไม่พบผู้ส่งกลับแก้ไขที่ใช้งานได้",
                    StatusCodes.Status400BadRequest);
        }

        currentUser.SetDelegatee(assignee.DelegateeId)
                   .Reject(req.Remark);

        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.AssigneeReject,
            ActivityLogActionTypeConstant.AssigneeReject,
            entity.Status.ToString(),
            req.Remark));

        entity.SetRejected();
    }

    private void ApproverReject(PPrincipleApprovalRental entity, RejectPrincipleApprovalRentalRequest req)
    {
        var approverAcceptor =
            entity.Acceptors
                  .Where(a => a is
                  {
                      Type: AcceptorType.Approver,
                      IsActive: true,
                      Status: AcceptorStatus.Pending
                  })
                  .Select(DelegatorExtensions.DelegatorToAcceptor)
                  .ToArray();

        var acceptor =
            approverAcceptor.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                        ? a.UserId == UserId.From(req.UserId)
                                        : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้ส่งกลับแก้ไขที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        var currentUser = entity.Acceptors.FirstOrDefault(a => a.Id == acceptor.Id);

        if (currentUser == null)
        {
            this.ThrowError(
                    "ไม่พบผู้ส่งกลับแก้ไขที่ใช้งานได้",
                    StatusCodes.Status400BadRequest);
        }

        currentUser.SetDelegatee(acceptor.DelegateeId)
                   .Reject(req.Remark);

        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            ActivityLogActionTypeConstant.Reject,
            entity.Status.ToString(),
            req.Remark));

        entity.SetRejectToAssignee();
    }
}