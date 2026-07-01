namespace GHB.DP2.Application.Features.Procurement.Pw119;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Pw119.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PExpenseDisbursement; // added
using GHB.DP2.Domain.Procurement.Pw119;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record Pw119ActionRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Pw119Action Action,
    string? Remark,
    Guid? JorPorId);

public class ActionPw119 : Pw119EndpointBase<Pw119ActionRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;

    public ActionPw119(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IOperationService operationService,
        ILogger<ActionPw119> logger)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Pw119")
             .WithName("Pw119Action")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Put("pw119/action/{Id:guid}");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(Pw119ActionRequest req, CancellationToken ct)
    {
        var pw119 = await this.dbContext
                              .Pw119s
                              .Include(ac => ac.Acceptors)
                              .ThenInclude(pw => pw.User)
                              .ThenInclude(pw => pw.Employee)
                              .Include(p => p.GLAccounts) // include GL accounts for expense disbursement creation
                              .SingleOrDefaultAsync(p => p.Id == Pw119Id.From(req.Id), ct);

        if (pw119 is null)
        {
            return TypedResults.NotFound("Not found pw119");
        }

        switch (req.Action)
        {
            case Pw119Action.Recall:
                this.RecallHandler(pw119);
                await this.ReplaceDocumentsAsync(pw119, false, ct);

                break;

            case Pw119Action.RequestApproval:
                this.WaitingApprovalHandler(pw119);

                break;

            case Pw119Action.RejectedAcceptor:
                this.AcceptorRejected(pw119, UserId.From(req.UserId), req.Remark);
                await this.ReplaceDocumentsAsync(pw119, false, ct);

                break;

            case Pw119Action.ApprovedAcceptor:
                await this.AcceptorApproved(pw119, UserId.From(req.UserId), req.Remark, ct);

                break;

            case Pw119Action.ConfirmDisbursement:
                this.ConfirmDisbursement(pw119);

                break;

            default:
                throw new InvalidOperationException($"unknown pw119 action {req.Action}");
        }

        this.dbContext.Pw119s.Update(pw119);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private void WaitingApprovalHandler(
        Pw119 pw119)
    {
        pw119.Acceptors
             .Iter(r => r.Pending());

        pw119.SetStatus(Pw119Status.WaitingApproval);

        pw119.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.SendApprove,
                ActivityLogActionTypeConstant.SendApprove,
                pw119.Status.ToString()));

        var approver = pw119.Acceptors
                          .Select(DelegatorExtensions.DelegatorToAcceptor)
                          .FirstOrDefault(p => p.Sequence == 1);

        if (approver != null)
        {
            if (approver is null)
            {
                this.ThrowError($"ไม่พบข้อมูลผู้อนมัติ", StatusCodes.Status404NotFound);
            }

            approver.SetIsCurrent(true);

            foreach (var targetUserId in approver.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    pw119,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.W119.Name, pw119.Pw119Number));
            }
        }
    }

    private void RecallHandler(Pw119 pw119)
    {
        if (pw119.Acceptors.Where(a => a.Type == AcceptorType.Approver)
                 .Any(w => w.Status is AcceptorStatus.Approved or AcceptorStatus.Rejected))
        {
            this.ThrowError(
                "Acceptor already Decision",
                StatusCodes.Status403Forbidden);
        }

        pw119.Acceptors.Iter(r => r.Draft());

        pw119.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Recall,
            ActivityLogActionTypeConstant.Recall,
            pw119.Status.ToString()));

        pw119.SetStatus(Pw119Status.Edit);
    }

    private async Task AcceptorApproved(
        Pw119 pw119,
        UserId userId,
        string? remark,
        CancellationToken ct)
    {
        var approveUser = pw119.Acceptors
                               .Select(DelegatorExtensions.DelegatorToAcceptor)
                               .FirstOrDefault(w => w.Status == AcceptorStatus.Pending &&
                                                    !w.IsDeleted && w.Delegatee?.SuUserId == null
                                   ? w.UserId == userId
                                   : w.Delegatee?.SuUserId == userId) ??
                          throw new InvalidOperationException($"User {userId} does not have an accept or rejected.");

        var currentAcceptorUser =
            pw119.Acceptors
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

        UpdateSequentialCurrents(pw119);

        pw119.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Approved,
            ActivityLogActionTypeConstant.Approved,
            nameof(Pw119Status.Approved),
            remark));

        var newStatus = pw119.Acceptors.Where(w => !w.IsDeleted && w.Type == AcceptorType.Approver).All(w => w.Status == AcceptorStatus.Approved)
            ? Pw119Status.WaitingAccountingApproval
            : Pw119Status.WaitingApproval;

        var isBranch = EmployeeConstant.OrganizationLevel.BranchLevels.Contains(pw119.Department?.OrganizationLevel);

        if (newStatus == Pw119Status.WaitingAccountingApproval)
        {
            var accountingApprovers = pw119.Acceptors
                                           .Where(a => !a.IsDeleted && (a.Type == AcceptorType.AccountingApprover || a.Type == AcceptorType.AccountingOperator))
                                           .OrderBy(a => a.Type == AcceptorType.AccountingOperator ? 0 : 1)
                                           .ThenBy(a => a.Sequence)
                                           .ToList();

            if (accountingApprovers.Any())
            {
                foreach (var approver in pw119.Acceptors.Where(a => !a.IsDeleted))
                {
                    approver.SetCurrent(false);
                }

                var firstPending = accountingApprovers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

                if (firstPending != null)
                {
                    var firstSeq = firstPending.Sequence;

                    foreach (var a in accountingApprovers.Where(a => a.Sequence == firstSeq && a.Status == AcceptorStatus.Pending))
                    {
                        a.SetCurrent(true);
                    }

                    var isLastAccountingPending = accountingApprovers.Count(a => a.Status == AcceptorStatus.Pending) == 1;

                    if (isBranch || accountingApprovers.Any(a => a.Type == AcceptorType.AccountingOperator))
                    {
                        foreach (var targetUserId in firstPending.GetNotificationTargets())
                        {
                            _ = SendNotificationAsync(
                                pw119,
                                targetUserId,
                                isLastAccountingPending ? NotificationConstant.WaitForApprove.Title : NotificationConstant.WaitForLike.Title,
                                string.Format(isLastAccountingPending ? NotificationConstant.WaitForApprove.Message : NotificationConstant.WaitForLike.Message, ProgramConstant.W119.Name, pw119.Pw119Number));
                        }
                    }
                    else
                    {
                        var segmentMembers = await this.operationService.GetSegmentAccountingMembersAsync(ct);

                        foreach (var member in segmentMembers)
                        {
                            _ = SendNotificationAsync(
                                pw119,
                                member.UserId,
                                NotificationConstant.WaitForApprove.Title,
                                string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.W119.Name, pw119.Pw119Number));
                        }
                    }
                }
            }
        }

        var accountingAcceptors = pw119.Acceptors
            .Where(w => !w.IsDeleted && (w.Type == AcceptorType.AccountingApprover || w.Type == AcceptorType.AccountingOperator))
            .ToList();

        if (accountingAcceptors.Any()
                ? accountingAcceptors.All(w => w.Status == AcceptorStatus.Approved)
                : !isBranch)
        {
            newStatus = Pw119Status.WaitingDisbursementDate;

            foreach (var approver in pw119.Acceptors.Where(a => !a.IsDeleted && (a.Type == AcceptorType.AccountingApprover || a.Type == AcceptorType.AccountingOperator)))
            {
                approver.SetCurrent(false);
            }

            var confirmers = pw119.Acceptors
                                  .Where(a => !a.IsDeleted && a.Type == AcceptorType.AccountingConfirmer)
                                  .ToList();

            if (confirmers.Any())
            {
                foreach (var targetUserId in confirmers.SelectMany(c => c.GetNotificationTargets()))
                {
                    _ = SendNotificationAsync(
                        pw119,
                        targetUserId,
                        NotificationConstant.WaitConfirmDisbursement.Title,
                        string.Format(NotificationConstant.WaitConfirmDisbursement.Message, ProgramConstant.W119.Name, pw119.Pw119Number));
                }
            }
            else if (!isBranch)
            {
                var segmentMembers = await this.operationService.GetSegmentAccountingMembersAsync(ct);

                foreach (var member in segmentMembers)
                {
                    _ = SendNotificationAsync(
                        pw119,
                        member.UserId,
                        NotificationConstant.WaitConfirmDisbursement.Title,
                        string.Format(NotificationConstant.WaitConfirmDisbursement.Message, ProgramConstant.W119.Name, pw119.Pw119Number));
                }
            }
        }

        if (newStatus == Pw119Status.Approved)
        {
            var updateInfo = new PExpenseDisbursement.ExpenseDisbursementUpdateInfo(
                PExpenseDisbursementStatus.Draft,
                pw119.IsAdvance,
                pw119.AdvanceName,
                pw119.AdvancePaymentMethodCode,
                pw119.AdvancePaymentDate,
                pw119.AdvanceBankCode,
                null, // IsInvoiceAmount not present in Pw119
                null, // InvoiceAmount not present in Pw119
                pw119.AdvanceBankAccount,
                pw119.AdvanceBankBranch,
                pw119.AdvanceBankAccountName,
                pw119.AdvanceDetail,
                DateTimeOffset.UtcNow,
                null);
            var expense = PExpenseDisbursement.Create(PExpenseDisbursementSourceType.W119, pw119.Id.Value)
                                              .SetValue(updateInfo);

            // Map GL Accounts
            if (pw119.GLAccounts is not null)
            {
                foreach (var gl in pw119.GLAccounts.OrderBy(g => g.Sequence))
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
                    pw119.GLAccounts?.Sum(x => x.Amount) ?? 0,
                    "SectionApprover001",
                    null,
                    ct,
                    false);

                userIdsList.AddRange(acceptorData.Map(x => x.UserId));

                var usersIncomingList = await this.dbContext.SuUsers
                                                  .Include(r => r.Employee)
                                                  .ThenInclude(r => r.View)
                                                  .Where(w => userIdsList.Contains(w.Id))
                                                  .ToListAsync(ct);

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

        if (pw119.Status is Pw119Status.WaitingApproval)
        {
            await this.ReplaceDocumentsAsync(pw119, true, ct);
        }

        pw119.SetStatus(newStatus);
    }

    private void ConfirmDisbursement(Pw119 pw119)
    {
        pw119.SetStatus(Pw119Status.Paid);

        pw119.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.ConfirmDisbursement,
            ActivityLogActionTypeConstant.ConfirmDisbursement,
            nameof(Pw119Status.Paid),
            null));

        _ = SendNotificationAsync(
            pw119,
            UserId.From(pw119.AuditInfo.CreatedBy),
            NotificationConstant.DisbursementPaid.Title,
            string.Format(NotificationConstant.DisbursementPaid.Message, ProgramConstant.W119.Name, pw119.Pw119Number));
    }

    private void AcceptorRejected(Pw119 pw119, UserId userId, string? remark)
    {
        var isRejected = pw119.Acceptors
                              .Any(w => !w.IsDeleted && w.Status == AcceptorStatus.Rejected);

        if (isRejected)
        {
            this.ThrowError(
                "already rejected",
                StatusCodes.Status409Conflict);
        }

        var rejectedUser = pw119.Acceptors
                                .Select(DelegatorExtensions.DelegatorToAcceptor)
                                .FirstOrDefault(w => !w.IsDeleted && w.Delegatee?.SuUserId == null
                                    ? w.UserId == userId
                                    : w.Delegatee?.SuUserId == userId) ??
                           throw new InvalidOperationException($"User {userId} does not have an accept or rejected.");

        var isAccountingReject = rejectedUser.Type is AcceptorType.AccountingApprover or AcceptorType.AccountingOperator;

        var currentPendingUser = pw119.Acceptors
                                      .Where(w => !w.IsDeleted
                                                  && w.Status == AcceptorStatus.Pending
                                                  && (isAccountingReject
                                                      ? w.Type is AcceptorType.AccountingApprover or AcceptorType.AccountingOperator
                                                      : w.Type == rejectedUser.Type))
                                      .OrderBy(w => isAccountingReject && w.Type == AcceptorType.AccountingOperator ? 0 : 1)
                                      .ThenBy(o => o.Sequence)
                                      .FirstOrDefault() ??
                                 throw new InvalidOperationException($"Pw119 error, Acceptor must be one or more.");

        if (rejectedUser.Id != currentPendingUser.Id)
        {
            this.ThrowError(
                "The approval order is incorrect.",
                StatusCodes.Status409Conflict);
        }

        var currentAcceptorUser =
            pw119.Acceptors
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

        pw119.SetStatus(Pw119Status.Rejected);

        pw119.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            ActivityLogActionTypeConstant.Reject,
            nameof(Pw119Status.Rejected),
            remark));

        _ = SendNotificationAsync(
            pw119,
            UserId.From(pw119.AuditInfo.CreatedBy),
            NotificationConstant.ReturnToCreator.Title,
            string.Format(NotificationConstant.ReturnToCreator.Message, ProgramConstant.W119.Name, pw119.Pw119Number));
    }

    private async ValueTask ReplaceDocumentsAsync(
        Pw119 entity,
        bool hasAcceptor,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var replaceDto =
            await this.MapToReplaceDtoAsync(entity, ct, hasAcceptor, null);

        var timeStamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        await ReplaceDocumentAsync(Pw119DocumentType.Approval);

        async ValueTask ReplaceDocumentAsync(Pw119DocumentType documentType)
        {
            var replaceTemplate =
                entity.LastedNotReplacedDocument(documentType)
                ?? entity.DocumentHistories
                    .Where(dh => dh.DocumentType == documentType)
                    .OrderVersions()
                    .FirstOrDefault();

            if (replaceTemplate is null)
            {
                this.ThrowError($"ไม่พบข้อมูลเอกสาร {documentType.ToString()} ที่ส่งเห็นชอบ");
            }

            var finalFileId =
                await documentService.CopyDocumentTemplateAsync(
                    replaceTemplate.FileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                    parentDirectory: $"{DocumentTemplateGroups.Pw119}/{entity.Id}_{documentType.ToString()}_{timeStamp}.odt",
                    cancellationToken: ct);

            if (finalFileId is null)
            {
                this.ThrowError("ไม่สามารถคัดลอกเอกสารที่ส่งเห็นชอบ");
            }

            entity.AddDocumentHistory(documentType, finalFileId.Value, entity.Status is Pw119Status.WaitingApproval);
        }
    }

    private async ValueTask UpdateAndReplaceDocumentAsync(
        Pw119 entity,
        bool? isDocumentIdReplaced,
        bool? isWinnerDocumentIdReplaced,
        CancellationToken ct,
        UserId? creatorUserId)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var lastedDraftApprovalDocument =
            entity.LastedDraftDocument(Pw119DocumentType.Approval);

        var lastedDraftWinnerDocument =
            entity.LastedDraftDocument(Pw119DocumentType.WinnerAnnouncement);

        if (lastedDraftApprovalDocument is null || lastedDraftWinnerDocument is null)
        {
            this.ThrowError("ไม่พบเอกสาร");
        }

        var replaceDto =
            await this.MapToReplaceDtoAsync(entity, ct, true, creatorUserId);

        var approvalFileId =
            await ReplaceDocument(
                lastedDraftApprovalDocument.FileId,
                isDocumentIdReplaced ?? false,
                Pw119DocumentType.Approval);

        var winnerFileId =
            await ReplaceDocument(
                lastedDraftWinnerDocument.FileId,
                isWinnerDocumentIdReplaced ?? false,
                Pw119DocumentType.WinnerAnnouncement);

        entity.AddDocumentHistory(
            Pw119DocumentType.Approval,
            approvalFileId,
            true);

        entity.AddDocumentHistory(
            Pw119DocumentType.WinnerAnnouncement,
            winnerFileId,
            true);

        return;

        async Task<FileId> ReplaceDocument(
            FileId fileId,
            bool isReplace,
            Pw119DocumentType documentType)
        {
            if (!isReplace)
            {
                return fileId;
            }

            var replaceDocumentAsync =
                documentService.CopyDocumentTemplateAsync(
                    fileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                    parentDirectory: $"{DocumentTemplateGroups.Pw119}/{entity.Id}_{documentType.ToString()}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                    cancellationToken: ct);

            var fileIdResult = await replaceDocumentAsync;

            if (fileIdResult is null)
            {
                this.ThrowError("ไม่สามารถคัดลอกเอกสารร่างได้");
            }

            return (FileId)fileIdResult;
        }
    }

    private static new async Task SendNotificationAsync(Pw119 entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.W119.Url, entity.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static void UpdateSequentialCurrents(Pw119 entity)
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

        if ((next.Type == AcceptorType.Approver && isLastPending && entity.Status == Pw119Status.WaitingApproval) ||
            (next.Type == AcceptorType.AccountingApprover && isLastAccountingPending && entity.Status == Pw119Status.WaitingAccountingApproval))
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.W119.Name, entity.Pw119Number));
            }
        }
        else if ((next.Type == AcceptorType.Approver && !isLastPending && entity.Status == Pw119Status.WaitingApproval) ||
                 (next.Type == AcceptorType.AccountingApprover && !isLastAccountingPending))
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.W119.Name, entity.Pw119Number));
            }
        }
    }
}