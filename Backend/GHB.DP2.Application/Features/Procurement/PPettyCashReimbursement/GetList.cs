namespace GHB.DP2.Application.Features.Procurement.PPettyCashReimbursement;

using System.IdentityModel.Tokens.Jwt;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Features.WorkList;
using GHB.DP2.Application.Features.WorkList.Programs;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPettyCashReimbursement;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetPettyCashReimbursementListRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    int PageNumber = 1,
    int PageSize = 10,
    WorkProcess? WorkProcess = null,
    string? Keyword = null,
    string? DepartmentCode = null,
    PPettyCashReimbursementStatus? Status = null)
    : IProcurementBaseQuery
{
    public int? BudgetYear { get; }

    public string? SupplyMethodCode { get; }

    public string? SupplyMethodTypeCode { get; }

    public string? SupplyMethodSpecialTypeCode { get; }

    public bool? IsMd { get; }

    public bool? IsPendingDepartment => false;
}

public record GetPettyCashReimbursementListItem(
    Guid Id,
    string Number,
    PPettyCashReimbursementStatus Status,
    DateTimeOffset ReimbursementDate,
    string Subject,
    decimal TotalAmount,
    string? DepartmentName,
    DateTimeOffset CreatedDate);

public record GetStatusCount(
    int All,
    int Draft,
    int Edit,
    int WaitingApproval,
    int Approved,
    int Rejected,
    int Cancelled,
    int WaitingAccountingApproval,
    int WaitingDisbursementDate,
    int Paid);

public record GetPettyCashReimbursementListResponse(
    PaginatedQueryResult<GetPettyCashReimbursementListItem> Data,
    GetStatusCount StatusCount);

public class GetPettyCashReimbursementListEndpoint : EndpointBase<GetPettyCashReimbursementListRequest, Ok<GetPettyCashReimbursementListResponse>>
{
    private readonly Dp2DbContext dbContext;
    private readonly ProcurementProgram procurementProgram;

    public GetPettyCashReimbursementListEndpoint(ILogger<GetPettyCashReimbursementListEndpoint> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.procurementProgram = new ProcurementProgram();
    }

    public override void Configure()
    {
        this.Get("petty-cash-reimbursement");
        this.Description(b => b
                              .WithTags("Procurement/PPettyCashReimbursement")
                              .WithName("GetPettyCashReimbursementList")
                              .Produces<Ok<PaginatedQueryResult<GetPettyCashReimbursementListItem>>>()
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Ok<GetPettyCashReimbursementListResponse>> HandleRequestAsync(GetPettyCashReimbursementListRequest req, CancellationToken ct)
    {
        var query = req.WorkProcess switch
        {
            WorkProcess.InProcess => await this.ApplyInProcessQueryAsync(req, ct),
            WorkProcess.Related => this.ApplyRelatedQuery(req),
            WorkProcess.Completed => this.ApplyCompletedQuery(req),
            _ => this.PettyCashReimbursementQueryBase(req),
        };

        query = query
                .OrderByDescending(x => x.AuditInfo.LastModifiedAt ?? x.AuditInfo.CreatedAt)
                .ThenByDescending(x => x.ReimbursementDate)
                .ThenByDescending(x => x.Id)
                .AsNoTracking();

        var paginated = await PaginatedList<PPettyCashReimbursement>.CreateAsync(
            query,
            req.PageNumber,
            req.PageSize,
            ct);

        var items = paginated.Select(e => new GetPettyCashReimbursementListItem(
            e.Id.Value,
            e.Number,
            e.Status,
            e.ReimbursementDate,
            e.Subject,
            e.Items?.Sum(i => i.PettyCashGlAccount?.Amount) ?? 0m,
            e.Department.Name,
            e.AuditInfo?.CreatedAt ?? e.ReimbursementDate)).ToList();

        var baseData = await query.ToListAsync(ct);

        var statusCount = new GetStatusCount(
            baseData.Count,
            baseData.Count(s => s.Status == PPettyCashReimbursementStatus.Draft),
            baseData.Count(s => s.Status == PPettyCashReimbursementStatus.Edit),
            baseData.Count(s => s.Status == PPettyCashReimbursementStatus.WaitingApproval),
            baseData.Count(s => s.Status == PPettyCashReimbursementStatus.Approved),
            baseData.Count(s => s.Status == PPettyCashReimbursementStatus.Rejected),
            baseData.Count(s => s.Status == PPettyCashReimbursementStatus.Cancelled),
            baseData.Count(s => s.Status == PPettyCashReimbursementStatus.WaitingAccountingApproval),
            baseData.Count(s => s.Status == PPettyCashReimbursementStatus.WaitingDisbursementDate),
            baseData.Count(s => s.Status == PPettyCashReimbursementStatus.Paid));

        var result = new PaginatedQueryResult<GetPettyCashReimbursementListItem>(items, paginated.TotalCount);

        return TypedResults.Ok(new GetPettyCashReimbursementListResponse(result, statusCount));
    }

    private IQueryable<PPettyCashReimbursement> PettyCashReimbursementQueryBase(GetPettyCashReimbursementListRequest req)
        => this.dbContext.PPettyCashReimbursements
               .Include(d => d.Department)
               .Include(p => p.Items)!
               .ThenInclude(i => i.PettyCashGlAccount)
               .ThenInclude(g => g.PettyCash)
               .ThenInclude(w => w.Department)
               .Include(p => p.Acceptors)
               .WhereIfTrue(req.Status.HasValue, x => x.Status == req.Status)
               .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), x =>
                   EF.Functions.ILike(x.Number ?? string.Empty, $"%{req.Keyword}%") ||
                   EF.Functions.ILike(x.Subject ?? string.Empty, $"%{req.Keyword}%"))
               .WhereIfTrue(!string.IsNullOrWhiteSpace(req.DepartmentCode), x =>
                   x.DepartmentId.HasValue && x.DepartmentId == BusinessUnitId.From(req.DepartmentCode!));

    private async Task<IQueryable<PPettyCashReimbursement>> ApplyInProcessQueryAsync(
        GetPettyCashReimbursementListRequest req,
        CancellationToken ct)
    {
        var userId = UserId.From(req.UserId);
        var userContext = await this.GetUserContextAsync(userId, ct);

        if (userContext is null)
        {
            return null!;
        }

        var query =
            this.procurementProgram.BuildPPettyCashReimbursementBaseQuery(
                this.dbContext,
                req,
                userContext);

        return query;
    }

    private async Task<UserContext?> GetUserContextAsync(UserId userId, CancellationToken ct)
    {
        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .ThenInclude(e => e.Positions)
                             .ThenInclude(p => p.BusinessUnit)
                             .Include(u => u.Employee)
                             .ThenInclude(e => e.View)
                             .Include(e => e.Delegatees)
                             .ThenInclude(e => e.SuDelegator)
                             .ThenInclude(suDelegator => suDelegator.SuUser)
                             .ThenInclude(suUser => suUser.Employee)
                             .ThenInclude(rawEmployee => rawEmployee.View)
                             .FirstOrDefaultAsync(u => u.Id == userId, ct);

        var viewUser = user?.Employee?.View;

        if (viewUser is null)
        {
            return null;
        }

        var effectiveUserIds = new List<EffectiveUserId>();

        if (user?.EffectiveDelegator is { SuUserId: var uid })
        {
            effectiveUserIds.Add(new EffectiveUserId(uid, true));
        }

        effectiveUserIds.Add(new EffectiveUserId(user!.Id, false));

        return new UserContext(
            user!,
            viewUser,
            user!.Employee?.PrimaryBusinessUnit?.Id,
            user.Employee?.IsJorPor ?? false,
            user.Employee?.PrimaryOrganizationLevel,
            effectiveUserIds);
    }

    private IQueryable<PPettyCashReimbursement> ApplyRelatedQuery(GetPettyCashReimbursementListRequest req)
    {
        var userId = UserId.From(req.UserId);
        var query =
            this.PettyCashReimbursementQueryBase(req);

        return query.Where(e =>
            e.Acceptors.Any(a =>
                a.UserId == userId &&
                a.IsActive &&
                !a.IsCurrent &&
                (a.Status == AcceptorStatus.Approved ||
                 a.Status == AcceptorStatus.Rejected)));
    }

    private IQueryable<PPettyCashReimbursement> ApplyCompletedQuery(GetPettyCashReimbursementListRequest req)
    {
        var userId = UserId.From(req.UserId);
        var query = this.PettyCashReimbursementQueryBase(req);

        var finalStatuses = new[]
        {
            PPettyCashReimbursementStatus.Paid,
            PPettyCashReimbursementStatus.Cancelled,
        };

        return query.Where(e =>
            finalStatuses.Contains(e.Status) &&
            (e.AuditInfo.CreatedBy == userId.Value ||
             e.Acceptors.Any(a => a.UserId == userId)));
    }
}