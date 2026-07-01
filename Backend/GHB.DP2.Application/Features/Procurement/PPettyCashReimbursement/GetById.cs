namespace GHB.DP2.Application.Features.Procurement.PPettyCashReimbursement;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPettyCashReimbursement;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetPettyCashReimbursementByIdRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id);

public record PettyCashReimbursementGlItemResponse(
    Guid? Id,
    Guid PettyCashGlAccountId,
    int Sequence,
    DateTimeOffset PettyCashDate,
    string PettyCashNumber,
    string PettyCashSubject,
    string SoId,
    string DepartmentName,
    string BudgetTypeCode,
    string? BudgetTypeLabel,
    string GlAccountCode,
    string? GlAccountLabel,
    string? ProjectNumber,
    decimal Amount);

public record GetPettyCashReimbursementByIdResponse(
    Guid Id,
    string Number,
    PPettyCashReimbursementStatus Status,
    DateTimeOffset ReimbursementDate,
    string Subject,
    string? Description,
    string? ReferredTo,
    string DepartmentId,
    string? DepartmentOrganizationLevel,
    string BankAccountName,
    string BankAccountNumber,
    IEnumerable<AcceptorResponse> Acceptors,
    IEnumerable<PettyCashReimbursementGlItemResponse> Items,
    IEnumerable<AcceptorResponse> AcceptanceConfirmers,
    AttachmentsDtoWithId[] Attachments,
    bool HasPermission,
    DateTimeOffset? DisbursementDate,
    decimal? DisbursementAmount,
    string? DisbursementDescription);

public class GetPettyCashReimbursementByIdEndpoint : EndpointBase<GetPettyCashReimbursementByIdRequest, Results<Ok<GetPettyCashReimbursementByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetPettyCashReimbursementByIdEndpoint(ILogger<GetPettyCashReimbursementByIdEndpoint> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("petty-cash-reimbursement/{id:guid}");
        this.Description(b => b
                              .WithTags("Procurement/PPettyCashReimbursement")
                              .WithName("GetPettyCashReimbursementById")
                              .Produces<Ok<GetPettyCashReimbursementByIdResponse>>()
                              .ProducesProblem(StatusCodes.Status404NotFound)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok<GetPettyCashReimbursementByIdResponse>, NotFound<string>>> HandleRequestAsync(GetPettyCashReimbursementByIdRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PPettyCashReimbursements
                               .Include(e => e.Acceptors)
                               .Include(e => e.Items)!
                               .ThenInclude(i => i.PettyCashGlAccount)
                               .ThenInclude(g => g.BudgetType)
                               .Include(e => e.Items)!
                               .ThenInclude(i => i.PettyCashGlAccount)
                               .ThenInclude(g => g.GLAccount)
                               .Include(e => e.Items)!
                               .ThenInclude(i => i.PettyCashGlAccount)
                               .ThenInclude(g => g.PettyCash)
                               .ThenInclude(pettyCash => pettyCash.Department)
                               .Include(pPettyCashReimbursement => pPettyCashReimbursement.Items)
                               .Include(auditableEntity => auditableEntity.AuditInfo)
                               .Include(pPettyCashReimbursement => pPettyCashReimbursement.Attachments)
                               .Include(pPettyCashReimbursement => pPettyCashReimbursement.Department)
                               .AsNoTracking()
                               .FirstOrDefaultAsync(e => e.Id == PPettyCashReimbursementId.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลเบิกเงินชดเชยเงินสดย่อย {req.Id}");
        }

        var acceptors = (entity.Acceptors ?? Enumerable.Empty<PPettyCashReimbursementAcceptor>())
                        .Where(a => !a.IsDeleted)
                        .OrderBy(a => a.Sequence)
                        .Select(DelegatorExtensions.DelegatorToAcceptor)
                        .Select(a => new AcceptorResponse(
                            a.Id.Value,
                            a.Type,
                            a.UserId.Value,
                            a.Sequence,
                            a.FullName,
                            a.PositionName,
                            a.BusinessUnitName,
                            a.Status,
                            a.Remark,
                            a.ActionAt,
                            null,
                            null,
                            a.IsCurrent,
                            DelegateeUserId: a.Delegatee?.SuUserId.Value));

        var accountingConfirmer = (entity.Acceptors ?? Enumerable.Empty<PPettyCashReimbursementAcceptor>())
                                  .Where(a => !a.IsDeleted && a.Type == AcceptorType.AccountingConfirmer)
                                  .OrderBy(a => a.Sequence)
                                  .Select(DelegatorExtensions.DelegatorToAcceptor)
                                  .Select(a => new AcceptorResponse(
                                      a.Id.Value,
                                      a.Type,
                                      a.UserId.Value,
                                      a.Sequence,
                                      a.FullName,
                                      a.PositionName,
                                      a.BusinessUnitName,
                                      a.Status,
                                      a.Remark,
                                      a.ActionAt,
                                      null,
                                      null,
                                      a.IsCurrent,
                                      DelegateeUserId: a.Delegatee?.SuUserId.Value));

        var items = (entity.Items ?? Enumerable.Empty<PPettyCashReimbursementItems>())
                    .OrderBy(i => i.Sequence)
                    .Select(i => new PettyCashReimbursementGlItemResponse(
                        i.Id.Value,
                        i.PettyCashGlAccount.Id.Value,
                        i.Sequence,
                        i.PettyCashGlAccount.PettyCash?.PettyCashDate ?? default,
                        i.PettyCashGlAccount.PettyCash?.PettyCashNumber.Value ?? string.Empty,
                        i.PettyCashGlAccount.PettyCash?.Subject ?? string.Empty,
                        i.PettyCashGlAccount.SoId,
                        i.PettyCashGlAccount.PettyCash?.Department.Name ?? string.Empty,
                        i.PettyCashGlAccount.BudgetTypeCode.Value,
                        i.PettyCashGlAccount.BudgetType?.Label,
                        i.PettyCashGlAccount.GLAccountCode.Value,
                        i.PettyCashGlAccount.GLAccount?.Label,
                        i.PettyCashGlAccount.ProjectNumber,
                        i.PettyCashGlAccount.Amount));

        var response = new GetPettyCashReimbursementByIdResponse(
            entity.Id.Value,
            entity.Number,
            entity.Status,
            entity.ReimbursementDate,
            entity.Subject,
            entity.Description,
            entity.ReferredTo,
            entity.DepartmentId.ToString() ?? string.Empty,
            entity.Department?.OrganizationLevel,
            entity.BankAccountName,
            entity.BankAccountNumber,
            acceptors,
            items,
            accountingConfirmer,
            [
                .. entity.Attachments
                         .GroupBy(
                             a => a.DocumentTypeCode,
                             (key, g) => new AttachmentsDtoWithId(
                                 key.Value,
                                 [
                                     .. g
                                        .OrderBy(s => s.Sequence)
                                        .Select(s => new FileAttachmentsWithId(s.Id.Value, s.FileId.Value, s.FileName, s.Sequence, s.IsPublic, s.AuditInfo.CreatedBy))
                                 ]))
            ],
            entity.AuditInfo.CreatedBy == req.UserId,
            entity.DisbursementDate,
            entity.DisbursementAmount,
            entity.DisbursementDescription);

        return TypedResults.Ok(response);
    }
}