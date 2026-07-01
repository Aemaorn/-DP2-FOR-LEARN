namespace GHB.DP2.Application.Features.WorkList.Programs;

using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations.Dto;
using GHB.DP2.Application.Features.WorkList.AccessHelpers;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.P79Clause2;
using GHB.DP2.Domain.Procurement.PPettyCash;
using GHB.DP2.Domain.Procurement.PPettyCashReimbursement;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.Procurement.Pw119;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.EntityFrameworkCore;

public interface IProcurementBaseQuery
{
    string? Keyword { get; }

    string? DepartmentCode { get; }

    int? BudgetYear { get; }

    string? SupplyMethodCode { get; }

    string? SupplyMethodTypeCode { get; }

    string? SupplyMethodSpecialTypeCode { get; }

    bool? IsMd { get; }

    bool? IsPendingDepartment { get; }
}

internal sealed class ProcurementProgram
{
    public async Task<(int PreProcurementCount, int ProcurementCount, int ContractAgreementCount, SectionResult<ProcurementItem>? PreProcurement, SectionResult<ProcurementItem>? Procurement,
        SectionResult<ProcurementItem>? ContractAgreement)> ProcessProcurementSectionsAsync(
        IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory,
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        bool isSegmentAccountingMember,
        CancellationToken ct)
    {
        SectionResult<ProcurementItem>? preProcurementSection = null;
        SectionResult<ProcurementItem>? procurementSection = null;
        SectionResult<ProcurementItem>? contractAgreementSection = null;

        if (req.IncludePreProcurement)
        {
            preProcurementSection = await this.CreateStepSectionAsync(
                dbContextFactory, req, userContext, ProcurementStep.PreProcurement, ct);
        }

        if (req.IncludeProcurement)
        {
            procurementSection = await this.CreateProcurementWithPw119AndP79Clause2AndPettyCashSectionAsync(
                dbContextFactory, req, userContext, isSegmentAccountingMember, ct);
        }

        if (req.IncludeContractAgreement)
        {
            contractAgreementSection = await this.CreateContractAgreementSectionAsync(
                dbContextFactory, req, userContext, ct);
        }

        var preProcurementCount = preProcurementSection?.Page.TotalRecords ?? 0;
        var procurementCount = procurementSection?.Page.TotalRecords ?? 0;
        var contractAgreementCount = contractAgreementSection?.Page.TotalRecords ?? 0;

        return (preProcurementCount, procurementCount, contractAgreementCount, preProcurementSection, procurementSection, contractAgreementSection);
    }

    private async Task<SectionResult<ProcurementItem>> CreateStepSectionAsync(
        IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory,
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        ProcurementStep step,
        CancellationToken ct)
    {
        // Step 1: evaluate access predicate ONCE — lightweight projection of accessible IDs + sort key
        await using var ctxIds = await dbContextFactory.CreateDbContextAsync(ct);
        var allAccessible = await this.BuildProcurementBaseQuery(ctxIds, req, userContext)
                                      .Where(x => x.Step == step)
                                      .AsNoTracking()
                                      .Select(x => new
                                      {
                                          Id = x.Id,
                                          SortAtLastModified = x.AuditInfo.LastModifiedAt,
                                          SortAtCreated = x.AuditInfo.CreatedAt,
                                          ProcurementNumber = x.ProcurementNumber.HasValue ? (string?)x.ProcurementNumber.Value.Value : null,
                                      })
                                      .ToListAsync(ct);

        var total = allAccessible.Count;

        var skip = Math.Max(0, (req.PageNumber - 1) * req.PageSize);
        var pageIds = allAccessible
                      .OrderByDescending(a => a.SortAtLastModified ?? a.SortAtCreated)
                      .ThenByDescending(a => a.ProcurementNumber)
                      .Skip(skip)
                      .Take(req.PageSize)
                      .Select(a => a.Id)
                      .ToList();

        if (pageIds.Count == 0)
        {
            return new SectionResult<ProcurementItem>(
                new PaginatedQueryResult<ProcurementItem>([], total));
        }

        var hydrated = await HydrateProcurementAsync(dbContextFactory, pageIds, ct);

        // Preserve order from pageIds
        var lookup = hydrated.ToDictionary(h => h.Id);
        var ordered = pageIds.Select(id => lookup.GetValueOrDefault(id.Value))
                             .Where(item => item is not null)
                             .Select(item => item!)
                             .ToList();

        return new SectionResult<ProcurementItem>(
            new PaginatedQueryResult<ProcurementItem>(ordered, total));
    }

    private async Task<SectionResult<ProcurementItem>> CreateContractAgreementSectionAsync(
        IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory,
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        CancellationToken ct)
    {
        // Step 1: get accessible procurement IDs + process type for ContractAgreement step
        await using var ctxIds = await dbContextFactory.CreateDbContextAsync(ct);
        var allAccessible = await this.BuildProcurementBaseQuery(ctxIds, req, userContext)
                                      .Where(x => x.Step == ProcurementStep.ContractAgreement)
                                      .AsNoTracking()
                                      .Select(x => new
                                      {
                                          Id = x.Id,
                                          ProcessType = x.ProcessType,
                                          SortAtLastModified = x.AuditInfo.LastModifiedAt,
                                          SortAtCreated = x.AuditInfo.CreatedAt,
                                          ProcurementNumber = x.ProcurementNumber.HasValue ? (string?)x.ProcurementNumber.Value.Value : null,
                                      })
                                      .ToListAsync(ct);

        var total = allAccessible.Count;

        var skip = Math.Max(0, (req.PageNumber - 1) * req.PageSize);
        var pageMeta = allAccessible
                       .OrderByDescending(a => a.SortAtLastModified ?? a.SortAtCreated)
                       .ThenByDescending(a => a.ProcurementNumber)
                       .Skip(skip)
                       .Take(req.PageSize)
                       .ToList();

        if (pageMeta.Count == 0)
        {
            return new SectionResult<ProcurementItem>(
                new PaginatedQueryResult<ProcurementItem>([], total));
        }

        var pageIds = pageMeta.Select(m => m.Id).ToList();
        var contractDraftIds = pageMeta
                               .Where(m => m.ProcessType == ProcessType.ContractDraft)
                               .Select(m => m.Id)
                               .ToList();

        // Step 2: two parallel projection queries — no Includes, no AsSplitQuery
        //  - vendor rows (flat) for ContractDraft procurements
        //  - procurement-only rows for all pageIds (fallback when ContractDraft has no vendors,
        //    and primary path for non-ContractDraft process types)
        await using var ctxVendors = await dbContextFactory.CreateDbContextAsync(ct);

        var vendorRowsTask = ProjectContractAgreementVendorsAsync(ctxVendors, contractDraftIds, ct);
        var procItemsTask = HydrateProcurementAsync(dbContextFactory, pageIds, ct);

        await Task.WhenAll(vendorRowsTask, procItemsTask);

        var vendorRows = await vendorRowsTask;
        var procItems = await procItemsTask;

        var vendorByProcurement = vendorRows
                                  .GroupBy(v => v.ProcurementId)
                                  .ToDictionary(g => g.Key, g => g.ToList());
        var procById = procItems.ToDictionary(p => p.Id);

        var pageItems = new List<ProcurementItem>();
        foreach (var meta in pageMeta)
        {
            if (meta.ProcessType == ProcessType.ContractDraft
                && vendorByProcurement.TryGetValue(meta.Id.Value, out var vrows)
                && vrows.Count > 0)
            {
                foreach (var v in vrows)
                {
                    pageItems.Add(BuildContractAgreementVendorItem(v));
                }
            }
            else if (procById.TryGetValue(meta.Id.Value, out var prow))
            {
                pageItems.Add(prow);
            }
        }

        return new SectionResult<ProcurementItem>(
            new PaginatedQueryResult<ProcurementItem>(pageItems, total));
    }

    private static async Task<List<ContractAgreementVendorRow>> ProjectContractAgreementVendorsAsync(
        Dp2ReadOnlyDbContext ctx,
        List<ProcurementId> ids,
        CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await ctx.Procurements
                        .AsNoTracking()
                        .Where(x => ids.Contains(x.Id))
                        .SelectMany(x => x.ContractDrafts.SelectMany(cd => cd.Vendors.Select(v => new ContractAgreementVendorRow
                        {
                            ProcurementId = x.Id.Value,
                            Type = x.Type,
                            PlanNumber = x.Plan != null ? x.Plan.PlanNumber.Value : null,
                            PlanTypeName = x.Plan != null ? x.Plan.Type.ToString() : null,
                            ProcurementNumber = x.ProcurementNumber.HasValue ? (string?)x.ProcurementNumber.Value.Value : null,
                            Name = x.Name,
                            DepartmentName = x.Department.Name,
                            SupplyMethodLabel = x.SupplyMethod.Label,
                            SupplyMethodTypeLabel = x.SupplyMethodType != null ? x.SupplyMethodType.Label : string.Empty,
                            SupplyMethodSpecialTypeLabel = x.SupplyMethodSpecialType != null ? x.SupplyMethodSpecialType.Label : string.Empty,
                            FirstApprovalRentTypeLabel = x.PrincipleApprovals.OrderBy(p => p.Id).Select(p => p.RentTypeCodeInfo.Label).FirstOrDefault(),
                            FirstApprovalRentalDurationYear = x.PrincipleApprovals.OrderBy(p => p.Id).Select(p => (int?)p.RentalDurationYear).FirstOrDefault(),
                            FirstApprovalRentalDurationMonth = x.PrincipleApprovals.OrderBy(p => p.Id).Select(p => (int?)p.RentalDurationMonth).FirstOrDefault(),
                            FirstApprovalRentalDurationDay = x.PrincipleApprovals.OrderBy(p => p.Id).Select(p => (int?)p.RentalDurationDay).FirstOrDefault(),
                            Step = x.Step,
                            ProcessType = x.ProcessType,
                            VendorId = v.Id.Value,
                            VendorContractNumber = v.ContractNumber,
                            VendorContractName = v.ContractName,
                            VendorBudget = v.Budget,
                            VendorStatusName = v.Status.ToString(),
                            EntrepreneurName = v.ContractInvitationVendors.PurchaseOrderApprovalContract.Entrepreneur != null
                                ? v.ContractInvitationVendors.PurchaseOrderApprovalContract.Entrepreneur.SuVendor.EstablishmentName
                                : null,
                            PARentEntrepreneurName = v.ContractInvitationVendors.PurchaseOrderApprovalContract.PrincipleApprovalRentalEntrepreneurs != null
                                ? v.ContractInvitationVendors.PurchaseOrderApprovalContract.PrincipleApprovalRentalEntrepreneurs.Vendor.EstablishmentName
                                : null,
                            BudgetDescPARental = v.ContractInvitationVendors.PurchaseOrderApprovalContract.PrincipleApprovalRentalBudget != null
                                ? v.ContractInvitationVendors.PurchaseOrderApprovalContract.PrincipleApprovalRentalBudget.Description
                                : null,
                            BudgetDescBudget = v.ContractInvitationVendors.PurchaseOrderApprovalContract.Budget != null
                                ? v.ContractInvitationVendors.PurchaseOrderApprovalContract.Budget.Description
                                : null,
                            BudgetDescPR = v.ContractInvitationVendors.PurchaseOrderApprovalContract.PpPurchaseRequisitionBudget != null
                                ? v.ContractInvitationVendors.PurchaseOrderApprovalContract.PpPurchaseRequisitionBudget.Description
                                : null,
                            BudgetDescPOA = v.ContractInvitationVendors.PurchaseOrderApprovalContract.PPurchaseOrderApprovalBudget != null
                                ? v.ContractInvitationVendors.PurchaseOrderApprovalContract.PPurchaseOrderApprovalBudget.Description
                                : null,
                        })))
                        .ToListAsync(ct);
    }

    private static ProcurementItem BuildContractAgreementVendorItem(ContractAgreementVendorRow v)
    {
        var durationParts = new List<string>();
        if ((v.FirstApprovalRentalDurationYear ?? 0) > 0)
        {
            durationParts.Add($"{v.FirstApprovalRentalDurationYear} ปี");
        }

        if ((v.FirstApprovalRentalDurationMonth ?? 0) > 0)
        {
            durationParts.Add($"{v.FirstApprovalRentalDurationMonth} เดือน");
        }

        if ((v.FirstApprovalRentalDurationDay ?? 0) > 0)
        {
            durationParts.Add($"{v.FirstApprovalRentalDurationDay} วัน");
        }

        string? budgetDesc =
            v.Type == ProcurementType.Rent && v.BudgetDescPARental != null ? v.BudgetDescPARental :
            v.Type == ProcurementType.Procurement && v.BudgetDescBudget != null ? v.BudgetDescBudget :
            v.BudgetDescPR != null ? v.BudgetDescPR :
            v.BudgetDescPOA != null ? v.BudgetDescPOA :
            null;

        var vendorName = v.VendorContractNumber ?? string.Empty;
        if (v.EntrepreneurName != null || v.PARentEntrepreneurName != null)
        {
            vendorName = $"{v.VendorContractNumber} : {v.EntrepreneurName}{v.PARentEntrepreneurName}";
        }

        return new ProcurementItem(
            v.ProcurementId,
            v.Type.ToString(),
            v.PlanNumber ?? string.Empty,
            v.ProcurementNumber,
            v.Name,
            v.VendorBudget,
            v.PlanTypeName ?? string.Empty,
            v.DepartmentName,
            v.SupplyMethodLabel,
            v.SupplyMethodTypeLabel,
            v.SupplyMethodSpecialTypeLabel,
            v.FirstApprovalRentTypeLabel ?? string.Empty,
            v.Step,
            v.ProcessType,
            v.VendorStatusName,
            string.Join(" ", durationParts),
            v.VendorId,
            v.VendorContractName,
            budgetDesc ?? vendorName);
    }

    private sealed class ContractAgreementVendorRow
    {
        public Guid ProcurementId { get; init; }

        public ProcurementType Type { get; init; }

        public string? PlanNumber { get; init; }

        public string? PlanTypeName { get; init; }

        public string? ProcurementNumber { get; init; }

        public string Name { get; init; } = string.Empty;

        public string DepartmentName { get; init; } = string.Empty;

        public string SupplyMethodLabel { get; init; } = string.Empty;

        public string SupplyMethodTypeLabel { get; init; } = string.Empty;

        public string SupplyMethodSpecialTypeLabel { get; init; } = string.Empty;

        public string? FirstApprovalRentTypeLabel { get; init; }

        public int? FirstApprovalRentalDurationYear { get; init; }

        public int? FirstApprovalRentalDurationMonth { get; init; }

        public int? FirstApprovalRentalDurationDay { get; init; }

        public ProcurementStep Step { get; init; }

        public ProcessType ProcessType { get; init; }

        public Guid VendorId { get; init; }

        public string? VendorContractNumber { get; init; }

        public string? VendorContractName { get; init; }

        public decimal? VendorBudget { get; init; }

        public string VendorStatusName { get; init; } = string.Empty;

        public string? EntrepreneurName { get; init; }

        public string? PARentEntrepreneurName { get; init; }

        public string? BudgetDescPARental { get; init; }

        public string? BudgetDescBudget { get; init; }

        public string? BudgetDescPR { get; init; }

        public string? BudgetDescPOA { get; init; }
    }

    private async Task<SectionResult<ProcurementItem>> CreateProcurementWithPw119AndP79Clause2AndPettyCashSectionAsync(
        IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory,
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        bool isSegmentAccountingMember,
        CancellationToken ct)
    {
        var topN = Math.Max(req.PageNumber, 1) * req.PageSize;

        await using var ctxProcList = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctxProcCount = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctx2 = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctx3 = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctx4 = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctx5 = await dbContextFactory.CreateDbContextAsync(ct);

        var procurementTask = this.ProjectProcurementForComboParallel(ctxProcList, ctxProcCount, req, userContext, topN, ct);
        var pw119Task = this.ProjectPw119(ctx2, req, userContext, isSegmentAccountingMember, topN, ct);
        var p79Task = this.ProjectP79Clause2(ctx3, req, userContext, isSegmentAccountingMember, topN, ct);
        var pettyCashTask = this.ProjectPPettyCash(ctx4, req, userContext, topN, ct);
        var reimbursementTask = this.ProjectPPettyCashReimbursement(ctx5, req, userContext, topN, ct);

        await Task.WhenAll(procurementTask, pw119Task, p79Task, pettyCashTask, reimbursementTask);

        var procurementResult = await procurementTask;
        var pw119Result = await pw119Task;
        var p79Result = await p79Task;
        var pettyCashResult = await pettyCashTask;
        var reimbursementResult = await reimbursementTask;

        var totalCount = procurementResult.Total + pw119Result.Total + p79Result.Total + pettyCashResult.Total + reimbursementResult.Total;

        var allProjections = procurementResult.Items
                                              .Concat(pw119Result.Items)
                                              .Concat(p79Result.Items)
                                              .Concat(pettyCashResult.Items)
                                              .Concat(reimbursementResult.Items)
                                              .OrderByDescending(x => x.SortKey)
                                              .ToList();

        var skip = Math.Max(0, (req.PageNumber - 1) * req.PageSize);
        var pageProjections = allProjections.Skip(skip).Take(req.PageSize).ToList();

        if (pageProjections.Count == 0)
        {
            return new SectionResult<ProcurementItem>(
                new PaginatedQueryResult<ProcurementItem>([], totalCount));
        }

        var hydratedItems = await this.HydrateComboPageAsync(dbContextFactory, pageProjections, ct);

        var orderedItems = pageProjections
                           .Select(p => hydratedItems.GetValueOrDefault((p.Type, p.Id)))
                           .Where(item => item is not null)
                           .Select(item => item!)
                           .ToList();

        return new SectionResult<ProcurementItem>(
            new PaginatedQueryResult<ProcurementItem>(orderedItems, totalCount));
    }

    private sealed record WorklistProjection(string Type, Guid Id, string? SortKey);

    private async Task<(List<WorklistProjection> Items, int Total)> ProjectProcurementForComboParallel(
        Dp2ReadOnlyDbContext ctxList, Dp2ReadOnlyDbContext ctxCount, GetWorklistSeparatedRequest req, UserContext userContext, int topN, CancellationToken ct)
    {
        var listQuery = this.BuildProcurementBaseQuery(ctxList, req, userContext)
                            .Where(x => !x.IsDeleted && x.Step == ProcurementStep.Procurement)
                            .AsNoTracking();

        var itemsTask = listQuery
                        .OrderByDescending(x => x.ProcurementNumber)
                        .Select(x => new WorklistProjection(
                            "Procurement",
                            x.Id.Value,
                            x.ProcurementNumber.HasValue ? (string?)x.ProcurementNumber.Value.Value : null))
                        .Take(topN)
                        .ToListAsync(ct);

        var countQuery = this.BuildProcurementBaseQuery(ctxCount, req, userContext)
                             .Where(x => !x.IsDeleted && x.Step == ProcurementStep.Procurement)
                             .AsNoTracking();

        var totalTask = countQuery.CountAsync(ct);

        await Task.WhenAll(itemsTask, totalTask);
        return (await itemsTask, await totalTask);
    }

    private async Task<(List<WorklistProjection> Items, int Total)> ProjectPw119(
        Dp2ReadOnlyDbContext ctx, GetWorklistSeparatedRequest req, UserContext userContext, bool isSegmentAccountingMember, int topN, CancellationToken ct)
    {
        var query = this.BuildPw119BaseQuery(ctx, req, userContext, isSegmentAccountingMember).AsNoTracking();

        var items = await query
                          .OrderByDescending(x => x.Pw119Number)
                          .Select(x => new WorklistProjection("Pw119", x.Id.Value, x.Pw119Number.Value))
                          .Take(topN)
                          .ToListAsync(ct);

        var total = await query.CountAsync(ct);
        return (items, total);
    }

    private async Task<(List<WorklistProjection> Items, int Total)> ProjectP79Clause2(
        Dp2ReadOnlyDbContext ctx, GetWorklistSeparatedRequest req, UserContext userContext, bool isSegmentAccountingMember, int topN, CancellationToken ct)
    {
        var query = this.BuildP79Clause2BaseQuery(ctx, req, userContext, isSegmentAccountingMember).AsNoTracking();

        var items = await query
                          .OrderByDescending(x => x.P79Clause2Number)
                          .Select(x => new WorklistProjection("P79Clause2", x.Id.Value, x.P79Clause2Number.Value))
                          .Take(topN)
                          .ToListAsync(ct);

        var total = await query.CountAsync(ct);
        return (items, total);
    }

    private async Task<(List<WorklistProjection> Items, int Total)> ProjectPPettyCash(
        Dp2ReadOnlyDbContext ctx, GetWorklistSeparatedRequest req, UserContext userContext, int topN, CancellationToken ct)
    {
        var query = this.BuildPPettyCashBaseQuery(ctx, req, userContext).AsNoTracking();

        var items = await query
                          .OrderByDescending(x => x.PettyCashNumber)
                          .Select(x => new WorklistProjection("PettyCash", x.Id.Value, x.PettyCashNumber.Value))
                          .Take(topN)
                          .ToListAsync(ct);

        var total = await query.CountAsync(ct);
        return (items, total);
    }

    private async Task<(List<WorklistProjection> Items, int Total)> ProjectPPettyCashReimbursement(
        Dp2ReadOnlyDbContext ctx, GetWorklistSeparatedRequest req, UserContext userContext, int topN, CancellationToken ct)
    {
        var query = this.BuildPPettyCashReimbursementBaseQuery(ctx, req, userContext).AsNoTracking();

        var items = await query
                          .OrderByDescending(x => x.Number)
                          .Select(x => new WorklistProjection("PettyCashReimbursement", x.Id.Value, x.Number))
                          .Take(topN)
                          .ToListAsync(ct);

        var total = await query.CountAsync(ct);
        return (items, total);
    }

    private async Task<Dictionary<(string Type, Guid Id), ProcurementItem>> HydrateComboPageAsync(
        IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory,
        List<WorklistProjection> pageProjections,
        CancellationToken ct)
    {
        var procurementIds = pageProjections.Where(p => p.Type == "Procurement").Select(p => ProcurementId.From(p.Id)).ToList();
        var pw119Ids = pageProjections.Where(p => p.Type == "Pw119").Select(p => Pw119Id.From(p.Id)).ToList();
        var p79Ids = pageProjections.Where(p => p.Type == "P79Clause2").Select(p => P79Clause2Id.From(p.Id)).ToList();
        var pettyCashIds = pageProjections.Where(p => p.Type == "PettyCash").Select(p => PettyCashId.From(p.Id)).ToList();
        var reimbursementIds = pageProjections.Where(p => p.Type == "PettyCashReimbursement").Select(p => PPettyCashReimbursementId.From(p.Id)).ToList();

        var result = new Dictionary<(string Type, Guid Id), ProcurementItem>();

        var procurementTask = HydrateProcurementAsync(dbContextFactory, procurementIds, ct);
        var pw119Task = HydratePw119Async(dbContextFactory, pw119Ids, ct);
        var p79Task = HydrateP79Async(dbContextFactory, p79Ids, ct);
        var pettyCashTask = HydratePettyCashAsync(dbContextFactory, pettyCashIds, ct);
        var reimbursementTask = HydrateReimbursementAsync(dbContextFactory, reimbursementIds, ct);

        await Task.WhenAll(procurementTask, pw119Task, p79Task, pettyCashTask, reimbursementTask);

        foreach (var item in await procurementTask)
        {
            result[("Procurement", item.Id)] = item;
        }

        foreach (var item in await pw119Task)
        {
            result[("Pw119", item.Id)] = item;
        }

        foreach (var item in await p79Task)
        {
            result[("P79Clause2", item.Id)] = item;
        }

        foreach (var item in await pettyCashTask)
        {
            result[("PettyCash", item.Id)] = item;
        }

        foreach (var item in await reimbursementTask)
        {
            result[("PettyCashReimbursement", item.Id)] = item;
        }

        return result;
    }

    private static async Task<List<ProcurementItem>> HydrateProcurementAsync(
        IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory,
        List<ProcurementId> ids,
        CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        await using var ctx = await dbContextFactory.CreateDbContextAsync(ct);
        var rows = await ctx.Procurements
                            .AsNoTracking()
                            .Where(x => ids.Contains(x.Id))
                            .Select(x => new
                            {
                                Id = x.Id.Value,
                                Type = x.Type,
                                PlanNumber = x.Plan != null ? x.Plan.PlanNumber.Value : null,
                                PlanTypeName = x.Plan != null ? x.Plan.Type.ToString() : null,
                                ProcurementNumber = x.ProcurementNumber.HasValue ? (string?)x.ProcurementNumber.Value.Value : null,
                                Name = x.Name,
                                Budget = x.Budget,
                                FirstApprovalTotalRental = x.PrincipleApprovals.OrderBy(p => p.Id).Select(p => (decimal?)p.TotalRentalAmount).FirstOrDefault(),
                                DepartmentName = x.Department.Name,
                                SupplyMethodLabel = x.SupplyMethod.Label,
                                SupplyMethodTypeLabel = x.SupplyMethodType != null ? x.SupplyMethodType.Label : string.Empty,
                                SupplyMethodSpecialTypeLabel = x.SupplyMethodSpecialType != null ? x.SupplyMethodSpecialType.Label : string.Empty,
                                FirstApprovalRentTypeLabel = x.PrincipleApprovals.OrderBy(p => p.Id).Select(p => p.RentTypeCodeInfo.Label).FirstOrDefault(),
                                FirstApprovalRentalDurationYear = x.PrincipleApprovals.OrderBy(p => p.Id).Select(p => (int?)p.RentalDurationYear).FirstOrDefault(),
                                FirstApprovalRentalDurationMonth = x.PrincipleApprovals.OrderBy(p => p.Id).Select(p => (int?)p.RentalDurationMonth).FirstOrDefault(),
                                FirstApprovalRentalDurationDay = x.PrincipleApprovals.OrderBy(p => p.Id).Select(p => (int?)p.RentalDurationDay).FirstOrDefault(),
                                Step = x.Step,
                                ProcessType = x.ProcessType,
                                ProcurementStatus = x.Status,
                                AppointStatus = x.Appoints.Select(a => a.Status.ToString()).FirstOrDefault(),
                                TorDraftStatus = x.TorDrafts.Select(a => a.Status.ToString()).FirstOrDefault(),
                                MedianPriceStatus = x.MedianPrices.Select(a => a.Status.ToString()).FirstOrDefault(),
                                PrincipleApprovalStatus = x.PrincipleApprovals.Select(a => a.Status.ToString()).FirstOrDefault(),
                                PrincipleApprovalRentalStatus = x.PrincipleApprovalRentals.Select(a => a.Status.ToString()).FirstOrDefault(),
                                PurchaseRequisitionStatus = x.PurchaseRequisitions.Select(a => a.Status.ToString()).FirstOrDefault(),
                                Jp005Status = x.Jp005.Select(a => a.Status.ToString()).FirstOrDefault(),
                                InviteStatus = x.Invites.Select(a => a.Status.ToString()).FirstOrDefault(),
                                PurchaseOrderStatus = x.PurchaseOrder.Select(a => a.Status.ToString()).FirstOrDefault(),
                                PurchaseOrderApprovalStatus = x.PurchaseOrderApprovals.Select(a => a.Status.ToString()).FirstOrDefault(),
                                ContractInvitationStatus = x.ContractInvitations.Select(a => a.Status.ToString()).FirstOrDefault(),
                            })
                            .ToListAsync(ct);

        return rows.Select(p =>
        {
            var childStatus = p.ProcessType switch
            {
                ProcessType.Appoint => p.AppointStatus,
                ProcessType.TorDraft => p.TorDraftStatus,
                ProcessType.MedianPrice => p.MedianPriceStatus,
                ProcessType.PrincipleApproval => p.PrincipleApprovalStatus,
                ProcessType.PrincipleApprovalRental => p.PrincipleApprovalRentalStatus,
                ProcessType.PurchaseRequisition => p.PurchaseRequisitionStatus,
                ProcessType.Jp005 => p.Jp005Status,
                ProcessType.Invite => p.InviteStatus,
                ProcessType.PurchaseOrder => p.PurchaseOrderStatus,
                ProcessType.PurchaseOrderApproval => p.PurchaseOrderApprovalStatus,
                ProcessType.ContractInvitation => p.ContractInvitationStatus,
                _ => null,
            };

            childStatus ??= p.ProcurementStatus.ToString();

            var durationParts = new List<string>();
            if ((p.FirstApprovalRentalDurationYear ?? 0) > 0)
            {
                durationParts.Add($"{p.FirstApprovalRentalDurationYear} ปี");
            }

            if ((p.FirstApprovalRentalDurationMonth ?? 0) > 0)
            {
                durationParts.Add($"{p.FirstApprovalRentalDurationMonth} เดือน");
            }

            if ((p.FirstApprovalRentalDurationDay ?? 0) > 0)
            {
                durationParts.Add($"{p.FirstApprovalRentalDurationDay} วัน");
            }

            return new ProcurementItem(
                p.Id,
                p.Type.ToString(),
                p.PlanNumber ?? string.Empty,
                p.ProcurementNumber,
                p.Name,
                p.Type == ProcurementType.Procurement ? p.Budget : p.FirstApprovalTotalRental,
                p.PlanTypeName ?? string.Empty,
                p.DepartmentName,
                p.SupplyMethodLabel,
                p.SupplyMethodTypeLabel,
                p.SupplyMethodSpecialTypeLabel,
                p.FirstApprovalRentTypeLabel ?? string.Empty,
                p.Step,
                p.ProcessType,
                childStatus,
                string.Join(" ", durationParts));
        }).ToList();
    }

    private static async Task<List<ProcurementItem>> HydratePw119Async(
        IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory,
        List<Pw119Id> ids,
        CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        await using var ctx = await dbContextFactory.CreateDbContextAsync(ct);
        return await ctx.Pw119s
                        .AsNoTracking()
                        .Where(x => ids.Contains(x.Id))
                        .Select(x => new ProcurementItem(
                            x.Id.Value,
                            "Pw119",
                            string.Empty,
                            x.Pw119Number.Value,
                            x.Subject,
                            x.Budget,
                            string.Empty,
                            x.Department.Name,
                            x.SupplyMethod.Label,
                            string.Empty,
                            x.SupplyMethodSpecialType != null ? x.SupplyMethodSpecialType.Label : string.Empty,
                            x.W119Categories.Label,
                            ProcurementStep.Procurement,
                            ProcessType.W119,
                            x.Status.ToString(),
                            string.Empty,
                            null,
                            null,
                            null))
                        .ToListAsync(ct);
    }

    private static async Task<List<ProcurementItem>> HydrateP79Async(
        IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory,
        List<P79Clause2Id> ids,
        CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        await using var ctx = await dbContextFactory.CreateDbContextAsync(ct);
        return await ctx.P79Clause2s
                        .AsNoTracking()
                        .Where(x => ids.Contains(x.Id))
                        .Select(x => new ProcurementItem(
                            x.Id.Value,
                            "P79Clause2",
                            string.Empty,
                            x.P79Clause2Number.Value,
                            x.Subject,
                            x.Budget,
                            string.Empty,
                            x.Department.Name,
                            x.SupplyMethod.Label,
                            x.SupplyMethodType != null ? x.SupplyMethodType.Label : string.Empty,
                            x.SupplyMethodSpecialType != null ? x.SupplyMethodSpecialType.Label : string.Empty,
                            string.Empty,
                            ProcurementStep.Procurement,
                            ProcessType.P79Clause2,
                            x.Status.ToString(),
                            string.Empty,
                            null,
                            null,
                            null))
                        .ToListAsync(ct);
    }

    private static async Task<List<ProcurementItem>> HydratePettyCashAsync(
        IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory,
        List<PettyCashId> ids,
        CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        await using var ctx = await dbContextFactory.CreateDbContextAsync(ct);
        return await ctx.PPettyCashs
                        .AsNoTracking()
                        .Where(x => ids.Contains(x.Id))
                        .Select(x => new ProcurementItem(
                            x.Id.Value,
                            "PettyCash",
                            string.Empty,
                            x.PettyCashNumber.Value,
                            x.Subject,
                            x.Budget,
                            string.Empty,
                            x.Department.Name,
                            x.SupplyMethod.Label,
                            x.SupplyMethodType != null ? x.SupplyMethodType.Label : string.Empty,
                            x.SupplyMethodSpecialType != null ? x.SupplyMethodSpecialType.Label : string.Empty,
                            string.Empty,
                            ProcurementStep.Procurement,
                            ProcessType.PettyCash,
                            x.Status.ToString(),
                            string.Empty,
                            null,
                            null,
                            null))
                        .ToListAsync(ct);
    }

    private static async Task<List<ProcurementItem>> HydrateReimbursementAsync(
        IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory,
        List<PPettyCashReimbursementId> ids,
        CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        await using var ctx = await dbContextFactory.CreateDbContextAsync(ct);
        return await ctx.PPettyCashReimbursements
                        .AsNoTracking()
                        .Where(x => ids.Contains(x.Id))
                        .Select(x => new ProcurementItem(
                            x.Id.Value,
                            "PettyCashReimbursement",
                            string.Empty,
                            x.Number,
                            x.Subject,
                            x.Items!.SelectMany(i => i.PettyCashGlAccount.PettyCash.Vendors).SelectMany(v => v.VendorParcels).Sum(vp => (decimal?)vp.TotalPriceVat) ?? 0m,
                            string.Empty,
                            x.Department.Name,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            ProcurementStep.Procurement,
                            ProcessType.PettyCashReimbursement,
                            x.Status.ToString(),
                            string.Empty,
                            null,
                            null,
                            null))
                        .ToListAsync(ct);
    }

    public IQueryable<Procurement> BuildProcurementBaseQuery(
        Dp2DbContext dbContext,
        IProcurementBaseQuery req,
        UserContext userContext)
    {
        var baseQuery = this.BuildProcurementInProcessBase(
            dbContext,
            userContext.IsJorPor,
            userContext.PrimaryOrganizationLevel,
            userContext.EffectiveUserIds,
            userContext.ViewUser.BusinessUnitId,
            req.IsMd == true,
            req.IsPendingDepartment == true);

        return baseQuery
               .Where(a => !a.IsDeleted)
               .Where(a => a.Status != ProcurementStatus.Cancelled)
               .WhereIfTrue(
                   !string.IsNullOrWhiteSpace(req.Keyword),
                   x =>
                       EF.Functions.ILike(x.Name, $"%{req.Keyword}%") ||
                       EF.Functions.ILike((string)x.Plan.PlanNumber, $"%{req.Keyword}%") ||
                       EF.Functions.ILike((string)x.ProcurementNumber.Value, $"%{req.Keyword}%"))
               .WhereIfTrue(!string.IsNullOrWhiteSpace(req.DepartmentCode), x => x.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
               .WhereIfTrue(req.BudgetYear.HasValue, x => x.BudgetYear == req.BudgetYear)
               .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodCode), x => x.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
               .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodTypeCode), x => x.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!))
               .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodSpecialTypeCode), x => x.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!));
    }

    private IQueryable<Procurement> BuildProcurementInProcessBase(
        Dp2DbContext dbContext,
        bool isJorPor,
        string? organizationLevel,
        IEnumerable<EffectiveUserId> userIds,
        BusinessUnitId? userDepartmentId,
        bool isMd,
        bool isPendingDepartment = false)
    {
        var q = dbContext.Procurements.AsQueryable();

        if (isMd)
        {
            return q;
        }

        var noPrIds = isPendingDepartment && organizationLevel == GHB.DP2.Domain.Raws.Constants.EmployeeConstant.OrganizationLevel.Segment && userDepartmentId != null
            ? dbContext.Procurements
                       .Where(p => p.ProcessType == ProcessType.PurchaseRequisition &&
                                   p.DepartmentId == userDepartmentId &&
                                   !p.PurchaseRequisitions.Any())
                       .OrderByDescending(p => p.AuditInfo.LastModifiedAt)
                       .Take(10)
                       .Select(p => p.Id)
                       .ToList()
            : new List<ProcurementId>();

        var poaIds = isPendingDepartment && isJorPor && organizationLevel == GHB.DP2.Domain.Raws.Constants.EmployeeConstant.OrganizationLevel.Segment
            ? dbContext.Procurements
                       .Where(p => p.ProcessType == ProcessType.PurchaseOrderApproval &&
                                   (!p.PurchaseOrderApprovals.Any() || p.PurchaseOrderApprovals.Any(poa =>
                                       poa.Status == PurchaseOrderApprovalStatus.Draft ||
                                       poa.Status == PurchaseOrderApprovalStatus.Edit ||
                                       poa.Status == PurchaseOrderApprovalStatus.Rejected)))
                       .OrderByDescending(p => p.AuditInfo.LastModifiedAt)
                       .Take(10)
                       .Select(p => p.Id)
                       .ToList()
            : new List<ProcurementId>();

        var accessHelper = new ProcurementAccessHelper();
        var expression = accessHelper.IsPreProcurementAccessible(userIds, noPrIds)
                                     .Or(accessHelper.IsProcurementAccessible(userIds, poaIds))
                                     .Or(accessHelper.IsContractAgreementAccessible(userIds));

        return q.Where(expression);
    }

    public static bool IsSegmentAccountingMember(
        IEnumerable<OperationInfo> segmentAccountingMembers,
        UserContext userContext)
    {
        var memberUserIds = segmentAccountingMembers.Select(m => m.UserId).ToHashSet();

        return userContext.EffectiveUserIds.Any(e => memberUserIds.Contains(e.UserId));
    }

    public IQueryable<Pw119> BuildPw119BaseQuery(
        Dp2DbContext dbContext,
        IProcurementBaseQuery req,
        UserContext userContext,
        bool isSegmentAccountingMember = false)
    {
        var userIds = userContext.EffectiveUserIds.Select(e => e.UserId);
        var userId = userContext.User.Id.Value;

        return dbContext.Pw119s
                        .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), x => EF.Functions.ILike(x.Subject, $"%{req.Keyword}%") || EF.Functions.ILike((string)x.Pw119Number, $"%{req.Keyword}%") || x.GLAccounts.Any(g => EF.Functions.ILike(g.GLAccount.Label, $"%{req.Keyword}%")))
                        .WhereIfTrue(!string.IsNullOrWhiteSpace(req.DepartmentCode), x => x.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                        .WhereIfTrue(req.BudgetYear.HasValue, x => x.BudgetYear == req.BudgetYear)
                        .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodCode), x => x.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                        .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodSpecialTypeCode), x => x.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
                        .Where(p =>
                            (
                                (p.Status == Pw119Status.Draft || p.Status == Pw119Status.Rejected ||
                                 p.Status == Pw119Status.Edit) &&
                                p.AuditInfo.CreatedBy == userId
                            ) ||
                            (
                                (p.Status == Pw119Status.WaitingApproval) &&
                                p.Acceptors.Any(a =>
                                    !a.IsDeleted &&
                                    a.Status == AcceptorStatus.Pending &&
                                    (a.Type == AcceptorType.Approver || a.Type == AcceptorType.DepartmentDirectorAgree) &&
                                    (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))) &&
                                    !p.Acceptors.Any(b =>
                                        !b.IsDeleted &&
                                        b.Status == AcceptorStatus.Pending &&
                                        (b.Type == AcceptorType.Approver || b.Type == AcceptorType.DepartmentDirectorAgree) &&
                                        b.Sequence < a.Sequence))
                            ) ||
                            (
                                (p.Status == Pw119Status.WaitingAccountingApproval) &&
                                (p.Acceptors.Any(a =>
                                    !a.IsDeleted &&
                                    a.Status == AcceptorStatus.Pending &&
                                    a.Type == AcceptorType.AccountingOperator &&
                                    (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))) &&
                                    !p.Acceptors.Any(b =>
                                        !b.IsDeleted &&
                                        b.Status == AcceptorStatus.Pending &&
                                        b.Type == AcceptorType.AccountingOperator &&
                                        b.Sequence < a.Sequence)) ||
                                p.Acceptors.Any(a =>
                                    !a.IsDeleted &&
                                    a.Status == AcceptorStatus.Pending &&
                                    a.Type == AcceptorType.AccountingApprover &&
                                    (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))) &&
                                    !p.Acceptors.Any(b =>
                                        !b.IsDeleted &&
                                        b.Status == AcceptorStatus.Pending &&
                                        b.Type == AcceptorType.AccountingOperator) &&

                                    (
                                        EmployeeConstant.OrganizationLevel.BranchLevels.Contains(p.Department.OrganizationLevel) ||
                                        p.Acceptors.Any(o => !o.IsDeleted && o.Type == AcceptorType.AccountingOperator)
                                    ) &&
                                    !p.Acceptors.Any(b =>
                                        !b.IsDeleted &&
                                        b.Status == AcceptorStatus.Pending &&
                                        b.Type == AcceptorType.AccountingApprover &&
                                        b.Sequence < a.Sequence)))
                            ) ||
                            (p.Status == Pw119Status.WaitingDisbursementDate &&
                             (p.Acceptors.Any(a =>
                                  !a.IsDeleted &&
                                  a.Type == AcceptorType.AccountingConfirmer &&
                                  (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId)))) ||
                              (!p.Acceptors.Any(c =>
                                  c.Type == AcceptorType.AccountingConfirmer &&
                                  !c.IsDeleted) &&
                               p.Acceptors.Any(a =>
                                   a.Type == AcceptorType.AccountingOperator &&
                                   !a.IsDeleted &&
                                   (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))))))) ||
                            (
                                isSegmentAccountingMember &&
                                p.Status == Pw119Status.WaitingAccountingApproval &&
                                !p.Acceptors.Any(a => !a.IsDeleted && a.Type == AcceptorType.AccountingOperator) &&
                                !p.Acceptors.Any(a => !a.IsDeleted && a.Type == AcceptorType.AccountingApprover && a.Status == AcceptorStatus.Approved) &&
                                !EmployeeConstant.OrganizationLevel.BranchLevels.Contains(p.Department.OrganizationLevel)
                            ) ||
                            (
                                isSegmentAccountingMember &&
                                p.Status == Pw119Status.WaitingDisbursementDate &&
                                !p.Acceptors.Any(a => !a.IsDeleted && a.Type == AcceptorType.AccountingConfirmer) &&
                                !EmployeeConstant.OrganizationLevel.BranchLevels.Contains(p.Department.OrganizationLevel)
                            ));
    }

    public IQueryable<P79Clause2> BuildP79Clause2BaseQuery(
        Dp2DbContext dbContext,
        IProcurementBaseQuery req,
        UserContext userContext,
        bool isSegmentAccountingMember = false)
    {
        var userIds = userContext.EffectiveUserIds.Select(e => e.UserId);
        var userId = userContext.User.Id.Value;

        return dbContext.P79Clause2s
                        .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), x => EF.Functions.ILike(x.Subject, $"%{req.Keyword}%") || EF.Functions.ILike((string)x.P79Clause2Number, $"%{req.Keyword}%") || x.GLAccounts.Any(g => EF.Functions.ILike(g.GLAccount.Label, $"%{req.Keyword}%")))
                        .WhereIfTrue(!string.IsNullOrWhiteSpace(req.DepartmentCode), x => x.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                        .WhereIfTrue(req.BudgetYear.HasValue, x => x.BudgetYear == req.BudgetYear)
                        .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodCode), x => x.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                        .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodTypeCode), x => x.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!))
                        .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodSpecialTypeCode), x => x.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
                        .Where(p =>
                            (
                                (p.Status == P79Clause2Status.Draft || p.Status == P79Clause2Status.Rejected ||
                                 p.Status == P79Clause2Status.Edit) &&
                                p.AuditInfo.CreatedBy == userId
                            ) ||
                            (
                                (p.Status == P79Clause2Status.WaitingApproval) &&
                                p.Acceptors.Any(a =>
                                    !a.IsDeleted &&
                                    a.Status == AcceptorStatus.Pending &&
                                    (a.Type == AcceptorType.Approver || a.Type == AcceptorType.DepartmentDirectorAgree) &&
                                    (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))) &&
                                    !p.Acceptors.Any(b =>
                                        !b.IsDeleted &&
                                        b.Status == AcceptorStatus.Pending &&
                                        (b.Type == AcceptorType.Approver || b.Type == AcceptorType.DepartmentDirectorAgree) &&
                                        b.Sequence < a.Sequence))
                            ) ||
                            (
                                (p.Status == P79Clause2Status.WaitingAccountingApproval) &&
                                (p.Acceptors.Any(a =>
                                    !a.IsDeleted &&
                                    a.Status == AcceptorStatus.Pending &&
                                    a.Type == AcceptorType.AccountingOperator &&
                                    (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))) &&
                                    !p.Acceptors.Any(b =>
                                        !b.IsDeleted &&
                                        b.Status == AcceptorStatus.Pending &&
                                        b.Type == AcceptorType.AccountingOperator &&
                                        b.Sequence < a.Sequence)) ||
                                p.Acceptors.Any(a =>
                                    !a.IsDeleted &&
                                    a.Status == AcceptorStatus.Pending &&
                                    a.Type == AcceptorType.AccountingApprover &&
                                    (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))) &&
                                    !p.Acceptors.Any(b =>
                                        !b.IsDeleted &&
                                        b.Status == AcceptorStatus.Pending &&
                                        b.Type == AcceptorType.AccountingOperator) &&

                                    (
                                        EmployeeConstant.OrganizationLevel.BranchLevels.Contains(p.Department.OrganizationLevel) ||
                                        p.Acceptors.Any(o => !o.IsDeleted && o.Type == AcceptorType.AccountingOperator)
                                    ) &&
                                    !p.Acceptors.Any(b =>
                                        !b.IsDeleted &&
                                        b.Status == AcceptorStatus.Pending &&
                                        b.Type == AcceptorType.AccountingApprover &&
                                        b.Sequence < a.Sequence)))
                            ) ||
                            (p.Status == P79Clause2Status.WaitingDisbursementDate &&
                             (p.Acceptors.Any(a =>
                                  !a.IsDeleted &&
                                  a.Type == AcceptorType.AccountingConfirmer &&
                                  (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId)))) ||
                              (!p.Acceptors.Any(c =>
                                  c.Type == AcceptorType.AccountingConfirmer &&
                                  !c.IsDeleted) &&
                               p.Acceptors.Any(a =>
                                   a.Type == AcceptorType.AccountingOperator &&
                                   !a.IsDeleted &&
                                   (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))))))) ||
                            (
                                isSegmentAccountingMember &&
                                p.Status == P79Clause2Status.WaitingAccountingApproval &&
                                !p.Acceptors.Any(a => !a.IsDeleted && a.Type == AcceptorType.AccountingOperator) &&
                                !p.Acceptors.Any(a => !a.IsDeleted && a.Type == AcceptorType.AccountingApprover && a.Status == AcceptorStatus.Approved) &&
                                !EmployeeConstant.OrganizationLevel.BranchLevels.Contains(p.Department.OrganizationLevel)
                            ) ||
                            (
                                isSegmentAccountingMember &&
                                p.Status == P79Clause2Status.WaitingDisbursementDate &&
                                !p.Acceptors.Any(a => !a.IsDeleted && a.Type == AcceptorType.AccountingConfirmer) &&
                                !EmployeeConstant.OrganizationLevel.BranchLevels.Contains(p.Department.OrganizationLevel)
                            ));
    }

    public IQueryable<PPettyCash> BuildPPettyCashBaseQuery(
        Dp2DbContext dbContext,
        IProcurementBaseQuery req,
        UserContext userContext)
    {
        var userIds = userContext.EffectiveUserIds.Select(e => e.UserId);
        var userId = userContext.User.Id.Value;

        return dbContext.PPettyCashs
                        .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), x => EF.Functions.ILike(x.Subject, $"%{req.Keyword}%") || EF.Functions.ILike((string)x.PettyCashNumber, $"%{req.Keyword}%"))
                        .WhereIfTrue(!string.IsNullOrWhiteSpace(req.DepartmentCode), x => x.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                        .WhereIfTrue(req.BudgetYear.HasValue, x => x.BudgetYear == req.BudgetYear)
                        .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodCode), x => x.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                        .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodTypeCode), x => x.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!))
                        .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodSpecialTypeCode), x => x.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
                        .Where(p =>
                            (
                                (p.Status == PettyCashStatus.Draft || p.Status == PettyCashStatus.Rejected ||
                                 p.Status == PettyCashStatus.Edit) &&
                                p.AuditInfo.CreatedBy == userId
                            ) ||
                            (
                                (p.Status == PettyCashStatus.WaitingApproval) &&
                                p.Acceptors.Any(a => a.Type == AcceptorType.DepartmentDirectorAgree && !a.IsDeleted &&
                                                     (
                                                         userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))
                                                     ) &&
                                                     a.Status == AcceptorStatus.Pending)
                            ) ||
                            (
                                (p.Status == PettyCashStatus.WaitingForInspector) &&
                                p.Acceptors.Any(a => a.Type == AcceptorType.InspectionCommittee && !a.IsDeleted && userIds.Contains(a.UserId) && a.Status == AcceptorStatus.Pending)
                            )
                            ||
                            ((p.Status == PettyCashStatus.WaitingForAssignment) &&
                             userIds.Contains(p.Assignees.OrderBy(a => a.Sequence).Last().UserId))
                            ||
                            ((p.Status == PettyCashStatus.WaitingForCompletion) &&
                             userIds.Contains(p.Assignees.OrderBy(a => a.Sequence).Last().UserId)));
    }

    public IQueryable<PPettyCashReimbursement> BuildPPettyCashReimbursementBaseQuery(
        Dp2DbContext dbContext,
        IProcurementBaseQuery req,
        UserContext userContext)
    {
        var userIds = userContext.EffectiveUserIds.Select(e => e.UserId);
        var userId = userContext.User.Id.Value;

        return dbContext.PPettyCashReimbursements
                        .Include(d => d.Department)
                        .Include(p => p.Items)!
                        .ThenInclude(i => i.PettyCashGlAccount)
                        .ThenInclude(g => g.PettyCash)
                        .ThenInclude(w => w.Department)
                        .Include(p => p.Acceptors)
                        .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), x => EF.Functions.ILike(x.Subject, $"%{req.Keyword}%") || EF.Functions.ILike(x.Number, $"%{req.Keyword}%"))
                        .WhereIfTrue(!string.IsNullOrWhiteSpace(req.DepartmentCode), x => x.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                        .Where(p =>
                            (
                                (p.Status == PPettyCashReimbursementStatus.Draft || p.Status == PPettyCashReimbursementStatus.Rejected ||
                                 p.Status == PPettyCashReimbursementStatus.Edit) &&
                                p.AuditInfo.CreatedBy == userId
                            ) ||
                            (
                                (p.Status == PPettyCashReimbursementStatus.WaitingApproval) &&
                                p.Acceptors.Any(a =>
                                    !a.IsDeleted &&
                                    a.Status == AcceptorStatus.Pending &&
                                    (a.Type == AcceptorType.Approver || a.Type == AcceptorType.DepartmentDirectorAgree) &&
                                    (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))) &&
                                    !p.Acceptors.Any(b =>
                                        !b.IsDeleted &&
                                        b.Status == AcceptorStatus.Pending &&
                                        (b.Type == AcceptorType.Approver || b.Type == AcceptorType.DepartmentDirectorAgree) &&
                                        b.Sequence < a.Sequence))
                            ) ||
                            (
                                (p.Status == PPettyCashReimbursementStatus.WaitingAccountingApproval) &&
                                (p.Acceptors.Any(a =>
                                    !a.IsDeleted &&
                                    a.Status == AcceptorStatus.Pending &&
                                    a.Type == AcceptorType.AccountingOperator &&
                                    (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))) &&
                                    !p.Acceptors.Any(b =>
                                        !b.IsDeleted &&
                                        b.Status == AcceptorStatus.Pending &&
                                        b.Type == AcceptorType.AccountingOperator &&
                                        b.Sequence < a.Sequence)) ||
                                p.Acceptors.Any(a =>
                                    !a.IsDeleted &&
                                    a.Status == AcceptorStatus.Pending &&
                                    a.Type == AcceptorType.AccountingApprover &&
                                    (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))) &&
                                    !p.Acceptors.Any(b =>
                                        !b.IsDeleted &&
                                        b.Status == AcceptorStatus.Pending &&
                                        b.Type == AcceptorType.AccountingOperator) &&
                                    !p.Acceptors.Any(b =>
                                        !b.IsDeleted &&
                                        b.Status == AcceptorStatus.Pending &&
                                        b.Type == AcceptorType.AccountingApprover &&
                                        b.Sequence < a.Sequence)))
                            ) ||
                            (p.Status == PPettyCashReimbursementStatus.WaitingDisbursementDate &&
                             (p.Acceptors.Any(a =>
                                  !a.IsDeleted &&
                                  a.Type == AcceptorType.AccountingConfirmer &&
                                  (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId)))) ||
                              (!p.Acceptors.Any(c =>
                                  c.Type == AcceptorType.AccountingConfirmer &&
                                  !c.IsDeleted) &&
                               p.Acceptors.Any(a =>
                                   a.Type == AcceptorType.AccountingOperator &&
                                   !a.IsDeleted &&
                                   (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))))))));
    }

    private static ProcurementItem MapPw119(Pw119 x) => new(
        x.Id.Value,
        "Pw119", // Type
        string.Empty, // PlanNumber - Pw119 doesn't have a Plan
        x.Pw119Number.Value, // ProcurementNumber
        x.Subject, // Name
        x.Budget,
        string.Empty, // PlanTypeName
        x.Department.Name,
        x.SupplyMethod.Label,
        string.Empty, // SupplyMethodType - Pw119 doesn't have this
        x.SupplyMethodSpecialType?.Label ?? string.Empty,
        x.W119Categories.Label, // RentTypeName
        ProcurementStep.Procurement, // Step - always Procurement for Pw119
        ProcessType.W119, // ProcessType - default for Pw119
        x.Status.ToString(),
        string.Empty); // Period

    private static ProcurementItem MapP79Clause2(P79Clause2 x) => new(
        x.Id.Value,
        "P79Clause2", // Type
        string.Empty, // PlanNumber - P79Clause2 doesn't have a Plan
        x.P79Clause2Number.Value, // ProcurementNumber
        x.Subject, // Name
        x.Budget,
        string.Empty, // PlanTypeName
        x.Department.Name,
        x.SupplyMethod.Label,
        x.SupplyMethodType?.Label ?? string.Empty,
        x.SupplyMethodSpecialType?.Label ?? string.Empty,
        string.Empty, // RentTypeName - P79Clause2 doesn't have this concept
        ProcurementStep.Procurement, // Step - always Procurement for P79Clause2
        ProcessType.P79Clause2, // ProcessType - default for P79Clause2
        x.Status.ToString(),
        string.Empty); // Period

    private static ProcurementItem MapPettyCash(PPettyCash x) => new(
        x.Id.Value,
        "PettyCash", // Type
        string.Empty, // PlanNumber - PettyCash doesn't have a Plan
        x.PettyCashNumber.Value, // ProcurementNumber
        x.Subject, // Name
        x.Budget,
        string.Empty, // PlanTypeName
        x.Department.Name,
        x.SupplyMethod.Label,
        x.SupplyMethodType?.Label ?? string.Empty,
        x.SupplyMethodSpecialType?.Label ?? string.Empty,
        string.Empty, // RentTypeName - PettyCash doesn't have this concept
        ProcurementStep.Procurement, // Step - always Procurement for PettyCash
        ProcessType.PettyCash, // ProcessType - default for PettyCash
        x.Status.ToString(),
        string.Empty); // Period

    private static ProcurementItem MapPettyCashReimbursement(PPettyCashReimbursement x) => new(
        x.Id.Value,
        "PettyCashReimbursement", // Type
        string.Empty, // PlanNumber - PettyCashReimbursement doesn't have a Plan
        x.Number, // ProcurementNumber
        x.Subject, // Name
        x.Items?.SelectMany(i => i.PettyCashGlAccount.PettyCash.Vendors)?.SelectMany(v => v.VendorParcels)?.Sum(vp => vp.TotalPriceVat) ?? 0, // Budget - PettyCashReimbursement doesn't have budget
        string.Empty, // PlanTypeName
        x.Department.Name,
        string.Empty, // SupplyMethod - PettyCashReimbursement doesn't have this
        string.Empty, // SupplyMethodType - PettyCashReimbursement doesn't have this
        string.Empty, // SupplyMethodSpecialType - PettyCashReimbursement doesn't have this
        string.Empty, // RentTypeName - PettyCashReimbursement doesn't have this concept
        ProcurementStep.Procurement, // Step - always Procurement for PettyCashReimbursement
        ProcessType.PettyCashReimbursement, // ProcessType - default for PettyCashReimbursement
        x.Status.ToString(),
        string.Empty); // Period
}