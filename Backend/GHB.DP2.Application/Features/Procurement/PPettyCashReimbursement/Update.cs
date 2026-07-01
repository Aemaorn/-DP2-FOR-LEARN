namespace GHB.DP2.Application.Features.Procurement.PPettyCashReimbursement;

using Codehard.FileService.Client.Abstractions;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Procurement.PPettyCashReimbursement.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPettyCashReimbursement;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using System.IdentityModel.Tokens.Jwt;

public record UpdatePettyCashReimbursementRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    PPettyCashReimbursementStatus Status,
    DateTimeOffset ReimbursementDate,
    string Subject,
    string? Description,
    string DepartmentId,
    string BankAccountName,
    string BankAccountNumber,
    IEnumerable<AcceptorRequest>? Acceptors,
    IEnumerable<PettyCashReimbursementItemDto>? Items,
    DateTimeOffset? DisbursementDate,
    decimal? DisbursementAmount,
    string? DisbursementDescription,
    AcceptorRequest[] AcceptanceConfirmers);

public class UpdatePettyCashReimbursementValidator : Validator<UpdatePettyCashReimbursementRequest>
{
    public UpdatePettyCashReimbursementValidator()
    {
        this.RuleFor(x => x.ReimbursementDate).NotEmpty();
        this.RuleFor(x => x.Subject).NotEmpty();
        this.RuleFor(x => x.BankAccountName).NotEmpty();
        this.RuleFor(x => x.BankAccountNumber).NotEmpty();

        this.When(x => x.Status == PPettyCashReimbursementStatus.WaitingApproval, () =>
        {
            this.RuleFor(x => x.Acceptors)
                .NotNull().WithMessage("ต้องระบุผู้มีอำนาจอนุมัติ")
                .Must(a => a != null && a.Any())
                .WithMessage("ต้องระบุผู้มีอำนาจอนุมัติ");
            this.RuleFor(x => x.Items)
                .NotNull().WithMessage("ต้องมีรายละเอียด")
                .Must(d => d != null && d.Any())
                .WithMessage("ต้องมีรายละเอียด");
        });
    }
}

public class UpdatePettyCashReimbursementEndpoint : PPettyCashReimbursementAbstractEndpoint<UpdatePettyCashReimbursementRequest, Results<Ok, NotFound<string>, BadRequest<ValidationProblemDetails>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdatePettyCashReimbursementEndpoint(ILogger<UpdatePettyCashReimbursementEndpoint> logger, Dp2DbContext dbContext, IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("petty-cash-reimbursement/{id:guid}");
        this.Description(b => b
                              .WithTags("Procurement/PPettyCashReimbursement")
                              .WithName("UpdatePettyCashReimbursement")
                              .Produces<Ok>()
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status404NotFound)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<ValidationProblemDetails>>> HandleRequestAsync(UpdatePettyCashReimbursementRequest req, CancellationToken ct)
    {
        var validator = new UpdatePettyCashReimbursementValidator();
        var result = await validator.ValidateAsync(req, ct);

        if (!result.IsValid)
        {
            return TypedResults.BadRequest(new ValidationProblemDetails(result.ToDictionary()));
        }

        var entity = await this.dbContext.PPettyCashReimbursements
                               .Include(e => e.Acceptors)
                               .Include(e => e.Items).Include(auditableEntity => auditableEntity.AuditInfo)
                               .FirstOrDefaultAsync(e => e.Id == PPettyCashReimbursementId.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลเบิกเงินชดเชยเงินสดย่อย {req.Id}");
        }

        if (req.Acceptors != null)
        {
            var previousFirstPendingUserId = entity.Acceptors
                .Where(a => !a.IsDeleted &&
                            (a.Type == AcceptorType.AccountingApprover || a.Type == AcceptorType.AccountingOperator))
                .OrderBy(a => a.Type == AcceptorType.AccountingOperator ? 0 : 1)
                .ThenBy(a => a.Sequence)
                .FirstOrDefault(a => a.Status == AcceptorStatus.Pending)
                ?.UserId;

            var newConfirmerUserIds = (req.AcceptanceConfirmers ?? [])
                .Where(a => !a.Id.HasValue)
                .Select(a => UserId.From(a.UserId))
                .ToHashSet();

            var acceptorRequest = req.Status is PPettyCashReimbursementStatus.Paid or PPettyCashReimbursementStatus.WaitingDisbursementDate
                ? [.. (req.Acceptors ?? []).Where(a => a.AcceptorType != AcceptorType.AccountingConfirmer), .. req.AcceptanceConfirmers ?? []]
                : req.Acceptors;

            var previousAcceptorIds = entity.Acceptors.Select(a => a.Id.Value).ToHashSet();

            var keptAcceptorIds = (acceptorRequest ?? [])
                .Where(a => a.Id.HasValue)
                .Select(a => a.Id!.Value)
                .ToHashSet();

            var removedAcceptors = entity.Acceptors
                .Where(a => !keptAcceptorIds.Contains(a.Id.Value) &&
                            a.Type is AcceptorType.AccountingOperator or AcceptorType.AccountingApprover or AcceptorType.AccountingConfirmer)
                .ToList();

            await this.UpsertAcceptors(entity, acceptorRequest ?? [], req.Status, ct, UserId.From(req.UserId));

            var addedAcceptors = entity.Acceptors
                .Where(a => !previousAcceptorIds.Contains(a.Id.Value) &&
                            a.Type is AcceptorType.AccountingOperator or AcceptorType.AccountingApprover or AcceptorType.AccountingConfirmer)
                .ToList();

            if (entity.Status == PPettyCashReimbursementStatus.WaitingAccountingApproval)
            {
                var accountingAcceptors = entity.Acceptors
                    .Where(a => !a.IsDeleted &&
                                (a.Type == AcceptorType.AccountingApprover || a.Type == AcceptorType.AccountingOperator))
                    .OrderBy(a => a.Type == AcceptorType.AccountingOperator ? 0 : 1)
                    .ThenBy(a => a.Sequence)
                    .ToList();

                var firstPending = accountingAcceptors.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

                if (firstPending != null && firstPending.UserId != previousFirstPendingUserId)
                {
                    var isLastPending = accountingAcceptors.Count(a => a.Status == AcceptorStatus.Pending) == 1;
                    var title = isLastPending ? NotificationConstant.WaitForApprove.Title : NotificationConstant.WaitForLike.Title;
                    var message = string.Format(
                        isLastPending ? NotificationConstant.WaitForApprove.Message : NotificationConstant.WaitForLike.Message,
                        ProgramConstant.PettyCashReimbursement.Name,
                        entity.Number);

                    foreach (var targetUserId in firstPending.GetNotificationTargets())
                    {
                        _ = SendNotificationAsync(entity, targetUserId, title, message);
                    }
                }
            }

            if (newConfirmerUserIds.Count > 0)
            {
                var confirmerTargets = entity.Acceptors
                    .Where(a => !a.IsDeleted &&
                                a.Type == AcceptorType.AccountingConfirmer &&
                                newConfirmerUserIds.Contains(a.UserId))
                    .SelectMany(a => a.GetNotificationTargets());

                foreach (var targetUserId in confirmerTargets)
                {
                    _ = SendNotificationAsync(
                        entity,
                        targetUserId,
                        NotificationConstant.WaitConfirmDisbursement.Title,
                        string.Format(NotificationConstant.WaitConfirmDisbursement.Message, ProgramConstant.PettyCashReimbursement.Name, entity.Number));
                }
            }

            foreach (var removed in removedAcceptors)
            {
                entity.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.RemoveAcceptor,
                    ActivityLogActionTypeConstant.RemoveAcceptor,
                    nameof(entity.Acceptors),
                    removed.FullName));
            }

            foreach (var added in addedAcceptors)
            {
                entity.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.AddAcceptor,
                    ActivityLogActionTypeConstant.AddAcceptor,
                    nameof(entity.Acceptors),
                    added.FullName));
            }
        }

        // Preserve running number; update other values
        entity.SetValues(
            entity.Number,
            req.Status,
            req.ReimbursementDate,
            req.Subject,
            req.Description,
            req.BankAccountName,
            req.BankAccountNumber,
            BusinessUnitId.From(req.DepartmentId))
           .SetDocumentDate(req.ReimbursementDate);

        if (req.DisbursementDate is not null
            && req.DisbursementAmount is not null
            && req.DisbursementDescription is not null)
        {
            entity.SetDisbursement(req.DisbursementDate, req.DisbursementAmount, req.DisbursementDescription);
        }

        if (req.Items != null)
        {
            await this.UpsertItems(entity, req.Items, ct);
        }

        if (entity.Status == req.Status)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                string.Empty,
                req.Status.ToString()));
        }

        if (req.Status == PPettyCashReimbursementStatus.WaitingApproval)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.SendApprove,
                ActivityLogActionTypeConstant.SendApprove,
                req.Status.ToString()));

            var firstAcceptor = entity.Acceptors.OrderBy(a => a.Sequence).FirstOrDefault();

            if (firstAcceptor is not null)
            {
                foreach (var targetUserId in firstAcceptor.GetNotificationTargets())
                {
                    _ = SendNotificationAsync(
                        entity,
                        targetUserId,
                        NotificationConstant.WaitForLike.Title,
                        string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PettyCashReimbursement.Name, entity.Number));
                }
            }
        }

        if (req.Status == PPettyCashReimbursementStatus.Paid)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.ConfirmDisbursement,
                ActivityLogActionTypeConstant.ConfirmDisbursement,
                req.Status.ToString()));

            _ = SendNotificationAsync(
                entity,
                UserId.From(entity.AuditInfo.CreatedBy),
                NotificationConstant.DisbursementPaid.Title,
                string.Format(NotificationConstant.DisbursementPaid.Message, ProgramConstant.PettyCashReimbursement.Name, entity.Number));
        }

        this.dbContext.PPettyCashReimbursements.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
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