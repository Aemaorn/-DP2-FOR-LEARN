namespace GHB.DP2.Application.Features.Procurement.ExpenseDisbursement;

using Codehard.FileService.Client.Abstractions;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Procurement.ExpenseDisbursement.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PExpenseDisbursement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateExpenseDisbursementRequest(
    Guid Id,
    PExpenseDisbursementStatus Status,
    DateTimeOffset Date,
    string? Description,
    bool IsAdvance,
    string? AdvanceName,
    string? AdvancePaymentMethodCode,
    DateTimeOffset? AdvancePaymentDate,
    string? AdvanceBankCode,
    bool? IsInvoiceAmount,
    decimal? InvoiceAmount,
    string? AdvanceBankAccount,
    string? AdvanceBankBranch,
    string? AdvanceBankAccountName,
    string? AdvanceDetail,
    string? Remarks,
    List<AssigneeDtoRequest>? Assignees,
    List<AcceptorRequest>? Acceptors,
    IEnumerable<GlAccountDto>? GlAccounts);

public record AssigneeDtoRequest(
    Guid? Id,
    AssigneeGroup AssigneeGroup,
    AssigneeType AssigneeType,
    Guid UserId,
    int Sequence,
    string? Remark);

public class UpdateExpenseDisbursementRequestValidator : Validator<UpdateExpenseDisbursementRequest>
{
    public UpdateExpenseDisbursementRequestValidator()
    {
        this.RuleFor(x => x.Id)
            .NotEmpty();

        this.RuleFor(x => x.Date)
            .NotEmpty();

        this.When(x => x.Status == PExpenseDisbursementStatus.WaitingApproval, () =>
        {
            this.RuleFor(x => x.Acceptors)
                .NotNull().WithMessage("ต้องระบุผู้มีอำนาจย่างน้อย 1 คน")
                .Must(a => a != null && a.Any())
                .WithMessage("ต้องระบุผู้มีอำนาจย่างน้อย 1 คน");
        });

        this.When(x => x.IsAdvance, () =>
        {
            this.RuleFor(x => x.AdvanceName)
                .NotEmpty().WithMessage("กรุณาระบุชื่อผู้รับเงินล่วงหน้า");

            this.RuleFor(x => x.AdvancePaymentMethodCode)
                .NotEmpty().WithMessage("กรุณาระบุวิธีการจ่ายล่วงหน้า");
        });
    }
}

public class UpdateExpenseDisbursementEndpoint : ExpenseDisbursementAbstractEndpoint<UpdateExpenseDisbursementRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateExpenseDisbursementEndpoint(
        ILogger<UpdateExpenseDisbursementEndpoint> logger,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("expense-disbursement/{id:guid}");
        this.Description(b => b
                              .WithTags("Procurement/ExpenseDisbursement")
                              .WithName("UpdateExpenseDisbursement")
                              .Produces<Ok>(StatusCodes.Status200OK)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status404NotFound)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpdateExpenseDisbursementRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PExpenseDisbursements
                               .Include(x => x.Acceptors)
                               .Include(x => x.GlAccounts)
                               .SingleOrDefaultAsync(x => x.Id == PExpenseDisbursementId.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลเบิกจ่าย ที่มีรหัส {req.Id}");
        }

        var previousStatus = entity.Status;

        // Upsert acceptors prior to status change
        if (req.Acceptors != null && (req.Status != PExpenseDisbursementStatus.Approved && req.Status != PExpenseDisbursementStatus.WaitingForCompletion))
        {
            await this.UpsertAcceptorAsync(entity, req.Acceptors, req.Status, ct);
        }

        // Upsert GL Accounts
        if (req.GlAccounts != null)
        {
            this.UpsertDetails(entity, req.GlAccounts);
        }

        // Update scalar fields & status
        var updateInfo = new PExpenseDisbursement.ExpenseDisbursementUpdateInfo(
            req.Status,
            req.IsAdvance,
            req.AdvanceName,
            string.IsNullOrWhiteSpace(req.AdvancePaymentMethodCode) ? null : ParameterCode.From(req.AdvancePaymentMethodCode),
            req.AdvancePaymentDate,
            string.IsNullOrWhiteSpace(req.AdvanceBankCode) ? null : ParameterCode.From(req.AdvanceBankCode),
            req.IsInvoiceAmount,
            req.InvoiceAmount,
            req.AdvanceBankAccount,
            req.AdvanceBankBranch,
            req.AdvanceBankAccountName,
            req.AdvanceDetail,
            req.Date,
            req.Description);

        entity.SetValue(updateInfo);

        if (previousStatus == entity.Status)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                "อัปเดตข้อมูลเบิกจ่าย",
                entity.Status.ToString()));
        }
        else
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                $"เปลี่ยนสถานะ {previousStatus} -> {entity.Status}",
                entity.Status.ToString(),
                req.Remarks));
        }

        this.dbContext.PExpenseDisbursements.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task ManageAssigneeAsync(
        PExpenseDisbursement entity,
        List<AssigneeDtoRequest> requestsAssignee,
        CancellationToken ct)
    {
        _ = entity.Assignees
                  .ExceptBy(
                      requestsAssignee
                          .Where(w => w.Id.HasValue)
                          .Select(s => s.Id.Value),
                      a => a.Id.Value)
                  .Iter(r => entity.RemoveAssigneeById(r.Id));

        _ = requestsAssignee.Where(w => w.Id.HasValue)
                            .Join(
                                entity.Assignees,
                                db => db.Id.Value,
                                payload => payload.Id.Value,
                                (payload, db) => new { db, payload })
                            .Iter(r => r.db.SetSequence(r.payload.Sequence));

        var assigneeIds = requestsAssignee
                          .Where(w => !w.Id.HasValue)
                          .Select(s => UserId.From(s.UserId))
                          .ToArray();

        var userData = await this.dbContext.SuUsers
                                 .Include(e => e.Employee)
                                 .ThenInclude(v => v.View)
                                 .Where(w => assigneeIds.Contains(w.Id))
                                 .ToArrayAsync(ct);

        this.ValidateUsers(userData, assigneeIds);

        AddAssignee(
            entity,
            [.. requestsAssignee.Where(w => !w.Id.HasValue)],
            userData);

        // Update assignee remarks
        _ = requestsAssignee
            .Where(w => w.Id.HasValue)
            .Join(
                entity.Assignees,
                db => db.Id.Value,
                payload => payload.Id.Value,
                (payload, db) => new { db, payload })
            .Iter(r => r.db.SetRemark(r.payload.Remark));
    }

    private static void AddAssignee(PExpenseDisbursement entity, AssigneeDtoRequest[] assignees, SuUser[] users)
    {
        _ = assignees
            .Join(
                users,
                a => UserId.From(a.UserId),
                u => u.Id,
                (a, u) => PExpenseDisbursementAssignee.Create(a.AssigneeGroup, a.AssigneeType, u, a.Sequence, a.Remark))
            .Iter(s => entity.AddAssignee(s));
    }

    private void ValidateUsers(SuUser[] users, UserId[] userIds)
    {
        var foundUserIds = users.Select(u => u.Id).ToArray();

        var missingUserIds = userIds.Except(foundUserIds).ToArray();

        if (missingUserIds.Length > 0)
        {
            this.ThrowError(
                r => r.Assignees,
                $"Users with IDs {string.Join(", ", missingUserIds)} not found.",
                StatusCodes.Status404NotFound);
        }
    }

    private async Task UpsertAcceptorAsync(
        PExpenseDisbursement entity,
        List<AcceptorRequest> acceptorsRequest,
        PExpenseDisbursementStatus status,
        CancellationToken ct)
    {
        var acceptorNotNew = acceptorsRequest.Where(x => x.Id.HasValue);

        var toRemove = entity.Acceptors.Where(x => acceptorNotNew.All(r => x.Id != AcceptorId.From(r.Id.Value))).ToList();

        foreach (var item in toRemove)
        {
            entity.RemoveAcceptor(item);
        }

        var userIdsIncoming =
               acceptorsRequest.Map(s => s.UserId)
                               .Map(UserId.From)
                               .ToArray();

        var usersIncoming =
               await this.dbContext.SuUsers
                         .Include(r => r.Employee)
                         .ThenInclude(r => r.View)
                         .Where(w => userIdsIncoming.Contains(w.Id))
                         .ToArrayAsync(ct);

        var userNotExistsInDb
               = userIdsIncoming
                 .Except(usersIncoming.Map(u => u.Id))
                 .ToArray();

        if (userNotExistsInDb.Length > 0)
        {
            this.ThrowError(
                $"User with ID {string.Join(", ", userNotExistsInDb)} not found.",
                StatusCodes.Status404NotFound);
        }

        var newAcceptors =
               acceptorsRequest.Where(ar => !ar.Id.HasValue)
                               .Join(
                                   usersIncoming,
                                   a => a.UserId,
                                   u => u.Id.Value,
                                   (a, u) => PExpenseDisbursementAcceptor.Create(
                                       a.AcceptorType,
                                       u,
                                       a.Sequence))
                               .ToHashSet();

        _ = entity.Acceptors
                            .Join(
                                acceptorsRequest.Where(w => w.Id.HasValue),
                                db => db.Id.Value,
                                payload => payload.Id,
                                (db, payload) =>
                                {
                                    db.SetSequence(payload.Sequence)
                                      .SetStatus(
                                          status is PExpenseDisbursementStatus.WaitingApproval
                                              ? AcceptorStatus.Pending
                                              : AcceptorStatus.Draft);

                                    return db;
                                }).ToHashSet();

        _ = newAcceptors.Map(entity.AddAcceptor)
                           .ToHashSet();
    }
}