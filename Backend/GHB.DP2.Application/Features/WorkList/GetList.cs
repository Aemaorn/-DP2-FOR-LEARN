namespace GHB.DP2.Application.Features.WorkList;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.WorkList.Helpers;
using GHB.DP2.Application.Features.WorkList.Infrastructure;
using GHB.DP2.Application.Features.WorkList.Programs;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PExpenseDisbursement;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// ---------- Request ----------
public record GetWorklistSeparatedRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    int PageNumber,
    int PageSize,

    // เปิด/ปิดแต่ละส่วน
    bool IncludePlans,
    bool IncludeAnnouncements,
    bool IncludePreProcurement,
    bool IncludeProcurement,
    bool IncludeContractAgreement,
    bool IncludeContractManagement,
    bool IncludeContractAmendment,
    bool IncludeExpenseDisbursement,
    bool IncludeAll, // NEW

    // ---- Plan filters ----
    string? Keyword,
    string? DepartmentCode,
    int? BudgetYear,
    string? SupplyMethodCode,
    string? SupplyMethodTypeCode,
    string? SupplyMethodSpecialTypeCode,
    bool? IsMd,
    bool? IsPendingDepartment
) : IPlanBaseQuery,
    IPlanAnnouncementBaseQuery,
    IProcurementBaseQuery;

// ---------- Response DTO ----------
public record PlanItem(
    Guid Id,
    string PlanNumber,
    string Name,
    int BudgetYear,
    decimal Budget,
    PlanType Type,
    string DepartmentName,
    string SupplyMethod,
    bool IsChange,
    bool IsCancel,
    PlanStatus Status);

public record PlanAnnouncementItem(
    Guid Id,
    string AnnouncementNumber,
    string? AnnouncementName,
    int Year,
    int PlanCount,
    decimal SummaryBudget,
    string SupplyMethodName,
    DateTimeOffset? AnnouncementDate,
    PlanAnnouncementStatus Status);

public record ProcurementItem(
    Guid Id,
    string? Type,
    string? PlanNumber,
    string? ProcurementNumber,
    string Name,
    decimal? Budget,
    string? PlanTypeName,
    string DepartmentName,
    string SupplyMethod,
    string SupplyMethodType,
    string SupplyMethodSpecialType,
    string? RentTypeName,
    ProcurementStep Step,
    ProcessType ProcessType,
    string Status,
    string? Period = "",
    Guid? ContractDraftVendorId = null,
    string? ContractName = null,
    string? VendorName = null);

public record ExpenseDisbursementItem(
    Guid Id,
    string? Number,
    string? Name,
    PExpenseDisbursementStatus Status,
    PExpenseDisbursementSourceType SourceType,
    Guid SourceId,
    DateTimeOffset? AdvancePaymentDate,
    DateTimeOffset Date,
    decimal? Budget);

public record ContractAmendmentItem(
    Guid Id,
    string CamContractAmendmentNumber,
    string Type,
    string Status,
    string? Remark,
    ContractDraftVendorId? ContractDraftVendorId,
    string ContractNumber,
    string ContractName,
    string PoNumber,
    DateTimeOffset? ContractSignedDate,
    decimal Budget,
    string Department,
    string? ContractTypeLabel);

internal sealed record ContractManagementRow(
    Guid Id,
    string ContractNumber,
    string PoNumber,
    string ContractName,
    DateTimeOffset? ContractSignedDate,
    decimal Budget,
    string ContractTypeName,
    string Source,
    string CmStatus);

public record ContractManagementItem(
    Guid Id,
    Guid DetailId,
    Guid? RefId,
    string? PeriodNumber,
    string ContractNumber,
    string? PoNumber,
    string ContractName,
    string? VendorName,
    DateTimeOffset? ContractSignedDate,
    decimal Budget,
    string DepartmentName,
    string SupplyMethod,
    string? SupplyMethodType,
    string? SupplyMethodSpecialType,
    string? Status,
    SourceType? SourceType,
    string? ContractTypeLabel,
    CmProcessType ProcessType);

// กล่องผลลัพธ์แยกส่วน (แต่ละส่วนมี count + page)
public record SectionResult<T>(PaginatedQueryResult<T> Page);

public record CombinedWorklistItem(
    string Source,
    string? Type,
    Guid Id,
    Guid? DetailId,
    string Number,
    string? Name,
    int? Year,
    decimal SummaryBudget,
    int? PlanCount,
    string? DepartmentName,
    string SupplyMethodName,
    string StatusCode,
    string? Step,
    DateTimeOffset? SortAt,
    string? ProcessType,
    Guid? ContractDraftVendorId = null,
    string? VendorName = null,
    IReadOnlyCollection<string>? GLAccounts = null,
    string? PlanNumber = null);

public record WorklistCounts(
    int Plans,
    int PlanAnnouncements,
    int PreProcurement,
    int Procurement,
    int ContractAgreement,
    int ContractManagement,
    int ExpenseDisbursement,
    int Combined);

// ---------- Helper Classes ----------
public sealed record UserContext(
    SuUser User,
    RawEmployeeView ViewUser,
    BusinessUnitId? PrimaryDepartmentId,
    bool IsJorPor,
    string? PrimaryOrganizationLevel,
    IEnumerable<EffectiveUserId> EffectiveUserIds);

public sealed record EffectiveUserId(UserId UserId, bool IsDelegateeUser);

public sealed class WorklistCountsAccumulator
{
    public int Plans { get; set; }

    public int PlanAnnouncements { get; set; }

    public int PreProcurement { get; set; }

    public int Procurement { get; set; }

    public int ContractAgreement { get; set; }

    public int ContractManagement { get; set; }

    public int ExpenseDisbursement { get; set; }

    public WorklistCounts ToWorklistCounts()
    {
        var combined = this.Plans + this.PlanAnnouncements + this.PreProcurement +
                       this.Procurement + this.ContractAgreement + this.ContractManagement + this.ExpenseDisbursement;

        return new WorklistCounts(
            this.Plans,
            this.PlanAnnouncements,
            this.PreProcurement,
            this.Procurement,
            this.ContractAgreement,
            this.ContractManagement,
            this.ExpenseDisbursement,
            combined);
    }
}

public sealed class WorklistSections
{
    public SectionResult<PlanItem>? Plans { get; set; }

    public SectionResult<PlanAnnouncementItem>? Announcements { get; set; }

    public SectionResult<ProcurementItem>? PreProcurement { get; set; }

    public SectionResult<ProcurementItem>? Procurement { get; set; }

    public SectionResult<ProcurementItem>? ContractAgreement { get; set; }

    public SectionResult<ContractManagementItem>? ContractManagement { get; set; }

    public SectionResult<ContractAmendmentItem>? ContractAmendments { get; set; }

    public SectionResult<ExpenseDisbursementItem>? ExpenseDisbursement { get; set; }
}

public record GetWorklistSeparatedResponse(
    SectionResult<PlanItem>? Plans,
    SectionResult<PlanAnnouncementItem>? PlanAnnouncements,
    SectionResult<ProcurementItem>? PreProcurement,
    SectionResult<ProcurementItem>? Procurement,
    SectionResult<ProcurementItem>? ContractAgreement,
    SectionResult<ContractManagementItem>? ContractManagement,
    SectionResult<ContractAmendmentItem>? ContractAmendments,
    SectionResult<ExpenseDisbursementItem>? ExpenseDisbursements,
    PaginatedQueryResult<CombinedWorklistItem>? Combined,
    WorklistCounts Counts);

// ---------- Endpoint ----------

/// <summary>
/// Endpoint for retrieving separated worklist data with pagination and filtering capabilities.
/// </summary>
public class GetWorklistSeparated : EndpointBase<GetWorklistSeparatedRequest, Ok<GetWorklistSeparatedResponse>>
{
    private readonly IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory;
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;
    private readonly PlanProgram planProgram;
    private readonly PlanAnnouncementProgram planAnnouncementProgram;
    private readonly ProcurementProgram procurementProgram;
    private readonly ContractManagementProgram contractManagementProgram;
    private readonly ContractAmendmentProgram contractAmendmentProgram;
    private readonly ExpenseDisbursementProgram expenseDisbursementProgram;

    public GetWorklistSeparated(
        IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory,
        Dp2DbContext dbContext,
        IOperationService operationService,
        ILogger<GetWorklistSeparated> logger)
        : base(logger)
    {
        this.dbContextFactory = dbContextFactory;
        this.dbContext = dbContext;
        this.operationService = operationService;
        this.planProgram = new PlanProgram();
        this.planAnnouncementProgram = new PlanAnnouncementProgram();
        this.procurementProgram = new ProcurementProgram();
        this.contractManagementProgram = new ContractManagementProgram();
        this.contractAmendmentProgram = new ContractAmendmentProgram();
        this.expenseDisbursementProgram = new ExpenseDisbursementProgram();
    }

    public override void Configure()
    {
        this.Options(b => b.WithTags("Worklist").WithName("GetWorklistSeparated"));
        this.Get("worklist");
    }

    /// <summary>
    /// Handles the worklist request and returns separated worklist sections.
    /// </summary>
    /// <param name="req">The worklist request parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Separated worklist response with pagination and counts.</returns>
    protected override async ValueTask<Ok<GetWorklistSeparatedResponse>> HandleRequestAsync(
        GetWorklistSeparatedRequest req,
        CancellationToken ct)
    {
        var userId = UserId.From(req.UserId);
        var userContext = await this.GetUserContextAsync(userId, ct);

        if (userContext is null)
        {
            // User has no employee/org-chart data (missing entry in raw_employee_view).
            // Return an empty worklist instead of crashing FastEndpoints' SendResultAsync(null).
            return TypedResults.Ok(new GetWorklistSeparatedResponse(
                Plans: null,
                PlanAnnouncements: null,
                PreProcurement: null,
                Procurement: null,
                ContractAgreement: null,
                ContractManagement: null,
                ContractAmendments: null,
                ExpenseDisbursements: null,
                Combined: null,
                Counts: new WorklistCounts(0, 0, 0, 0, 0, 0, 0, 0)));
        }

        // คำนวณครั้งเดียวก่อน fan-out (เลี่ยงเรียก GetSegmentAccountingMembersAsync ซ้ำ/ชนกันบน DbContext เดียว)
        var segmentAccountingMembers = await this.operationService.GetSegmentAccountingMembersAsync(ct);
        var isSegmentAccountingMember = ProcurementProgram.IsSegmentAccountingMember(segmentAccountingMembers, userContext);

        var results = new ConcurrentWorklistResults();

        // Execute ALL programs in parallel using Task.WhenAll
        await Task.WhenAll(
            this.ExecutePlanProgramAsync(req, userContext, results, ct),
            this.ExecuteAnnouncementProgramAsync(req, userContext, results, ct),
            this.ExecuteProcurementProgramAsync(req, userContext, isSegmentAccountingMember, results, ct),
            this.ExecuteContractManagementProgramAsync(req, userContext, isSegmentAccountingMember, results, ct),
            this.ExecuteContractAmendmentProgramAsync(req, results, ct),
            this.ExecuteExpenseDisbursementProgramAsync(req, userContext, results, ct));

        // Process combined worklist if requested
        PaginatedQueryResult<CombinedWorklistItem>? combined = null;

        if (req.IncludeAll)
        {
            await using var ctx = await this.dbContextFactory.CreateDbContextAsync(ct);
            var combinedBuilder = new CombinedWorklistBuilder(
                this.dbContextFactory,
                this.planProgram,
                this.planAnnouncementProgram,
                this.procurementProgram,
                this.contractManagementProgram,
                this.expenseDisbursementProgram);
            combined = await combinedBuilder.BuildCombinedWorklistAsync(req, userContext, isSegmentAccountingMember, ct);
        }

        var sections = results.ToSections();
        var counts = results.ToCounts();

        return TypedResults.Ok(new GetWorklistSeparatedResponse(
            sections.Plans,
            sections.Announcements,
            sections.PreProcurement,
            sections.Procurement,
            sections.ContractAgreement,
            sections.ContractManagement,
            sections.ContractAmendments,
            sections.ExpenseDisbursement,
            combined,
            counts));
    }

    private async Task ExecutePlanProgramAsync(
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        ConcurrentWorklistResults results,
        CancellationToken ct)
    {
        await using var ctx = await this.dbContextFactory.CreateDbContextAsync(ct);
        var (count, section) = await this.planProgram.ProcessPlanSectionAsync(ctx, req, userContext, ct);
        results.SetPlanResult(count, section);
    }

    private async Task ExecuteAnnouncementProgramAsync(
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        ConcurrentWorklistResults results,
        CancellationToken ct)
    {
        await using var ctx = await this.dbContextFactory.CreateDbContextAsync(ct);
        var (count, section) = await this.planAnnouncementProgram.ProcessPlanAnnouncementSectionAsync(ctx, req, userContext, ct);
        results.SetAnnouncementResult(count, section);
    }

    private async Task ExecuteProcurementProgramAsync(
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        bool isSegmentAccountingMember,
        ConcurrentWorklistResults results,
        CancellationToken ct)
    {
        var (preProcurementCount, procurementCount, contractAgreementCount, preProcurementSection, procurementSection, contractAgreementSection) =
            await this.procurementProgram.ProcessProcurementSectionsAsync(this.dbContextFactory, req, userContext, isSegmentAccountingMember, ct);
        results.SetPreProcurementResult(preProcurementCount, preProcurementSection);
        results.SetProcurementResult(procurementCount, procurementSection);
        results.SetContractAgreementResult(contractAgreementCount, contractAgreementSection);
    }

    private async Task ExecuteContractManagementProgramAsync(
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        bool isSegmentAccountingMember,
        ConcurrentWorklistResults results,
        CancellationToken ct)
    {
        await using var ctx = await this.dbContextFactory.CreateDbContextAsync(ct);
        var (count, section) = await this.contractManagementProgram.ProcessContractManagementSectionAsync(ctx, this.dbContextFactory, req, userContext, isSegmentAccountingMember, ct);
        results.SetContractManagementResult(count, section);
    }

    private async Task ExecuteContractAmendmentProgramAsync(
        GetWorklistSeparatedRequest req,
        ConcurrentWorklistResults results,
        CancellationToken ct)
    {
        var section = await this.contractAmendmentProgram.ProcessContractAmendmentSectionAsync(this.dbContextFactory, req, ct);
        results.SetContractAmendmentResult(section);
    }

    private async Task ExecuteExpenseDisbursementProgramAsync(
        GetWorklistSeparatedRequest req,
        UserContext userContext,
        ConcurrentWorklistResults results,
        CancellationToken ct)
    {
        await using var ctx = await this.dbContextFactory.CreateDbContextAsync(ct);
        var (count, section) = await this.expenseDisbursementProgram.ProcessExpenseDisbursementSectionAsync(ctx, req, userContext, ct);
        results.SetExpenseDisbursementResult(count, section);
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

        var now = DateTimeOffset.UtcNow;
        var activeDelegatorIds = await this.dbContext.Set<SuDelegatee>()
                                           .Where(dg => dg.Active
                                               && dg.SuUserId == userId
                                               && dg.SuDelegator.DelegationStartDate <= now
                                               && dg.SuDelegator.DelegationEndDate >= now)
                                           .Select(dg => dg.SuDelegator.SuUserId)
                                           .Distinct()
                                           .ToListAsync(ct);

        foreach (var delegatorId in activeDelegatorIds)
        {
            effectiveUserIds.Add(new EffectiveUserId(delegatorId, true));
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
}