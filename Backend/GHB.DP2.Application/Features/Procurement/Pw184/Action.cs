namespace GHB.DP2.Application.Features.Procurement.Pw184;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Pw184.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Pw184;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record Pw184ActionRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Pw184Action Action,
    string? Remark);

public class ActionPw184Endpoint : Pw184EndpointBase<Pw184ActionRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;

    public ActionPw184Endpoint(Dp2DbContext dbContext, ILogger<ActionPw184Endpoint> logger, IOperationService operationService)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Pw184")
             .WithName("Pw184Action")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Put("pw184/action/{Id:guid}");
        this.AuditLog("รายการ ว 184", "ดำเนินการ ว 184");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(
        Pw184ActionRequest req,
        CancellationToken ct)
    {
        var pw184 = await this.dbContext.Pw184s
                              .Include(p => p.Acceptors)
                              .ThenInclude(a => a.User)
                              .ThenInclude(u => u.Employee)
                              .Include(p => p.Committees)
                              .SingleOrDefaultAsync(p => p.Id == Pw184Id.From(req.Id), ct);

        if (pw184 is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลรายการ ว 184");
        }

        switch (req.Action)
        {
            case Pw184Action.ApproveAcceptor:
                this.HandleApproveAcceptor(pw184, UserId.From(req.UserId), req.Remark);
                break;

            case Pw184Action.RejectAcceptor:
                this.HandleRejectAcceptor(pw184, UserId.From(req.UserId), req.Remark);
                break;

            case Pw184Action.CommitteeApprove:
                await this.HandleCommitteeApproveAsync(pw184, UserId.From(req.UserId), req.Remark, ct);
                break;

            case Pw184Action.CommitteeReject:
                this.HandleCommitteeReject(pw184, UserId.From(req.UserId), req.Remark);
                break;

            case Pw184Action.AccountingApprove:
                this.HandleAccountingApprove(pw184, UserId.From(req.UserId), req.Remark);
                break;

            case Pw184Action.AccountingReject:
                this.HandleAccountingReject(pw184, UserId.From(req.UserId), req.Remark);
                break;

            default:
                throw new InvalidOperationException($"Unknown Pw184 action: {req.Action}");
        }

        this.dbContext.Pw184s.Update(pw184);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Action handlers
    // ──────────────────────────────────────────────────────────────────────────
    private void HandleApproveAcceptor(Pw184 pw184, UserId userId, string? remark)
    {
        if (pw184.Status != Pw184Status.WaitingApproval)
        {
            this.ThrowError("ไม่สามารถเห็นชอบได้ เนื่องจากสถานะไม่ถูกต้อง", StatusCodes.Status400BadRequest);
        }

        var approveUser = pw184.Acceptors
                               .Select(DelegatorExtensions.DelegatorToAcceptor)
                               .FirstOrDefault(a => !a.IsDeleted &&
                                                    a.Type == AcceptorType.Approver &&
                                                    a.Status == AcceptorStatus.Pending &&
                                                    (a.Delegatee?.SuUserId == null
                                                        ? a.UserId == userId
                                                        : a.Delegatee.SuUserId == userId)) ??
                          throw new InvalidOperationException($"User {userId} is not a pending approver.");

        var acceptorEntity = pw184.Acceptors.FirstOrDefault(a => a.Id == approveUser.Id);

        if (acceptorEntity is null)
        {
            this.ThrowError("ไม่พบผู้อนุมัติที่ใช้งานได้", StatusCodes.Status400BadRequest);
        }

        acceptorEntity
            .SetDelegatee(approveUser.DelegateeId)
            .Approve(remark: remark);

        pw184.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Approved,
            ActivityLogActionTypeConstant.Approved,
            nameof(Pw184Status.WaitingApproval),
            remark));

        UpdateApproverCurrents(pw184);

        var allApproved = pw184.Acceptors
                               .Where(a => !a.IsDeleted && a.Type == AcceptorType.Approver)
                               .All(a => a.Status == AcceptorStatus.Approved);

        if (allApproved)
        {
            // Clear all approver currents
            pw184.Acceptors
                 .Where(a => !a.IsDeleted && a.Type == AcceptorType.Approver)
                 .Iter(a => a.SetCurrent(false));

            pw184.SetCurrentCommitteeSequence(1);
            pw184.SetStatus(Pw184Status.WaitingCommitteeApprove);

            pw184.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Approved,
                "ผ่านการเห็นชอบ ส่งคณะกรรมการตรวจรับ",
                nameof(Pw184Status.WaitingCommitteeApprove)));
        }
    }

    private void HandleRejectAcceptor(Pw184 pw184, UserId userId, string? remark)
    {
        if (pw184.Status != Pw184Status.WaitingApproval)
        {
            this.ThrowError("ไม่สามารถส่งกลับแก้ไขได้ เนื่องจากสถานะไม่ถูกต้อง", StatusCodes.Status400BadRequest);
        }

        var alreadyRejected = pw184.Acceptors
                                   .Any(a => !a.IsDeleted && a.Type == AcceptorType.Approver && a.Status == AcceptorStatus.Rejected);

        if (alreadyRejected)
        {
            this.ThrowError("มีการส่งกลับแก้ไขไปแล้ว", StatusCodes.Status409Conflict);
        }

        var currentPending = pw184.Acceptors
                                  .Where(a => !a.IsDeleted && a.Type == AcceptorType.Approver)
                                  .OrderBy(a => a.Sequence)
                                  .FirstOrDefault(a => a.Status == AcceptorStatus.Pending) ??
                             throw new InvalidOperationException("No pending approver found.");

        var rejectedUser = pw184.Acceptors
                                .Select(DelegatorExtensions.DelegatorToAcceptor)
                                .FirstOrDefault(a => !a.IsDeleted &&
                                                     a.Type == AcceptorType.Approver &&
                                                     (a.Delegatee?.SuUserId == null
                                                         ? a.UserId == userId
                                                         : a.Delegatee.SuUserId == userId)) ??
                           throw new InvalidOperationException($"User {userId} is not an approver.");

        if (rejectedUser.Sequence != currentPending.Sequence)
        {
            this.ThrowError("ลำดับการอนุมัติไม่ถูกต้อง", StatusCodes.Status409Conflict);
        }

        var acceptorEntity = pw184.Acceptors.FirstOrDefault(a => a.Id == rejectedUser.Id);

        if (acceptorEntity is null)
        {
            this.ThrowError("ไม่พบผู้อนุมัติที่ใช้งานได้", StatusCodes.Status400BadRequest);
        }

        acceptorEntity
            .SetDelegatee(rejectedUser.DelegateeId)
            .Reject(remark: remark);

        pw184.SetStatus(Pw184Status.Rejected);

        pw184.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            ActivityLogActionTypeConstant.Reject,
            nameof(Pw184Status.Rejected),
            remark));
    }

    private async Task HandleCommitteeApproveAsync(Pw184 pw184, UserId userId, string? remark, CancellationToken ct)
    {
        if (pw184.Status != Pw184Status.WaitingCommitteeApprove)
        {
            this.ThrowError("ไม่สามารถเห็นชอบได้ เนื่องจากสถานะไม่ถูกต้อง", StatusCodes.Status400BadRequest);
        }

        var inspectionCommittees = pw184.Committees
                                        .Where(c => c.GroupType == Pw184CommitteeGroupType.InspectionCommittee)
                                        .OrderBy(c => c.Sequence)
                                        .ToArray();

        if (inspectionCommittees.Length == 0)
        {
            this.ThrowError("ไม่พบคณะกรรมการตรวจรับ", StatusCodes.Status400BadRequest);
        }

        var currentSeq = pw184.CurrentCommitteeSequence;
        var currentMember = inspectionCommittees.FirstOrDefault(c => c.Sequence == currentSeq);

        if (currentMember is null)
        {
            this.ThrowError($"ไม่พบคณะกรรมการตรวจรับลำดับที่ {currentSeq}", StatusCodes.Status400BadRequest);
        }

        if (currentMember.SuUserId != userId)
        {
            this.ThrowError("ไม่ใช่คณะกรรมการตรวจรับที่มีสิทธิ์เห็นชอบในลำดับนี้", StatusCodes.Status403Forbidden);
        }

        var nextSeq = currentSeq + 1;
        var hasNext = inspectionCommittees.Any(c => c.Sequence == nextSeq);

        pw184.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Approved,
            $"คณะกรรมการตรวจรับลำดับที่ {currentSeq} เห็นชอบ",
            nameof(Pw184Status.WaitingCommitteeApprove),
            remark));

        if (hasNext)
        {
            pw184.SetCurrentCommitteeSequence(nextSeq);
        }
        else
        {
            // All inspection committee members have approved → WaitingAccounting
            pw184.SetCurrentCommitteeSequence(0);

            var hasAccountingApprovers = pw184.Acceptors
                                              .Any(a => !a.IsDeleted && a.Type == AcceptorType.AccountingApprover);

            if (!hasAccountingApprovers)
            {
                // Fetch default accounting approvers and add to entity
                var expenseAcceptor = await this.operationService.GetDefaultExpenseDisbursementDirectorAsync(ct);

                if (expenseAcceptor is null)
                {
                    this.ThrowError("ไม่พบผู้รับผิดชอบฝ่ายเบิกจ่ายค่าใช้จ่าย", StatusCodes.Status400BadRequest);
                }

                var userIdsList = new List<UserId> { expenseAcceptor.UserId };

                var accountAcceptors = await this.operationService.GetDefaultAcceptorAsync(
                    SectionProcessType.ExpenseDisbursement,
                    expenseAcceptor.UserId.Value,
                    pw184.Budget,
                    "SectionApprover001",
                    null,
                    ct,
                    false);

                accountAcceptors?.Iter(x =>
                {
                    if (!userIdsList.Contains(x.UserId))
                    {
                        userIdsList.Add(x.UserId);
                    }
                });

                var usersIncomingList = await this.dbContext.SuUsers
                                                  .Include(r => r.Employee)
                                                  .ThenInclude(r => r.View)
                                                  .Where(w => userIdsList.Contains(w.Id))
                                                  .ToArrayAsync(ct);

                var usersById = usersIncomingList.ToDictionary(u => u.Id);

                var usersIncomingOrdered = userIdsList
                                           .Where(id => usersById.ContainsKey(id))
                                           .Select(id => usersById[id])
                                           .ToArray();

                usersIncomingOrdered.Iter((sequence, x) =>
                {
                    var acceptor = Pw184Acceptor.CreateWithPending(
                        AcceptorType.AccountingApprover,
                        x,
                        sequence + 1);

                    pw184.AddAcceptor(acceptor);
                });
            }
            else
            {
                // Existing accounting approvers — reset them to pending
                pw184.Acceptors
                     .Where(a => !a.IsDeleted && a.Type == AcceptorType.AccountingApprover)
                     .Iter(a => a.Pending());
            }

            var firstAccounting = pw184.Acceptors
                                       .Where(a => !a.IsDeleted && a.Type == AcceptorType.AccountingApprover)
                                       .OrderBy(a => a.Sequence)
                                       .FirstOrDefault();

            firstAccounting?.SetIsCurrent(true);

            pw184.SetStatus(Pw184Status.WaitingAccounting);

            pw184.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Approved,
                "คณะกรรมการตรวจรับทุกคนเห็นชอบแล้ว ส่งบัญชี",
                nameof(Pw184Status.WaitingAccounting)));
        }
    }

    private void HandleCommitteeReject(Pw184 pw184, UserId userId, string? remark)
    {
        if (pw184.Status != Pw184Status.WaitingCommitteeApprove)
        {
            this.ThrowError("ไม่สามารถส่งกลับแก้ไขได้ เนื่องจากสถานะไม่ถูกต้อง", StatusCodes.Status400BadRequest);
        }

        var currentSeq = pw184.CurrentCommitteeSequence;

        var currentMember = pw184.Committees
                                  .Where(c => c.GroupType == Pw184CommitteeGroupType.InspectionCommittee)
                                  .FirstOrDefault(c => c.Sequence == currentSeq);

        if (currentMember is null)
        {
            this.ThrowError($"ไม่พบคณะกรรมการตรวจรับลำดับที่ {currentSeq}", StatusCodes.Status400BadRequest);
        }

        if (currentMember.SuUserId != userId)
        {
            this.ThrowError("ไม่ใช่คณะกรรมการตรวจรับที่มีสิทธิ์ดำเนินการในลำดับนี้", StatusCodes.Status403Forbidden);
        }

        pw184.SetCurrentCommitteeSequence(0);
        pw184.SetStatus(Pw184Status.Rejected);

        pw184.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            $"คณะกรรมการตรวจรับลำดับที่ {currentSeq} ส่งกลับแก้ไข",
            nameof(Pw184Status.Rejected),
            remark));
    }

    private void HandleAccountingApprove(Pw184 pw184, UserId userId, string? remark)
    {
        if (pw184.Status != Pw184Status.WaitingAccounting)
        {
            this.ThrowError("ไม่สามารถเห็นชอบได้ เนื่องจากสถานะไม่ถูกต้อง", StatusCodes.Status400BadRequest);
        }

        var approveUser = pw184.Acceptors
                               .Select(DelegatorExtensions.DelegatorToAcceptor)
                               .FirstOrDefault(a => !a.IsDeleted &&
                                                    a.Type == AcceptorType.AccountingApprover &&
                                                    a.Status == AcceptorStatus.Pending &&
                                                    (a.Delegatee?.SuUserId == null
                                                        ? a.UserId == userId
                                                        : a.Delegatee.SuUserId == userId)) ??
                          throw new InvalidOperationException($"User {userId} is not a pending accounting approver.");

        var acceptorEntity = pw184.Acceptors.FirstOrDefault(a => a.Id == approveUser.Id);

        if (acceptorEntity is null)
        {
            this.ThrowError("ไม่พบผู้อนุมัติที่ใช้งานได้", StatusCodes.Status400BadRequest);
        }

        acceptorEntity
            .SetDelegatee(approveUser.DelegateeId)
            .Approve(remark: remark);

        pw184.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Approved,
            ActivityLogActionTypeConstant.Approved,
            nameof(Pw184Status.WaitingAccounting),
            remark));

        UpdateAccountingCurrents(pw184);

        var allApproved = pw184.Acceptors
                               .Where(a => !a.IsDeleted && a.Type == AcceptorType.AccountingApprover)
                               .All(a => a.Status == AcceptorStatus.Approved);

        if (allApproved)
        {
            pw184.Acceptors
                 .Where(a => !a.IsDeleted && a.Type == AcceptorType.AccountingApprover)
                 .Iter(a => a.SetCurrent(false));

            pw184.SetStatus(Pw184Status.WaitingDisbursementDate);

            pw184.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Approved,
                "บัญชีเห็นชอบแล้ว รอบันทึกวันที่เบิกจ่าย",
                nameof(Pw184Status.WaitingDisbursementDate)));
        }
    }

    private void HandleAccountingReject(Pw184 pw184, UserId userId, string? remark)
    {
        if (pw184.Status != Pw184Status.WaitingAccounting)
        {
            this.ThrowError("ไม่สามารถส่งกลับแก้ไขได้ เนื่องจากสถานะไม่ถูกต้อง", StatusCodes.Status400BadRequest);
        }

        var alreadyRejected = pw184.Acceptors
                                   .Any(a => !a.IsDeleted && a.Type == AcceptorType.AccountingApprover && a.Status == AcceptorStatus.Rejected);

        if (alreadyRejected)
        {
            this.ThrowError("มีการส่งกลับแก้ไขไปแล้ว", StatusCodes.Status409Conflict);
        }

        var currentPending = pw184.Acceptors
                                  .Where(a => !a.IsDeleted && a.Type == AcceptorType.AccountingApprover)
                                  .OrderBy(a => a.Sequence)
                                  .FirstOrDefault(a => a.Status == AcceptorStatus.Pending) ??
                             throw new InvalidOperationException("No pending accounting approver found.");

        var rejectedUser = pw184.Acceptors
                                .Select(DelegatorExtensions.DelegatorToAcceptor)
                                .FirstOrDefault(a => !a.IsDeleted &&
                                                     a.Type == AcceptorType.AccountingApprover &&
                                                     (a.Delegatee?.SuUserId == null
                                                         ? a.UserId == userId
                                                         : a.Delegatee.SuUserId == userId)) ??
                           throw new InvalidOperationException($"User {userId} is not an accounting approver.");

        if (rejectedUser.Sequence != currentPending.Sequence)
        {
            this.ThrowError("ลำดับการอนุมัติไม่ถูกต้อง", StatusCodes.Status409Conflict);
        }

        var acceptorEntity = pw184.Acceptors.FirstOrDefault(a => a.Id == rejectedUser.Id);

        if (acceptorEntity is null)
        {
            this.ThrowError("ไม่พบผู้อนุมัติที่ใช้งานได้", StatusCodes.Status400BadRequest);
        }

        acceptorEntity
            .SetDelegatee(rejectedUser.DelegateeId)
            .Reject(remark: remark);

        pw184.SetStatus(Pw184Status.Rejected);

        pw184.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            ActivityLogActionTypeConstant.Reject,
            nameof(Pw184Status.Rejected),
            remark));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────
    private static void UpdateApproverCurrents(Pw184 pw184)
    {
        var approvers = pw184.Acceptors
                             .Where(a => a.IsActive && a.Type == AcceptorType.Approver)
                             .OrderBy(a => a.Sequence)
                             .ToList();

        foreach (var a in approvers)
        {
            a.SetCurrent(false);
        }

        var next = approvers
                   .Select(DelegatorExtensions.DelegatorToAcceptor)
                   .FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        next.SetCurrent(true);
    }

    private static void UpdateAccountingCurrents(Pw184 pw184)
    {
        var approvers = pw184.Acceptors
                             .Where(a => a.IsActive && a.Type == AcceptorType.AccountingApprover)
                             .OrderBy(a => a.Sequence)
                             .ToList();

        foreach (var a in approvers)
        {
            a.SetCurrent(false);
        }

        var next = approvers
                   .Select(DelegatorExtensions.DelegatorToAcceptor)
                   .FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        next.SetCurrent(true);
    }
}
