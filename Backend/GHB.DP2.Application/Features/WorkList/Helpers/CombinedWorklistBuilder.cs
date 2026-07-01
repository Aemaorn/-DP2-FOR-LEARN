namespace GHB.DP2.Application.Features.WorkList.Helpers;

using System.Linq;
using EFCoreSecondLevelCacheInterceptor;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Features.WorkList.Programs;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PExpenseDisbursement;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Infrastructure;
using Microsoft.EntityFrameworkCore;

internal sealed class NcdProcurementProjection
{
    public ProcurementStep Step { get; set; }

    public ProcurementType Type { get; set; }

    public Guid Id { get; set; }

    public string ProcurementNumber { get; set; } = string.Empty;

    public string? PlanNumber { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? BudgetYear { get; set; }

    public decimal Budget { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public string SupplyMethodLabel { get; set; } = string.Empty;

    public ProcessType ProcessType { get; set; }

    public ProcurementStatus ProcurementStatus { get; set; }

    public DateTimeOffset SortAt { get; set; }

    public string? AppointStatus { get; set; }

    public string? TorDraftStatus { get; set; }

    public string? MedianPriceStatus { get; set; }

    public string? PrincipleApprovalStatus { get; set; }

    public string? PrincipleApprovalRentalStatus { get; set; }

    public string? PurchaseRequisitionStatus { get; set; }

    public string? Jp005Status { get; set; }

    public string? InviteStatus { get; set; }

    public string? PurchaseOrderStatus { get; set; }

    public string? PurchaseOrderApprovalStatus { get; set; }

    public string? ContractInvitationStatus { get; set; }
}

internal sealed class CombinedWorklistBuilder
{
    private readonly IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory;
    private readonly PlanProgram planProgram;
    private readonly PlanAnnouncementProgram planAnnouncementProgram;
    private readonly ProcurementProgram procurementProgram;
    private readonly ContractManagementProgram contractManagementProgram;
    private readonly ExpenseDisbursementProgram expenseDisbursementProgram;

    public CombinedWorklistBuilder(
        IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory,
        PlanProgram planProgram,
        PlanAnnouncementProgram planAnnouncementProgram,
        ProcurementProgram procurementProgram,
        ContractManagementProgram contractManagementProgram,
        ExpenseDisbursementProgram expenseDisbursementProgram)
    {
        this.dbContextFactory = dbContextFactory;
        this.planProgram = planProgram;
        this.planAnnouncementProgram = planAnnouncementProgram;
        this.procurementProgram = procurementProgram;
        this.contractManagementProgram = contractManagementProgram;
        this.expenseDisbursementProgram = expenseDisbursementProgram;
    }

    public async Task<PaginatedQueryResult<CombinedWorklistItem>?> BuildCombinedWorklistAsync(
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        bool isSegmentAccountingMember,
        CancellationToken ct)
    {
        var topN = Math.Max(req.PageNumber, 1) * req.PageSize;

        var planTask = this.ExecutePlanQueryAsync(req, userContext, topN, ct);
        var announcementTask = this.ExecuteAnnouncementQueryAsync(req, userContext, topN, ct);
        var procurementTask = this.ExecuteProcurementQueryAsync(req, userContext, topN, ct);
        var pw119Task = this.ExecutePw119QueryAsync(req, userContext, isSegmentAccountingMember, topN, ct);
        var p79Clause2Task = this.ExecuteP79Clause2QueryAsync(req, userContext, isSegmentAccountingMember, topN, ct);
        var pettyCashTask = this.ExecutePettyCashQueryAsync(req, userContext, topN, ct);
        var pettyCashReimbursementTask = this.ExecutePettyCashReimbursementQueryAsync(req, userContext, topN, ct);
        var deliveryAcceptanceTask = this.ExecuteDeliveryAcceptanceQueryAsync(req, userContext, isSegmentAccountingMember, topN, ct);
        var contractTerminationTask = this.ExecuteContractTerminationQueryAsync(req, userContext, topN, ct);
        var contractGuaranteeReturnTask = this.ExecuteContractGuaranteeReturnQueryAsync(req, userContext, topN, ct);
        var expenseDisbursementTask = this.ExecuteExpenseDisbursementQueryAsync(req, userContext, topN, ct);
        var contractDraftVendorEditTask = this.ExecuteContractDraftVendorEditQueryAsync(req, userContext, topN, ct);

        await Task.WhenAll(
            planTask,
            announcementTask,
            procurementTask,
            pw119Task,
            p79Clause2Task,
            pettyCashTask,
            pettyCashReimbursementTask,
            deliveryAcceptanceTask,
            contractTerminationTask,
            contractGuaranteeReturnTask,
            expenseDisbursementTask,
            contractDraftVendorEditTask);

        var planResult = await planTask;
        var announcementResult = await announcementTask;
        var procurementResult = await procurementTask;
        var pw119Result = await pw119Task;
        var p79Clause2Result = await p79Clause2Task;
        var pettyCashResult = await pettyCashTask;
        var pettyCashReimbursementResult = await pettyCashReimbursementTask;
        var deliveryAcceptanceResult = await deliveryAcceptanceTask;
        var contractTerminationResult = await contractTerminationTask;
        var contractGuaranteeReturnResult = await contractGuaranteeReturnTask;
        var expenseDisbursementResult = await expenseDisbursementTask;
        var contractDraftVendorEditResult = await contractDraftVendorEditTask;

        var totalCount = planResult.Total + announcementResult.Total + procurementResult.Total +
                         pw119Result.Total + p79Clause2Result.Total + pettyCashResult.Total +
                         pettyCashReimbursementResult.Total + deliveryAcceptanceResult.Total +
                         contractTerminationResult.Total + contractGuaranteeReturnResult.Total +
                         expenseDisbursementResult.Total + contractDraftVendorEditResult.Total;

        var combinedItems = planResult.Items
                                      .Concat(announcementResult.Items)
                                      .Concat(procurementResult.Items)
                                      .Concat(pw119Result.Items)
                                      .Concat(p79Clause2Result.Items)
                                      .Concat(pettyCashResult.Items)
                                      .Concat(pettyCashReimbursementResult.Items)
                                      .Concat(deliveryAcceptanceResult.Items)
                                      .Concat(contractTerminationResult.Items)
                                      .Concat(contractGuaranteeReturnResult.Items)
                                      .Concat(expenseDisbursementResult.Items)
                                      .Concat(contractDraftVendorEditResult.Items)
                                      .OrderByDescending(x => x.SortAt)
                                      .ThenByDescending(x => x.Number)
                                      .ToList();

        var skip = Math.Max(0, (req.PageNumber - 1) * req.PageSize);
        var pageItems = combinedItems.Skip(skip).Take(req.PageSize).ToList();

        return new PaginatedQueryResult<CombinedWorklistItem>(pageItems, totalCount);
    }

    private async Task<(List<CombinedWorklistItem> Items, int Total)> ExecutePlanQueryAsync(
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        int topN,
        CancellationToken ct)
    {
        await using var dbContext = await this.dbContextFactory.CreateDbContextAsync(ct);
        var baseQuery = this.planProgram.BuildPlanBaseQuery(dbContext, req, userContext).AsNoTracking();

        var items = await baseQuery
                          .OrderByDescending(p => p.AuditInfo.LastModifiedAt ?? p.AuditInfo.CreatedAt)
                          .Select(p => new CombinedWorklistItem(
                              "Plan",
                              string.Empty,
                              p.Id.Value,
                              null,
                              p.PlanNumber.Value,
                              p.Name,
                              p.BudgetYear,
                              p.Budget,
                              null,
                              p.Department.Name,
                              p.SupplyMethod.Label,
                              p.Status.ToString(),
                              null,
                              p.AuditInfo.LastModifiedAt ?? p.AuditInfo.CreatedAt,
                              null,
                              null,
                              null,
                              null,
                              null))
                          .Take(topN)
                          .ToListAsync(ct);

        var total = await baseQuery.CountAsync(ct);
        return (items, total);
    }

    private async Task<(List<CombinedWorklistItem> Items, int Total)> ExecuteAnnouncementQueryAsync(
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        int topN,
        CancellationToken ct)
    {
        await using var dbContext = await this.dbContextFactory.CreateDbContextAsync(ct);
        var baseQuery = this.planAnnouncementProgram.BuildPlanAnnouncementBaseQuery(dbContext, req, userContext.EffectiveUserIds).AsNoTracking();

        var items = await baseQuery
                          .OrderByDescending(a => a.AnnouncementDate ?? a.AuditInfo.LastModifiedAt ?? a.AuditInfo.CreatedAt)
                          .Select(a => new CombinedWorklistItem(
                              "PlanAnnouncement",
                              string.Empty,
                              a.Id.Value,
                              null,
                              a.PlanAnnouncementNumber.Value,
                              a.AnnouncementTitle,
                              a.Year,
                              a.AnnouncementSelectedInformations.Select(s => s.Plan).Sum(p => p.Budget),
                              a.AnnouncementSelectedInformations.Count,
                              null,
                              a.SupplyMethodInfo.Label,
                              a.Status.ToString(),
                              null,
                              a.AnnouncementDate ?? a.AuditInfo.LastModifiedAt ?? a.AuditInfo.CreatedAt,
                              null,
                              null,
                              null,
                              null,
                              null))
                          .Take(topN)
                          .ToListAsync(ct);

        var total = await baseQuery.CountAsync(ct);
        return (items, total);
    }

    private async Task<(List<CombinedWorklistItem> Items, int Total)> ExecuteProcurementQueryAsync(
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        int topN,
        CancellationToken ct)
    {
        // Step 1: evaluate access predicate ONCE — get all accessible procurement IDs + sort/group keys
        await using var ctxIds = await this.dbContextFactory.CreateDbContextAsync(ct);
        var allAccessible = await this.procurementProgram.BuildProcurementBaseQuery(ctxIds, req, userContext)
            .Where(p =>
                p.Step == ProcurementStep.PreProcurement ||
                p.Step == ProcurementStep.Procurement ||
                p.Step == ProcurementStep.ContractAgreement)
            .NotCacheable()
            .AsNoTracking()
            .Select(p => new
            {
                Id = p.Id,
                IsContractDraft = p.ProcessType == ProcessType.ContractDraft,
                SortAt = p.AuditInfo.LastModifiedAt ?? p.AuditInfo.CreatedAt,
            })
            .ToListAsync(ct);

        var contractDraftCount = allAccessible.Count(a => a.IsContractDraft);
        var nonContractDraftCount = allAccessible.Count - contractDraftCount;

        var cdIds = allAccessible.Where(a => a.IsContractDraft).OrderByDescending(a => a.SortAt).Take(topN).Select(a => a.Id).ToList();
        var ncdIds = allAccessible.Where(a => !a.IsContractDraft).OrderByDescending(a => a.SortAt).Take(topN).Select(a => a.Id).ToList();

        // Step 2: hydrate cd + ncd in parallel via cheap WHERE Id IN (...) lookups
        await using var ctxCd = await this.dbContextFactory.CreateDbContextAsync(ct);
        await using var ctxNcd = await this.dbContextFactory.CreateDbContextAsync(ct);

        var cdListTask = cdIds.Count == 0
            ? Task.FromResult(new List<Procurement>())
            : ctxCd.Procurements
                   .Where(p => cdIds.Contains(p.Id))
                   .OrderByDescending(p => p.AuditInfo.LastModifiedAt ?? p.AuditInfo.CreatedAt)
                   .Include(x => x.Department)
                   .Include(x => x.SupplyMethod)
                   .Include(x => x.Plan)
                   .Include(x => x.ContractDrafts)
                       .ThenInclude(cd => cd.Vendors)
                       .ThenInclude(v => v.ContractInvitationVendors)
                       .ThenInclude(civ => civ.PurchaseOrderApprovalContract)
                   .AsSplitQuery()
                   .NotCacheable()
                   .AsNoTracking()
                   .ToListAsync(ct);

        var ncdListTask = ncdIds.Count == 0
            ? Task.FromResult(new List<NcdProcurementProjection>())
            : ctxNcd.Procurements
                    .Where(p => ncdIds.Contains(p.Id))
                    .OrderByDescending(p => p.AuditInfo.LastModifiedAt ?? p.AuditInfo.CreatedAt)
                    .NotCacheable()
                    .AsNoTracking()
                    .Select(p => new NcdProcurementProjection
                    {
                        Step = p.Step,
                        Type = p.Type,
                        Id = p.Id.Value,
                        ProcurementNumber = p.ProcurementNumber != null ? p.ProcurementNumber.Value.ToString() : string.Empty,
                        PlanNumber = p.Plan != null ? p.Plan.PlanNumber.Value : null,
                        Name = p.Name,
                        BudgetYear = p.BudgetYear,
                        Budget = p.Type == ProcurementType.Procurement
                            ? p.Budget ?? 0
                            : p.PrincipleApprovals.OrderBy(x => x.Id).Select(x => (decimal?)x.TotalRentalAmount).FirstOrDefault() ?? 0,
                        DepartmentName = p.Department.Name,
                        SupplyMethodLabel = p.SupplyMethod.Label,
                        ProcessType = p.ProcessType,
                        ProcurementStatus = p.Status,
                        SortAt = p.AuditInfo.LastModifiedAt ?? p.AuditInfo.CreatedAt,
                        AppointStatus = p.Appoints.Select(x => x.Status.ToString()).FirstOrDefault(),
                        TorDraftStatus = p.TorDrafts.Select(x => x.Status.ToString()).FirstOrDefault(),
                        MedianPriceStatus = p.MedianPrices.Select(x => x.Status.ToString()).FirstOrDefault(),
                        PrincipleApprovalStatus = p.PrincipleApprovals.Select(x => x.Status.ToString()).FirstOrDefault(),
                        PrincipleApprovalRentalStatus = p.PrincipleApprovalRentals.Select(x => x.Status.ToString()).FirstOrDefault(),
                        PurchaseRequisitionStatus = p.PurchaseRequisitions.Select(x => x.Status.ToString()).FirstOrDefault(),
                        Jp005Status = p.Jp005.Select(x => x.Status.ToString()).FirstOrDefault(),
                        InviteStatus = p.Invites.Select(x => x.Status.ToString()).FirstOrDefault(),
                        PurchaseOrderStatus = p.PurchaseOrder.Select(x => x.Status.ToString()).FirstOrDefault(),
                        PurchaseOrderApprovalStatus = p.PurchaseOrderApprovals.Select(x => x.Status.ToString()).FirstOrDefault(),
                        ContractInvitationStatus = p.ContractInvitations.Select(x => x.Status.ToString()).FirstOrDefault(),
                    })
                    .ToListAsync(ct);

        await Task.WhenAll(cdListTask, ncdListTask);

        var contractDraftProcurements = await cdListTask;
        var nonContractDraftProcurements = await ncdListTask;

        var items = new List<CombinedWorklistItem>();

        foreach (var p in contractDraftProcurements)
        {
            if (!p.ContractDrafts.Any())
            {
                continue;
            }

            var contractDraft = p.ContractDrafts.FirstOrDefault()!;

            foreach (var vendor in contractDraft.Vendors)
            {
                var vendorName = $"{vendor.ContractNumber}";

                if (vendor.ContractInvitationVendors.PurchaseOrderApprovalContract.Entrepreneur != null ||
                    vendor.ContractInvitationVendors.PurchaseOrderApprovalContract.PrincipleApprovalRentalEntrepreneurs != null)
                {
                    vendorName =
                        $"{vendor.ContractNumber} : {vendor.ContractInvitationVendors.PurchaseOrderApprovalContract.Entrepreneur?.SuVendor.EstablishmentName}{vendor.ContractInvitationVendors.PurchaseOrderApprovalContract.PrincipleApprovalRentalEntrepreneurs?.Vendor.EstablishmentName}";
                }

                items.Add(new CombinedWorklistItem(
                    p.Step.ToString(),
                    p.Type.ToString(),
                    p.Id.Value,
                    null,
                    p.ProcurementNumber != null ? p.ProcurementNumber.Value.ToString() : string.Empty,
                    p.Name,
                    p.BudgetYear,
                    vendor.Budget,
                    null,
                    p.Department.Name,
                    p.SupplyMethod.Label,
                    vendor.Status.ToString(),
                    p.Step.ToString(),
                    p.AuditInfo.LastModifiedAt ?? p.AuditInfo.CreatedAt,
                    p.ProcessType.ToString(),
                    vendor.Id.Value,
                    ResolveBudgetDescription(p.Type, vendor.ContractInvitationVendors.PurchaseOrderApprovalContract) ?? vendorName,
                    PlanNumber: p.Plan != null ? p.Plan.PlanNumber.Value : null));
            }
        }

        foreach (var p in nonContractDraftProcurements)
        {
            var childStatus = p.ProcessType switch
            {
                ProcessType.Appoint => p.AppointStatus ?? p.ProcurementStatus.ToString(),
                ProcessType.TorDraft => p.TorDraftStatus ?? p.ProcurementStatus.ToString(),
                ProcessType.MedianPrice => p.MedianPriceStatus ?? p.ProcurementStatus.ToString(),
                ProcessType.PrincipleApproval => p.PrincipleApprovalStatus ?? p.ProcurementStatus.ToString(),
                ProcessType.PrincipleApprovalRental => p.PrincipleApprovalRentalStatus ?? p.ProcurementStatus.ToString(),
                ProcessType.PurchaseRequisition => p.PurchaseRequisitionStatus ?? p.ProcurementStatus.ToString(),
                ProcessType.Jp005 => p.Jp005Status ?? p.ProcurementStatus.ToString(),
                ProcessType.Invite => p.InviteStatus ?? p.ProcurementStatus.ToString(),
                ProcessType.PurchaseOrder => p.PurchaseOrderStatus ?? p.ProcurementStatus.ToString(),
                ProcessType.PurchaseOrderApproval => p.PurchaseOrderApprovalStatus ?? p.ProcurementStatus.ToString(),
                ProcessType.ContractInvitation => p.ContractInvitationStatus ?? p.ProcurementStatus.ToString(),
                _ => p.ProcurementStatus.ToString(),
            };

            items.Add(new CombinedWorklistItem(
                p.Step.ToString(),
                p.Type.ToString(),
                p.Id,
                null,
                p.ProcurementNumber,
                p.Name,
                p.BudgetYear,
                p.Budget,
                null,
                p.DepartmentName,
                p.SupplyMethodLabel,
                childStatus,
                p.Step.ToString(),
                p.SortAt,
                p.ProcessType.ToString(),
                PlanNumber: p.PlanNumber));
        }

        return (items, contractDraftCount + nonContractDraftCount);
    }

    private static string? ResolveBudgetDescription(ProcurementType type, PPurchaseOrderApprovalContract contract)
    {
        if (type == ProcurementType.Rent && contract.PrincipleApprovalRentalBudget is not null)
        {
            return contract.PrincipleApprovalRentalBudget.Description;
        }

        if (type == ProcurementType.Procurement && contract.Budget is not null)
        {
            return contract.Budget.Description;
        }

        if (contract.PpPurchaseRequisitionBudget is not null)
        {
            return contract.PpPurchaseRequisitionBudget.Description;
        }

        if (contract.PPurchaseOrderApprovalBudget is not null)
        {
            return contract.PPurchaseOrderApprovalBudget.Description;
        }

        return null;
    }

    private async Task<(List<CombinedWorklistItem> Items, int Total)> ExecutePw119QueryAsync(
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        bool isSegmentAccountingMember,
        int topN,
        CancellationToken ct)
    {
        await using var dbContext = await this.dbContextFactory.CreateDbContextAsync(ct);
        var baseQuery = this.procurementProgram.BuildPw119BaseQuery(dbContext, req, userContext, isSegmentAccountingMember).AsNoTracking();

        var rows = await baseQuery
                         .OrderByDescending(p => p.AuditInfo.LastModifiedAt ?? p.AuditInfo.CreatedAt)
                         .Select(p => new
                         {
                             Id = p.Id.Value,
                             Number = p.Pw119Number.Value.ToString(),
                             p.Subject,
                             p.BudgetYear,
                             p.Budget,
                             DepartmentName = p.Department.Name,
                             SupplyMethodLabel = p.SupplyMethod.Label,
                             Status = p.Status.ToString(),
                             SortAt = p.AuditInfo.LastModifiedAt ?? p.AuditInfo.CreatedAt,
                             GLAccountLabels = p.GLAccounts.Select(g => g.GLAccount.Label).ToList(),
                         })
                         .Take(topN)
                         .ToListAsync(ct);

        var items = rows
                    .Select(p => new CombinedWorklistItem(
                        "Procurement",
                        "Pw119",
                        p.Id,
                        null,
                        p.Number,
                        p.Subject,
                        p.BudgetYear,
                        p.Budget,
                        null,
                        p.DepartmentName,
                        p.SupplyMethodLabel,
                        p.Status,
                        "Procurement",
                        p.SortAt,
                        "W119",
                        null,
                        null,
                        p.GLAccountLabels.Distinct().ToList()))
                    .ToList();

        var total = await baseQuery.CountAsync(ct);
        return (items, total);
    }

    private async Task<(List<CombinedWorklistItem> Items, int Total)> ExecuteP79Clause2QueryAsync(
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        bool isSegmentAccountingMember,
        int topN,
        CancellationToken ct)
    {
        await using var dbContext = await this.dbContextFactory.CreateDbContextAsync(ct);
        var baseQuery = this.procurementProgram.BuildP79Clause2BaseQuery(dbContext, req, userContext, isSegmentAccountingMember).AsNoTracking();

        var rows = await baseQuery
                         .OrderByDescending(p => p.AuditInfo.LastModifiedAt ?? p.AuditInfo.CreatedAt)
                         .Select(p => new
                         {
                             Id = p.Id.Value,
                             Number = p.P79Clause2Number.Value.ToString(),
                             p.Subject,
                             p.BudgetYear,
                             p.Budget,
                             DepartmentName = p.Department.Name,
                             SupplyMethodLabel = p.SupplyMethod.Label,
                             Status = p.Status.ToString(),
                             SortAt = p.AuditInfo.LastModifiedAt ?? p.AuditInfo.CreatedAt,
                             GLAccountLabels = p.GLAccounts.Select(g => g.GLAccount.Label).ToList(),
                         })
                         .Take(topN)
                         .ToListAsync(ct);

        var items = rows
                    .Select(p => new CombinedWorklistItem(
                        "Procurement",
                        "P79Clause2",
                        p.Id,
                        null,
                        p.Number,
                        p.Subject,
                        p.BudgetYear,
                        p.Budget,
                        null,
                        p.DepartmentName,
                        p.SupplyMethodLabel,
                        p.Status,
                        "Procurement",
                        p.SortAt,
                        "P79Clause2",
                        null,
                        null,
                        p.GLAccountLabels.Distinct().ToList()))
                    .ToList();

        var total = await baseQuery.CountAsync(ct);
        return (items, total);
    }

    private async Task<(List<CombinedWorklistItem> Items, int Total)> ExecutePettyCashQueryAsync(
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        int topN,
        CancellationToken ct)
    {
        await using var dbContext = await this.dbContextFactory.CreateDbContextAsync(ct);
        var baseQuery = this.procurementProgram.BuildPPettyCashBaseQuery(dbContext, req, userContext).AsNoTracking();

        var items = await baseQuery
                          .OrderByDescending(p => p.AuditInfo.LastModifiedAt ?? p.AuditInfo.CreatedAt)
                          .Select(p => new CombinedWorklistItem(
                              "Procurement",
                              "PettyCash",
                              p.Id.Value,
                              null,
                              p.PettyCashNumber.Value.ToString(),
                              p.Subject,
                              p.BudgetYear,
                              p.Budget,
                              null,
                              p.Department.Name,
                              p.SupplyMethod.Label,
                              p.Status.ToString(),
                              "Procurement",
                              p.AuditInfo.LastModifiedAt ?? p.AuditInfo.CreatedAt,
                              "PettyCash",
                              null,
                              null,
                              null,
                              null))
                          .Take(topN)
                          .ToListAsync(ct);

        var total = await baseQuery.CountAsync(ct);
        return (items, total);
    }

    private async Task<(List<CombinedWorklistItem> Items, int Total)> ExecutePettyCashReimbursementQueryAsync(
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        int topN,
        CancellationToken ct)
    {
        await using var dbContext = await this.dbContextFactory.CreateDbContextAsync(ct);
        var baseQuery = this.procurementProgram.BuildPPettyCashReimbursementBaseQuery(dbContext, req, userContext);

        var reimbursements = await baseQuery
                                   .OrderByDescending(p => p.AuditInfo.LastModifiedAt ?? p.AuditInfo.CreatedAt)
                                   .Include(x => x.Department)
                                   .Include(x => x.Items)
                                   .ThenInclude(x => x.PettyCashGlAccount)
                                   .ThenInclude(x => x.PettyCash)
                                   .ThenInclude(x => x.Vendors)
                                   .ThenInclude(x => x.VendorParcels)
                                   .AsNoTracking()
                                   .Take(topN)
                                   .ToListAsync(ct);

        var total = await baseQuery.CountAsync(ct);

        var items = new List<CombinedWorklistItem>();

        foreach (var p in reimbursements)
        {
            var amount = p.Items?
                          .SelectMany(i => i.PettyCashGlAccount.PettyCash.Vendors)
                          .SelectMany(v => v.VendorParcels)
                          .Sum(vp => vp.TotalPriceVat) ?? 0m;

            items.Add(new CombinedWorklistItem(
                "Procurement",
                "PettyCashReimbursement",
                p.Id.Value,
                null,
                p.Number,
                p.Subject,
                null,
                amount,
                null,
                p.Department.Name,
                string.Empty,
                p.Status.ToString(),
                "Procurement",
                p.AuditInfo.LastModifiedAt ?? p.AuditInfo.CreatedAt,
                "PettyCashReimbursement"));
        }

        return (items, total);
    }

    private async Task<(List<CombinedWorklistItem> Items, int Total)> ExecuteDeliveryAcceptanceQueryAsync(
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        bool isSegmentAccountingMember,
        int topN,
        CancellationToken ct)
    {
        await using var dbContext = await this.dbContextFactory.CreateDbContextAsync(ct);
        var keywordDeliveryAcceptanceIds = await this.contractManagementProgram.GetDeliveryAcceptanceKeywordIdsAsync(dbContext, req.Keyword, ct);
        var segmentEligiblePeriodIds = await this.contractManagementProgram.GetSegmentEligiblePeriodIdsAsync(dbContext, isSegmentAccountingMember, ct);
        var baseQuery = this.contractManagementProgram
                            .BuildDeliveryAcceptanceBaseQuery(dbContext, userContext, keywordDeliveryAcceptanceIds, segmentEligiblePeriodIds);

        var deliveryPeriods = await baseQuery
                                    .OrderByDescending(r => r.AuditInfo.LastModifiedAt ?? r.AuditInfo.CreatedAt)
                                    .Include(r => r.CmDeliveryAcceptance)
                                        .ThenInclude(da => da.Department)
                                    .Include(r => r.CmDeliveryAcceptance)
                                        .ThenInclude(da => da.SupplyMethod)
                                    .Include(r => r.PaymentTerms)
                                    .Include(r => r.Budgets)
                                        .ThenInclude(b => b.AccountNo)
                                    .AsNoTracking()
                                    .Take(topN)
                                    .ToListAsync(ct);

        var total = await baseQuery.CountAsync(ct);

        var planRefIds = deliveryPeriods
                         .Where(p => p.CmDeliveryAcceptance.SourceType == SourceType.Plan)
                         .Select(p => PlanId.From((Guid)p.CmDeliveryAcceptance.RefId))
                         .Distinct()
                         .ToList();

        var contractDraftVendorRefIds = deliveryPeriods
                                        .Where(p => p.CmDeliveryAcceptance.SourceType == SourceType.ContractDraftVendor)
                                        .Select(p => ContractDraftVendorId.From((Guid)p.CmDeliveryAcceptance.RefId))
                                        .Distinct()
                                        .ToList();

        var plans = planRefIds.Count != 0
            ? await dbContext.Plans
                             .Include(p => p.Department)
                             .Include(p => p.SupplyMethod)
                             .Where(p => planRefIds.Contains(p.Id))
                             .AsNoTracking()
                             .ToDictionaryAsync(p => p.Id.Value, ct)
            : new Dictionary<Guid, Plan>();

        var contractDraftVendors = contractDraftVendorRefIds.Count != 0
            ? await dbContext.CaContractDraftVendors
                             .Include(cdv => cdv.ContractDraft)
                             .ThenInclude(cd => cd.Procurement)
                             .ThenInclude(p => p.Department)
                             .Include(cdv => cdv.ContractDraft)
                             .ThenInclude(cd => cd.Procurement)
                             .ThenInclude(p => p.SupplyMethod)
                             .Include(cdv => cdv.Vendor)
                             .ThenInclude(v => v.VendorInfo)
                             .Where(cdv => contractDraftVendorRefIds.Contains(cdv.Id))
                             .AsNoTracking()
                             .ToDictionaryAsync(cdv => cdv.Id.Value, ct)
            : new Dictionary<Guid, CaContractDraftVendor>();

        var poaRefIds = deliveryPeriods
                        .Where(p => p.CmDeliveryAcceptance.SourceType == SourceType.Procurement)
                        .Select(p => PurchaseOrderApprovalId.From((Guid)p.CmDeliveryAcceptance.RefId))
                        .Distinct()
                        .ToList();

        var purchaseOrderApprovals = poaRefIds.Count != 0
            ? await dbContext.PPurchaseOrderApprovals
                             .Include(poa => poa.Procurement).ThenInclude(p => p.Plan)
                             .Include(poa => poa.Procurement).ThenInclude(p => p.Department)
                             .Include(poa => poa.Procurement).ThenInclude(p => p.SupplyMethod)
                             .Where(poa => poaRefIds.Contains(poa.Id))
                             .AsNoTracking()
                             .ToDictionaryAsync(poa => poa.Id.Value, ct)
            : new Dictionary<Guid, PPurchaseOrderApproval>();

        var items = new List<CombinedWorklistItem>();

        foreach (var period in deliveryPeriods)
        {
            var deliveryAcceptance = period.CmDeliveryAcceptance;

            var glAccounts = (period.Budgets ?? [])
                             .Where(b => b.AccountNo != null)
                             .Select(b => b.AccountNo.Label)
                             .Distinct()
                             .ToList();

            if (deliveryAcceptance.SourceType == SourceType.Plan)
            {
                plans.TryGetValue((Guid)deliveryAcceptance.RefId, out var planData);

                items.Add(new CombinedWorklistItem(
                    "DeliveryPeriods",
                    nameof(CmProcessType.DeliveryAcceptance),
                    period.Id.Value,
                    period.CmDeliveryAcceptanceId.Value,
                    planData?.PlanNumber.Value ?? string.Empty,
                    $"{planData?.Name ?? string.Empty} งวดที่ {string.Join(",", (period.PaymentTerms ?? []).Select(x => x.PaymentTerm))}",
                    planData?.BudgetYear,
                    planData?.Budget ?? 0m,
                    null,
                    planData?.Department?.Name ?? string.Empty,
                    planData?.SupplyMethod?.Label ?? string.Empty,
                    period.Status == CmDeliveryAcceptancePeriodStatus.Approved ? period.AccountStatus.ToString() : period.Status.ToString(),
                    nameof(CmProcessType.DeliveryAcceptance),
                    period.AuditInfo.LastModifiedAt ?? period.AuditInfo.CreatedAt,
                    "contractReceive",
                    GLAccounts: glAccounts));
            }
            else if (deliveryAcceptance.SourceType == SourceType.Procurement)
            {
                purchaseOrderApprovals.TryGetValue((Guid)deliveryAcceptance.RefId, out var poaData);
                var proc = poaData?.Procurement;

                items.Add(new CombinedWorklistItem(
                    "DeliveryPeriods",
                    nameof(CmProcessType.DeliveryAcceptance),
                    period.Id.Value,
                    period.CmDeliveryAcceptanceId.Value,
                    proc?.ProcurementNumber.Value.ToString() ?? string.Empty,
                    $"{proc?.Name ?? string.Empty} งวดที่ {string.Join(",", (period.PaymentTerms ?? []).Select(x => x.PaymentTerm))}",
                    proc?.BudgetYear,
                    proc?.Plan?.Budget ?? 0m,
                    null,
                    proc?.Department?.Name ?? string.Empty,
                    proc?.SupplyMethod?.Label ?? string.Empty,
                    period.Status == CmDeliveryAcceptancePeriodStatus.Approved ? period.AccountStatus.ToString() : period.Status.ToString(),
                    nameof(CmProcessType.DeliveryAcceptance),
                    period.AuditInfo.LastModifiedAt ?? period.AuditInfo.CreatedAt,
                    "contractReceive",
                    GLAccounts: glAccounts));
            }
            else if (deliveryAcceptance.SourceType == SourceType.Manual)
            {
                items.Add(new CombinedWorklistItem(
                    "DeliveryPeriods",
                    nameof(CmProcessType.DeliveryAcceptance),
                    period.Id.Value,
                    period.CmDeliveryAcceptanceId.Value,
                    deliveryAcceptance.Number ?? string.Empty,
                    $"{deliveryAcceptance.Name ?? string.Empty} งวดที่ {string.Join(",", (period.PaymentTerms ?? []).Select(x => x.PaymentTerm))}",
                    null,
                    deliveryAcceptance.Budget ?? 0m,
                    null,
                    deliveryAcceptance.Department?.Name ?? string.Empty,
                    deliveryAcceptance.SupplyMethod?.Label ?? string.Empty,
                    period.Status == CmDeliveryAcceptancePeriodStatus.Approved ? period.AccountStatus.ToString() : period.Status.ToString(),
                    nameof(CmProcessType.DeliveryAcceptance),
                    period.AuditInfo.LastModifiedAt ?? period.AuditInfo.CreatedAt,
                    "contractReceive",
                    GLAccounts: glAccounts));
            }
            else
            {
                contractDraftVendors.TryGetValue((Guid)deliveryAcceptance.RefId, out var contractDraftVendor);

                items.Add(new CombinedWorklistItem(
                    "DeliveryPeriods",
                    nameof(CmProcessType.DeliveryAcceptance),
                    period.Id.Value,
                    period.CmDeliveryAcceptanceId.Value,
                    contractDraftVendor?.ContractNumber ?? string.Empty,
                    $"{contractDraftVendor?.ContractName ?? string.Empty} งวดที่ {string.Join(",", (period.PaymentTerms ?? []).Select(x => x.PaymentTerm))}",
                    null,
                    contractDraftVendor?.Budget ?? 0m,
                    null,
                    contractDraftVendor?.ContractDraft?.Procurement?.Department?.Name ?? string.Empty,
                    contractDraftVendor?.ContractDraft?.Procurement?.SupplyMethod?.Label ?? string.Empty,
                    period.Status == CmDeliveryAcceptancePeriodStatus.Approved ? period.AccountStatus.ToString() : period.Status.ToString(),
                    nameof(CmProcessType.DeliveryAcceptance),
                    period.AuditInfo.LastModifiedAt ?? period.AuditInfo.CreatedAt,
                    "contractReceive",
                    GLAccounts: glAccounts));
            }
        }

        return (items, total);
    }

    private async Task<(List<CombinedWorklistItem> Items, int Total)> ExecuteContractTerminationQueryAsync(
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        int topN,
        CancellationToken ct)
    {
        await using var dbContext = await this.dbContextFactory.CreateDbContextAsync(ct);
        var baseQuery = this.contractManagementProgram
                            .BuildContractTerminationBaseQuery(dbContext, req, userContext)
                            .AsNoTracking();

        var items = await baseQuery
                          .OrderByDescending(d => d.AuditInfo.LastModifiedAt ?? d.AuditInfo.CreatedAt)
                          .Select(d => new CombinedWorklistItem(
                              "ContractTermination",
                              nameof(CmProcessType.ContractTermination),
                              d.Id.Value,
                              d.ContractDraftVendorId.Value,
                              d.CaContractDraftVendor.ContractNumber,
                              d.CaContractDraftVendor.ContractName,
                              null,
                              d.CaContractDraftVendor.Budget,
                              null,
                              d.CaContractDraftVendor.ContractDraft.Procurement.Department.Name,
                              d.CaContractDraftVendor.ContractDraft.Procurement.SupplyMethod.Label,
                              d.Status.ToString(),
                              nameof(CmProcessType.ContractTermination),
                              d.AuditInfo.LastModifiedAt ?? d.AuditInfo.CreatedAt,
                              null,
                              null,
                              null,
                              null,
                              null))
                          .Take(topN)
                          .ToListAsync(ct);

        var total = await baseQuery.CountAsync(ct);
        return (items, total);
    }

    private async Task<(List<CombinedWorklistItem> Items, int Total)> ExecuteContractGuaranteeReturnQueryAsync(
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        int topN,
        CancellationToken ct)
    {
        await using var dbContext = await this.dbContextFactory.CreateDbContextAsync(ct);
        var baseQuery = this.contractManagementProgram
                            .BuildContractGuaranteeReturnBaseQuery(dbContext, req, userContext)
                            .AsNoTracking();

        var items = await baseQuery
                          .OrderByDescending(d => d.AuditInfo.LastModifiedAt ?? d.AuditInfo.CreatedAt)
                          .Select(d => new CombinedWorklistItem(
                              "GuaranteeReturn",
                              nameof(CmProcessType.ContractGuaranteeReturn),
                              d.Id.Value,
                              d.ContractDraftVendorId.Value,
                              d.CaContractDraftVendor.ContractNumber,
                              d.CaContractDraftVendor.ContractName,
                              null,
                              d.CaContractDraftVendor.Budget,
                              null,
                              d.CaContractDraftVendor.ContractDraft.Procurement.Department.Name,
                              d.CaContractDraftVendor.ContractDraft.Procurement.SupplyMethod.Label,
                              d.Status.ToString(),
                              nameof(CmProcessType.ContractGuaranteeReturn),
                              d.AuditInfo.LastModifiedAt ?? d.AuditInfo.CreatedAt,
                              "contractReturnCollateral",
                              null,
                              null,
                              null,
                              null))
                          .Take(topN)
                          .ToListAsync(ct);

        var total = await baseQuery.CountAsync(ct);
        return (items, total);
    }

    private async Task<(List<CombinedWorklistItem> Items, int Total)> ExecuteContractDraftVendorEditQueryAsync(
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        int topN,
        CancellationToken ct)
    {
        var userWithOutDelegateeId = userContext.EffectiveUserIds.Where(e => !e.IsDelegateeUser).Select(e => e.UserId);
        var userWithDelegateeId = userContext.EffectiveUserIds.Select(e => e.UserId);

        await using var dbContext = await this.dbContextFactory.CreateDbContextAsync(ct);
        var baseQuery = dbContext.CaContractDraftVendorEdits
            .Where(e => !e.IsDeleted)
            .WhereIfTrue(
                !string.IsNullOrWhiteSpace(req.Keyword),
                e => EF.Functions.ILike(e.ContractNumber, $"%{req.Keyword}%") ||
                     EF.Functions.ILike(e.ContractName, $"%{req.Keyword}%"))
            .Where(e =>
                ((e.Status == ContractDraftVendorEditStatus.Draft ||
                  e.Status == ContractDraftVendorEditStatus.Editing ||
                  e.Status == ContractDraftVendorEditStatus.Rejected) &&
                 e.Acceptors.Any(a => a.Type == Domain.Common.AcceptorType.AcceptanceCommittee && userWithOutDelegateeId.Contains(a.UserId))) ||
                (e.Status == ContractDraftVendorEditStatus.WaitingCommitteeApproval &&
                 e.Acceptors.Any(a => a.Type == Domain.Common.AcceptorType.AcceptanceCommittee && userWithOutDelegateeId.Contains(a.UserId))) ||
                (e.Status == ContractDraftVendorEditStatus.WaitingAssignment &&
                 !e.Assignees.Any(a => a.Type == Domain.Common.AssigneeType.Assignee) &&
                 userWithDelegateeId.Contains(e.Assignees.OrderBy(a => a.Sequence).Last().UserId)) ||
                ((e.Status == ContractDraftVendorEditStatus.WaitingAssignment ||
                  e.Status == ContractDraftVendorEditStatus.WaitingComment ||
                  e.Status == ContractDraftVendorEditStatus.RejectedToAssignee) &&
                 e.Assignees.Any(a => a.Type == Domain.Common.AssigneeType.Assignee) &&
                 userWithDelegateeId.Contains(e.Assignees.Where(a => a.Type == Domain.Common.AssigneeType.Assignee).OrderBy(a => a.Sequence).Last().UserId)) ||
                (e.Status == ContractDraftVendorEditStatus.WaitingApproval &&
                 e.Acceptors.Any(a =>
                     !a.IsDeleted &&
                     a.Type == Domain.Common.AcceptorType.Approver &&
                     a.Status == Domain.Common.AcceptorStatus.Pending &&
                     (userWithDelegateeId.Contains(a.UserId) ||
                      (a.Delegatee != null && userWithDelegateeId.Contains(a.Delegatee.SuUserId))) &&
                     !e.Acceptors.Any(b =>
                         !b.IsDeleted &&
                         b.Type == Domain.Common.AcceptorType.Approver &&
                         b.Status == Domain.Common.AcceptorStatus.Pending &&
                         b.Sequence < a.Sequence))))
            .AsNoTracking();

        var items = await baseQuery
                          .OrderByDescending(e => e.AuditInfo.LastModifiedAt ?? e.AuditInfo.CreatedAt)
                          .Select(e => new CombinedWorklistItem(
                              "ContractAmendment",
                              "ContractDraftEditVendor",
                              e.Id.Value,
                              null,
                              e.ContractNumber ?? string.Empty,
                              e.ContractName,
                              null,
                              e.Budget,
                              null,
                              e.ContractDraftVendor.ContractDraft.Procurement.Department.Name,
                              e.ContractDraftVendor.ContractDraft.Procurement.SupplyMethod.Label,
                              e.Status.ToString(),
                              null,
                              e.AuditInfo.LastModifiedAt ?? e.AuditInfo.CreatedAt,
                              "ContractDraftEditVendor",
                              e.ContractDraftVendorId.Value,
                              null,
                              null,
                              null))
                          .Take(topN)
                          .ToListAsync(ct);

        var total = await baseQuery.CountAsync(ct);
        return (items, total);
    }

    private async Task<(List<CombinedWorklistItem> Items, int Total)> ExecuteExpenseDisbursementQueryAsync(
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        int topN,
        CancellationToken ct)
    {
        await using var dbContext = await this.dbContextFactory.CreateDbContextAsync(ct);
        var departmentSourceIds = await this.expenseDisbursementProgram.PreCalculateDepartmentSourceIdsAsync(dbContext, req.DepartmentCode, ct);

        var baseQuery = this.expenseDisbursementProgram
                            .BuildExpenseDisbursementBaseQuery(dbContext, req, userContext, departmentSourceIds);

        var expenseDisbursementEntities = await baseQuery
                                                .OrderByDescending(d => d.AuditInfo.LastModifiedAt ?? d.AuditInfo.CreatedAt)
                                                .Include(x => x.GlAccounts)
                                                .AsNoTracking()
                                                .Take(topN)
                                                .ToListAsync(ct);

        var total = await baseQuery.CountAsync(ct);

        var w119Ids = new HashSet<Guid>();
        var clause79Ids = new HashSet<Guid>();
        var guaranteeReturnIds = new HashSet<Guid>();
        var pettyCashReimbursementIds = new HashSet<Guid>();

        foreach (var e in expenseDisbursementEntities)
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

        var sourceLookups = await this.expenseDisbursementProgram.BuildSourceLookupsAsync(
            dbContext, w119Ids, clause79Ids, guaranteeReturnIds, pettyCashReimbursementIds, ct);

        var items = new List<CombinedWorklistItem>();

        foreach (var d in expenseDisbursementEntities)
        {
            var (code, name, dept, sourceAmount) = ExpenseDisbursementProgram.GetSourceDataForEntity(d, sourceLookups);
            var budget = d.GlAccounts?.Sum(g => g.Amount) ?? sourceAmount;

            items.Add(new CombinedWorklistItem(
                "ExpenseDisbursement",
                nameof(PExpenseDisbursement),
                d.Id.Value,
                null,
                code,
                name,
                null,
                budget,
                null,
                dept,
                string.Empty,
                d.Status.ToString(),
                nameof(PExpenseDisbursement),
                d.AuditInfo.LastModifiedAt ?? d.AuditInfo.CreatedAt,
                null));
        }

        return (items, total);
    }
}