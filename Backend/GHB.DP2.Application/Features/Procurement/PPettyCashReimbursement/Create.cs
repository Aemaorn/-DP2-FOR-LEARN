namespace GHB.DP2.Application.Features.Procurement.PPettyCashReimbursement;

using Codehard.FileService.Client.Abstractions;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Procurement.PPettyCashReimbursement.Abstract;
using GHB.DP2.Domain.Procurement.PPettyCashReimbursement;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

public record CreatePettyCashReimbursementRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    PPettyCashReimbursementStatus Status,
    DateTimeOffset ReimbursementDate,
    string Subject,
    string? Description,
    string DepartmentId,
    string BankAccountName,
    string BankAccountNumber,
    IEnumerable<AcceptorRequest>? Acceptors,
    IEnumerable<PettyCashReimbursementItemDto>? Items);

public class CreatePettyCashReimbursementValidator : Validator<CreatePettyCashReimbursementRequest>
{
    public CreatePettyCashReimbursementValidator()
    {
        this.RuleFor(x => x.ReimbursementDate)
            .NotEmpty().WithMessage("กรุณาระบุวันที่");
        this.RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("กรุณาระบุเรื่อง");
        this.RuleFor(x => x.BankAccountName)
            .NotEmpty().WithMessage("กรุณาระบุชื่อบัญชีธนาคาร");
        this.RuleFor(x => x.BankAccountNumber)
            .NotEmpty().WithMessage("กรุณาระบุเลขที่บัญชีธนาคาร");

        this.When(x => x.Status == PPettyCashReimbursementStatus.WaitingApproval, () =>
        {
            this.RuleFor(x => x.Acceptors)
                .NotNull().WithMessage("ต้องระบุผู้มีอำนาจอนุมัติอย่างน้อย 1 คน")
                .Must(a => a != null && a.Any())
                .WithMessage("ต้องระบุผู้มีอำนาจอนุมัติอย่างน้อย 1 คน");
            this.RuleFor(x => x.Items)
                .NotNull().WithMessage("ต้องมีรายละเอียดอย่างน้อย 1 รายการ")
                .Must(d => d != null && d.Any())
                .WithMessage("ต้องมีรายละเอียดอย่างน้อย 1 รายการ");
        });
    }
}

public class CreatePettyCashReimbursementEndpoint : PPettyCashReimbursementAbstractEndpoint<CreatePettyCashReimbursementRequest, Results<Created<Guid>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CreatePettyCashReimbursementEndpoint(ILogger<CreatePettyCashReimbursementEndpoint> logger, Dp2DbContext dbContext, IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("petty-cash-reimbursement");
        this.Description(b => b
            .WithTags("Procurement/PPettyCashReimbursement")
            .WithName("CreatePettyCashReimbursement")
            .Produces<Created<Guid>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    private async Task<string> GenerateRunningNumberAsync(DateTimeOffset date, CancellationToken ct)
    {
        // Assumption: Prefix PCR + Buddhist year (2 digits) + 5 running digits similar to other modules.
        var year = (date.Year + 543) % 100;
        var yearStr = year.ToString("D2");
        var prefix = $"PCR{yearStr}";

        var latest = await this.dbContext.PPettyCashReimbursements
            .Where(x => x.Number.StartsWith(prefix))
            .OrderByDescending(x => x.Number)
            .FirstOrDefaultAsync(ct);

        int nextSeq = 1;
        if (latest != null && latest.Number.Length >= prefix.Length + 5)
        {
            var seqStr = latest.Number.Substring(prefix.Length, 5);
            if (int.TryParse(seqStr, out var lastSeq))
            {
                nextSeq = lastSeq + 1;
            }
        }

        return $"{prefix}{nextSeq:00000}";
    }

    protected override async ValueTask<Results<Created<Guid>, BadRequest<string>>> HandleRequestAsync(CreatePettyCashReimbursementRequest req, CancellationToken ct)
    {
        var number = await this.GenerateRunningNumberAsync(req.ReimbursementDate, ct);

        var entity = PPettyCashReimbursement.Create()
            .SetValues(
                number,
                req.Status,
                req.ReimbursementDate,
                req.Subject,
                req.Description,
                req.BankAccountName,
                req.BankAccountNumber,
                BusinessUnitId.From(req.DepartmentId))
            .SetDocumentDate(req.ReimbursementDate);

        if (req.Acceptors != null)
        {
            await this.UpsertAcceptors(entity, req.Acceptors, req.Status, ct, UserId.From(req.UserId));
        }

        if (req.Items != null)
        {
            await this.UpsertItems(entity, req.Items, ct);
        }

        this.dbContext.PPettyCashReimbursements.Add(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, entity.Id.Value);
    }
}