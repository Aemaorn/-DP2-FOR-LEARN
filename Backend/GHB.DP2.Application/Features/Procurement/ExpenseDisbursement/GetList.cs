namespace GHB.DP2.Application.Features.Procurement.ExpenseDisbursement;

using System.IdentityModel.Tokens.Jwt;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Features.WorkList;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Domain.Procurement.P79Clause2;
using GHB.DP2.Domain.Procurement.PExpenseDisbursement;
using GHB.DP2.Domain.Procurement.PPettyCashReimbursement;
using GHB.DP2.Domain.Procurement.Pw119;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetExpenseDisbursementListRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    int PageNumber,
    int PageSize,
    WorkProcess? WorkProcess,
    string? Keyword,
    string? DepartmentCode,
    PExpenseDisbursementSourceType? SourceType = null,
    DateTimeOffset? AdvancePaymentDateFrom = null,
    DateTimeOffset? AdvancePaymentDateTo = null,
    Guid? SourceId = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    PExpenseDisbursementStatus? Status = null);

public record GetExpenseDisbursementListItem(
    Guid Id,
    PExpenseDisbursementStatus Status,
    PExpenseDisbursementSourceType SourceType,
    Guid SourceId,
    string SourceCode,
    string SourceName,
    string DepartmentName,
    DateTimeOffset? AdvancePaymentDate,
    DateTimeOffset Date,
    decimal? Budget);

public record GetStatusCount(
    int All,
    int Draft,
    int Edit,
    int WaitingApproval,
    int Approved,
    int Rejected);

public record GetExpenseDisbursementListResult(
    GetStatusCount StatusCount,
    PaginatedQueryResult<GetExpenseDisbursementListItem> Data
);

public class GetExpenseDisbursementListEndpoint : EndpointBase<GetExpenseDisbursementListRequest, Ok<GetExpenseDisbursementListResult>>
{
    private readonly Dp2DbContext dbContext;

    public GetExpenseDisbursementListEndpoint(ILogger<GetExpenseDisbursementListEndpoint> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("expense-disbursement");
        this.Description(b => b
                              .WithTags("Procurement/ExpenseDisbursement")
                              .WithName("GetExpenseDisbursementList")
                              .Produces<Ok<PaginatedQueryResult<GetExpenseDisbursementListItem>>>()
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Ok<GetExpenseDisbursementListResult>> HandleRequestAsync(GetExpenseDisbursementListRequest req, CancellationToken ct)
    {
        var query = await this.ApplyWorkProcessFilter(req, ct);

        var departmentSourceIds = await this.PreCalculateDepartmentSourceIdsAsync(req.DepartmentCode, ct);

        query = this.ApplyAdditionalFilters(query, req, departmentSourceIds);

        var paginatedQuery = query.WhereIfTrue(req.Status.HasValue, p => p.Status == req.Status);
        var paginated = await PaginatedList<Domain.Procurement.PExpenseDisbursement.PExpenseDisbursement>.CreateAsync(
            paginatedQuery,
            req.PageNumber,
            req.PageSize,
            ct);

        var sourceIds = CollectSourceIdsFromPage([.. paginated]);
        var lookups = await this.BuildSourceLookupsAsync(sourceIds, ct);
        var items = MapToListItems([.. paginated], lookups);

        var result = new PaginatedQueryResult<GetExpenseDisbursementListItem>(items, paginated.TotalCount);
        var statusCount = await CalculateStatusCountsAsync(query, ct);

        return TypedResults.Ok(new GetExpenseDisbursementListResult(statusCount, result));
    }

    private async Task<IQueryable<Domain.Procurement.PExpenseDisbursement.PExpenseDisbursement>> ApplyWorkProcessFilter(
        GetExpenseDisbursementListRequest req,
        CancellationToken ct)
    {
        return req.WorkProcess switch
        {
            WorkProcess.InProcess => await this.ApplyInProcessFilter(req, ct),
            WorkProcess.Related => this.ApplyRelatedFilter(req),
            WorkProcess.Completed => this.ApplyCompletedFilter(req),
            _ => this.ApplyAllFilter(),
        };
    }

    private async Task<IQueryable<Domain.Procurement.PExpenseDisbursement.PExpenseDisbursement>> ApplyInProcessFilter(
        GetExpenseDisbursementListRequest req,
        CancellationToken ct)
    {
        var userId = UserId.From(req.UserId);
        var userContext = await this.GetUserContextAsync(userId, ct);

        if (userContext is null)
        {
            return Enumerable.Empty<Domain.Procurement.PExpenseDisbursement.PExpenseDisbursement>().AsQueryable();
        }

        var userIds = userContext.EffectiveUserIds.Select(e => e.UserId);

        return this.dbContext.PExpenseDisbursements
                   .Include(x => x.GlAccounts)
                   .Include(x => x.Acceptors)
                   .ThenInclude(a => a.Delegatee)
                   .AsNoTracking()
                   .Where(p =>
                       (
                           (p.Status == PExpenseDisbursementStatus.Draft ||
                            p.Status == PExpenseDisbursementStatus.Rejected ||
                            p.Status == PExpenseDisbursementStatus.Edit) &&
                           p.AuditInfo.CreatedBy == req.UserId
                       ) ||
                       (
                           p.Status == PExpenseDisbursementStatus.WaitingApproval &&
                           p.Acceptors.Any(a =>
                               a.Sequence == p.Acceptors.Min(x => x.Sequence) &&
                               (
                                   userIds.Contains(a.UserId) ||
                                   (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))
                               ) &&
                               (a.Type == AcceptorType.Approver || a.Type == AcceptorType.DepartmentDirectorAgree))
                       ))
                   .OrderByDescending(x => x.Date)
                   .ThenByDescending(x => x.Id);
    }

    private IQueryable<Domain.Procurement.PExpenseDisbursement.PExpenseDisbursement> ApplyRelatedFilter(
        GetExpenseDisbursementListRequest req)
    {
        var userId = UserId.From(req.UserId);

        return this.dbContext.PExpenseDisbursements
                   .Include(x => x.GlAccounts)
                   .Include(x => x.Acceptors)
                   .AsNoTracking()
                   .Where(x =>
                       (
                           (x.Status != PExpenseDisbursementStatus.Draft &&
                            x.Status != PExpenseDisbursementStatus.Rejected) &&
                           x.AuditInfo.CreatedBy == req.UserId
                       ) ||
                       (
                           x.Status == PExpenseDisbursementStatus.WaitingApproval &&
                           x.Acceptors.Any(ac =>
                               ac.Sequence != x.Acceptors.Min(a => a.Sequence) &&
                               ac.UserId == userId &&
                               (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected))
                       ))
                   .OrderByDescending(x => x.Date)
                   .ThenByDescending(x => x.Id);
    }

    private IQueryable<Domain.Procurement.PExpenseDisbursement.PExpenseDisbursement> ApplyCompletedFilter(
        GetExpenseDisbursementListRequest req)
    {
        var userId = UserId.From(req.UserId);

        return this.dbContext.PExpenseDisbursements
                   .Include(x => x.GlAccounts)
                   .Include(x => x.Acceptors)
                   .AsNoTracking()
                   .Where(x =>
                       (
                           (x.Status == PExpenseDisbursementStatus.Approved ||
                            x.Status == PExpenseDisbursementStatus.WaitingForCompletion) &&
                           x.AuditInfo.CreatedBy == req.UserId
                       ) ||
                       (
                           (x.Status == PExpenseDisbursementStatus.Approved ||
                            x.Status == PExpenseDisbursementStatus.WaitingForCompletion) &&
                           x.Acceptors.Any(ac => ac.UserId == userId)
                       ))
                   .OrderByDescending(x => x.Date)
                   .ThenByDescending(x => x.Id);
    }

    private IQueryable<Domain.Procurement.PExpenseDisbursement.PExpenseDisbursement> ApplyAllFilter()
    {
        return this.dbContext.PExpenseDisbursements
                   .Include(x => x.GlAccounts)
                   .Include(x => x.Acceptors)
                   .AsNoTracking()
                   .OrderByDescending(x => x.Date)
                   .ThenByDescending(x => x.Id);
    }

    private async Task<UserContext?> GetUserContextAsync(UserId userId, CancellationToken ct)
    {
        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .ThenInclude(e => e.Positions)
                             .ThenInclude(p => p.BusinessUnit)
                             .Include(u => u.Employee)
                             .ThenInclude(e => e.View)
                             .Include(u => u.Delegatees)
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

    private async Task<DepartmentSourceIds> PreCalculateDepartmentSourceIdsAsync(string? departmentCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(departmentCode))
        {
            return new DepartmentSourceIds([], [], [], []);
        }

        var deptW119Ids = await this.dbContext.Pw119s
                                    .Where(w => (string)w.DepartmentId == departmentCode)
                                    .Select(w => w.Id)
                                    .ToHashSetAsync(ct);

        var deptClause79Ids = await this.dbContext.P79Clause2s
                                        .Where(p => (string)p.DepartmentId == departmentCode)
                                        .Select(p => p.Id)
                                        .ToHashSetAsync(ct);

        var deptGuaranteeReturnIds = await this.dbContext.CmContractGuaranteeReturns
                                               .Where(g => (string)g.CaContractDraftVendor.ContractDraft.Procurement.DepartmentId == departmentCode)
                                               .Select(g => g.Id)
                                               .ToHashSetAsync(ct);

        var deptPettyCashReimbursementIds = await this.dbContext.PPettyCashReimbursements
                                                      .Where(g => (string?)g.DepartmentId == departmentCode)
                                                      .Select(p => p.Id)
                                                      .ToHashSetAsync(ct);

        return new DepartmentSourceIds(
            deptW119Ids?.Select(x => x.Value).ToArray() ?? [],
            deptClause79Ids?.Select(x => x.Value).ToArray() ?? [],
            deptGuaranteeReturnIds?.Select(x => x.Value).ToArray() ?? [],
            deptPettyCashReimbursementIds?.Select(x => x.Value).ToArray() ?? []);
    }

    private IQueryable<Domain.Procurement.PExpenseDisbursement.PExpenseDisbursement> ApplyAdditionalFilters(
        IQueryable<Domain.Procurement.PExpenseDisbursement.PExpenseDisbursement> query,
        GetExpenseDisbursementListRequest req,
        DepartmentSourceIds departmentSourceIds)
    {
        return query
               .WhereIfTrue(req.SourceType.HasValue, e => e.SourceType == req.SourceType)
               .WhereIfTrue(req.SourceId.HasValue, e => e.SourceId == req.SourceId)
               .WhereIfTrue(req.DateFrom.HasValue, e => e.Date >= req.DateFrom)
               .WhereIfTrue(req.DateTo.HasValue, e => e.Date <= req.DateTo)
               .WhereIfTrue(req.AdvancePaymentDateFrom.HasValue, e => e.AdvancePaymentDate >= req.AdvancePaymentDateFrom)
               .WhereIfTrue(req.AdvancePaymentDateTo.HasValue, e => e.AdvancePaymentDate <= req.AdvancePaymentDateTo)
               .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), e =>
                   EF.Functions.ILike(e.Description ?? string.Empty, $"%{req.Keyword}%") ||
                   EF.Functions.ILike(e.AdvanceName ?? string.Empty, $"%{req.Keyword}%") ||
                   (
                       e.SourceType == PExpenseDisbursementSourceType.W119 &&
                       this.dbContext.Pw119s
                           .Any(w => w.Id == e.SourceId &&
                                     (
                                         EF.Functions.ILike(w.Subject ?? string.Empty, $"%{req.Keyword}%") ||
                                         EF.Functions.ILike((string)w.Pw119Number ?? string.Empty, $"%{req.Keyword}%")
                                     ))
                   ) ||
                   (
                       e.SourceType == PExpenseDisbursementSourceType.Clause79_2 &&
                       this.dbContext.P79Clause2s
                           .Any(w => w.Id == e.SourceId &&
                                     (
                                         EF.Functions.ILike(w.Subject ?? string.Empty, $"%{req.Keyword}%") ||
                                         EF.Functions.ILike((string)w.P79Clause2Number ?? string.Empty, $"%{req.Keyword}%")
                                     ))
                   ) ||
                   (
                       e.SourceType == PExpenseDisbursementSourceType.ContractGuaranteeReturn &&
                       this.dbContext.CmContractGuaranteeReturns
                           .Any(w => w.Id == e.SourceId &&
                                     (
                                         EF.Functions.ILike(w.CaContractDraftVendor.ContractName ?? string.Empty, $"%{req.Keyword}%") ||
                                         EF.Functions.ILike(w.CaContractDraftVendor.ContractNumber ?? string.Empty, $"%{req.Keyword}%") ||
                                         EF.Functions.ILike(w.CaContractDraftVendor.ContractDraft.Procurement.Department.Name ?? string.Empty, $"%{req.Keyword}%")
                                     ))
                   ) ||
                   (
                       e.SourceType == PExpenseDisbursementSourceType.PettyCashReimbursement &&
                       this.dbContext.PPettyCashReimbursements
                           .Any(w => w.Id == e.SourceId &&
                                     (
                                         EF.Functions.ILike(w.Number ?? string.Empty, $"%{req.Keyword}%") ||
                                         EF.Functions.ILike(w.Subject ?? string.Empty, $"%{req.Keyword}%")
                                     ))
                   ))
               .WhereIfTrue(!string.IsNullOrWhiteSpace(req.DepartmentCode), e =>
                   (e.SourceType == PExpenseDisbursementSourceType.W119 && departmentSourceIds.W119Ids.Contains(e.SourceId)) ||
                   (e.SourceType == PExpenseDisbursementSourceType.Clause79_2 && departmentSourceIds.Clause79Ids.Contains(e.SourceId)) ||
                   (e.SourceType == PExpenseDisbursementSourceType.ContractGuaranteeReturn && departmentSourceIds.GuaranteeReturnIds.Contains(e.SourceId)) ||
                   (e.SourceType == PExpenseDisbursementSourceType.PettyCashReimbursement && departmentSourceIds.PettyCashReimbursementIds.Contains(e.SourceId)));
    }

    private IQueryable<Domain.Procurement.PExpenseDisbursement.PExpenseDisbursement> BuildBaseQuery(
        GetExpenseDisbursementListRequest req,
        DepartmentSourceIds departmentSourceIds)
    {
        var query = this.dbContext.PExpenseDisbursements
                        .Include(e => e.GlAccounts)
                        .Include(e => e.Acceptors)
                        .AsNoTracking()
                        .WhereIfTrue(req.SourceType.HasValue, e => e.SourceType == req.SourceType)
                        .WhereIfTrue(req.SourceId.HasValue, e => e.SourceId == req.SourceId)
                        .WhereIfTrue(req.DateFrom.HasValue, e => e.Date >= req.DateFrom)
                        .WhereIfTrue(req.DateTo.HasValue, e => e.Date <= req.DateTo)
                        .WhereIfTrue(req.AdvancePaymentDateFrom.HasValue, e => e.AdvancePaymentDate >= req.AdvancePaymentDateFrom)
                        .WhereIfTrue(req.AdvancePaymentDateTo.HasValue, e => e.AdvancePaymentDate <= req.AdvancePaymentDateTo)
                        .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), e =>
                            EF.Functions.ILike(e.Description ?? string.Empty, $"%{req.Keyword}%") ||
                            EF.Functions.ILike(e.AdvanceName ?? string.Empty, $"%{req.Keyword}%") ||
                            (
                                e.SourceType == PExpenseDisbursementSourceType.W119 &&
                                this.dbContext.Pw119s
                                    .Any(w => w.Id == e.SourceId &&
                                              (
                                                  EF.Functions.ILike(w.Subject ?? string.Empty, $"%{req.Keyword}%") ||
                                                  EF.Functions.ILike((string)w.Pw119Number ?? string.Empty, $"%{req.Keyword}%")
                                              ))
                            ) ||
                            (
                                e.SourceType == PExpenseDisbursementSourceType.Clause79_2 &&
                                this.dbContext.P79Clause2s
                                    .Any(w => w.Id == e.SourceId &&
                                              (
                                                  EF.Functions.ILike(w.Subject ?? string.Empty, $"%{req.Keyword}%") ||
                                                  EF.Functions.ILike((string)w.P79Clause2Number ?? string.Empty, $"%{req.Keyword}%")
                                              ))
                            ) ||
                            (
                                e.SourceType == PExpenseDisbursementSourceType.ContractGuaranteeReturn &&
                                this.dbContext.CmContractGuaranteeReturns
                                    .Any(w => w.Id == e.SourceId &&
                                              (
                                                  EF.Functions.ILike(w.CaContractDraftVendor.ContractName ?? string.Empty, $"%{req.Keyword}%") ||
                                                  EF.Functions.ILike(w.CaContractDraftVendor.ContractNumber ?? string.Empty, $"%{req.Keyword}%") ||
                                                  EF.Functions.ILike(w.CaContractDraftVendor.ContractDraft.Procurement.Department.Name ?? string.Empty, $"%{req.Keyword}%")
                                              ))
                            ) ||
                            (
                                e.SourceType == PExpenseDisbursementSourceType.PettyCashReimbursement &&
                                this.dbContext.PPettyCashReimbursements
                                    .Any(w => w.Id == e.SourceId &&
                                              (
                                                  EF.Functions.ILike(w.Number ?? string.Empty, $"%{req.Keyword}%") ||
                                                  EF.Functions.ILike(w.Subject ?? string.Empty, $"%{req.Keyword}%")
                                              ))
                            ))
                        .WhereIfTrue(!string.IsNullOrWhiteSpace(req.DepartmentCode), e =>
                            (e.SourceType == PExpenseDisbursementSourceType.W119 && departmentSourceIds.W119Ids.Contains(e.SourceId)) ||
                            (e.SourceType == PExpenseDisbursementSourceType.Clause79_2 && departmentSourceIds.Clause79Ids.Contains(e.SourceId)) ||
                            (e.SourceType == PExpenseDisbursementSourceType.ContractGuaranteeReturn && departmentSourceIds.GuaranteeReturnIds.Contains(e.SourceId)) ||
                            (e.SourceType == PExpenseDisbursementSourceType.PettyCashReimbursement && departmentSourceIds.PettyCashReimbursementIds.Contains(e.SourceId)));

        return query.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id);
    }

    private static PageSourceIds CollectSourceIdsFromPage(List<Domain.Procurement.PExpenseDisbursement.PExpenseDisbursement> items)
    {
        var w119Ids = new HashSet<Guid>();
        var clause79Ids = new HashSet<Guid>();
        var disbursementIds = new HashSet<Guid>();
        var guaranteeReturnIds = new HashSet<Guid>();
        var pettycashRegisterIds = new HashSet<Guid>();

        foreach (var e in items)
        {
            switch (e.SourceType)
            {
                case PExpenseDisbursementSourceType.W119:
                    w119Ids.Add(e.SourceId);

                    break;

                case PExpenseDisbursementSourceType.Clause79_2:
                    clause79Ids.Add(e.SourceId);

                    break;

                case PExpenseDisbursementSourceType.ContractGuaranteeReturn:
                    guaranteeReturnIds.Add(e.SourceId);

                    break;

                case PExpenseDisbursementSourceType.PettyCashReimbursement:
                    pettycashRegisterIds.Add(e.SourceId);

                    break;
            }
        }

        return new PageSourceIds(w119Ids, clause79Ids, disbursementIds, guaranteeReturnIds, pettycashRegisterIds);
    }

    private async Task<SourceLookups> BuildSourceLookupsAsync(PageSourceIds sourceIds, CancellationToken ct)
    {
        var w119Lookup = await this.BuildW119LookupAsync(sourceIds.W119Ids, ct);
        var clause79Lookup = await this.BuildClause79LookupAsync(sourceIds.Clause79Ids, ct);
        var guaranteeReturnLookup = await this.BuildGuaranteeReturnLookupAsync(sourceIds.GuaranteeReturnIds, ct);
        var pettyCashReimbursementLookup = await this.BuildPettyCashReimbursementLookupAsync(sourceIds.PettyCashReimbursementIds, ct);

        return new SourceLookups(
            w119Lookup,
            clause79Lookup,
            guaranteeReturnLookup,
            pettyCashReimbursementLookup);
    }

    private async Task<Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)>> BuildW119LookupAsync(HashSet<Guid> w119Ids, CancellationToken ct)
    {
        if (w119Ids.Count == 0)
        {
            return new Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)>();
        }

        var w119VoArray = w119Ids.Select(g => Pw119Id.From(g)).ToArray();

        return await this.dbContext.Pw119s
                         .AsNoTracking()
                         .Where(w => System.Linq.Enumerable.Contains<Pw119Id>(w119VoArray, w.Id))
                         .Select(w => new
                         {
                             w.Id,
                             Code = (string)w.Pw119Number,
                             Name = w.Subject,
                             Dept = w.Department.Name,
                             Amount = w.Budget,
                         })
                         .ToDictionaryAsync(k => k.Id.Value, v => (v.Code, v.Name, v.Dept, v.Amount), ct);
    }

    private async Task<Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)>> BuildClause79LookupAsync(HashSet<Guid> clause79Ids, CancellationToken ct)
    {
        if (clause79Ids.Count == 0)
        {
            return new Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)>();
        }

        var clause79VoArray = clause79Ids.Select(g => P79Clause2Id.From(g)).ToArray();

        return await this.dbContext.P79Clause2s
                         .AsNoTracking()
                         .Where(p => System.Linq.Enumerable.Contains<P79Clause2Id>(clause79VoArray, p.Id))
                         .Select(p => new
                         {
                             p.Id,
                             Code = (string)p.P79Clause2Number,
                             Name = p.Subject,
                             Dept = p.Department.Name,
                             Amount = p.Budget,
                         })
                         .ToDictionaryAsync(k => k.Id.Value, v => (v.Code, v.Name, v.Dept, v.Amount), ct);
    }

    private async Task<Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)>> BuildGuaranteeReturnLookupAsync(HashSet<Guid> guaranteeReturnIds, CancellationToken ct)
    {
        if (guaranteeReturnIds.Count == 0)
        {
            return new Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)>();
        }

        var guaranteeReturnVoArray = guaranteeReturnIds.Select(g => CmContractGuaranteeReturnId.From(g)).ToArray();

        return await this.dbContext.CmContractGuaranteeReturns
                         .AsNoTracking()
                         .Where(g => System.Linq.Enumerable.Contains<CmContractGuaranteeReturnId>(guaranteeReturnVoArray, g.Id))
                         .Select(g => new
                         {
                             g.Id,
                             Code = g.CaContractDraftVendor.ContractNumber,
                             Name = g.CaContractDraftVendor.ContractName,
                             Dept = g.CaContractDraftVendor.ContractDraft.Procurement.Department.Name,
                             Amount = g.DeductedAmount ?? 0M,
                         })
                         .ToDictionaryAsync(k => k.Id.Value, v => (v.Code ?? string.Empty, v.Name, v.Dept, v.Amount), ct);
    }

    private async Task<Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)>> BuildPettyCashReimbursementLookupAsync(HashSet<Guid> pettyCashReimbursementIds, CancellationToken ct)
    {
        if (pettyCashReimbursementIds.Count == 0)
        {
            return new Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)>();
        }

        var pettycashReimbursementArray = pettyCashReimbursementIds.Select(p => PPettyCashReimbursementId.From(p)).ToArray();

        return await this.dbContext.PPettyCashReimbursements
                         .AsNoTracking()
                         .Where(g => System.Linq.Enumerable.Contains<PPettyCashReimbursementId>(pettycashReimbursementArray, g.Id))
                         .Select(p => new
                         {
                             p.Id,
                             Code = p.Number,
                             Name = p.Subject,
                             Dept = p.Department.Name,
                             Amount = 0M,
                         })
                         .ToDictionaryAsync(p => p.Id.Value, v => (v.Code, v.Name, v.Dept, v.Amount), ct);
    }

    private static List<GetExpenseDisbursementListItem> MapToListItems(
        List<Domain.Procurement.PExpenseDisbursement.PExpenseDisbursement> items,
        SourceLookups lookups)
    {
        return
        [
            .. items.Select(e =>
            {
                var data = GetSourceDataForEntity(e, lookups);

                return new GetExpenseDisbursementListItem(
                    e.Id.Value,
                    e.Status,
                    e.SourceType,
                    e.SourceId,
                    data.Code,
                    data.Name,
                    data.Dept,
                    e.AdvancePaymentDate,
                    e.Date,
                    (e.GlAccounts?.Any() == true ? e.GlAccounts.Sum(g => g.Amount) : data.Amount) ?? 0M);
            })
        ];
    }

    private static (string Code, string Name, string Dept, decimal? Amount) GetSourceDataForEntity(
        Domain.Procurement.PExpenseDisbursement.PExpenseDisbursement entity,
        SourceLookups lookups)
    {
        return entity.SourceType switch
        {
            PExpenseDisbursementSourceType.W119 =>
                lookups.W119Lookup.TryGetValue(entity.SourceId, out var w119Data)
                    ? w119Data
                    : (string.Empty, string.Empty, string.Empty, null),

            PExpenseDisbursementSourceType.Clause79_2 =>
                lookups.Clause79Lookup.TryGetValue(entity.SourceId, out var clause79Data)
                    ? clause79Data
                    : (string.Empty, string.Empty, string.Empty, null),

            PExpenseDisbursementSourceType.ContractGuaranteeReturn =>
                lookups.GuaranteeReturnLookup.TryGetValue(entity.SourceId, out var guaranteeData)
                    ? guaranteeData
                    : (string.Empty, string.Empty, string.Empty, null),

            PExpenseDisbursementSourceType.PettyCashReimbursement =>
                lookups.PettyCashReimbursementLookup.TryGetValue(entity.SourceId, out var pettyCashData)
                    ? pettyCashData
                    : (string.Empty, string.Empty, string.Empty, null),

            _ => (string.Empty, string.Empty, string.Empty, null),
        };
    }

    private static async Task<GetStatusCount> CalculateStatusCountsAsync(
        IQueryable<Domain.Procurement.PExpenseDisbursement.PExpenseDisbursement> query,
        CancellationToken ct)
    {
        var countItem = await query.ToListAsync(ct);

        return new GetStatusCount(
            countItem.Count,
            countItem.Count(s => s.Status == PExpenseDisbursementStatus.Draft),
            countItem.Count(s => s.Status == PExpenseDisbursementStatus.Edit),
            countItem.Count(s => s.Status == PExpenseDisbursementStatus.WaitingApproval),
            countItem.Count(s => s.Status == PExpenseDisbursementStatus.Approved),
            countItem.Count(s => s.Status == PExpenseDisbursementStatus.Rejected));
    }

    private record DepartmentSourceIds(
        Guid[] W119Ids,
        Guid[] Clause79Ids,
        Guid[] GuaranteeReturnIds,
        Guid[] PettyCashReimbursementIds);

    private record PageSourceIds(
        HashSet<Guid> W119Ids,
        HashSet<Guid> Clause79Ids,
        HashSet<Guid> DisbursementIds,
        HashSet<Guid> GuaranteeReturnIds,
        HashSet<Guid> PettyCashReimbursementIds);

    private record SourceLookups(
        Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)> W119Lookup,
        Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)> Clause79Lookup,
        Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)> GuaranteeReturnLookup,
        Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)> PettyCashReimbursementLookup);
}