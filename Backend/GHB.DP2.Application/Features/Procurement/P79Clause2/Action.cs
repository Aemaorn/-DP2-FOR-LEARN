namespace GHB.DP2.Application.Features.Procurement.P79Clause2;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.P79Clause2.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.P79Clause2;
using GHB.DP2.Domain.Procurement.PExpenseDisbursement;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record P79Clause2ActionRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    P79Clause2Action Action,
    string? Remark,
    Guid? JorPorId,
    AcceptorRequest[]? Acceptors);

public class ActionP79Clause2 : P79Clause2EndpointBase<P79Clause2ActionRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;

    public ActionP79Clause2(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IOperationService operationService,
        ILogger<ActionP79Clause2> logger)
        : base(logger, dbContext, operationService, fileServiceClient)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("P79Clause2")
             .WithName("P79Clause2Action")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Put("p79Clause2/action/{Id:guid}");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(P79Clause2ActionRequest req, CancellationToken ct)
    {
        var p79Clause2 = await this.dbContext
                                   .P79Clause2s
                                   .Include(ac => ac.Acceptors)
                                   .Include(v => v.Vendors)
                                   .ThenInclude(v => v.VendorParcels)
                                   .Include(g => g.GLAccounts) // include GL accounts for mapping
                                   .Include(a => a.Attachments)
                                   .SingleOrDefaultAsync(p => p.Id == P79Clause2Id.From(req.Id), ct);

        if (p79Clause2 is null)
        {
            return TypedResults.NotFound("Not found p79Clause2");
        }

        switch (req.Action)
        {
            case P79Clause2Action.Draft:
                await this.MangeAcceptorsAsync(p79Clause2, req.Acceptors ?? [], ct, UserId.From(req.UserId));

                break;

            case P79Clause2Action.Edit:
            case P79Clause2Action.Recall:
                this.RecallHandler(p79Clause2);
                await this.ReplaceDocumentsAsync(p79Clause2, false, ct);

                break;

            case P79Clause2Action.RequestApproval:
                this.WaitingApprovalHandler(p79Clause2);

                break;

            case P79Clause2Action.RejectedAcceptor:
                this.AcceptorRejected(p79Clause2, UserId.From(req.UserId), req.Remark);
                await this.ReplaceDocumentsAsync(p79Clause2, false, ct);

                break;

            case P79Clause2Action.ApprovedAcceptor:
                await this.AcceptorApproved(
                    p79Clause2,
                    UserId.From(req.UserId),
                    req.Remark,
                    ct);

                break;

            case P79Clause2Action.ConfirmDisbursement:
                this.ConfirmDisbursement(p79Clause2);

                break;

            default:
                throw new InvalidOperationException($"unknown p79Clause2 action {req.Action}");
        }

        this.dbContext.P79Clause2s.Update(p79Clause2);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private void WaitingApprovalHandler(
        P79Clause2 p79Clause2)
    {
        p79Clause2.Acceptors
                  .Iter(r => r.Pending());

        p79Clause2.SetStatus(P79Clause2Status.WaitingApproval);

        var approver = p79Clause2.Acceptors
                                 .FirstOrDefault(p => p.Sequence == 1);

        if (approver != null)
        {
            approver.SetIsCurrent(true);

            foreach (var targetUserId in approver.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    p79Clause2,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.Urgent79Clause2.Name, p79Clause2.P79Clause2Number));
            }
        }

        p79Clause2.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.SendApprove,
                ActivityLogActionTypeConstant.SendApprove,
                p79Clause2.Status.ToString()));
    }

    private void RecallHandler(P79Clause2 p79Clause2)
    {
        if (p79Clause2.Acceptors.Where(a => a.Type == AcceptorType.Approver)
                      .Any(w => w.Status is AcceptorStatus.Approved or AcceptorStatus.Rejected))
        {
            this.ThrowError(
                "Acceptor already Decision",
                StatusCodes.Status403Forbidden);
        }

        p79Clause2.Acceptors.Iter(r => r.Draft());

        p79Clause2.SetStatus(P79Clause2Status.Edit);

        p79Clause2.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Recall,
            ActivityLogActionTypeConstant.Recall,
            p79Clause2.Status.ToString()));
    }

    private async Task MangeAcceptorsAsync(
        P79Clause2 p79Clause2,
        AcceptorRequest[] acceptors,
        CancellationToken ct,
        UserId? sendToAcceptorId = null)
    {
        if (acceptors.Length == 0)
        {
            return;
        }

        _ = p79Clause2.Acceptors
                      .ExceptBy(
                          acceptors
                              .Where(w => w.Id.HasValue)
                              .Select(s => s.Id.Value),
                          a => a.Id.Value)
                      .Iter(r => p79Clause2.RemoveAcceptorById(r.Id));

        _ = acceptors.Where(w => w.Id.HasValue)
                     .Join(
                         p79Clause2.Acceptors,
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
                         (a, u) => P79Clause2Acceptor.Create(
                             AcceptorType.Approver,
                             u,
                             a.Sequence))
                     .Iter(a =>
                     {
                         a.SetSendToAcceptorId(sendToAcceptorId);
                         p79Clause2.AddAcceptor(a);
                     });
    }

    private async Task AcceptorApproved(
        P79Clause2 p79Clause2,
        UserId userId,
        string? remark,
        CancellationToken ct)
    {
        var approveUser = p79Clause2.Acceptors
                                    .Select(DelegatorExtensions.DelegatorToAcceptor)
                                    .FirstOrDefault(w => !w.IsDeleted && w.Status == AcceptorStatus.Pending && w.Delegatee?.SuUserId == null
                                        ? w.UserId == userId
                                        : w.Delegatee?.SuUserId == userId) ??
                          throw new InvalidOperationException($"User {userId} does not have an accept or rejected.");

        var currentAcceptorUser =
            p79Clause2.Acceptors
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

        UpdateSequentialCurrents(p79Clause2);
        p79Clause2.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Approved,
                ActivityLogActionTypeConstant.Approved,
                p79Clause2.Status.ToString(),
                remark));

        var newStatus = p79Clause2.Acceptors.Where(w => !w.IsDeleted && w.Type == AcceptorType.Approver).All(w => w.Status == AcceptorStatus.Approved)
            ? P79Clause2Status.WaitingAccountingApproval
            : P79Clause2Status.WaitingApproval;

        var isBranch = EmployeeConstant.OrganizationLevel.BranchLevels.Contains(p79Clause2.Department?.OrganizationLevel);

        if (newStatus == P79Clause2Status.WaitingAccountingApproval)
        {
            var accountingApprovers = p79Clause2.Acceptors
                                                .Where(a => !a.IsDeleted && (a.Type == AcceptorType.AccountingApprover || a.Type == AcceptorType.AccountingOperator))
                                                .OrderBy(a => a.Type == AcceptorType.AccountingOperator ? 0 : 1)
                                                .ThenBy(a => a.Sequence)
                                                .ToList();

            if (accountingApprovers.Any())
            {
                foreach (var approver in p79Clause2.Acceptors.Where(a => !a.IsDeleted))
                {
                    approver.SetCurrent(false);
                }

                var firstPending = accountingApprovers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

                if (firstPending != null)
                {
                    var firstSeq = firstPending.Sequence;
                    var currentApprovers = accountingApprovers.Where(a => a.Sequence == firstSeq && a.Status == AcceptorStatus.Pending).ToList();

                    foreach (var a in currentApprovers)
                    {
                        a.SetCurrent(true);
                    }

                    var isLastAccountingPending = accountingApprovers.Count(a => a.Status == AcceptorStatus.Pending) == 1;

                    if (isBranch || accountingApprovers.Any(a => a.Type == AcceptorType.AccountingOperator))
                    {
                        foreach (var targetUserId in firstPending.GetNotificationTargets())
                        {
                            _ = SendNotificationAsync(
                                   p79Clause2,
                                   targetUserId,
                                   isLastAccountingPending ? NotificationConstant.WaitForApprove.Title : NotificationConstant.WaitForLike.Title,
                                   string.Format(isLastAccountingPending ? NotificationConstant.WaitForApprove.Message : NotificationConstant.WaitForLike.Message, ProgramConstant.Urgent79Clause2.Name, p79Clause2.P79Clause2Number));
                        }
                    }
                    else
                    {
                        var segmentMembers = await this.operationService.GetSegmentAccountingMembersAsync(ct);

                        foreach (var member in segmentMembers)
                        {
                            _ = SendNotificationAsync(
                                   p79Clause2,
                                   member.UserId,
                                   NotificationConstant.WaitForApprove.Title,
                                   string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.Urgent79Clause2.Name, p79Clause2.P79Clause2Number));
                        }
                    }
                }
            }
        }

        var accountingAcceptors = p79Clause2.Acceptors
            .Where(w => !w.IsDeleted && (w.Type == AcceptorType.AccountingApprover || w.Type == AcceptorType.AccountingOperator))
            .ToList();

        if (accountingAcceptors.Any()
                ? accountingAcceptors.All(w => w.Status == AcceptorStatus.Approved)
                : !isBranch)
        {
            newStatus = P79Clause2Status.WaitingDisbursementDate;

            foreach (var approver in p79Clause2.Acceptors.Where(a => !a.IsDeleted && (a.Type == AcceptorType.AccountingApprover || a.Type == AcceptorType.AccountingOperator)))
            {
                approver.SetCurrent(false);
            }

            var confirmers = p79Clause2.Acceptors
                                       .Where(a => !a.IsDeleted && a.Type == AcceptorType.AccountingConfirmer)
                                       .ToList();

            if (confirmers.Any())
            {
                foreach (var targetUserId in confirmers.SelectMany(c => c.GetNotificationTargets()))
                {
                    _ = SendNotificationAsync(
                        p79Clause2,
                        targetUserId,
                        NotificationConstant.WaitConfirmDisbursement.Title,
                        string.Format(NotificationConstant.WaitConfirmDisbursement.Message, ProgramConstant.Urgent79Clause2.Name, p79Clause2.P79Clause2Number));
                }
            }
            else if (!isBranch)
            {
                var segmentMembers = await this.operationService.GetSegmentAccountingMembersAsync(ct);

                foreach (var member in segmentMembers)
                {
                    _ = SendNotificationAsync(
                        p79Clause2,
                        member.UserId,
                        NotificationConstant.WaitConfirmDisbursement.Title,
                        string.Format(NotificationConstant.WaitConfirmDisbursement.Message, ProgramConstant.Urgent79Clause2.Name, p79Clause2.P79Clause2Number));
                }
            }
        }

        if (newStatus == P79Clause2Status.Approved)
        {
            // Prevent duplicate creation
            var exists = await this.dbContext.PExpenseDisbursements
                                   .AnyAsync(e => e.SourceType == PExpenseDisbursementSourceType.Clause79_2 && e.SourceId == p79Clause2.Id.Value, CancellationToken.None);

            if (!exists)
            {
                var updateInfo = new PExpenseDisbursement.ExpenseDisbursementUpdateInfo(
                    PExpenseDisbursementStatus.Draft,
                    p79Clause2.IsAdvance,
                    p79Clause2.AdvanceName,
                    p79Clause2.AdvancePaymentMethodCode,
                    p79Clause2.AdvancePaymentDate,
                    p79Clause2.AdvanceBankCode,
                    null,
                    null,
                    p79Clause2.AdvanceBankAccount,
                    p79Clause2.AdvanceBankBranch,
                    p79Clause2.AdvanceBankAccountName,
                    p79Clause2.AdvanceDetail,
                    DateTimeOffset.UtcNow,
                    null);
                var expense = PExpenseDisbursement.Create(PExpenseDisbursementSourceType.Clause79_2, p79Clause2.Id.Value)
                                                  .SetValue(updateInfo);

                if (p79Clause2.GLAccounts is not null)
                {
                    foreach (var gl in p79Clause2.GLAccounts.OrderBy(g => g.Sequence))
                    {
                        var glEntity = PExpenseDisbursementGlAccount
                                       .Create()
                                       .SetValue(
                                           gl.Sequence,
                                           gl.SoId,
                                           gl.BudgetTypeCode,
                                           gl.GLAccountCode,
                                           gl.ProjectNumber,
                                           gl.Amount);
                        expense.AddGlAccount(glEntity);
                    }
                }

                var expenseAcceptor = await this.operationService.GetDefaultExpenseDisbursementDirectorAsync(ct);

                if (expenseAcceptor is not null)
                {
                    var userIdsList = new List<UserId> { expenseAcceptor.UserId };

                    var acceptorData = await this.operationService.GetDefaultAcceptorAsync(
                        SectionProcessType.ExpenseDisbursement,
                        expenseAcceptor.UserId.Value,
                        p79Clause2.GLAccounts?.Sum(x => x.Amount) ?? 0,
                        "SectionApprover001",
                        null,
                        ct,
                        false);

                    userIdsList.AddRange(acceptorData.Map(x => x.UserId));

                    var usersIncomingList =
                        await this.dbContext.SuUsers
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
                        var acceptor = PExpenseDisbursementAcceptor.Create(
                            AcceptorType.Approver,
                            x,
                            sequence + 1);
                        expense.AddAcceptor(acceptor);
                    });

                    expense.SetStatus(PExpenseDisbursementStatus.WaitingApproval);
                }

                this.dbContext.PExpenseDisbursements.Add(expense);
            }
        }

        if (p79Clause2.Status is P79Clause2Status.WaitingApproval)
        {
            await this.ReplaceDocumentsAsync(p79Clause2, true, ct);
        }

        p79Clause2.SetStatus(newStatus);
    }

    private void ConfirmDisbursement(P79Clause2 p79Clause2)
    {
        p79Clause2.SetStatus(P79Clause2Status.Paid);

        p79Clause2.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.ConfirmDisbursement,
            ActivityLogActionTypeConstant.ConfirmDisbursement,
            nameof(P79Clause2Status.Paid),
            null));

        _ = SendNotificationAsync(
            p79Clause2,
            UserId.From(p79Clause2.AuditInfo.CreatedBy),
            NotificationConstant.DisbursementPaid.Title,
            string.Format(NotificationConstant.DisbursementPaid.Message, ProgramConstant.Urgent79Clause2.Name, p79Clause2.P79Clause2Number));
    }

    private void AcceptorRejected(P79Clause2 p79Clause2, UserId userId, string? remark)
    {
        var isRejected = p79Clause2.Acceptors
                                   .Any(w => !w.IsDeleted && w.Status == AcceptorStatus.Rejected);

        if (isRejected)
        {
            this.ThrowError(
                "already rejected",
                StatusCodes.Status409Conflict);
        }

        var rejectedUser = p79Clause2.Acceptors
                                     .OrderBy(o => o.Sequence)
                                     .Select(DelegatorExtensions.DelegatorToAcceptor)
                                     .FirstOrDefault(w => w.Status == AcceptorStatus.Pending &&
                                                          !w.IsDeleted && w.Delegatee?.SuUserId == null
                                         ? w.UserId == userId
                                         : w.Delegatee?.SuUserId == userId) ??
                           throw new InvalidOperationException($"User {userId} does not have an accept or rejected.");

        var isAccountingReject = rejectedUser.Type is AcceptorType.AccountingApprover or AcceptorType.AccountingOperator;

        var currentPendingUser = p79Clause2.Acceptors
                                           .Where(w => !w.IsDeleted
                                                       && w.Status == AcceptorStatus.Pending
                                                       && (isAccountingReject
                                                           ? w.Type is AcceptorType.AccountingApprover or AcceptorType.AccountingOperator
                                                           : w.Type == rejectedUser.Type))
                                           .OrderBy(w => isAccountingReject && w.Type == AcceptorType.AccountingOperator ? 0 : 1)
                                           .ThenBy(o => o.Sequence)
                                           .FirstOrDefault() ??
                                 throw new InvalidOperationException($"P79Clause2 error, Acceptor must be one or more.");

        if (rejectedUser.Id != currentPendingUser.Id)
        {
            this.ThrowError(
                "The approval order is incorrect.",
                StatusCodes.Status409Conflict);
        }

        var currentAcceptorUser =
            p79Clause2.Acceptors
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

        p79Clause2.SetStatus(P79Clause2Status.Rejected);

        p79Clause2.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Reject,
                ActivityLogActionTypeConstant.Reject,
                p79Clause2.Status.ToString(),
                remark));
    }

    private static new async Task SendNotificationAsync(P79Clause2 entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Urgent79Clause2.Url, entity.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static void UpdateSequentialCurrents(P79Clause2 entity)
    {
        var approvers = entity.Acceptors
                              .Where(a => a.IsActive)
                              .OrderBy(a => a.Type == AcceptorType.Approver ? 0 :
                                  a.Type == AcceptorType.AccountingApprover ? 1 : 2)
                              .ThenBy(a => a.Sequence)
                              .ToList();

        if (approvers.Count == 0)
        {
            return;
        }

        foreach (var a in approvers)
        {
            a.SetCurrent(false);
        }

        var next = approvers.Select(DelegatorExtensions.DelegatorToAcceptor).FirstOrDefault(a =>
            a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        next.SetCurrent(true);

        var pendingOfType = approvers.Where(a => a.Type == AcceptorType.Approver && a.Status == AcceptorStatus.Pending).ToList();
        var isLastPending = pendingOfType.Count == 1;

        var pendingOfTypeAccounting = approvers.Where(a => a.Type == AcceptorType.AccountingApprover && a.Status == AcceptorStatus.Pending).ToList();
        var isLastAccountingPending = pendingOfTypeAccounting.Count == 1;

        if ((next.Type == AcceptorType.Approver && isLastPending && entity.Status == P79Clause2Status.WaitingApproval) ||
            (next.Type == AcceptorType.AccountingApprover && isLastAccountingPending && entity.Status == P79Clause2Status.WaitingAccountingApproval))
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.Urgent79Clause2.Name, entity.P79Clause2Number));
            }
        }
        else if ((next.Type == AcceptorType.Approver && !isLastPending && entity.Status == P79Clause2Status.WaitingApproval) ||
                 (next.Type == AcceptorType.AccountingApprover && !isLastAccountingPending))
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.Urgent79Clause2.Name, entity.P79Clause2Number));
            }
        }
    }

    private async ValueTask ReplaceDocumentsAsync(
        P79Clause2 entity,
        bool hasAcceptor,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();
        var replaceDto =
            await this.MapToReplaceDto(entity, ct, hasAcceptor, null);

        var timeStamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        await ReplaceDocumentAsync(P79Clause2DocumentType.Approval);

        async ValueTask ReplaceDocumentAsync(P79Clause2DocumentType documentType)
        {
            var replaceTemplate = entity.LastedNotReplacedDocument(documentType)
                                  ?? entity.DocumentHistories
                                           .Where(dh => dh.DocumentType == documentType)
                                           .OrderVersions()
                                           .FirstOrDefault();

            if (replaceTemplate is not null)
            {
                var finalFileId =
                    await documentService.CopyDocumentTemplateAsync(
                        replaceTemplate.FileId,
                        contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                        parentDirectory: $"{DocumentTemplateGroups.P79Clause2}/{entity.Id}_{documentType.ToString()}_{timeStamp}.odt",
                        cancellationToken: ct);

                if (finalFileId is null)
                {
                    this.ThrowError("ไม่สามารถคัดลอกเอกสารที่ส่งเห็นชอบ");
                }

                entity.AddDocumentHistory(documentType, finalFileId.Value, entity.Status is P79Clause2Status.WaitingApproval);
            }
        }
    }

    private async ValueTask UpdateAndReplaceDocumentAsync(
        P79Clause2 entity,
        bool? isDocumentIdReplaced,
        bool? isWinnerDocumentIdReplaced,
        CancellationToken ct,
        UserId? creatorUserId)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var lastedDraftApprovalDocument =
            entity.LastedDraftDocument(P79Clause2DocumentType.Approval);

        var lastedDraftWinnerDocument =
            entity.LastedDraftDocument(P79Clause2DocumentType.WinnerAnnouncement);

        if (lastedDraftApprovalDocument is null || lastedDraftWinnerDocument is null)
        {
            this.ThrowError("ไม่พบเอกสาร");
        }

        var replaceDto =
            await this.MapToReplaceDto(entity, ct, false, creatorUserId);

        var approvalFileId =
            await ReplaceDocument(
                lastedDraftApprovalDocument.FileId,
                isDocumentIdReplaced ?? false,
                P79Clause2DocumentType.Approval);

        var winnerFileId =
            await ReplaceDocument(
                lastedDraftWinnerDocument.FileId,
                isWinnerDocumentIdReplaced ?? false,
                P79Clause2DocumentType.WinnerAnnouncement);

        entity.AddDocumentHistory(
            P79Clause2DocumentType.Approval,
            approvalFileId,
            true);

        entity.AddDocumentHistory(
            P79Clause2DocumentType.WinnerAnnouncement,
            winnerFileId,
            true);

        return;

        async Task<FileId> ReplaceDocument(
            FileId fileId,
            bool isReplace,
            P79Clause2DocumentType documentType)
        {
            if (!isReplace)
            {
                return fileId;
            }

            var replaceDocumentAsync =
                documentService.CopyDocumentTemplateAsync(
                    fileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                    parentDirectory: $"{DocumentTemplateGroups.P79Clause2}/{entity.Id}_{documentType.ToString()}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                    cancellationToken: ct);

            var fileIdResult = await replaceDocumentAsync;

            if (fileIdResult is null)
            {
                this.ThrowError("ไม่สามารถคัดลอกเอกสารร่างได้");
            }

            return (FileId)fileIdResult;
        }
    }
}