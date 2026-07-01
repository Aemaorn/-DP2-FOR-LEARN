namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoAddendum;

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoAddendum.Abstract;
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

public record RejectPoAddendumRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    CamContractAmendmentId CamContractAmendmentId,
    CamContractAmendmentPoAddendumId Id,
    string? Remark);

public class RejectPoAddendumEndpoint : PoAddendumAbstractEndpoint<RejectPoAddendumRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectPoAddendumEndpoint(ILogger<RejectPoAddendumEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment/PoAddendum")
             .WithName("RejectPoAddendum")
             .Accepts<RejectPoAddendumRequest>("application/json"));

        this.Post("contract-amendment/{CamContractAmendmentId:guid}/po-addendum/{Id:guid}/reject");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RejectPoAddendumRequest req, CancellationToken ct)
    {
        var cam = await this.dbContext.CamContractAmendments
                            .Include(c => c.ContractDraftVendor)
                            .ThenInclude(v => v.PaymentTerms)
                            .Include(c => c.ContractDraftVendor)
                            .ThenInclude(v => v.Vendor)
                            .ThenInclude(v => v.VendorInfo)
                            .Include(c => c.ContractDraftVendor)
                            .ThenInclude(cd => cd.ContractDraft)
                            .ThenInclude(p => p.Procurement)
                            .SingleOrDefaultAsync(c => c.Id == req.CamContractAmendmentId, ct);

        if (cam is null || cam.ContractDraftVendor is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการแก้ไขสัญญาหรือคู่ค้าสัญญาที่เกี่ยวข้อง");
        }

        var po = await this.dbContext.CamContractAmendmentPoAddendums
                           .Include(p => p.Acceptors).ThenInclude(a => a.CommitteePosition)
                           .Include(p => p.Assignees)
                           .SingleOrDefaultAsync(p => p.Id == req.Id && p.CamContractAmendmentId == req.CamContractAmendmentId, ct);

        if (po is null)
        {
            return TypedResults.NotFound("ไม่พบบันทึกต่อท้ายสัญญาที่ระบุ");
        }

        if (po.Status is
            CamContractAmendmentPoAddendumStatus.Draft or
            CamContractAmendmentPoAddendumStatus.Rejected or
            CamContractAmendmentPoAddendumStatus.Approved)
        {
            return TypedResults.BadRequest("ไม่สามารถปฏิเสธเอกสารที่อยู่ในสถานะนี้ได้");
        }

        switch (po.Status)
        {
            case CamContractAmendmentPoAddendumStatus.WaitingCommitteeApproval:
                this.CommitteeReject(po, req);

                break;

            case CamContractAmendmentPoAddendumStatus.WaitingComment:
                this.AssigneeReject(po, req);

                break;

            case CamContractAmendmentPoAddendumStatus.WaitingApproval:
                this.ApproverReject(po, req);

                break;

            default:
                return TypedResults.BadRequest("ไม่สามารถปฏิเสธเอกสารที่อยู่ในสถานะนี้ได้");
        }

        this.dbContext.CamContractAmendmentPoAddendums.Update(po);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private void CommitteeReject(CamContractAmendmentPoAddendum po, RejectPoAddendumRequest req)
    {
        var committeeAcceptor = po.Acceptors
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

        if (po.HasMajorityRejection() || acceptor.IsBoardChairman())
        {
            po.SetRejected(req.Remark);
        }
    }

    private void AssigneeReject(CamContractAmendmentPoAddendum po, RejectPoAddendumRequest req)
    {
        var assignee = po.Assignees
                         .Select(DelegatorExtensions.DelegatorToAssignee)
                         .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                            ? a.UserId == UserId.From(req.UserId)
                                                            : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (assignee is null)
        {
            this.ThrowError("ไม่พบผู้มอบหมาย", StatusCodes.Status400BadRequest);
        }

        var currentAcceptorUser = po.Assignees.FirstOrDefault(a => a.Id == assignee.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(assignee.DelegateeId)
            .Reject(remark: req.Remark);

        po.SetRejected(req.Remark);
    }

    private void ApproverReject(CamContractAmendmentPoAddendum po, RejectPoAddendumRequest req)
    {
        var approverAcceptor = po.Acceptors
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

        var currentAcceptorUser = po.Acceptors.FirstOrDefault(a => a.Id == acceptor.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(acceptor.DelegateeId)
            .Reject(remark: req.Remark);

        po.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            $"ส่งกลับแก้ไข",
            CamContractAmendmentPoAddendumStatus.WaitingComment.ToString(),
            req.Remark));

        po.SetWaitingComment();
    }
}