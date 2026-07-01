namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoAddendum;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
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

public record ApprovePoAddendumRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    CamContractAmendmentId CamContractAmendmentId,
    CamContractAmendmentPoAddendumId Id,
    string? Remark);

public class ApprovePoAddendumEndpoint : PoAddendumAbstractEndpoint<ApprovePoAddendumRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ApprovePoAddendumEndpoint(ILogger<ApprovePoAddendumEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b => b.WithTags("ContractAmendment/PoAddendum").WithName("ApprovePoAddendum").AllowAnonymous().Accepts<ApprovePoAddendumRequest>("application/json"));
        this.Post("contract-amendment/{CamContractAmendmentId:guid}/po-addendum/{Id:guid}/approve");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApprovePoAddendumRequest req, CancellationToken ct)
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
            .Include(p => p.Acceptors)
            .Include(p => p.Assignees)
            .SingleOrDefaultAsync(p => p.Id == req.Id && p.CamContractAmendmentId == req.CamContractAmendmentId, ct);

        if (po is null)
        {
            return TypedResults.NotFound("ไม่พบบันทึกต่อท้ายสัญญาที่ระบุ");
        }

        switch (po.Status)
        {
            case CamContractAmendmentPoAddendumStatus.WaitingCommitteeApproval:
                this.CommitteeApproved(po, req);
                UpdateCommitteeCurrents(po);

                break;

            case CamContractAmendmentPoAddendumStatus.WaitingApproval:
                this.AcceptorApproved(po, req);
                UpdateSequentialCurrents(po, AcceptorType.Approver);

                await this.UpdateDocumentAsync(cam, po, UserId.From(req.UserId), isReplace: true, hasCreator: false, hasAcceptor: true, ct);
                break;
        }

        this.dbContext.CamContractAmendmentPoAddendums.Update(po);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private void CommitteeApproved(CamContractAmendmentPoAddendum po, ApprovePoAddendumRequest req)
    {
        var acceptors = po.Acceptors
                          .Where(a => a is { IsActive: true, IsUnableToPerformDuties: false, Status: AcceptorStatus.Pending, Type: AcceptorType.AcceptanceCommittee })
                          .OrderBy(a => a.Sequence)
                          .ToList();

        var current = acceptors.FirstOrDefault(a => a.UserId == UserId.From(req.UserId));

        if (current == null)
        {
            this.ThrowError("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้", StatusCodes.Status400BadRequest);
        }

        current.Approve(req.Remark);
        current.SetCurrent(false);
        po.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.CommitteeApproved,
            $"คณะกรรมการเห็นชอบ",
            po.Status.ToString(),
            req.Remark));

        if (current.IsBoardChairman())
        {
            po.SetWaitingAssigned();
        }
    }

    private static void UpdateCommitteeCurrents(CamContractAmendmentPoAddendum po)
    {
        var committee = po.Acceptors
                          .Where(a => a.Type == AcceptorType.TorDraftCommittee && a.IsActive && !a.IsUnableToPerformDuties)
                          .ToList();

        if (committee.Count == 0)
        {
            return;
        }

        var chairman = committee.FirstOrDefault(a =>
            (a.CommitteePositionsCode != null && a.CommitteePositionsCode == ParameterCode.From("PosBoard001")) || a.IsBoardChairman());
        var nonChair = chairman is null ? committee : [.. committee.Where(a => a.Id != chairman.Id)];

        foreach (var a in committee)
        {
            a.SetCurrent(false);
        }

        var pendingNonChair = nonChair.Where(a => a.Status == AcceptorStatus.Pending).ToList();
        if (pendingNonChair.Count > 0)
        {
            foreach (var p in pendingNonChair)
            {
                p.SetCurrent(true);
            }

            return;
        }

        var allNonChairReady = nonChair.All(a => a.Status == AcceptorStatus.Approved || a.IsUnableToPerformDuties);
        if (chairman is not null && chairman.Status == AcceptorStatus.Pending && allNonChairReady)
        {
            _ = SendNotificationAsync(po, chairman.UserId, NotificationConstant.WaitForLike.Title, string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.ContractAmendment.Name, string.Empty));
            chairman.SetCurrent(true);
        }
    }

    private void AcceptorApproved(CamContractAmendmentPoAddendum po, ApprovePoAddendumRequest req)
    {
        var acceptors = po.Acceptors
                          .Where(a => a is { IsActive: true, IsUnableToPerformDuties: false, Status: AcceptorStatus.Pending, Type: AcceptorType.Approver })
                          .Select(DelegatorExtensions.DelegatorToAcceptor)
                          .OrderBy(a => a.Sequence)
                          .ToList();

        var current = acceptors.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                       ? a.UserId == req.UserId
                       : a.Delegatee?.SuUserId == UserId.From(req.UserId)
                         && a.Status == AcceptorStatus.Pending);

        if (current == null)
        {
            this.ThrowError("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้", StatusCodes.Status400BadRequest);
        }

        var currentAcceptorUser = po.Acceptors.FirstOrDefault(a => a.Id == current.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(current.DelegateeId)
            .Approve(remark: req.Remark);

        current.SetCurrent(false);
        po.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Approved,
            $"ผู้มีอำนาจเห็นชอบ/อนุมัติ",
            po.Status.ToString(),
            req.Remark));

        if (po.Acceptors.Where(w => w.Type is AcceptorType.Approver).All(e => e.Status is AcceptorStatus.Approved))
        {
            po.SetApproval();
        }
    }

    private static void UpdateSequentialCurrents(CamContractAmendmentPoAddendum po, AcceptorType type)
    {
        var approvers = po.Acceptors
                          .Where(a => a.Type == type && a.IsActive && !a.IsUnableToPerformDuties)
                          .OrderBy(a => a.Sequence)
                          .ToList();

        if (approvers.Count == 0)
        {
            return;
        }

        foreach (var a in approvers)
        {
            a.SetCurrent(false);
        }

        var next = approvers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        next.SetCurrent(true);

        var pendingOfType = approvers.Where(a => a.Status == AcceptorStatus.Pending).ToList();
        var isLastPending = pendingOfType.Count == 1;

        if (next.Type == AcceptorType.DepartmentDirectorAgree ||
            (next.Type == AcceptorType.Approver && !isLastPending))
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    po,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.ContractAmendment.Name, string.Empty));
            }
        }
        else if (next.Type == AcceptorType.Approver && isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    po,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.ContractAmendment.Name, string.Empty));
            }
        }
    }

    private static async Task SendNotificationAsync(CamContractAmendmentPoAddendum po, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.ContractAmendment)
              .SetReferenceId(po.Id.Value)
              .SetLinkUrl(string.Empty, "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}