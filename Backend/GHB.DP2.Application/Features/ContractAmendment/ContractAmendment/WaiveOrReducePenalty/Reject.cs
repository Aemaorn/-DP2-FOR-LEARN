namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record RejectWaiveOrReducePenaltyRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid CamContractAmendmentId,
    Guid WaiveOrReducePenaltyId,
    string? Remark);

public class RejectWaiveOrReducePenaltyEndpoint : WaiveOrReducePenaltyEndpointBase<RejectWaiveOrReducePenaltyRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    public RejectWaiveOrReducePenaltyEndpoint(ILogger<RejectWaiveOrReducePenaltyEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment/WaiveOrReducePenalty")
             .WithName("RejectWaiveOrReducePenalty")
             .Accepts<RejectWaiveOrReducePenaltyRequest>("application/json"));
        this.Post("contract-amendment/{CamContractAmendmentId:guid}/waive-or-reduce-penalty/{WaiveOrReducePenaltyId:guid}/reject");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RejectWaiveOrReducePenaltyRequest req, CancellationToken ct)
    {
        var entity =
            await this.DbContext.CamContractAmendmentWaiveOrReducePenalties
                      .FirstOrDefaultAsync(
                          e =>
                              e.Id == WaiveOrReducePenaltyId.From(req.WaiveOrReducePenaltyId)
                              && e.CamContractAmendmentId == CamContractAmendmentId.From(req.CamContractAmendmentId),
                          ct);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการแก้ไขสัญญาที่ระบุ");
        }

        if (entity.Status is
            CamContractAmendmentWaiveOrReducePenaltyStatus.Draft or
            CamContractAmendmentWaiveOrReducePenaltyStatus.Rejected or
            CamContractAmendmentWaiveOrReducePenaltyStatus.Approved)
        {
            return TypedResults.BadRequest("ไม่สามารถปฏิเสธเอกสารที่อยู่ในสถานะนี้ได้");
        }

        switch (entity.Status)
        {
            case CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingCommitteeApproval:
                this.CommitteeReject(entity, req);

                break;

            case CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingComment:
                this.AssigneeReject(entity, req);

                break;

            case CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingApproval:
                this.ApproverReject(entity, req);

                break;

            default:
                return TypedResults.BadRequest("ไม่สามารถปฏิเสธเอกสารที่อยู่ในสถานะนี้ได้");
        }

        this.DbContext.CamContractAmendmentWaiveOrReducePenalties.Update(entity);
        await this.DbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private void CommitteeReject(CamContractAmendmentWaiveOrReducePenalty entity, RejectWaiveOrReducePenaltyRequest req)
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

    private void AssigneeReject(CamContractAmendmentWaiveOrReducePenalty entity, RejectWaiveOrReducePenaltyRequest req)
    {
        var assignee = entity.Assignees.FirstOrDefault(a => a.UserId == req.UserId);

        if (assignee is null)
        {
            this.ThrowError("ไม่พบผู้มอบหมาย", StatusCodes.Status400BadRequest);
        }

        assignee.Reject(req.Remark);

        entity.SetRejected(req.Remark);
    }

    private void ApproverReject(CamContractAmendmentWaiveOrReducePenalty entity, RejectWaiveOrReducePenaltyRequest req)
    {
        var approverAcceptor = entity.Acceptors
                                     .Where(a => a is
                                     {
                                         Type: AcceptorType.Approver,
                                         IsActive: true,
                                         Status: AcceptorStatus.Pending
                                     })
                                     .ToArray();

        var acceptor = approverAcceptor.FirstOrDefault(a => a.UserId == req.UserId);

        if (acceptor is null)
        {
            this.ThrowError("ไม่พบผู้อนุมัติที่ใช้งานได้", StatusCodes.Status400BadRequest);
        }

        acceptor.Reject(req.Remark);
        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            $"ส่งกลับแก้ไข",
            nameof(CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingComment),
            req.Remark));

        entity.SetWaitingComment();
    }
}