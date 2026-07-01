namespace GHB.DP2.Application.Features.WorkList.Programs;

using GHB.DP2.Application.Common;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Domain.Procurement.P79Clause2;
using GHB.DP2.Domain.Procurement.PExpenseDisbursement;
using GHB.DP2.Domain.Procurement.PPettyCashReimbursement;
using GHB.DP2.Domain.Procurement.Pw119;
using GHB.DP2.Infrastructure;
using Microsoft.EntityFrameworkCore;

internal sealed class ExpenseDisbursementProgram
{
    public async Task<(int Count, SectionResult<ExpenseDisbursementItem>? Section)> ProcessExpenseDisbursementSectionAsync(
        Dp2DbContext dbContext,
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        CancellationToken ct)
    {
        var departmentSourceIds = await this.PreCalculateDepartmentSourceIdsAsync(dbContext, req.DepartmentCode, ct);

        var baseQuery = this.BuildExpenseDisbursementBaseQuery(dbContext, req, userContext, departmentSourceIds);

        var count = await baseQuery.CountAsync(ct);

        SectionResult<ExpenseDisbursementItem>? section = null;

        if (req.IncludeExpenseDisbursement)
        {
            var entities = await baseQuery
                                 .Include(x => x.GlAccounts)
                                 .Skip((req.PageNumber - 1) * req.PageSize)
                                 .Take(req.PageSize)
                                 .ToListAsync(ct);

            var totalCount = count;

            // Collect source IDs from the entities
            var w119Ids = new HashSet<Guid>();
            var clause79Ids = new HashSet<Guid>();
            var guaranteeReturnIds = new HashSet<Guid>();
            var pettyCashReimbursementIds = new HashSet<Guid>();

            foreach (var e in entities)
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
                        pettyCashReimbursementIds.Add(e.SourceId);

                        break;
                }
            }

            var sourceLookups = await this.BuildSourceLookupsAsync(
                dbContext, w119Ids, clause79Ids, guaranteeReturnIds, pettyCashReimbursementIds, ct);

            var items = entities.Select(e =>
            {
                var (code, name, _, sourceAmount) = GetSourceDataForEntity(e, sourceLookups);
                var budget = e.GlAccounts?.Sum(g => g.Amount) ?? sourceAmount;

                return new ExpenseDisbursementItem(
                    e.Id.Value,
                    code,
                    name,
                    e.Status,
                    e.SourceType,
                    e.SourceId,
                    e.AdvancePaymentDate,
                    e.Date,
                    budget);
            }).ToList();

            var pageDto = new PaginatedQueryResult<ExpenseDisbursementItem>(items, totalCount);
            section = new SectionResult<ExpenseDisbursementItem>(pageDto);
        }

        return (count, section);
    }

    public IQueryable<PExpenseDisbursement> BuildExpenseDisbursementBaseQuery(
        Dp2DbContext dbContext,
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        DepartmentSourceIds departmentSourceIds)
    {
        var userIds = userContext.EffectiveUserIds.Select(e => e.UserId);

        return dbContext.PExpenseDisbursements
                        .Include(x => x.GlAccounts)
                        .Include(x => x.Acceptors)
                        .AsNoTracking()
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.Keyword),
                            e =>
                                EF.Functions.ILike(e.Description ?? string.Empty, $"%{req.Keyword}%") ||
                                EF.Functions.ILike(e.AdvanceName ?? string.Empty, $"%{req.Keyword}%") ||
                                (
                                    e.SourceType == PExpenseDisbursementSourceType.W119 &&
                                    dbContext.Pw119s
                                             .Any(w => w.Id == e.SourceId &&
                                                       (
                                                           EF.Functions.ILike(w.Subject ?? string.Empty, $"%{req.Keyword}%") ||
                                                           EF.Functions.ILike((string)w.Pw119Number ?? string.Empty, $"%{req.Keyword}%")
                                                       ))
                                ) ||
                                (
                                    e.SourceType == PExpenseDisbursementSourceType.Clause79_2 &&
                                    dbContext.P79Clause2s
                                             .Any(w => w.Id == e.SourceId &&
                                                       (
                                                           EF.Functions.ILike(w.Subject ?? string.Empty, $"%{req.Keyword}%") ||
                                                           EF.Functions.ILike((string)w.P79Clause2Number ?? string.Empty, $"%{req.Keyword}%")
                                                       ))
                                ) ||
                                (
                                    e.SourceType == PExpenseDisbursementSourceType.ContractGuaranteeReturn &&
                                    dbContext.CmContractGuaranteeReturns
                                             .Any(w => w.Id == e.SourceId &&
                                                       (
                                                           EF.Functions.ILike(w.CaContractDraftVendor.ContractName ?? string.Empty, $"%{req.Keyword}%") ||
                                                           EF.Functions.ILike(w.CaContractDraftVendor.ContractNumber ?? string.Empty, $"%{req.Keyword}%") ||
                                                           EF.Functions.ILike(
                                                               w.CaContractDraftVendor.ContractDraft.Procurement.Department.Name ?? string.Empty,
                                                               $"%{req.Keyword}%")
                                                       ))
                                ) ||
                                (
                                    e.SourceType == PExpenseDisbursementSourceType.PettyCashReimbursement &&
                                    dbContext.PPettyCashReimbursements
                                             .Any(w => w.Id == e.SourceId &&
                                                       (
                                                           EF.Functions.ILike(w.Number ?? string.Empty, $"%{req.Keyword}%") ||
                                                           EF.Functions.ILike(w.Subject ?? string.Empty, $"%{req.Keyword}%")
                                                       ))
                                ))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.DepartmentCode),
                            e =>
                                (e.SourceType == PExpenseDisbursementSourceType.W119 && departmentSourceIds.W119Ids.Contains(e.SourceId)) ||
                                (e.SourceType == PExpenseDisbursementSourceType.Clause79_2 && departmentSourceIds.Clause79Ids.Contains(e.SourceId)) ||
                                (e.SourceType == PExpenseDisbursementSourceType.ContractGuaranteeReturn && departmentSourceIds.GuaranteeReturnIds.Contains(e.SourceId)) ||
                                (e.SourceType == PExpenseDisbursementSourceType.PettyCashReimbursement && departmentSourceIds.PettyCashReimbursementIds.Contains(e.SourceId)))
                        .Where(p =>
                            p.Status == PExpenseDisbursementStatus.WaitingApproval &&
                            p.Acceptors.Any(a =>
                                a.Sequence == p.Acceptors.Min(x => x.Sequence) &&
                                (
                                    userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))
                                ) &&
                                (a.Type == AcceptorType.Approver || a.Type == AcceptorType.DepartmentDirectorAgree)))
                        .AsSplitQuery()
                        .OrderByDescending(x => x.AuditInfo.CreatedAt);
    }

    public static (string Code, string Name, string Dept, decimal Amount) GetSourceDataForEntity(
        PExpenseDisbursement entity,
        SourceLookups lookups)
    {
        return entity.SourceType switch
        {
            PExpenseDisbursementSourceType.W119 => lookups.W119Lookup.TryGetValue(entity.SourceId, out var w119)
                ? w119
                : (string.Empty, string.Empty, string.Empty, 0m),
            PExpenseDisbursementSourceType.Clause79_2 => lookups.Clause79Lookup.TryGetValue(entity.SourceId, out var clause79)
                ? clause79
                : (string.Empty, string.Empty, string.Empty, 0m),
            PExpenseDisbursementSourceType.ContractGuaranteeReturn => lookups.GuaranteeReturnLookup.TryGetValue(entity.SourceId, out var guaranteeReturn)
                ? guaranteeReturn
                : (string.Empty, string.Empty, string.Empty, 0m),
            PExpenseDisbursementSourceType.PettyCashReimbursement => lookups.PettyCashReimbursementLookup.TryGetValue(entity.SourceId, out var pettyCashReimbursement)
                ? pettyCashReimbursement
                : (string.Empty, string.Empty, string.Empty, 0m),
            _ => (string.Empty, string.Empty, string.Empty, 0m),
        };
    }

    public async Task<DepartmentSourceIds> PreCalculateDepartmentSourceIdsAsync(Dp2DbContext dbContext, string? departmentCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(departmentCode))
        {
            return new DepartmentSourceIds([], [], [], []);
        }

        var deptW119Ids = await dbContext.Pw119s
                                         .Where(w => (string)w.DepartmentId == departmentCode)
                                         .Select(w => w.Id)
                                         .ToHashSetAsync(ct);

        var deptClause79Ids = await dbContext.P79Clause2s
                                             .Where(p => (string)p.DepartmentId == departmentCode)
                                             .Select(p => p.Id)
                                             .ToHashSetAsync(ct);

        var deptGuaranteeReturnIds = await dbContext.CmContractGuaranteeReturns
                                                    .Where(g => (string)g.CaContractDraftVendor.ContractDraft.Procurement.DepartmentId == departmentCode)
                                                    .Select(g => g.Id)
                                                    .ToHashSetAsync(ct);

        var deptPettyCashReimbursementIds = await dbContext.PPettyCashReimbursements
                                                           .Where(g => (string?)g.DepartmentId == departmentCode)
                                                           .Select(p => p.Id)
                                                           .ToHashSetAsync(ct);

        return new DepartmentSourceIds(
            deptW119Ids?.Select(x => x.Value).ToArray() ?? [],
            deptClause79Ids?.Select(x => x.Value).ToArray() ?? [],
            deptGuaranteeReturnIds?.Select(x => x.Value).ToArray() ?? [],
            deptPettyCashReimbursementIds?.Select(x => x.Value).ToArray() ?? []);
    }

    // Parallel overload — ใช้จาก CombinedWorklistBuilder ที่มี factory
    public async Task<DepartmentSourceIds> PreCalculateDepartmentSourceIdsAsync(
        IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory,
        string? departmentCode,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(departmentCode))
        {
            return new DepartmentSourceIds([], [], [], []);
        }

        await using var ctx1 = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctx2 = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctx3 = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctx4 = await dbContextFactory.CreateDbContextAsync(ct);

        var w119Task = ctx1.Pw119s
                           .Where(w => (string)w.DepartmentId == departmentCode)
                           .Select(w => w.Id)
                           .ToHashSetAsync(ct);

        var clause79Task = ctx2.P79Clause2s
                               .Where(p => (string)p.DepartmentId == departmentCode)
                               .Select(p => p.Id)
                               .ToHashSetAsync(ct);

        var guaranteeTask = ctx3.CmContractGuaranteeReturns
                                .Where(g => (string)g.CaContractDraftVendor.ContractDraft.Procurement.DepartmentId == departmentCode)
                                .Select(g => g.Id)
                                .ToHashSetAsync(ct);

        var pettyCashTask = ctx4.PPettyCashReimbursements
                                .Where(g => (string?)g.DepartmentId == departmentCode)
                                .Select(p => p.Id)
                                .ToHashSetAsync(ct);

        await Task.WhenAll(w119Task, clause79Task, guaranteeTask, pettyCashTask);

        return new DepartmentSourceIds(
            (await w119Task)?.Select(x => x.Value).ToArray() ?? [],
            (await clause79Task)?.Select(x => x.Value).ToArray() ?? [],
            (await guaranteeTask)?.Select(x => x.Value).ToArray() ?? [],
            (await pettyCashTask)?.Select(x => x.Value).ToArray() ?? []);
    }

    public async Task<SourceLookups> BuildSourceLookupsAsync(
        Dp2DbContext dbContext,
        HashSet<Guid> w119Ids,
        HashSet<Guid> clause79Ids,
        HashSet<Guid> guaranteeReturnIds,
        HashSet<Guid> pettyCashReimbursementIds,
        CancellationToken ct)
    {
        var w119Lookup = await this.BuildW119LookupAsync(dbContext, w119Ids, ct);
        var clause79Lookup = await this.BuildClause79LookupAsync(dbContext, clause79Ids, ct);
        var guaranteeReturnLookup = await this.BuildGuaranteeReturnLookupAsync(dbContext, guaranteeReturnIds, ct);
        var pettyCashReimbursementLookup = await this.BuildPettyCashReimbursementLookupAsync(dbContext, pettyCashReimbursementIds, ct);

        return new SourceLookups(
            w119Lookup,
            clause79Lookup,
            guaranteeReturnLookup,
            pettyCashReimbursementLookup);
    }

    // Parallel overload — ใช้จาก CombinedWorklistBuilder ที่มี factory
    public async Task<SourceLookups> BuildSourceLookupsAsync(
        IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory,
        HashSet<Guid> w119Ids,
        HashSet<Guid> clause79Ids,
        HashSet<Guid> guaranteeReturnIds,
        HashSet<Guid> pettyCashReimbursementIds,
        CancellationToken ct)
    {
        await using var ctx1 = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctx2 = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctx3 = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctx4 = await dbContextFactory.CreateDbContextAsync(ct);

        var w119Task = this.BuildW119LookupAsync(ctx1, w119Ids, ct);
        var clause79Task = this.BuildClause79LookupAsync(ctx2, clause79Ids, ct);
        var guaranteeReturnTask = this.BuildGuaranteeReturnLookupAsync(ctx3, guaranteeReturnIds, ct);
        var pettyCashTask = this.BuildPettyCashReimbursementLookupAsync(ctx4, pettyCashReimbursementIds, ct);

        await Task.WhenAll(w119Task, clause79Task, guaranteeReturnTask, pettyCashTask);

        return new SourceLookups(
            await w119Task,
            await clause79Task,
            await guaranteeReturnTask,
            await pettyCashTask);
    }

    private async Task<Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)>> BuildW119LookupAsync(Dp2DbContext dbContext, HashSet<Guid> w119Ids, CancellationToken ct)
    {
        if (w119Ids.Count == 0)
        {
            return new Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)>();
        }

        var w119VoArray = w119Ids.Select(g => Pw119Id.From(g)).ToArray();

        return await dbContext.Pw119s
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

    private async Task<Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)>> BuildClause79LookupAsync(Dp2DbContext dbContext, HashSet<Guid> clause79Ids, CancellationToken ct)
    {
        if (clause79Ids.Count == 0)
        {
            return new Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)>();
        }

        var clause79VoArray = clause79Ids.Select(g => P79Clause2Id.From(g)).ToArray();

        return await dbContext.P79Clause2s
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

    private async Task<Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)>> BuildGuaranteeReturnLookupAsync(Dp2DbContext dbContext, HashSet<Guid> guaranteeReturnIds, CancellationToken ct)
    {
        if (guaranteeReturnIds.Count == 0)
        {
            return new Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)>();
        }

        var guaranteeReturnVoArray = guaranteeReturnIds.Select(g => CmContractGuaranteeReturnId.From(g)).ToArray();

        return await dbContext.CmContractGuaranteeReturns
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

    private async Task<Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)>> BuildPettyCashReimbursementLookupAsync(
        Dp2DbContext dbContext,
        HashSet<Guid> pettyCashReimbursementIds,
        CancellationToken ct)
    {
        if (pettyCashReimbursementIds.Count == 0)
        {
            return new Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)>();
        }

        var pettycashReimbursementArray = pettyCashReimbursementIds.Select(p => PPettyCashReimbursementId.From(p)).ToArray();

        return await dbContext.PPettyCashReimbursements
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

    public record DepartmentSourceIds(
        Guid[] W119Ids,
        Guid[] Clause79Ids,
        Guid[] GuaranteeReturnIds,
        Guid[] PettyCashReimbursementIds);

    public record SourceLookups(
        Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)> W119Lookup,
        Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)> Clause79Lookup,
        Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)> GuaranteeReturnLookup,
        Dictionary<Guid, (string Code, string Name, string Dept, decimal Amount)> PettyCashReimbursementLookup);
}
