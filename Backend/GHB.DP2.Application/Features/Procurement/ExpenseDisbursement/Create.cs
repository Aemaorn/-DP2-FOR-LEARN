namespace GHB.DP2.Application.Features.Procurement.ExpenseDisbursement;

using System.Linq;
using Codehard.FileService.Client.Abstractions;
using FluentValidation;
using GHB.DP2.Application.Features.Procurement.ExpenseDisbursement.Abstract;
using GHB.DP2.Domain.Procurement.PExpenseDisbursement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateExpenseDisbursementRequest(
    PExpenseDisbursementStatus Status,
    PExpenseDisbursementSourceType SourceType,
    Guid SourceId,
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
    List<AssigneeDtoRequest>? Assignees,
    IEnumerable<GlAccountDto>? GlAccounts);

public class CreateExpenseDisbursementRequestValidator : Validator<CreateExpenseDisbursementRequest>
{
    public CreateExpenseDisbursementRequestValidator()
    {
        this.RuleFor(x => x.SourceId)
            .NotEmpty()
            .WithMessage("กรุณาระบุแหล่งที่มาของเอกสาร");

        this.RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("กรุณาระบุวันที่เอกสาร");

        this.When(x => x.Status == PExpenseDisbursementStatus.WaitingApproval, () =>
        {
            this.RuleFor(x => x.Assignees)
                .NotNull()
                .WithMessage("ต้องมอบหมายผู้รับผิดชอบิอย่างน้อย 1 คน")
                .Must(a => a != null && a.Any())
                .WithMessage("ต้องมอบหมายผู้รับผิดชอบิอย่างน้อย 1 คน");
        });
    }
}

public class CreateExpenseDisbursementEndpoint : ExpenseDisbursementAbstractEndpoint<CreateExpenseDisbursementRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreateExpenseDisbursementEndpoint(
        ILogger<CreateExpenseDisbursementEndpoint> logger,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("expense-disbursement");
        this.Description(b => b
                              .WithTags("Procurement/ExpenseDisbursement")
                              .WithName("CreateExpenseDisbursement")
                              .Produces<Created<Guid>>(StatusCodes.Status201Created)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(CreateExpenseDisbursementRequest req, CancellationToken ct)
    {
        var entity = PExpenseDisbursement.Create(req.SourceType, req.SourceId);

        if (req.Assignees != null)
        {
            await this.AddAssigneeAsync(entity, req.Assignees, ct);
        }

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

        if (req.GlAccounts != null)
        {
            this.UpsertDetails(entity, req.GlAccounts);
        }

        this.dbContext.Add(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, entity.Id.Value);
    }

    private async Task AddAssigneeAsync(
        PExpenseDisbursement expenseDisbursement,
        List<AssigneeDtoRequest> requestsAssignee,
        CancellationToken ct)
    {
        var assigneeIds = requestsAssignee.Select(s => UserId.From(s.UserId)).ToArray();

        var userData = await this.dbContext.SuUsers
                                 .Include(e => e.Employee)
                                 .ThenInclude(v => v.View)
                                 .Where(w => assigneeIds.Contains(w.Id))
                                 .ToArrayAsync(ct);

        this.ValidateUsers(userData, assigneeIds);

        requestsAssignee
            .Join(
                userData,
                a => UserId.From(a.UserId),
                u => u.Id,
                (a, u) => PExpenseDisbursementAssignee.Create(a.AssigneeGroup, a.AssigneeType, u, a.Sequence, a.Remark))
            .Iter(s => expenseDisbursement.AddAssignee(s));
    }

    private void ValidateUsers(SuUser[] users, UserId[] assignUserIds)
    {
        var foundUserIds = users.Select(u => u.Id).ToArray();

        var missingAssigneeUserIds = assignUserIds.Except(foundUserIds).ToArray();

        if (missingAssigneeUserIds.Any())
        {
            this.ThrowError(
                r => r.Assignees,
                $"Users with IDs {string.Join(", ", missingAssigneeUserIds)} not found.",
                StatusCodes.Status404NotFound);
        }
    }
}