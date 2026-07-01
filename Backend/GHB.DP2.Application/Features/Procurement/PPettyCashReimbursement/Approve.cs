namespace GHB.DP2.Application.Features.Procurement.PPettyCashReimbursement;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PExpenseDisbursement;
using GHB.DP2.Domain.Procurement.PPettyCashReimbursement;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ApprovePettyCashReimbursementRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    string? Remark);

public class ApprovePettyCashReimbursementEndpoint : EndpointBase<ApprovePettyCashReimbursementRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    private readonly IOperationService operationService;

    public ApprovePettyCashReimbursementEndpoint(ILogger<ApprovePettyCashReimbursementEndpoint> logger, Dp2DbContext dbContext, IOperationService operationService)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Post("petty-cash-reimbursement/{id:guid}/approve");
        this.Description(b => b
                              .WithTags("Procurement/PPettyCashReimbursement")
                              .WithName("ApprovePettyCashReimbursement")
                              .Produces(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound)
                              .Produces<string>(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApprovePettyCashReimbursementRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PPettyCashReimbursements
                               .Include(e => e.Acceptors).Include(pPettyCashReimbursement => pPettyCashReimbursement.Items)!
                               .ThenInclude(pPettyCashReimbursementItems => pPettyCashReimbursementItems.PettyCashGlAccount)
                               .FirstOrDefaultAsync(e => e.Id == PPettyCashReimbursementId.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูล เบิกเงินชดเชยเงินสดย่อย {req.Id}");
        }

        var acceptors = entity.Acceptors
                              ?.Where(a => a.IsActive)
                              .OrderBy(a => a.Sequence)
                              .ToList() ?? new List<PPettyCashReimbursementAcceptor>();

        var current = acceptors.Select(DelegatorExtensions.DelegatorToAcceptor)
                               .FirstOrDefault(a =>
                                   (a.Status == AcceptorStatus.Pending || a.Status == AcceptorStatus.Draft) &&
                                   (a.Delegatee?.SuUserId == null
                                       ? a.UserId == req.UserId
                                       : a.Delegatee?.SuUserId == UserId.From(req.UserId)));

        if (current == null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        // Check previous sequence approved
        if (!IsPreviousApproved(acceptors, current))
        {
            return TypedResults.BadRequest("ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน");
        }

        var currentAcceptorUser = entity.Acceptors?.FirstOrDefault(a => a.Id == current.Id);

        if (currentAcceptorUser == null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(current.DelegateeId)
            .Approve(remark: req.Remark);

        UpdateSequentialCurrents(entity);

        entity.EvaluateAcceptorApproval();

        if (entity.Status == PPettyCashReimbursementStatus.Approved)
        {
            var exists = await this.dbContext.PExpenseDisbursements
                                   .AnyAsync(e => e.SourceType == PExpenseDisbursementSourceType.PettyCashReimbursement && e.SourceId == entity.Id.Value, CancellationToken.None);

            if (!exists)
            {
                var updateInfo = new PExpenseDisbursement.ExpenseDisbursementUpdateInfo(
                    PExpenseDisbursementStatus.Draft,
                    false,
                    entity.Subject,
                    null,
                    entity.ReimbursementDate,
                    null,
                    null,
                    null,
                    null,
                    null,
                    entity.BankAccountName,
                    null,
                    DateTimeOffset.UtcNow,
                    entity.Subject);
                var expense = PExpenseDisbursement.Create(PExpenseDisbursementSourceType.PettyCashReimbursement, entity.Id.Value)
                                                  .SetValue(updateInfo);

                if (entity.Items != null)
                {
                    var seq = 1;

                    foreach (var item in entity.Items.OrderBy(i => i.Sequence))
                    {
                        var gl = item.PettyCashGlAccount;

                        var glEntity = PExpenseDisbursementGlAccount
                                       .Create()
                                       .SetValue(
                                           seq++,
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
                        entity.Items?.Sum(x => x.PettyCashGlAccount.Amount) ?? 0,
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

        if (entity.Status == PPettyCashReimbursementStatus.WaitingAccountingApproval)
        {
            var accountingApprovers = (entity.Acceptors ?? [])
                                      .Where(a => a.IsActive && (a.Type == AcceptorType.AccountingApprover || a.Type == AcceptorType.AccountingOperator))
                                      .OrderBy(a => a.Type == AcceptorType.AccountingOperator ? 0 : 1)
                                      .ThenBy(a => a.Sequence)
                                      .ToList();

            if (accountingApprovers.Any())
            {
                foreach (var approver in (entity.Acceptors ?? []).Where(a => a.IsActive))
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

                    foreach (var targetUserId in firstPending.GetNotificationTargets())
                    {
                        _ = SendNotificationAsync(
                            entity,
                            targetUserId,
                            isLastAccountingPending ? NotificationConstant.WaitForApprove.Title : NotificationConstant.WaitForLike.Title,
                            string.Format(isLastAccountingPending ? NotificationConstant.WaitForApprove.Message : NotificationConstant.WaitForLike.Message, ProgramConstant.PettyCashReimbursement.Name, entity.Number));
                    }
                }
            }
        }

        if (entity.Status == PPettyCashReimbursementStatus.WaitingDisbursementDate)
        {
            var committeeMembers = await this.dbContext.SuUsers
                                             .Include(u => u.Employee)
                                             .ThenInclude(e => e.View)
                                             .Where(x => x.Employee.Positions.Any(p =>
                                                 p.Acting == EmployeeConstant.Acting.Primary &&
                                                 p.BusinessUnit.BusinessUnitCode == ExpenseDisbursementConstant.DefaultDirector.BusinessUnitCode))
                                             .ToListAsync(ct);

            foreach (var member in committeeMembers)
            {
                _ = SendNotificationAsync(
                    entity,
                    member.Id,
                    NotificationConstant.AccountingSetDate.Title,
                    string.Format(NotificationConstant.AccountingSetDate.Message, ProgramConstant.PettyCashReimbursement.Name, entity.Number));
            }
        }

        this.dbContext.PPettyCashReimbursements.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static bool IsPreviousApproved(List<PPettyCashReimbursementAcceptor> acceptors, PPettyCashReimbursementAcceptor current)
    {
        if (current.Sequence <= 1)
        {
            return true;
        }

        var prev = acceptors.LastOrDefault(a => a.Sequence < current.Sequence && a.IsActive);

        return prev == null || prev.Status == AcceptorStatus.Approved;
    }

    private static void UpdateSequentialCurrents(PPettyCashReimbursement entity)
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

        var next = approvers.FirstOrDefault(a =>
            a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        next.SetCurrent(true);

        var pendingOfType = approvers.Where(a => a.Type == AcceptorType.Approver && a.Status == AcceptorStatus.Pending).ToList();
        var isLastPending = pendingOfType.Count == 1;

        var pendingOfTypeAccounting = approvers.Where(a => (a.Type == AcceptorType.AccountingApprover || a.Type == AcceptorType.AccountingOperator) && a.Status == AcceptorStatus.Pending).ToList();
        var isLastAccountingPending = pendingOfTypeAccounting.Count == 1;

        var isAccountingType = next.Type == AcceptorType.AccountingApprover || next.Type == AcceptorType.AccountingOperator;

        if ((next.Type == AcceptorType.Approver && isLastPending && entity.Status == PPettyCashReimbursementStatus.WaitingApproval) ||
            (isAccountingType && isLastAccountingPending && entity.Status == PPettyCashReimbursementStatus.WaitingAccountingApproval))
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.PettyCashReimbursement.Name, entity.Number));
            }
        }
        else if ((next.Type == AcceptorType.Approver && !isLastPending && entity.Status == PPettyCashReimbursementStatus.WaitingApproval) ||
                 (isAccountingType && !isLastAccountingPending))
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PettyCashReimbursement.Name, entity.Number));
            }
        }
    }

    private static async Task SendNotificationAsync(PPettyCashReimbursement entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.PettyCashReimbursement.Url, entity.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}