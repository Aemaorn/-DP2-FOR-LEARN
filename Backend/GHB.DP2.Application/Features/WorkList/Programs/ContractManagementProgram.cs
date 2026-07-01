namespace GHB.DP2.Application.Features.WorkList.Programs;

using GHB.DP2.Application.Common;
using GHB.DP2.Application.Constants;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.EntityFrameworkCore;

public enum CmProcessType
{
    /// <summary>
    /// บันทึกส่งมอบ และตรวจรับ
    /// </summary>
    DeliveryAcceptance,

    /// <summary>
    /// บอกเลิกสัญญา
    /// </summary>
    ContractTermination,

    /// <summary>
    /// รายการคืนหลักประกันสัญญา
    /// </summary>
    ContractGuaranteeReturn,
}

internal sealed class ContractManagementProgram
{
    public async Task<IReadOnlyList<CmDeliveryAcceptanceId>?> GetDeliveryAcceptanceKeywordIdsAsync(
        Dp2DbContext dbContext,
        string? keyword,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return null;
        }

        var planIds = await dbContext.Plans
            .Where(p => EF.Functions.ILike(p.Name, $"%{keyword}%") ||
                        EF.Functions.ILike((string)p.PlanNumber, $"%{keyword}%"))
            .Select(p => p.Id.Value)
            .ToListAsync(ct);

        var procurementIds = await dbContext.PPurchaseOrderApprovals
            .Where(poa => EF.Functions.ILike(poa.Procurement.Name, $"%{keyword}%") ||
                          EF.Functions.ILike((string)poa.Procurement.ProcurementNumber!.Value, $"%{keyword}%"))
            .Select(poa => poa.Id.Value)
            .ToListAsync(ct);

        var contractVendorIds = await dbContext.CaContractDraftVendors
            .Where(cdv => EF.Functions.ILike(cdv.ContractName, $"%{keyword}%") ||
                          EF.Functions.ILike(cdv.ContractNumber, $"%{keyword}%"))
            .Select(cdv => cdv.Id.Value)
            .ToListAsync(ct);

        var refIds = planIds.Concat(procurementIds).Concat(contractVendorIds).ToList();

        var refMatchedIds = refIds.Count == 0
            ? new List<CmDeliveryAcceptanceId>()
            : await dbContext.CmDeliveryAcceptances
                .Where(da => refIds.Contains((Guid)da.RefId))
                .Select(da => da.Id)
                .ToListAsync(ct);

        // ค้นหาด้วยรหัสบัญชี/ชื่อบัญชี (GL) จาก budget ของงวดส่งมอบ — Label เก็บเป็น "รหัส : ชื่อ"
        var glMatchedIds = await dbContext.CmDeliveryAcceptancePeriods
            .Where(period => period.Budgets.Any(b => EF.Functions.ILike(b.AccountNo.Label, $"%{keyword}%")))
            .Select(period => period.CmDeliveryAcceptanceId)
            .ToListAsync(ct);

        return refMatchedIds.Concat(glMatchedIds).Distinct().ToList();
    }

    public async Task<(int Count, SectionResult<ContractManagementItem>? Section)> ProcessContractManagementSectionAsync(
        Dp2DbContext dbContext,
        IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory,
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        bool isSegmentAccountingMember,
        CancellationToken ct)
    {
        var keywordDeliveryAcceptanceIds = await this.GetDeliveryAcceptanceKeywordIdsAsync(dbContext, req.Keyword, ct);
        var segmentEligiblePeriodIds = await this.GetSegmentEligiblePeriodIdsAsync(dbContext, isSegmentAccountingMember, ct);

        await using var ctxDeliveryCount = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctxTerminationCount = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctxGuaranteeReturnCount = await dbContextFactory.CreateDbContextAsync(ct);

        var deliveryCountTask = this.BuildDeliveryAcceptanceBaseQuery(ctxDeliveryCount, userContext, keywordDeliveryAcceptanceIds, segmentEligiblePeriodIds).CountAsync(ct);
        var terminationCountTask = this.BuildContractTerminationBaseQuery(ctxTerminationCount, req, userContext).CountAsync(ct);
        var guaranteeReturnCountTask = this.BuildContractGuaranteeReturnBaseQuery(ctxGuaranteeReturnCount, req, userContext).CountAsync(ct);

        await Task.WhenAll(deliveryCountTask, terminationCountTask, guaranteeReturnCountTask);

        var count = await deliveryCountTask + await terminationCountTask + await guaranteeReturnCountTask;

        SectionResult<ContractManagementItem>? section = null;

        if (req.IncludeContractManagement)
        {
            section = await this.CreateSectionResultAsync(
                dbContextFactory,
                req,
                userContext,
                keywordDeliveryAcceptanceIds,
                segmentEligiblePeriodIds,
                ct);
        }

        return (count, section);
    }

    private async Task<SectionResult<ContractManagementItem>> CreateSectionResultAsync(
        IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory,
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        IReadOnlyList<CmDeliveryAcceptanceId>? keywordDeliveryAcceptanceIds,
        IReadOnlyList<CmDeliveryAcceptancePeriodId>? segmentEligiblePeriodIds,
        CancellationToken ct)
    {
        await using var ctxDelivery = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctxTermination = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctxGuaranteeReturn = await dbContextFactory.CreateDbContextAsync(ct);

        var deliveryPeriodsTask = this.BuildDeliveryAcceptanceBaseQuery(ctxDelivery, userContext, keywordDeliveryAcceptanceIds, segmentEligiblePeriodIds)
                                      .Include(r => r.CmDeliveryAcceptance)
                                          .ThenInclude(da => da.Department)
                                      .Include(r => r.CmDeliveryAcceptance)
                                          .ThenInclude(da => da.SupplyMethod)
                                      .Include(r => r.CmDeliveryAcceptance)
                                          .ThenInclude(da => da.SupplyMethodType)
                                      .Include(r => r.CmDeliveryAcceptance)
                                          .ThenInclude(da => da.SupplyMethodSpecialType)
                                      .ToListAsync(ct);

        var contractTerminationsTask = this.BuildContractTerminationBaseQuery(ctxTermination, req, userContext)
                                           .ToListAsync(ct);

        var contractGuaranteeReturnsTask = this.BuildContractGuaranteeReturnBaseQuery(ctxGuaranteeReturn, req, userContext)
                                               .ToListAsync(ct);

        await Task.WhenAll(deliveryPeriodsTask, contractTerminationsTask, contractGuaranteeReturnsTask);

        var deliveryPeriods = await deliveryPeriodsTask;
        var contractTerminations = await contractTerminationsTask;
        var contractGuaranteeReturns = await contractGuaranteeReturnsTask;

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

        var poaRefIds = deliveryPeriods
                        .Where(p => p.CmDeliveryAcceptance.SourceType == SourceType.Procurement)
                        .Select(p => PurchaseOrderApprovalId.From((Guid)p.CmDeliveryAcceptance.RefId))
                        .Distinct()
                        .ToList();

        await using var ctxPlans = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctxVendors = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctxPoas = await dbContextFactory.CreateDbContextAsync(ct);

        var plansTask = planRefIds.Count != 0
            ? ctxPlans.Plans
                      .AsNoTracking()
                      .Include(p => p.Department)
                      .Include(p => p.SupplyMethod)
                      .Include(p => p.SupplyMethodType)
                      .Include(p => p.SupplyMethodSpecialType)
                      .Where(p => planRefIds.Contains(p.Id))
                      .ToDictionaryAsync(p => p.Id.Value, ct)
            : Task.FromResult(new Dictionary<Guid, Plan>());

        var contractDraftVendorsTask = contractDraftVendorRefIds.Count != 0
            ? ctxVendors.CaContractDraftVendors
                        .AsNoTracking()
                        .Include(cdv => cdv.ContractDraft)
                        .ThenInclude(cd => cd.Procurement)
                        .ThenInclude(p => p.Department)
                        .Include(cdv => cdv.ContractDraft)
                        .ThenInclude(cd => cd.Procurement)
                        .ThenInclude(p => p.SupplyMethod)
                        .Include(cdv => cdv.ContractDraft)
                        .ThenInclude(cd => cd.Procurement)
                        .ThenInclude(p => p.SupplyMethodType)
                        .Include(cdv => cdv.ContractDraft)
                        .ThenInclude(cd => cd.Procurement)
                        .ThenInclude(p => p.SupplyMethodSpecialType)
                        .Include(cdv => cdv.Vendor)
                        .ThenInclude(v => v.VendorInfo)
                        .Include(cdv => cdv.ContractType)
                        .Where(cdv => contractDraftVendorRefIds.Contains(cdv.Id))
                        .ToDictionaryAsync(cdv => cdv.Id.Value, ct)
            : Task.FromResult(new Dictionary<Guid, CaContractDraftVendor>());

        var purchaseOrderApprovalsTask = poaRefIds.Count != 0
            ? ctxPoas.PPurchaseOrderApprovals
                     .AsNoTracking()
                     .Include(poa => poa.Procurement).ThenInclude(p => p.Plan)
                     .Include(poa => poa.Procurement).ThenInclude(p => p.Department)
                     .Include(poa => poa.Procurement).ThenInclude(p => p.SupplyMethod)
                     .Include(poa => poa.Procurement).ThenInclude(p => p.SupplyMethodType)
                     .Include(poa => poa.Procurement).ThenInclude(p => p.SupplyMethodSpecialType)
                     .Where(poa => poaRefIds.Contains(poa.Id))
                     .ToDictionaryAsync(poa => poa.Id.Value, ct)
            : Task.FromResult(new Dictionary<Guid, PPurchaseOrderApproval>());

        await Task.WhenAll(plansTask, contractDraftVendorsTask, purchaseOrderApprovalsTask);

        var plans = await plansTask;
        var contractDraftVendors = await contractDraftVendorsTask;
        var purchaseOrderApprovals = await purchaseOrderApprovalsTask;

        var deliveryAcceptanceItems = deliveryPeriods.Select(r =>
                                                         MapContractDeliveryItem(
                                                             r,
                                                             plans,
                                                             contractDraftVendors,
                                                             purchaseOrderApprovals))
                                                     .ToList();

        var contractTerminationItems = contractTerminations.Select(r =>
                                                               MapContractManagementItem(
                                                                   r.Id.Value,
                                                                   r.ContractDraftVendorId.Value,
                                                                   r.CaContractDraftVendor,
                                                                   CmProcessType.ContractTermination,
                                                                   r.Status.ToString()))
                                                           .ToList();

        var contractGuaranteeReturnItems = contractGuaranteeReturns.Select(r =>
                                                                       MapContractManagementItem(
                                                                           r.Id.Value,
                                                                           r.ContractDraftVendorId.Value,
                                                                           r.CaContractDraftVendor,
                                                                           CmProcessType.ContractGuaranteeReturn,
                                                                           r.Status.ToString()))
                                                                   .ToList();

        var allItems = deliveryAcceptanceItems
                       .Concat(contractTerminationItems)
                       .Concat(contractGuaranteeReturnItems)
                       .OrderBy(x => x.ContractNumber)
                       .ToList();

        var skip = Math.Max(0, (req.PageNumber - 1) * req.PageSize);
        var pageItems = allItems.Skip(skip).Take(req.PageSize).ToList();

        var result = new PaginatedQueryResult<ContractManagementItem>(pageItems, allItems.Count);

        return new SectionResult<ContractManagementItem>(result);
    }

    private static ContractManagementItem MapContractManagementItem(Guid id, Guid detailId, CaContractDraftVendor contractDraftVendor, CmProcessType processType, string documentStatus)
    {
        return new ContractManagementItem(
            id,
            detailId,
            null,
            null,
            contractDraftVendor.ContractDraftNumber.ToString(),
            contractDraftVendor.PoNumber,
            contractDraftVendor.ContractName,
            contractDraftVendor.Vendor?.EstablishmentName,
            contractDraftVendor.ContractSignedDate,
            contractDraftVendor?.Budget ?? 0m,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            documentStatus,
            null,
            contractDraftVendor?.ContractType?.Label,
            processType);
    }

    private static ContractManagementItem MapContractDeliveryItem(
        CmDeliveryAcceptancePeriod period,
        Dictionary<Guid, Plan> plans,
        Dictionary<Guid, CaContractDraftVendor> contractDraftVendors,
        Dictionary<Guid, PPurchaseOrderApproval> purchaseOrderApprovals)
    {
        var deliveryAcceptance = period.CmDeliveryAcceptance;

        if (deliveryAcceptance.SourceType == SourceType.Plan)
        {
            plans.TryGetValue((Guid)deliveryAcceptance.RefId, out var planData);

            return new ContractManagementItem(
                period.Id.Value,
                period.CmDeliveryAcceptanceId.Value,
                deliveryAcceptance.RefId,
                period.AcceptanceNumber,
                planData?.PlanNumber.Value ?? string.Empty,
                null,
                planData?.Name ?? string.Empty,
                null,
                null,
                planData?.Budget ?? 0m,
                planData?.Department?.Name ?? string.Empty,
                planData?.SupplyMethod?.Label ?? string.Empty,
                planData?.SupplyMethodType?.Label,
                planData?.SupplyMethodSpecialType?.Label,
                period.Status.ToString(),
                SourceType.Plan,
                null,
                CmProcessType.DeliveryAcceptance);
        }
        else if (deliveryAcceptance.SourceType == SourceType.Procurement)
        {
            purchaseOrderApprovals.TryGetValue((Guid)deliveryAcceptance.RefId, out var poaData);
            var proc = poaData?.Procurement;

            return new ContractManagementItem(
                period.Id.Value,
                period.CmDeliveryAcceptanceId.Value,
                proc?.Id.Value,
                period.AcceptanceNumber,
                proc?.ProcurementNumber.HasValue ?? false ? proc.ProcurementNumber.Value.ToString() : string.Empty,
                null,
                proc?.Name ?? string.Empty,
                null,
                null,
                proc?.Plan?.Budget ?? 0m,
                proc?.Department?.Name ?? string.Empty,
                proc?.SupplyMethod?.Label ?? string.Empty,
                proc?.SupplyMethodType?.Label,
                proc?.SupplyMethodSpecialType?.Label,
                period.Status.ToString(),
                SourceType.Procurement,
                null,
                CmProcessType.DeliveryAcceptance);
        }
        else if (deliveryAcceptance.SourceType == SourceType.Manual)
        {
            return new ContractManagementItem(
                period.Id.Value,
                period.CmDeliveryAcceptanceId.Value,
                deliveryAcceptance.RefId,
                period.AcceptanceNumber,
                deliveryAcceptance.Number ?? string.Empty,
                null,
                deliveryAcceptance.Name ?? string.Empty,
                null,
                null,
                deliveryAcceptance.Budget ?? 0m,
                deliveryAcceptance.Department?.Name ?? string.Empty,
                deliveryAcceptance.SupplyMethod?.Label ?? string.Empty,
                deliveryAcceptance.SupplyMethodType?.Label,
                deliveryAcceptance.SupplyMethodSpecialType?.Label,
                period.Status.ToString(),
                SourceType.Manual,
                deliveryAcceptance.ContractType,
                CmProcessType.DeliveryAcceptance);
        }
        else
        {
            contractDraftVendors.TryGetValue((Guid)deliveryAcceptance.RefId, out var contractDraftVendor);

            return new ContractManagementItem(
                period.Id.Value,
                period.CmDeliveryAcceptanceId.Value,
                deliveryAcceptance.RefId,
                period.AcceptanceNumber,
                contractDraftVendor?.ContractNumber ?? string.Empty,
                contractDraftVendor?.PoNumber,
                contractDraftVendor?.ContractName ?? string.Empty,
                contractDraftVendor?.Vendor?.VendorInfo != null ? $"{contractDraftVendor.Vendor.VendorInfo.SapVendorNumber}" + $" : {contractDraftVendor.Vendor.VendorInfo.EstablishmentName}" : null,
                contractDraftVendor?.ContractSignedDate,
                contractDraftVendor?.Budget ?? 0m,
                contractDraftVendor?.ContractDraft?.Procurement?.Department?.Name ?? string.Empty,
                contractDraftVendor?.ContractDraft?.Procurement?.SupplyMethod?.Label ?? string.Empty,
                contractDraftVendor?.ContractDraft?.Procurement?.SupplyMethodType?.Label,
                contractDraftVendor?.ContractDraft?.Procurement?.SupplyMethodSpecialType?.Label,
                period.Status.ToString(),
                SourceType.ContractDraftVendor,
                contractDraftVendor?.ContractType?.Label,
                CmProcessType.DeliveryAcceptance);
        }
    }

    public async Task<IReadOnlyList<CmDeliveryAcceptancePeriodId>> GetSegmentEligiblePeriodIdsAsync(
        Dp2DbContext dbContext,
        bool isSegmentAccountingMember,
        CancellationToken ct)
    {
        if (!isSegmentAccountingMember)
        {
            return [];
        }

        var candidates = await dbContext.CmDeliveryAcceptancePeriods
                                        .AsNoTracking()
                                        .Where(p =>
                                            p.Status == CmDeliveryAcceptancePeriodStatus.Approved &&
                                            ((p.AccountStatus == CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval &&
                                              !p.Acceptors.Any(a => a.Type == AcceptorType.AccountingOperator && !a.IsDeleted) &&
                                              !p.Acceptors.Any(a => (a.Type == AcceptorType.Accounting || a.Type == AcceptorType.AccountingApprover) && !a.IsDeleted && a.Status == AcceptorStatus.Approved)) ||
                                             (p.AccountStatus == CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate &&
                                              !p.Acceptors.Any(a => a.Type == AcceptorType.AccountingConfirmer && !a.IsDeleted))))
                                        .Select(p => new
                                        {
                                            p.Id,
                                            p.CmDeliveryAcceptance.SourceType,
                                            p.CmDeliveryAcceptance.RefId,
                                            ManualOrganizationLevel = p.CmDeliveryAcceptance.Department != null
                                                ? p.CmDeliveryAcceptance.Department.OrganizationLevel
                                                : null,
                                        })
                                        .ToListAsync(ct);

        if (candidates.Count == 0)
        {
            return [];
        }

        var planRefIds = candidates
                         .Where(c => c.SourceType == SourceType.Plan && c.RefId != null)
                         .Select(c => PlanId.From((Guid)c.RefId!))
                         .Distinct()
                         .ToList();

        var poaRefIds = candidates
                        .Where(c => c.SourceType == SourceType.Procurement && c.RefId != null)
                        .Select(c => PurchaseOrderApprovalId.From((Guid)c.RefId!))
                        .Distinct()
                        .ToList();

        var contractDraftVendorRefIds = candidates
                                        .Where(c => c.SourceType == SourceType.ContractDraftVendor && c.RefId != null)
                                        .Select(c => ContractDraftVendorId.From((Guid)c.RefId!))
                                        .Distinct()
                                        .ToList();

        var vendorEditRefIds = candidates
                               .Where(c => c.SourceType == SourceType.ContractDraftVendorEdit && c.RefId != null)
                               .Select(c => ContractDraftVendorEditId.From((Guid)c.RefId!))
                               .Distinct()
                               .ToList();

        var planLevels = planRefIds.Count != 0
            ? await dbContext.Plans.AsNoTracking()
                             .Include(p => p.Department)
                             .Where(p => planRefIds.Contains(p.Id))
                             .ToDictionaryAsync(p => p.Id.Value, p => p.Department?.OrganizationLevel, ct)
            : new Dictionary<Guid, string?>();

        var poaLevels = poaRefIds.Count != 0
            ? await dbContext.PPurchaseOrderApprovals.AsNoTracking()
                             .Include(p => p.Procurement).ThenInclude(p => p.Department)
                             .Where(p => poaRefIds.Contains(p.Id))
                             .ToDictionaryAsync(p => p.Id.Value, p => p.Procurement?.Department?.OrganizationLevel, ct)
            : new Dictionary<Guid, string?>();

        var contractDraftVendorLevels = contractDraftVendorRefIds.Count != 0
            ? await dbContext.CaContractDraftVendors.AsNoTracking()
                             .Include(c => c.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.Department)
                             .Where(c => contractDraftVendorRefIds.Contains(c.Id))
                             .ToDictionaryAsync(c => c.Id.Value, c => c.ContractDraft?.Procurement?.Department?.OrganizationLevel, ct)
            : new Dictionary<Guid, string?>();

        var vendorEdits = await dbContext.CaContractDraftVendorEdits.AsNoTracking()
                                         .Where(e => vendorEditRefIds.Contains(e.Id))
                                         .Select(e => new { e.Id, e.ContractDraftVendorId })
                                         .ToListAsync(ct);

        var vendorEditVendorIds = vendorEdits.Select(e => e.ContractDraftVendorId).Distinct().ToList();

        var vendorEditVendorLevels = vendorEditVendorIds.Count != 0
            ? await dbContext.CaContractDraftVendors.AsNoTracking()
                             .Include(c => c.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.Department)
                             .Where(c => vendorEditVendorIds.Contains(c.Id))
                             .ToDictionaryAsync(c => c.Id, c => c.ContractDraft?.Procurement?.Department?.OrganizationLevel, ct)
            : new Dictionary<ContractDraftVendorId, string?>();

        var vendorEditLevels = vendorEdits.ToDictionary(
            e => e.Id.Value,
            e => vendorEditVendorLevels.TryGetValue(e.ContractDraftVendorId, out var level) ? level : null);

        var eligibleIds = new List<CmDeliveryAcceptancePeriodId>();

        foreach (var candidate in candidates)
        {
            var organizationLevel = candidate.SourceType switch
            {
                SourceType.Plan => candidate.RefId != null ? planLevels.GetValueOrDefault((Guid)candidate.RefId) : null,
                SourceType.Procurement => candidate.RefId != null ? poaLevels.GetValueOrDefault((Guid)candidate.RefId) : null,
                SourceType.ContractDraftVendor => candidate.RefId != null ? contractDraftVendorLevels.GetValueOrDefault((Guid)candidate.RefId) : null,
                SourceType.ContractDraftVendorEdit => candidate.RefId != null ? vendorEditLevels.GetValueOrDefault((Guid)candidate.RefId) : null,
                SourceType.Manual => candidate.ManualOrganizationLevel,
                _ => null,
            };

            if (organizationLevel == null || !EmployeeConstant.OrganizationLevel.BranchLevels.Contains(organizationLevel))
            {
                eligibleIds.Add(candidate.Id);
            }
        }

        return eligibleIds;
    }

    public IQueryable<CmDeliveryAcceptancePeriod> BuildDeliveryAcceptanceBaseQuery(
        Dp2DbContext dbContext,
        UserContext userContext,
        IReadOnlyList<CmDeliveryAcceptanceId>? keywordDeliveryAcceptanceIds = null,
        IReadOnlyList<CmDeliveryAcceptancePeriodId>? segmentEligiblePeriodIds = null)
    {
        var userId = userContext.User.Id;
        var userIds = userContext.EffectiveUserIds.Select(e => e.UserId);

        return dbContext.CmDeliveryAcceptancePeriods
                        .AsNoTracking()
                        .WhereIfTrue(
                            keywordDeliveryAcceptanceIds != null,
                            p => keywordDeliveryAcceptanceIds!.Contains(p.CmDeliveryAcceptanceId))
                        .Where(p =>
                            ((p.Status == CmDeliveryAcceptancePeriodStatus.Draft ||
                              p.Status == CmDeliveryAcceptancePeriodStatus.Rejected ||
                              p.Status == CmDeliveryAcceptancePeriodStatus.Edit) &&
                             p.Acceptors.Any(a =>
                                 a.Type == AcceptorType.AcceptanceCommittee &&
                                 (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))) &&
                                 a.IsActive &&
                                 !a.IsDeleted)) ||
                            (p.Status == CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval &&
                             p.Acceptors.Any(a =>
                                 a.Type == AcceptorType.AcceptanceCommittee &&
                                 a.Status == AcceptorStatus.Pending &&
                                 a.UserId == userId &&
                                 !p.Acceptors.Any(b =>
                                     b.Type == AcceptorType.AcceptanceCommittee &&
                                     b.Status == AcceptorStatus.Pending &&
                                     b.Sequence < a.Sequence))) ||
                            (p.Status == CmDeliveryAcceptancePeriodStatus.WaitingAcceptance &&
                             p.Acceptors.Any(a =>
                                 a.Type == AcceptorType.Approver &&
                                 a.Status == AcceptorStatus.Pending &&
                                 a.UserId == userId &&
                                 !p.Acceptors.Any(b =>
                                     b.Type == AcceptorType.Approver &&
                                     b.Status == AcceptorStatus.Pending &&
                                     b.Sequence < a.Sequence))) ||
                            (p.Status == CmDeliveryAcceptancePeriodStatus.WaitingAssign &&
                             p.Assignees.Any(a => a.UserId == userId)) ||
                            ((p.Status == CmDeliveryAcceptancePeriodStatus.WaitingComment ||
                              p.Status == CmDeliveryAcceptancePeriodStatus.RejectToAssignee) &&
                             p.Assignees.Any(a => a.UserId == userId)) ||
                            (p.Status == CmDeliveryAcceptancePeriodStatus.Approved &&
                             p.AccountStatus == CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval &&
                             (p.Acceptors.Any(a =>
                                 a.Type == AcceptorType.AccountingOperator &&
                                 (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))) &&
                                 a.IsActive &&
                                 !a.IsDeleted &&
                                 a.Status == AcceptorStatus.Pending &&
                                 !p.Acceptors.Any(b =>
                                     b.Type == AcceptorType.AccountingOperator &&
                                     b.IsActive &&
                                     !b.IsDeleted &&
                                     b.Status == AcceptorStatus.Pending &&
                                     b.Sequence < a.Sequence)) ||
                              (p.Acceptors.Any(a =>
                                 (a.Type == AcceptorType.Accounting || a.Type == AcceptorType.AccountingApprover) &&
                                 (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))) &&
                                 a.IsActive &&
                                 !a.IsDeleted &&
                                 a.Status == AcceptorStatus.Pending &&
                                 !p.Acceptors.Any(b =>
                                     b.Type == AcceptorType.AccountingOperator &&
                                     b.IsActive &&
                                     !b.IsDeleted &&
                                     b.Status == AcceptorStatus.Pending) &&
                                 !p.Acceptors.Any(b =>
                                     (b.Type == AcceptorType.Accounting || b.Type == AcceptorType.AccountingApprover) &&
                                     b.IsActive &&
                                     !b.IsDeleted &&
                                     b.Status == AcceptorStatus.Pending &&
                                     b.Sequence < a.Sequence)) &&
                               !(segmentEligiblePeriodIds != null && segmentEligiblePeriodIds.Contains(p.Id))) ||
                              (
                                 !p.Acceptors.Any(b =>
                                     (b.Type == AcceptorType.AccountingOperator ||
                                      b.Type == AcceptorType.Accounting ||
                                      b.Type == AcceptorType.AccountingApprover) &&
                                     b.IsActive &&
                                     !b.IsDeleted) &&
                                 p.Acceptors.Any(a =>
                                     a.Type == AcceptorType.AcceptanceCommittee &&
                                     (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))) &&
                                     a.IsActive &&
                                     !a.IsDeleted)) ||
                              (segmentEligiblePeriodIds != null && segmentEligiblePeriodIds.Contains(p.Id)))) ||
                            (p.Status == CmDeliveryAcceptancePeriodStatus.Approved &&
                             p.AccountStatus == CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate &&
                             (
                                 p.Acceptors.Any(a =>
                                     a.Type == AcceptorType.AccountingConfirmer &&
                                     !a.IsDeleted &&
                                     (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId)))) ||
                                 (!p.Acceptors.Any(c =>
                                      c.Type == AcceptorType.AccountingConfirmer &&
                                      !c.IsDeleted) &&
                                  p.Acceptors.Any(a =>
                                      a.Type == AcceptorType.AccountingOperator &&
                                      !a.IsDeleted &&
                                      (userIds.Contains(a.UserId) || (a.Delegatee != null && userIds.Contains(a.Delegatee.SuUserId))))) ||
                                 (segmentEligiblePeriodIds != null && segmentEligiblePeriodIds.Contains(p.Id))
                             )))
                        .OrderByDescending(p => p.AuditInfo.LastModifiedAt);
    }

    public IQueryable<CmContractTermination> BuildContractTerminationBaseQuery(Dp2DbContext dbContext, GetWorklistSeparatedRequest req, UserContext userContext)
    {
        var userWithOutDelegateeId = userContext.EffectiveUserIds.Where(e => !e.IsDelegateeUser).Select(e => e.UserId);
        var userWithDelegateeId = userContext.EffectiveUserIds.Select(e => e.UserId);

        return dbContext.CmContractTerminations
                        .Include(x => x.CaContractDraftVendor)
                        .ThenInclude(cd => cd.ContractType)
                        .Include(x => x.CaContractDraftVendor)
                        .ThenInclude(cd => cd.Vendor)
                        .AsNoTracking()
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.Keyword),
                            x => EF.Functions.ILike(x.CaContractDraftVendor.ContractNumber, $"%{req.Keyword}%") ||
                                 EF.Functions.ILike(x.CaContractDraftVendor.ContractName, $"%{req.Keyword}%") ||
                                 EF.Functions.ILike(x.CaContractDraftVendor.PoNumber, $"%{req.Keyword}%"))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.DepartmentCode),
                            x => x.CaContractDraftVendor
                                  .ContractDraft
                                  .Procurement.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.SupplyMethodCode),
                            x => x.CaContractDraftVendor
                                  .ContractDraft
                                  .Procurement.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.SupplyMethodTypeCode),
                            x => x.CaContractDraftVendor
                                  .ContractDraft
                                  .Procurement.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.SupplyMethodSpecialTypeCode),
                            x => x.CaContractDraftVendor
                                  .ContractDraft
                                  .Procurement.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
                        .Where(x =>
                            ((x.Status == CmContractTerminationStatus.Draft ||
                              x.Status == CmContractTerminationStatus.Rejected) &&
                             (x.CaContractDraftVendor.ContractDraft.Procurement.Jp005.Any(j =>
                                  j.Committees.Any(c => userWithOutDelegateeId.Contains(c.SuUserId) && c.GroupType == PJp005CommitteeGroupType.InspectionCommittee)) ||
                              x.CaContractDraftVendor.ContractDraft.Procurement.PrincipleApprovals.Any(j =>
                                  j.PrincipleApprovalCommittees.Any(c => userWithOutDelegateeId.Contains(c.SuUserId) && c.GroupType == CommitteeGroupType.AcceptanceCommittee))))
                            ||
                            (x.Status == CmContractTerminationStatus.WaitingCommitteeApproval &&
                             (x.CaContractDraftVendor.ContractDraft.Procurement.Jp005.Any(j =>
                                  j.Committees.Any(c => userWithOutDelegateeId.Contains(c.SuUserId) && c.GroupType == PJp005CommitteeGroupType.InspectionCommittee)) ||
                              x.CaContractDraftVendor.ContractDraft.Procurement.PrincipleApprovals.Any(j =>
                                  j.PrincipleApprovalCommittees.Any(c => userWithOutDelegateeId.Contains(c.SuUserId) && c.GroupType == CommitteeGroupType.AcceptanceCommittee))))
                            ||
                            (
                                (x.Status == CmContractTerminationStatus.WaitingAssign || x.Status == CmContractTerminationStatus.RejectToAssignee) &&
                                userWithDelegateeId.Contains(x.Assignees.Where(a => a.Type == AssigneeType.Director).OrderBy(a => a.Sequence).Last().UserId)
                            )
                            ||
                            (
                                x.Status == CmContractTerminationStatus.WaitingComment &&
                                userWithDelegateeId.Contains(x.Assignees.Where(a => a.Type == AssigneeType.Assignee).OrderBy(a => a.Sequence).Last().UserId)
                            )
                            ||
                            (x.Status == CmContractTerminationStatus.WaitingApproval && x.Acceptors.Any(ac =>
                                !ac.IsDeleted &&
                                ac.Status == AcceptorStatus.Pending &&
                                (userWithDelegateeId.Contains(ac.UserId) ||
                                 (ac.Delegatee != null && userWithDelegateeId.Contains(ac.Delegatee.SuUserId))) &&
                                !x.Acceptors.Any(b => !b.IsDeleted && b.Status == AcceptorStatus.Pending && b.Sequence < ac.Sequence))))
                        .OrderByDescending(x => x.AuditInfo.LastModifiedAt);
    }

    public IQueryable<CmContractGuaranteeReturn> BuildContractGuaranteeReturnBaseQuery(Dp2DbContext dbContext, GetWorklistSeparatedRequest req, UserContext userContext)
    {
        var userWithOutDelegateeId = userContext.EffectiveUserIds.Where(e => !e.IsDelegateeUser).Select(e => e.UserId);
        var userWithDelegateeId = userContext.EffectiveUserIds.Select(e => e.UserId);
        var userBusinessUnitCode = userContext.User.Employee?.PrimaryBusinessUnit?.BusinessUnitCode;
        var isAccountingUser = !userContext.ViewUser.FullPositionName.StartsWith(JorPor.DefaultSectionHead.PositionName);

        return dbContext.CmContractGuaranteeReturns
                        .Include(x => x.CaContractDraftVendor)
                        .ThenInclude(cd => cd.ContractType)
                        .Include(x => x.CaContractDraftVendor)
                        .ThenInclude(cd => cd.Vendor)
                        .AsNoTracking()
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.Keyword),
                            x => EF.Functions.ILike(x.CaContractDraftVendor.ContractNumber, $"%{req.Keyword}%") ||
                                 EF.Functions.ILike(x.CaContractDraftVendor.ContractName, $"%{req.Keyword}%") ||
                                 EF.Functions.ILike(x.CaContractDraftVendor.PoNumber, $"%{req.Keyword}%"))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.DepartmentCode),
                            x => x.CaContractDraftVendor
                                  .ContractDraft
                                  .Procurement.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.SupplyMethodCode),
                            x => x.CaContractDraftVendor
                                  .ContractDraft
                                  .Procurement.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.SupplyMethodTypeCode),
                            x => x.CaContractDraftVendor
                                  .ContractDraft
                                  .Procurement.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.SupplyMethodSpecialTypeCode),
                            x => x.CaContractDraftVendor
                                  .ContractDraft
                                  .Procurement.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
                        .Where(x =>
                            ((x.Status == CmContractGuaranteeReturnStatus.Draft ||
                              x.Status == CmContractGuaranteeReturnStatus.Rejected ||
                              x.Status == CmContractGuaranteeReturnStatus.WaitingCommitteeApproval) &&
                             x.Acceptors.Any(j =>
                                 j.Type == AcceptorType.AcceptanceCommittee &&
                                 userWithOutDelegateeId.Contains(j.UserId))) ||
                            (x.Status == CmContractGuaranteeReturnStatus.WaitingAssigned &&
                             x.Assignees.Any(j =>
                                 userWithDelegateeId.Contains(j.UserId) ||
                                 (j.Delegatee != null &&
                                  userWithDelegateeId.Contains(j.Delegatee.SuUserId)))) ||
                            (x.Status == CmContractGuaranteeReturnStatus.Assigned &&
                             x.Assignees.Any(j =>
                                 userWithDelegateeId.Contains(j.UserId) ||
                                 (j.Delegatee != null &&
                                  userWithDelegateeId.Contains(j.Delegatee.SuUserId)))) ||
                            (x.Status == CmContractGuaranteeReturnStatus.WaitingAcceptance &&
                             x.Acceptors.Any(a =>
                                 a.Type == AcceptorType.Approver &&
                                 a.Status == AcceptorStatus.Pending &&
                                 (userWithDelegateeId.Contains(a.UserId) ||
                                  (a.Delegatee != null &&
                                   userWithDelegateeId.Contains(a.Delegatee.SuUserId))) &&
                                 !x.Acceptors.Any(b =>
                                     b.Type == AcceptorType.Approver &&
                                     b.Status == AcceptorStatus.Pending &&
                                     b.Sequence < a.Sequence))) ||
                            (x.Status == CmContractGuaranteeReturnStatus.WaitingAccountingApproval &&
                             x.Acceptors.Any(a =>
                                 a.Type == AcceptorType.Accounting &&
                                 a.Status == AcceptorStatus.Pending &&
                                 (userWithDelegateeId.Contains(a.UserId) ||
                                  (a.Delegatee != null &&
                                   userWithDelegateeId.Contains(a.Delegatee.SuUserId))) &&
                                 !x.Acceptors.Any(b =>
                                     b.Type == AcceptorType.Accounting &&
                                     b.Status == AcceptorStatus.Pending &&
                                     b.Sequence < a.Sequence))) ||
                            (userBusinessUnitCode == ExpenseDisbursementConstant.DefaultDirector.BusinessUnitCode &&
                             isAccountingUser &&
                             x.Status == CmContractGuaranteeReturnStatus.WaitingDisbursementDate))
                        .OrderByDescending(x => x.AuditInfo.LastModifiedAt);
    }
}