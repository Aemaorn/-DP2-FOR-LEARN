namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration;

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
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

public record RejectAdjustContractDurationRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid CamContractAmendmentId,
    Guid Id,
    string? Remark);

public class RejectAdjustContractDurationEndpoint : AdjustContractDurationEndpointBase<RejectAdjustContractDurationRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    public RejectAdjustContractDurationEndpoint(ILogger<RejectAdjustContractDurationEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment/AdjustContractDuration")
             .WithName("RejectAdjustContractDuration")
             .Accepts<RejectAdjustContractDurationRequest>("application/json"));
        this.Post("contract-amendment/{CamContractAmendmentId:guid}/adjust-contract-duration/{Id:guid}/reject");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RejectAdjustContractDurationRequest req, CancellationToken ct)
    {
        var entity =
            await this.DbContext.CamContractAmendmentExtendChanges
                      .FirstOrDefaultAsync(
                          e =>
                              e.Id == ContractAmendmentExtendChangeId.From(req.Id)
                              && e.CamContractAmendmentId == CamContractAmendmentId.From(req.CamContractAmendmentId),
                          ct);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการแก้ไขสัญญาที่ระบุ");
        }

        switch (entity.Status)
        {
            case ContractAmendmentExtendChangeStatus.WaitingCommitteeApproval:
                this.CommitteeReject(entity, req);

                break;

            case ContractAmendmentExtendChangeStatus.WaitingComment:
                this.AssigneeReject(entity, req);

                break;

            case ContractAmendmentExtendChangeStatus.WaitingApproval:
                this.ApproverReject(entity, req);

                break;

            default:
                return TypedResults.BadRequest("ไม่สามารถปฏิเสธเอกสารที่อยู่ในสถานะนี้ได้");
        }

        await this.DbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private void CommitteeReject(CamContractAmendmentExtendChange entity, RejectAdjustContractDurationRequest req)
    {
        var committeeAcceptor = entity.Acceptors
                                      .Where(a => a is
                                      {
                                          Type: AcceptorType.AcceptanceCommittee,
                                          IsActive: true,
                                          IsUnableToPerformDuties: false,
                                          Status: AcceptorStatus.Pending
                                      })
                                      .ToArray();

        var acceptor = committeeAcceptor.FirstOrDefault(a => a.UserId == req.UserId);

        if (acceptor is null)
        {
            this.ThrowError("ไม่พบผู้อนุมัติคณะกรรมการที่ใช้งานได้", StatusCodes.Status400BadRequest);
        }

        acceptor.Reject(req.Remark);

        if (entity.HasMajorityRejection() || acceptor.IsBoardChairman())
        {
            entity.SetRejected(req.Remark);
        }
    }

    private void AssigneeReject(CamContractAmendmentExtendChange entity, RejectAdjustContractDurationRequest req)
    {
        var assignee = entity.Assignees
                             .Select(DelegatorExtensions.DelegatorToAssignee)
                             .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                            ? a.UserId == UserId.From(req.UserId)
                                                            : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (assignee is null)
        {
            this.ThrowError("ไม่พบผู้มอบหมาย", StatusCodes.Status400BadRequest);
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
            .Reject(remark: req.Remark);

        entity.SetRejected(req.Remark);
    }

    private void ApproverReject(CamContractAmendmentExtendChange entity, RejectAdjustContractDurationRequest req)
    {
        var approverAcceptor = entity.Acceptors
                                     .Where(a => a is
                                     {
                                         Type: AcceptorType.Approver,
                                         IsActive: true,
                                         Status: AcceptorStatus.Pending
                                     })
                                     .Select(DelegatorExtensions.DelegatorToAcceptor)
                                     .ToArray();

        var acceptor = approverAcceptor.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                            ? a.UserId == UserId.From(req.UserId)
                                                            : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError("ไม่พบผู้อนุมัติที่ใช้งานได้", StatusCodes.Status400BadRequest);
        }

        var currentAcceptorUser = entity.Acceptors.FirstOrDefault(a => a.Id == acceptor.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(acceptor.DelegateeId)
            .Reject(remark: req.Remark);

        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            $"ส่งกลับแก้ไข",
            nameof(ContractAmendmentExtendChangeStatus.WaitingComment),
            req.Remark));

        entity.SetWaitingComment();
    }
}