namespace GHB.DP2.Application.Features.Procurement.PPettyCash;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.PPettyCash.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPettyCash;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.Extensions;

public record PPettyCashActionRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    PettyCashAction Action,
    string? Remark,
    Guid? JorPorId,
    DateTimeOffset DisbursementDate,
    AcceptorRequest[]? Acceptors,
    AssigneeRequest[] Assignees);

public class ActionPPettyCash : PPettyCashEndpointBase<PPettyCashActionRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ActionPPettyCash(
        Dp2DbContext dbContext,
        ILogger<UpdatePPettyCashEndpoint> logger,
        IFileServiceClient fileServiceClient)
        : base(dbContext, logger, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("PPettyCash")
             .WithName("PPettyCashAction")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Put("PPettyCash/action/{Id:guid}");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(PPettyCashActionRequest req, CancellationToken ct)
    {
        var pPettyCash = await this.dbContext
                                   .PPettyCashs
                                   .Include(pw => pw.Vendors)
                                   .ThenInclude(pPettyCashVendor => pPettyCashVendor.VendorParcels)
                                   .Include(pPettyCash => pPettyCash.Categories)
                                   .Include(ac => ac.Acceptors)
                                   .ThenInclude(a => a.User)
                                   .ThenInclude(u => u.Employee)
                                   .Include(p => p.Assignees)
                                   .Include(p => p.Committees)
                                   .Include(pw => pw.GLAccounts)
                                   .SingleOrDefaultAsync(p => p.Id == PettyCashId.From(req.Id), ct);

        if (pPettyCash is null)
        {
            return TypedResults.NotFound("Not found pPettyCash");
        }

        switch (req.Action)
        {
            case PettyCashAction.Edit:
                this.RecallHandler(pPettyCash);
                await this.ReplaceDocumentsAsync(pPettyCash, false, ct);

                pPettyCash.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.Recall,
                        ActivityLogActionTypeConstant.Recall,
                        nameof(PettyCashStatus.Edit)));

                break;

            case PettyCashAction.RequestToDirectorAgree:
                WaitingApprovalHandler(pPettyCash);
                await this.AddInspectorToAcceptor(pPettyCash, ct, UserId.From(req.UserId));

                pPettyCash.AddActivity(
                    new ActivityInfo(
                        "ส่งผู้ให้ความเห็นชอบ",
                        "ส่งผู้ให้ความเห็นชอบ",
                        nameof(PettyCashStatus.WaitingApproval)));

                break;

            case PettyCashAction.DirectorAgreeApproved:
                await this.Approved(pPettyCash, UserId.From(req.UserId), AcceptorType.DepartmentDirectorAgree, req.Remark, ct);
                DirectorAgreeApproved(pPettyCash, ct);

                pPettyCash.AddActivity(
                    new ActivityInfo(
                        "ผู้ให้ความเห็นชอบ",
                        "ผู้ให้ความเห็นชอบ",
                        nameof(PettyCashStatus.WaitingApproval),
                        req.Remark));

                break;

            case PettyCashAction.DirectorAgreeRejected:
                this.Rejected(pPettyCash, UserId.From(req.UserId), AcceptorType.DepartmentDirectorAgree, req.Remark);
                await this.ReplaceDocumentsAsync(pPettyCash, false, ct);

                pPettyCash.AddActivity(
                    new ActivityInfo(
                        "ผู้ให้ความเไม่ห็นชอบ",
                        "ผู้ให้ความเไม่ห็นชอบ",
                        nameof(PettyCashStatus.Rejected),
                        req.Remark));

                break;

            case PettyCashAction.InspectionCommitteeApproved:
                await this.Approved(pPettyCash, UserId.From(req.UserId), AcceptorType.InspectionCommittee, req.Remark, ct);
                InspectionCommitteeApproved(pPettyCash);

                pPettyCash.AddActivity(
                    new ActivityInfo(
                        "ผู้ตรวจรับพัสดุเห็นชอบ",
                        "ผู้ตรวจรับพัสดุเห็นชอบ",
                        nameof(PettyCashStatus.WaitingForInspector),
                        req.Remark));

                break;

            case PettyCashAction.InspectionCommitteeRejected:
                this.Rejected(pPettyCash, UserId.From(req.UserId), AcceptorType.InspectionCommittee, req.Remark);
                await this.ReplaceDocumentsAsync(pPettyCash, false, ct);

                pPettyCash.AddActivity(
                    new ActivityInfo(
                        "ผู้ตรวจรับพัสดุไม่เห็นชอบ",
                        "ผู้ตรวจรับพัสดุไม่เห็นชอบ",
                        nameof(PettyCashStatus.Rejected),
                        req.Remark));

                break;

            case PettyCashAction.Assignment:
                await this.ManageAssigneeAsync(pPettyCash, req.Assignees!, ct, sendToAcceptorId: UserId.From(req.UserId));

                pPettyCash.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.WaitingAssign,
                        ActivityLogActionTypeConstant.WaitingAssign,
                        nameof(PettyCashStatus.WaitingForAssignment)));

                break;

            case PettyCashAction.ConfirmAssignment:
                await this.ManageAssigneeAsync(pPettyCash, req.Assignees!, ct, req.Remark, UserId.From(req.UserId));
                Assigned(pPettyCash);
                _ = SendNotificationAssigneeAsync(pPettyCash, ct);

                pPettyCash.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.Assigned,
                        ActivityLogActionTypeConstant.Assigned,
                        nameof(PettyCashStatus.WaitingForCompletion)));

                break;

            case PettyCashAction.ConfirmCompleted:
                await this.ManageAssigneeAsync(pPettyCash, req.Assignees!, ct, req.Remark, UserId.From(req.UserId));
                Completed(pPettyCash, req.DisbursementDate);

                pPettyCash.AddActivity(
                    new ActivityInfo(
                        "ยืนยันเบิกจ่าย",
                        "ยืนยันเบิกจ่าย",
                        nameof(PettyCashStatus.Completed)));

                break;

            case PettyCashAction.SendToAssignee:
                await this.ManageAssigneeAsync(pPettyCash, req.Assignees!, ct, sendToAcceptorId: UserId.From(req.UserId));
                pPettyCash.SetStatus(PettyCashStatus.WaitingForCompletion);
                _ = SendNotificationAssigneeAsync(pPettyCash, ct);

                pPettyCash.AddActivity(
                    new ActivityInfo(
                        "ส่งต่อผู้ถือเงินสดย่อย",
                        "ส่งต่อผู้ถือเงินสดย่อย",
                        nameof(PettyCashStatus.WaitingForCompletion)));

                break;

            case PettyCashAction.AssigneeRejected:
                pPettyCash.SetStatus(PettyCashStatus.Rejected);
                await this.ReplaceDocumentsAsync(pPettyCash, false, ct);

                pPettyCash.AddActivity(
                    new ActivityInfo(
                        "ผู้ถือเงินสดย่อยส่งกลับแก้ไข",
                        "ผู้ถือเงินสดย่อยส่งกลับแก้ไข",
                        nameof(PettyCashStatus.Rejected),
                        req.Remark));

                break;

            default:
                throw new InvalidOperationException($"unknown pPettyCash action {req.Action}");
        }

        this.dbContext.PPettyCashs.Update(pPettyCash);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static void WaitingApprovalHandler(
        PPettyCash pPettyCash)
    {
        SetAcceptorPendingHandler(pPettyCash, AcceptorType.DepartmentDirectorAgree);

        pPettyCash.SetStatus(PettyCashStatus.WaitingApproval);
    }

    private static void SetAcceptorPendingHandler(
        PPettyCash pPettyCash, AcceptorType acceptorType)
    {
        pPettyCash.Acceptors
                  .Where(w => w.Type == acceptorType)
                  .Iter(r => r.Pending());

        var approver = pPettyCash.Acceptors
                                 .FirstOrDefault(p => p.Sequence == 1 && p.Type == acceptorType);

        if (approver != null)
        {
            approver.SetIsCurrent(true);

            foreach (var targetUserId in approver.GetNotificationTargets())
            {
                _ = SendNotificationAsync(pPettyCash, targetUserId, NotificationConstant.WaitForLike.Title, string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PettyCash.Name, pPettyCash.PettyCashNumber));
            }
        }
    }

    private void RecallHandler(PPettyCash pPettyCash)
    {
        if (pPettyCash.Acceptors.Where(a => a.Type == AcceptorType.DepartmentDirectorAgree)
                      .Any(w => w.Status is AcceptorStatus.Approved or AcceptorStatus.Rejected))
        {
            this.ThrowError(
                "Acceptor already Decision",
                StatusCodes.Status403Forbidden);
        }

        pPettyCash.SetStatus(PettyCashStatus.Edit);
    }

    private async Task ManageAssigneeAsync(
        PPettyCash pPettyCash,
        AssigneeRequest[] assignees,
        CancellationToken ct,
        string? remark = null,
        UserId? userId = null,
        UserId? sendToAcceptorId = null)
    {
        var lastAssigneeUserId = pPettyCash.Assignees
            .Where(a => a.Type == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)a.UserId)
            .FirstOrDefault();

        if (lastAssigneeUserId is not null)
        {
            sendToAcceptorId = lastAssigneeUserId;
        }

        _ = pPettyCash.Assignees
                      .ExceptBy(
                          assignees
                              .Where(w => w.Id.HasValue)
                              .Select(s => s.Id.Value),
                          a => a.Id.Value)
                      .Iter(r => pPettyCash.RemoveAssigneeById(r.Id));

        _ = assignees.Where(w => w.Id.HasValue)
                     .Join(
                         pPettyCash.Assignees,
                         db => db.Id.Value,
                         payload => payload.Id.Value,
                         (payload, db) => new { db, payload })
                     .Iter(r =>
                     {
                         r.db.SetSequence(r.payload.Sequence);
                         r.db.SetSendToAcceptorId(sendToAcceptorId ?? userId);
                     });

        var newIds = assignees
                     .Where(w => !w.Id.HasValue)
                     .Select(s => UserId.From(s.UserId))
                     .ToArray();

        var userData = await this.dbContext.SuUsers
                                 .Include(e => e.Employee)
                                 .ThenInclude(v => v.View)
                                 .Where(w => newIds.Contains(w.Id))
                                 .ToArrayAsync(ct);

        _ = assignees.Where(w => !w.Id.HasValue)
                     .Join(
                         userData,
                         a => UserId.From(a.UserId),
                         u => u.Id,
                         (a, u) => PPettyCashAssignee.Create(
                             a.AssigneeType,
                             u,
                             a.Sequence))
                     .Iter(a =>
                     {
                         a.SetSendToAcceptorId(sendToAcceptorId ?? userId);
                         pPettyCash.AddAssignee(a);
                     });

        var remarkUser = pPettyCash.Assignees.FirstOrDefault(w => w.UserId == userId);

        if (remarkUser is not null)
        {
            remarkUser.SetRemark(remark);
        }
    }

    private async Task AddInspectorToAcceptor(PPettyCash pPettyCash, CancellationToken ct, UserId? sendToAcceptorId = null)
    {
        if (pPettyCash.Committees is not null)
        {
            var inspectionAcceptors = pPettyCash.Committees
                                                .Where(c => c.GroupType == GroupType.InspectionCommittee)
                                                .Select(c => new AcceptorRequest(
                                                    default,
                                                    AcceptorType.InspectionCommittee,
                                                    c.SuUserId.Value,
                                                    c.Sequence)).ToArray();

            await this.MangeAcceptorsAsync(pPettyCash, inspectionAcceptors, AcceptorType.InspectionCommittee, ct, sendToAcceptorId);

            SetAcceptorPendingHandler(pPettyCash, AcceptorType.InspectionCommittee);

            pPettyCash.SetStatus(PettyCashStatus.WaitingApproval);
        }
    }

    private async Task MangeAcceptorsAsync(
        PPettyCash pPettyCash,
        AcceptorRequest[] acceptors,
        AcceptorType acceptorType,
        CancellationToken ct,
        UserId? sendToAcceptorId = null)
    {
        var lastAssigneeUserId = pPettyCash.Assignees
            .Where(a => a.Type == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)a.UserId)
            .FirstOrDefault();

        if (lastAssigneeUserId is not null)
        {
            sendToAcceptorId = lastAssigneeUserId;
        }

        _ = pPettyCash.Acceptors.Where(a => a.Type == acceptorType)
                      .ExceptBy(
                          acceptors
                              .Where(w => w.Id.HasValue)
                              .Select(s => s.Id.Value),
                          a => a.Id.Value)
                      .Iter(r => pPettyCash.RemoveAcceptorById(r.Id));

        _ = acceptors.Where(w => w.Id.HasValue)
                     .Join(
                         pPettyCash.Acceptors,
                         db => db.Id.Value,
                         payload => payload.Id.Value,
                         (payload, db) => new { db, payload })
                     .Iter(r =>
                     {
                         r.db.SetSequence(r.payload.Sequence);
                         r.db.SetSendToAcceptorId(sendToAcceptorId);
                     });

        var newIds = acceptors
                     .Where(w => !w.Id.HasValue)
                     .Select(s => UserId.From(s.UserId))
                     .ToArray();

        var userData = await this.dbContext.SuUsers
                                 .Include(e => e.Employee)
                                 .ThenInclude(v => v.View)
                                 .Where(w => newIds.Contains(w.Id))
                                 .ToArrayAsync(ct);

        _ = acceptors.Where(w => !w.Id.HasValue)
                     .Join(
                         userData,
                         a => UserId.From(a.UserId),
                         u => u.Id,
                         (a, u) => PPettyCashAcceptor.Create(
                             a.AcceptorType,
                             u,
                             a.Sequence))
                     .Iter(a =>
                     {
                         a.SetSendToAcceptorId(sendToAcceptorId);
                         pPettyCash.AddAcceptor(a);
                     });
    }

    private async Task Approved(
        PPettyCash pPettyCash,
        UserId userId,
        AcceptorType acceptorType,
        string? remark,
        CancellationToken ct)
    {
        var approveUser = pPettyCash.Acceptors.FirstOrDefault(w => !w.IsDeleted && w.Type == acceptorType && w.UserId == userId);

        if (acceptorType != AcceptorType.InspectionCommittee)
        {
            approveUser = pPettyCash.Acceptors
                                    .Select(DelegatorExtensions.DelegatorToAcceptor)
                                    .FirstOrDefault(w => !w.IsDeleted && w.Type == acceptorType
                                                                      && w.Delegatee?.SuUserId == null
                                                                          ? w.UserId == userId
                                                                          : w.Delegatee?.SuUserId == userId);
        }

        if (approveUser == null)
        {
            throw new InvalidOperationException($"User {userId} does not have an accept or rejected.");
        }

        var currentPendingUser = pPettyCash.Acceptors
                                           .OrderBy(o => o.Sequence)
                                           .FirstOrDefault(w => !w.IsDeleted && w.Type == acceptorType && w.Status == AcceptorStatus.Pending) ??
                                 throw new InvalidOperationException($"PPettyCash error, Acceptor must be one or more.");

        if (approveUser.Sequence != currentPendingUser.Sequence)
        {
            this.ThrowError(
                "The approval order is incorrect.",
                StatusCodes.Status409Conflict);
        }

        var currentAcceptorUser =
                  pPettyCash.Acceptors
                  .FirstOrDefault(a => a.Id == approveUser.Id);

        if (currentAcceptorUser == null)
        {
            this.ThrowError(
                    "ไม่พบผู้อนุมัติที่ใช้งานได้",
                    StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(approveUser.DelegateeId)
            .Approve(remark: remark);

        await this.ReplaceDocumentsAsync(pPettyCash, true, ct);
    }

    private static void Assigned(PPettyCash pPettyCash)
    {
        pPettyCash.SetStatus(PettyCashStatus.WaitingForCompletion);
    }

    private static void Completed(PPettyCash pPettyCash, DateTimeOffset disbursementDate)
    {
        pPettyCash.SetDisbursementDate(disbursementDate);
        pPettyCash.SetStatus(PettyCashStatus.Completed);
    }

    private static void DirectorAgreeApproved(PPettyCash pPettyCash, CancellationToken ct)
    {
        var isAllApproved = pPettyCash.Acceptors
                                      .Where(w => !w.IsDeleted && w.Type == AcceptorType.DepartmentDirectorAgree)
                                      .All(w => w.Status == AcceptorStatus.Approved);

        if (!isAllApproved)
        {
            UpdateSequentialCurrents(pPettyCash, AcceptorType.DepartmentDirectorAgree);
            pPettyCash.SetStatus(PettyCashStatus.WaitingApproval);

            return;
        }

        // ไม่ออกแบบฟอร์ม จพ. 001: ข้ามผู้ตรวจรับ ส่งตรงให้ผู้ถือเงินสดย่อยเบิกจ่าย
        if (pPettyCash.IsFromJorPor001 == false)
        {
            pPettyCash.SetStatus(PettyCashStatus.WaitingForCompletion);
            _ = SendNotificationAssigneeAsync(pPettyCash, ct);

            return;
        }

        SetAcceptorPendingHandler(pPettyCash, AcceptorType.InspectionCommittee);
        pPettyCash.SetStatus(PettyCashStatus.WaitingForInspector);
    }

    private static void InspectionCommitteeApproved(PPettyCash pPettyCash)
    {
        var newStatus = pPettyCash.Acceptors.Where(w => !w.IsDeleted && w.Type == AcceptorType.InspectionCommittee).All(w => w.Status == AcceptorStatus.Approved)
            ? PettyCashStatus.WaitingForAssignment
            : PettyCashStatus.WaitingForInspector;

        pPettyCash.SetStatus(newStatus);

        if (newStatus == PettyCashStatus.WaitingForInspector)
        {
            UpdateSequentialCurrents(pPettyCash, AcceptorType.InspectionCommittee);
        }

        if (pPettyCash.Assignees.Any(a => a.Group == AssigneeGroup.JorPor))
        {
            var directors = pPettyCash.Assignees.FirstOrDefault(a => a.Group == AssigneeGroup.JorPor);

            if (directors is null)
            {
                return;
            }

            foreach (var targetUserId in directors.GetAssigneeNotificationTargets())
            {
                _ = SendNotificationAsync(pPettyCash, targetUserId, NotificationConstant.WaitForAssignment.Title, string.Format(NotificationConstant.WaitForAssignment.Message, ProgramConstant.PettyCash.Name, pPettyCash.PettyCashNumber));
            }
        }
    }

    private void Rejected(PPettyCash pPettyCash, UserId userId, AcceptorType acceptorType, string? remark)
    {
        var isRejected = pPettyCash.Acceptors
                                   .Any(w => !w.IsDeleted && w.Type == acceptorType && w.Status == AcceptorStatus.Rejected);

        if (isRejected)
        {
            this.ThrowError(
                "already rejected",
                StatusCodes.Status409Conflict);
        }

        var currentPendingUser = pPettyCash.Acceptors
                                           .OrderBy(o => o.Sequence)
                                           .FirstOrDefault(w => !w.IsDeleted && w.Type == acceptorType && w.Status == AcceptorStatus.Pending) ??
                                 throw new InvalidOperationException($"PPettyCash error, Acceptor must be one or more.");

        var rejectedUser = pPettyCash.Acceptors.FirstOrDefault(w => !w.IsDeleted && w.UserId == userId && w.Type == acceptorType);

        if (acceptorType != AcceptorType.InspectionCommittee)
        {
            rejectedUser = pPettyCash.Acceptors
                                    .Select(DelegatorExtensions.DelegatorToAcceptor)
                                    .FirstOrDefault(w => !w.IsDeleted && w.Type == acceptorType
                                                                      && w.Delegatee?.SuUserId == null
                                                                          ? w.UserId == userId
                                                                          : w.Delegatee?.SuUserId == userId);
        }

        if (rejectedUser == null)
        {
            throw new InvalidOperationException($"User {userId} does not have an accept or rejected.");
        }

        if (rejectedUser.Sequence != currentPendingUser.Sequence)
        {
            this.ThrowError(
                "The approval order is incorrect.",
                StatusCodes.Status409Conflict);
        }

        var currentAcceptorUser =
                  pPettyCash.Acceptors
                  .FirstOrDefault(a => a.Id == rejectedUser.Id);

        if (currentAcceptorUser == null)
        {
            this.ThrowError(
                    "ไม่พบผู้อนุมัติที่ใช้งานได้",
                    StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(rejectedUser.DelegateeId)
            .Reject(remark: remark);

        pPettyCash.SetStatus(PettyCashStatus.Rejected);
    }

    private static async Task SendNotificationAsync(PPettyCash pPettyCash, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(pPettyCash.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.PettyCash.Url, pPettyCash.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static void UpdateSequentialCurrents(PPettyCash pPettyCash, AcceptorType type)
    {
        var approvers = pPettyCash.Acceptors
                                  .Where(a => a.Type == type && a.IsActive)
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

        var next = approvers.FirstOrDefault(a =>
            a.Status == AcceptorStatus.Pending);

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
                    pPettyCash,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PettyCash.Name, pPettyCash.PettyCashNumber));
            }
        }
        else if (next.Type == AcceptorType.Approver && isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    pPettyCash,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.PettyCash.Name, pPettyCash.PettyCashNumber));
            }
        }
    }

    private static async Task SendNotificationAssigneeAsync(PPettyCash pPettyCash, CancellationToken ct)
    {
        _ = await pPettyCash.Assignees.Where(x => x.Type != AssigneeType.Director).Map(pa =>
                                Notification
                                    .Crate(
                                        pa.UserId,
                                        NotificationConstant.Assignment.Title,
                                        string.Format(NotificationConstant.Assignment.Message, ProgramConstant.PettyCash.Name, pPettyCash.PettyCashNumber),
                                        NotificationProgram.Procurement)
                                    .SetReferenceId(pPettyCash.Id.Value)
                                    .SetLinkUrl(
                                        string.Format(ProgramConstant.PettyCash.Url, pPettyCash.Id),
                                        "ดูรายละเอียด"))
                            .Map(n => n.PublishAsync(ct).ToUnit())
                            .SequenceSerial();
    }

    private async ValueTask ReplaceDocumentsAsync(
        PPettyCash entity,
        bool hasAcceptor,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var replaceDto =
            await this.MapToReplaceDto(entity, hasAcceptor, ct, null);

        var timeStamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        await ReplaceDocumentAsync();

        async ValueTask ReplaceDocumentAsync()
        {
            var replaceTemplate =
                entity.LastedNotReplacedDocument()
                ?? entity.DocumentHistories
                    .OrderVersions()
                    .FirstOrDefault();

            if (replaceTemplate is not null)
            {
                var finalFileId =
                    await documentService.CopyDocumentTemplateAsync(
                        replaceTemplate.FileId,
                        contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                        parentDirectory: $"{DocumentTemplateGroups.PettyCash}/{entity.Id}_{timeStamp}.odt",
                        cancellationToken: ct);

                if (finalFileId is null)
                {
                    this.ThrowError("ไม่สามารถคัดลอกเอกสารที่ส่งเห็นชอบ");
                }

                entity.AddDocumentHistory(finalFileId.Value, true);
            }
        }
    }
}