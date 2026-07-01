namespace GHB.DP2.Application.Features.SummaryAll;

using GHB.DP2.Application.Common;
using GHB.DP2.Application.Constants;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetListAllProgram(
    int PageNumber,
    int PageSize,
    string? SearchText);

public record WorkflowStepDto(string Name, string RefNumber, string Url);

public record DapPlanLookupItem(
    Guid Id,
    string? PlanNumber,
    string? PlanName,
    decimal? Budget,
    string? DepartmentName,
    string? SupplyMethod,
    string? SupplyMethodType,
    string? SupplyMethodSpecialType);

public record DapProcurementLookupItem(
    Guid Id,
    Guid? PlanId,
    string? PlanNumber,
    string? PlanName,
    string? ProcurementNumber,
    decimal? Budget,
    string? DepartmentName,
    string? SupplyMethod,
    string? SupplyMethodType,
    string? SupplyMethodSpecialType);

public record DapContractVendorLookupItem(
    Guid Id,
    string? ContractNumber,
    string? ContractName,
    Guid? ProcurementId,
    string? ProcurementNumber,
    Guid? PlanId,
    string? PlanNumber,
    decimal? Budget,
    string? DepartmentName,
    string? SupplyMethod,
    string? SupplyMethodType,
    string? SupplyMethodSpecialType,
    string? VendorEstablishmentName,
    string? VendorPlaceName);

public record DapVendorEditLookupItem(
    Guid Id,
    string? EditContractNumber,
    string? EditContractName,
    decimal? Budget,
    Guid? VendorId,
    string? VendorContractNumber,
    Guid? ProcurementId,
    string? ProcurementNumber,
    Guid? PlanId,
    string? PlanNumber,
    string? DepartmentName,
    string? SupplyMethod,
    string? SupplyMethodType,
    string? SupplyMethodSpecialType,
    string? VendorEstablishmentName,
    string? VendorPlaceName);

public record GetListAllProgramResponseItems(
    Guid Id,
    string? ProgramNumber,
    string? ProcurementNumber,
    string? ContractNumber,
    string? ProgramName,
    decimal? Budget,
    int? BudgetYear,
    string? DepartmentName,
    string? SupplyMethodName,
    string? SupplyMethodTypeName,
    string? SupplyMethodSpecialTypeName,
    string? VendorName,
    string? Type,
    DateTimeOffset CreatedDate,
    DateTimeOffset? LastModifiedDate,
    List<WorkflowStepDto> Steps);

public class GetListAllProgramList : EndpointBase<GetListAllProgram, Ok<PaginatedQueryResult<GetListAllProgramResponseItems>>>
{
    private readonly Dp2DbContext dbContext;

    public GetListAllProgramList(
        Dp2DbContext dbContext,
        ILogger<GetListAllProgramList> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("/summary-all/programs");
    }

    private static string Fmt(string template, object arg0) => string.Format(template, arg0);

    private static string Fmt(string template, object arg0, object arg1) => string.Format(template, arg0, arg1);

    protected override async ValueTask<Ok<PaginatedQueryResult<GetListAllProgramResponseItems>>> HandleRequestAsync(GetListAllProgram req, CancellationToken ct)
    {
        var keywords = string.IsNullOrWhiteSpace(req.SearchText)
            ? System.Array.Empty<string>()
            : req.SearchText.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var hasSearch = keywords.Length > 0;

        // Phase 1: Query all program types and materialize as anonymous types
        var plansQuery = this.dbContext.Plans.Where(p => !p.IsDeleted && p.IsActive).AsNoTracking();
        foreach (var kw in keywords)
        {
            var k = kw;
            plansQuery = plansQuery.Where(p =>
                EF.Functions.ILike((string)p.PlanNumber, $"%{k}%") ||
                EF.Functions.ILike(p.Name, $"%{k}%") ||
                EF.Functions.ILike(p.Department.Name, $"%{k}%") ||
                EF.Functions.ILike(p.SupplyMethod.Label, $"%{k}%") ||
                EF.Functions.ILike(p.Budget.ToString()!, $"%{k}%") ||
                EF.Functions.ILike(p.BudgetYear.ToString()!, $"%{k}%"));
        }

        var plansRaw = await plansQuery.Select(p => new
        {
            Id = p.Id.Value,
            PlanNumber = (string)p.PlanNumber,
            Name = p.Name,
            Budget = (decimal?)p.Budget,
            BudgetYear = (int?)p.BudgetYear,
            DepartmentName = p.Department.Name,
            SupplyMethod = p.SupplyMethod.Label,
            SupplyMethodType = p.SupplyMethodType != null ? p.SupplyMethodType.Label : null,
            SupplyMethodSpecialType = p.SupplyMethodSpecialType != null ? p.SupplyMethodSpecialType.Label : null,
            p.AuditInfo.CreatedAt,
            p.AuditInfo.LastModifiedAt,
        })
                                 .ToListAsync(ct);

        var planAnnouncementsQuery = this.dbContext.PlanAnnouncements.Where(pa => !pa.IsDeleted).AsNoTracking();
        foreach (var kw in keywords)
        {
            var k = kw;
            planAnnouncementsQuery = planAnnouncementsQuery.Where(pa =>
                EF.Functions.ILike((string)pa.PlanAnnouncementNumber, $"%{k}%") ||
                (!string.IsNullOrWhiteSpace(pa.AnnouncementTitle) && EF.Functions.ILike(pa.AnnouncementTitle!, $"%{k}%")) ||
                EF.Functions.ILike(pa.SupplyMethodInfo.Label, $"%{k}%") ||
                pa.AnnouncementSelectedInformations.Any(x => EF.Functions.ILike(x.Plan.Department.Name, $"%{k}%")) ||
                pa.AnnouncementSelectedInformations.Any(x => EF.Functions.ILike(x.Plan.Budget.ToString()!, $"%{k}%")) ||
                pa.AnnouncementSelectedInformations.Any(x => EF.Functions.ILike(x.Plan.BudgetYear.ToString()!, $"%{k}%")));
        }

        var planAnnouncementsRaw = await planAnnouncementsQuery.Select(pa => new
        {
            Id = pa.Id.Value,
            PlanAnnouncementNumber = (string)pa.PlanAnnouncementNumber,
            Title = pa.AnnouncementTitle,
            Budget = pa.AnnouncementSelectedInformations.Sum(x => x.Plan.Budget),
            DepartmentName = pa.AnnouncementSelectedInformations
                                                                    .Select(x => x.Plan.Department.Name)
                                                                    .FirstOrDefault(),
            SupplyMethod = pa.SupplyMethodInfo.Label,
            SupplyMethodType = pa.AnnouncementSelectedInformations
                                                                      .Select(x => x.Plan.SupplyMethodType != null ? x.Plan.SupplyMethodType.Label : null)
                                                                      .FirstOrDefault(),
            SupplyMethodSpecialType = pa.AnnouncementSelectedInformations
                                                                             .Select(x => x.Plan.SupplyMethodSpecialType != null ? x.Plan.SupplyMethodSpecialType.Label : null)
                                                                             .FirstOrDefault(),
            FirstPlanId = pa.AnnouncementSelectedInformations
                                                                 .Select(x => (Guid?)x.PlanId.Value)
                                                                 .FirstOrDefault(),
            FirstPlanNumber = pa.AnnouncementSelectedInformations
                                                                     .Select(x => (string?)x.Plan.PlanNumber.Value)
                                                                     .FirstOrDefault(),
            BudgetYear = pa.AnnouncementSelectedInformations
                                                              .Select(x => (int?)x.Plan.BudgetYear)
                                                              .FirstOrDefault(),
            pa.AuditInfo.CreatedAt,
            pa.AuditInfo.LastModifiedAt,
        })
                                             .ToListAsync(ct);

        var procurementsQuery = this.dbContext.Procurements.Where(p => !p.IsDeleted).AsNoTracking();
        foreach (var kw in keywords)
        {
            var k = kw;
            procurementsQuery = procurementsQuery.Where(p =>
                (p.ProcurementNumber != null && EF.Functions.ILike((string)p.ProcurementNumber.Value, $"%{k}%")) ||
                EF.Functions.ILike(p.Name, $"%{k}%") ||
                p.ContractInvitations.Any(ci => ci.Vendors.Any(v => v.ContractNumber != null && EF.Functions.ILike(v.ContractNumber, $"%{k}%"))) ||
                (p.Plan != null && EF.Functions.ILike((string)p.Plan.PlanNumber, $"%{k}%")) ||
                (p.Plan != null && EF.Functions.ILike(p.Plan.Name, $"%{k}%")) ||
                (p.Plan != null && p.Plan.AnnouncementSelectedInformation.Any(asi =>
                    !asi.IsDeleted && EF.Functions.ILike((string)asi.PlanAnnouncement.PlanAnnouncementNumber, $"%{k}%"))) ||
                EF.Functions.ILike(p.Department.Name, $"%{k}%") ||
                EF.Functions.ILike(p.SupplyMethod.Label, $"%{k}%") ||
                EF.Functions.ILike(p.Budget.ToString()!, $"%{k}%") ||
                EF.Functions.ILike(p.BudgetYear.ToString()!, $"%{k}%"));
        }

        var procurementsRaw = await procurementsQuery.Select(p => new
        {
            Id = p.Id.Value,
            PlanId = p.PlanId != null ? (Guid?)p.PlanId.Value : null,
            PlanNumber = p.Plan != null ? (string?)p.Plan.PlanNumber.Value : null,
            ProcurementNumber = p.ProcurementNumber != null ? (string)p.ProcurementNumber.Value : null,
            ContractNumber = string.Join(
                                                ", ",
                                                p.ContractInvitations
                                                 .SelectMany(ci => ci.Vendors)
                                                 .Where(v => !string.IsNullOrEmpty(v.ContractNumber))
                                                 .Select(v => v.ContractNumber)),
            p.Name,
            p.Budget,
            BudgetYear = (int?)p.BudgetYear,
            DepartmentName = p.Department.Name,
            SupplyMethod = p.SupplyMethod.Label,
            SupplyMethodType = p.SupplyMethodType != null ? p.SupplyMethodType.Label : null,
            SupplyMethodSpecialType = p.SupplyMethodSpecialType != null ? p.SupplyMethodSpecialType.Label : null,
            p.AuditInfo.CreatedAt,
            p.AuditInfo.LastModifiedAt,
        })
                                        .ToListAsync(ct);

        var pw119Query = this.dbContext.Pw119s.Where(pw => !pw.IsDeleted && pw.IsActive).AsNoTracking();
        foreach (var kw in keywords)
        {
            var k = kw;
            pw119Query = pw119Query.Where(pw =>
                EF.Functions.ILike((string)pw.Pw119Number, $"%{k}%") ||
                EF.Functions.ILike(pw.Subject, $"%{k}%") ||
                EF.Functions.ILike(pw.Department.Name, $"%{k}%") ||
                EF.Functions.ILike(pw.SupplyMethod.Label, $"%{k}%") ||
                EF.Functions.ILike(pw.Budget.ToString()!, $"%{k}%") ||
                EF.Functions.ILike(pw.BudgetYear.ToString()!, $"%{k}%"));
        }

        var pw119Raw = await pw119Query.Select(pw => new
        {
            Id = pw.Id.Value,
            Pw119Number = (string)pw.Pw119Number,
            pw.Subject,
            pw.Budget,
            BudgetYear = (int?)pw.BudgetYear,
            DepartmentName = pw.Department.Name,
            SupplyMethod = pw.SupplyMethod.Label,
            SupplyMethodSpecialType = pw.SupplyMethodSpecialType != null ? pw.SupplyMethodSpecialType.Label : null,
            pw.AuditInfo.CreatedAt,
            pw.AuditInfo.LastModifiedAt,
        })
                                 .ToListAsync(ct);

        var p79Clause2Query = this.dbContext.P79Clause2s.Where(p => !p.IsDeleted && p.IsActive).AsNoTracking();
        foreach (var kw in keywords)
        {
            var k = kw;
            p79Clause2Query = p79Clause2Query.Where(p =>
                EF.Functions.ILike((string)p.P79Clause2Number, $"%{k}%") ||
                EF.Functions.ILike(p.Subject, $"%{k}%") ||
                EF.Functions.ILike(p.Department.Name, $"%{k}%") ||
                EF.Functions.ILike(p.SupplyMethod.Label, $"%{k}%") ||
                EF.Functions.ILike(p.Budget.ToString()!, $"%{k}%") ||
                EF.Functions.ILike(p.BudgetYear.ToString()!, $"%{k}%"));
        }

        var p79Clause2Raw = await p79Clause2Query.Select(p => new
        {
            Id = p.Id.Value,
            P79Clause2Number = (string)p.P79Clause2Number,
            p.Subject,
            p.Budget,
            BudgetYear = (int?)p.BudgetYear,
            DepartmentName = p.Department.Name,
            SupplyMethod = p.SupplyMethod.Label,
            SupplyMethodType = p.SupplyMethodType != null ? p.SupplyMethodType.Label : null,
            SupplyMethodSpecialType = p.SupplyMethodSpecialType != null ? p.SupplyMethodSpecialType.Label : null,
            p.AuditInfo.CreatedAt,
            p.AuditInfo.LastModifiedAt,
        })
                                      .ToListAsync(ct);

        var pettyCashQuery = this.dbContext.PPettyCashs.Where(p => !p.IsDeleted && p.IsActive).AsNoTracking();
        foreach (var kw in keywords)
        {
            var k = kw;
            pettyCashQuery = pettyCashQuery.Where(p =>
                EF.Functions.ILike((string)p.PettyCashNumber, $"%{k}%") ||
                EF.Functions.ILike(p.Subject, $"%{k}%") ||
                EF.Functions.ILike(p.Department.Name, $"%{k}%") ||
                EF.Functions.ILike(p.SupplyMethod.Label, $"%{k}%") ||
                EF.Functions.ILike(p.Budget.ToString()!, $"%{k}%") ||
                EF.Functions.ILike(p.BudgetYear.ToString()!, $"%{k}%"));
        }

        var pettyCashRaw = await pettyCashQuery.Select(p => new
        {
            Id = p.Id.Value,
            PettyCashNumber = (string)p.PettyCashNumber,
            p.Subject,
            p.Budget,
            BudgetYear = (int?)p.BudgetYear,
            DepartmentName = p.Department.Name,
            SupplyMethod = p.SupplyMethod.Label,
            SupplyMethodType = p.SupplyMethodType != null ? p.SupplyMethodType.Label : null,
            SupplyMethodSpecialType = p.SupplyMethodSpecialType != null ? p.SupplyMethodSpecialType.Label : null,
            p.AuditInfo.CreatedAt,
            p.AuditInfo.LastModifiedAt,
        })
                                     .ToListAsync(ct);

        var pettyCashReimbursementQuery = this.dbContext.PPettyCashReimbursements.Where(p => !p.IsDeleted).AsNoTracking();
        foreach (var kw in keywords)
        {
            var k = kw;
            pettyCashReimbursementQuery = pettyCashReimbursementQuery.Where(p =>
                EF.Functions.ILike(p.Number, $"%{k}%") ||
                EF.Functions.ILike(p.Subject, $"%{k}%"));
        }

        var pettyCashReimbursementRaw = await pettyCashReimbursementQuery.Select(p => new
        {
            Id = p.Id.Value,
            p.Number,
            p.Subject,
            DepartmentName = p.Department.Name,
            p.AuditInfo.CreatedAt,
            p.AuditInfo.LastModifiedAt,
        })
                                                  .ToListAsync(ct);

        var contractAgreementQuery = this.dbContext.CaContractDrafts.Where(c => !c.IsDeleted).AsNoTracking();
        foreach (var kw in keywords)
        {
            var k = kw;
            contractAgreementQuery = contractAgreementQuery.Where(c =>
                (c.Procurement != null && c.Procurement.ProcurementNumber != null &&
                 EF.Functions.ILike((string)c.Procurement.ProcurementNumber.Value, $"%{k}%")) ||
                (c.Procurement != null && EF.Functions.ILike(c.Procurement.Name, $"%{k}%")) ||
                c.Vendors.Any(v => v.ContractNumber != null && EF.Functions.ILike(v.ContractNumber, $"%{k}%")) ||
                c.Vendors.Any(v => v.Vendor.VendorInfo != null && !string.IsNullOrEmpty(v.Vendor.VendorInfo.EstablishmentName) && EF.Functions.ILike(v.Vendor.VendorInfo.EstablishmentName, $"%{k}%")) ||
                c.Vendors.Any(v => v.Vendor.VendorInfo != null && !string.IsNullOrEmpty(v.Vendor.VendorInfo.PlaceName) && EF.Functions.ILike(v.Vendor.VendorInfo.PlaceName, $"%{k}%")) ||
                (c.Procurement != null && c.Procurement.Plan != null &&
                 EF.Functions.ILike((string)c.Procurement.Plan.PlanNumber, $"%{k}%")) ||
                (c.Procurement != null && c.Procurement.Plan != null &&
                 EF.Functions.ILike(c.Procurement.Plan.Name, $"%{k}%")) ||
                (c.Procurement != null && c.Procurement.Plan != null &&
                 c.Procurement.Plan.AnnouncementSelectedInformation.Any(asi =>
                     !asi.IsDeleted && EF.Functions.ILike((string)asi.PlanAnnouncement.PlanAnnouncementNumber, $"%{k}%"))) ||
                (c.Procurement != null && c.Procurement.Department != null &&
                 EF.Functions.ILike(c.Procurement.Department.Name, $"%{k}%")) ||
                (c.Procurement != null && c.Procurement.SupplyMethod != null &&
                 EF.Functions.ILike(c.Procurement.SupplyMethod.Label, $"%{k}%")) ||
                (c.Procurement != null &&
                 EF.Functions.ILike(c.Procurement.Budget.ToString()!, $"%{k}%")) ||
                (c.Procurement != null &&
                 EF.Functions.ILike(c.Procurement.BudgetYear.ToString()!, $"%{k}%")));
        }

        var contractAgreementRaw = await contractAgreementQuery.Select(c => new
        {
            Id = c.Id.Value,
            PlanId = c.Procurement != null && c.Procurement.PlanId != null ? (Guid?)c.Procurement.PlanId.Value : null,
            PlanNumber = c.Procurement != null && c.Procurement.Plan != null ? (string?)c.Procurement.Plan.PlanNumber.Value : null,
            ProcurementId = c.ProcurementId.Value,
            ProcurementNumber = c.Procurement != null && c.Procurement.ProcurementNumber != null ? (string?)c.Procurement.ProcurementNumber.Value : null,
            ProcurementType = c.Procurement != null ? (ProcurementType?)c.Procurement.Type : null,
            ContractNumber = string.Join(
                                                     ", ",
                                                     c.Vendors.Where(v => !string.IsNullOrEmpty(v.ContractNumber))
                                                      .Select(v => v.ContractNumber)),
            VendorNames = string.Join(
                                                     ", ",
                                                     c.Vendors.Where(v => !string.IsNullOrEmpty(v.Vendor.VendorInfo.EstablishmentName))
                                                      .Select(v => v.Vendor.VendorInfo.EstablishmentName!)),
            VendorPlaceNames = string.Join(
                                                     ", ",
                                                     c.Vendors.Where(v => !string.IsNullOrEmpty(v.Vendor.VendorInfo.PlaceName))
                                                      .Select(v => v.Vendor.VendorInfo.PlaceName!)),
            VendorInfos = c.Vendors
                                                     .Select(v => new { VendorId = v.Id.Value, v.ContractNumber })
                                                     .ToList(),
            Name = c.Procurement != null ? c.Procurement.Name : null,
            Budget = c.Procurement != null ? (decimal?)c.Procurement.Budget : null,
            BudgetYear = c.Procurement != null ? (int?)c.Procurement.BudgetYear : null,
            DepartmentName = c.Procurement != null && c.Procurement.Department != null ? c.Procurement.Department.Name : null,
            SupplyMethod = c.Procurement != null && c.Procurement.SupplyMethod != null ? c.Procurement.SupplyMethod.Label : null,
            SupplyMethodType = c.Procurement != null && c.Procurement.SupplyMethodType != null ? c.Procurement.SupplyMethodType.Label : null,
            SupplyMethodSpecialType = c.Procurement != null && c.Procurement.SupplyMethodSpecialType != null ? c.Procurement.SupplyMethodSpecialType.Label : null,
            c.AuditInfo.CreatedAt,
            c.AuditInfo.LastModifiedAt,
        })
                                             .ToListAsync(ct);

        var contractTerminationQuery = this.dbContext.CmContractTerminations.Where(ct2 => !ct2.IsDeleted).AsNoTracking();
        foreach (var kw in keywords)
        {
            var k = kw;
            contractTerminationQuery = contractTerminationQuery.Where(ct2 =>
                (ct2.CaContractDraftVendor != null &&
                 (EF.Functions.ILike(ct2.CaContractDraftVendor.ContractNumber, $"%{k}%") ||
                  EF.Functions.ILike(ct2.CaContractDraftVendor.ContractName, $"%{k}%"))) ||
                (ct2.CaContractDraftVendor != null && ct2.CaContractDraftVendor.ContractDraft != null && ct2.CaContractDraftVendor.ContractDraft.Procurement != null &&
                 ct2.CaContractDraftVendor.ContractDraft.Procurement.ProcurementNumber != null &&
                 EF.Functions.ILike((string)ct2.CaContractDraftVendor.ContractDraft.Procurement.ProcurementNumber.Value, $"%{k}%")) ||
                (ct2.CaContractDraftVendor != null && ct2.CaContractDraftVendor.ContractDraft != null && ct2.CaContractDraftVendor.ContractDraft.Procurement != null &&
                 EF.Functions.ILike(ct2.CaContractDraftVendor.ContractDraft.Procurement.Name, $"%{k}%")) ||
                (ct2.CaContractDraftVendor != null && ct2.CaContractDraftVendor.ContractDraft != null && ct2.CaContractDraftVendor.ContractDraft.Procurement != null &&
                 ct2.CaContractDraftVendor.ContractDraft.Procurement.Plan != null &&
                 EF.Functions.ILike((string)ct2.CaContractDraftVendor.ContractDraft.Procurement.Plan.PlanNumber, $"%{k}%")) ||
                (ct2.CaContractDraftVendor != null && ct2.CaContractDraftVendor.ContractDraft != null && ct2.CaContractDraftVendor.ContractDraft.Procurement != null &&
                 ct2.CaContractDraftVendor.ContractDraft.Procurement.Department != null &&
                 EF.Functions.ILike(ct2.CaContractDraftVendor.ContractDraft.Procurement.Department.Name, $"%{k}%")) ||
                (ct2.CaContractDraftVendor != null && ct2.CaContractDraftVendor.ContractDraft != null && ct2.CaContractDraftVendor.ContractDraft.Procurement != null &&
                 ct2.CaContractDraftVendor.ContractDraft.Procurement.SupplyMethod != null &&
                 EF.Functions.ILike(ct2.CaContractDraftVendor.ContractDraft.Procurement.SupplyMethod.Label, $"%{k}%")) ||
                (ct2.CaContractDraftVendor != null &&
                 EF.Functions.ILike(ct2.CaContractDraftVendor.Budget.ToString()!, $"%{k}%")) ||
                (ct2.CaContractDraftVendor != null && ct2.CaContractDraftVendor.ContractDraft != null && ct2.CaContractDraftVendor.ContractDraft.Procurement != null &&
                 EF.Functions.ILike(ct2.CaContractDraftVendor.ContractDraft.Procurement.BudgetYear.ToString()!, $"%{k}%")) ||
                (ct2.CaContractDraftVendor != null && ct2.CaContractDraftVendor.Vendor.VendorInfo != null && !string.IsNullOrEmpty(ct2.CaContractDraftVendor.Vendor.VendorInfo.EstablishmentName) &&
                 EF.Functions.ILike(ct2.CaContractDraftVendor.Vendor.VendorInfo.EstablishmentName, $"%{k}%")) ||
                (ct2.CaContractDraftVendor != null && ct2.CaContractDraftVendor.Vendor.VendorInfo != null && !string.IsNullOrEmpty(ct2.CaContractDraftVendor.Vendor.VendorInfo.PlaceName) &&
                 EF.Functions.ILike(ct2.CaContractDraftVendor.Vendor.VendorInfo.PlaceName, $"%{k}%")));
        }

        var contractTerminationRaw = await contractTerminationQuery.Select(ct2 => new
        {
            Id = ct2.Id.Value,
            ContractDraftVendorId = ct2.ContractDraftVendorId.Value,
            ContractDraftId = ct2.CaContractDraftVendor != null && ct2.CaContractDraftVendor.ContractDraft != null
                                                       ? (Guid?)ct2.CaContractDraftVendor.ContractDraft.Id.Value
                                                       : null,
            ContractNumber = ct2.CaContractDraftVendor != null ? ct2.CaContractDraftVendor.ContractNumber : null,
            ContractName = ct2.CaContractDraftVendor != null ? ct2.CaContractDraftVendor.ContractName : null,
            Budget = ct2.CaContractDraftVendor != null ? (decimal?)ct2.CaContractDraftVendor.Budget : null,
            BudgetYear = ct2.CaContractDraftVendor != null && ct2.CaContractDraftVendor.ContractDraft != null && ct2.CaContractDraftVendor.ContractDraft.Procurement != null
                                                       ? (int?)ct2.CaContractDraftVendor.ContractDraft.Procurement.BudgetYear
                                                       : null,
            PlanId = ct2.CaContractDraftVendor != null && ct2.CaContractDraftVendor.ContractDraft != null && ct2.CaContractDraftVendor.ContractDraft.Procurement != null &&
                                                            ct2.CaContractDraftVendor.ContractDraft.Procurement.PlanId != null
                                                       ? (Guid?)ct2.CaContractDraftVendor.ContractDraft.Procurement.PlanId.Value
                                                       : null,
            PlanNumber = ct2.CaContractDraftVendor != null && ct2.CaContractDraftVendor.ContractDraft != null && ct2.CaContractDraftVendor.ContractDraft.Procurement != null &&
                                                                ct2.CaContractDraftVendor.ContractDraft.Procurement.Plan != null
                                                       ? (string?)ct2.CaContractDraftVendor.ContractDraft.Procurement.Plan.PlanNumber.Value
                                                       : null,
            ProcurementId = ct2.CaContractDraftVendor != null && ct2.CaContractDraftVendor.ContractDraft != null && ct2.CaContractDraftVendor.ContractDraft.Procurement != null
                                                       ? (Guid?)ct2.CaContractDraftVendor.ContractDraft.ProcurementId.Value
                                                       : null,
            ProcurementNumber = ct2.CaContractDraftVendor != null && ct2.CaContractDraftVendor.ContractDraft != null &&
                                                                       ct2.CaContractDraftVendor.ContractDraft.Procurement != null && ct2.CaContractDraftVendor.ContractDraft.Procurement.ProcurementNumber != null
                                                       ? (string?)ct2.CaContractDraftVendor.ContractDraft.Procurement.ProcurementNumber.Value
                                                       : null,
            DepartmentName = ct2.CaContractDraftVendor != null && ct2.CaContractDraftVendor.ContractDraft != null &&
                                                                    ct2.CaContractDraftVendor.ContractDraft.Procurement != null && ct2.CaContractDraftVendor.ContractDraft.Procurement.Department != null
                                                       ? ct2.CaContractDraftVendor.ContractDraft.Procurement.Department.Name
                                                       : null,
            SupplyMethod = ct2.CaContractDraftVendor != null && ct2.CaContractDraftVendor.ContractDraft != null &&
                                                                  ct2.CaContractDraftVendor.ContractDraft.Procurement != null && ct2.CaContractDraftVendor.ContractDraft.Procurement.SupplyMethod != null
                                                       ? ct2.CaContractDraftVendor.ContractDraft.Procurement.SupplyMethod.Label
                                                       : null,
            SupplyMethodType = ct2.CaContractDraftVendor != null && ct2.CaContractDraftVendor.ContractDraft != null &&
                                                                      ct2.CaContractDraftVendor.ContractDraft.Procurement != null && ct2.CaContractDraftVendor.ContractDraft.Procurement.SupplyMethodType != null
                                                       ? ct2.CaContractDraftVendor.ContractDraft.Procurement.SupplyMethodType.Label
                                                       : null,
            SupplyMethodSpecialType =
                                                       ct2.CaContractDraftVendor != null && ct2.CaContractDraftVendor.ContractDraft != null && ct2.CaContractDraftVendor.ContractDraft.Procurement != null &&
                                                       ct2.CaContractDraftVendor.ContractDraft.Procurement.SupplyMethodSpecialType != null
                                                           ? ct2.CaContractDraftVendor.ContractDraft.Procurement.SupplyMethodSpecialType.Label
                                                           : null,
            VendorEstablishmentName = ct2.CaContractDraftVendor != null ? ct2.CaContractDraftVendor.Vendor.VendorInfo.EstablishmentName : null,
            VendorPlaceName = ct2.CaContractDraftVendor != null ? ct2.CaContractDraftVendor.Vendor.VendorInfo.PlaceName : null,
            ct2.AuditInfo.CreatedAt,
            ct2.AuditInfo.LastModifiedAt,
        })
                                               .ToListAsync(ct);

        var contractGuaranteeReturnQuery = this.dbContext.CmContractGuaranteeReturns.Where(cgr => !cgr.IsDeleted).AsNoTracking();
        foreach (var kw in keywords)
        {
            var k = kw;
            contractGuaranteeReturnQuery = contractGuaranteeReturnQuery.Where(cgr =>
                (cgr.CaContractDraftVendor != null &&
                 (EF.Functions.ILike(cgr.CaContractDraftVendor.ContractNumber, $"%{k}%") ||
                  EF.Functions.ILike(cgr.CaContractDraftVendor.ContractName, $"%{k}%"))) ||
                (cgr.CaContractDraftVendor != null && cgr.CaContractDraftVendor.ContractDraft != null &&
                 cgr.CaContractDraftVendor.ContractDraft.Procurement != null &&
                 cgr.CaContractDraftVendor.ContractDraft.Procurement.ProcurementNumber != null &&
                 EF.Functions.ILike((string)cgr.CaContractDraftVendor.ContractDraft.Procurement.ProcurementNumber.Value, $"%{k}%")) ||
                (cgr.CaContractDraftVendor != null && cgr.CaContractDraftVendor.ContractDraft != null &&
                 cgr.CaContractDraftVendor.ContractDraft.Procurement != null &&
                 EF.Functions.ILike(cgr.CaContractDraftVendor.ContractDraft.Procurement.Name, $"%{k}%")) ||
                (cgr.CaContractDraftVendor != null && cgr.CaContractDraftVendor.ContractDraft != null &&
                 cgr.CaContractDraftVendor.ContractDraft.Procurement != null &&
                 cgr.CaContractDraftVendor.ContractDraft.Procurement.Plan != null &&
                 EF.Functions.ILike((string)cgr.CaContractDraftVendor.ContractDraft.Procurement.Plan.PlanNumber, $"%{k}%")) ||
                (cgr.CaContractDraftVendor != null && cgr.CaContractDraftVendor.ContractDraft != null &&
                 cgr.CaContractDraftVendor.ContractDraft.Procurement != null &&
                 cgr.CaContractDraftVendor.ContractDraft.Procurement.Department != null &&
                 EF.Functions.ILike(cgr.CaContractDraftVendor.ContractDraft.Procurement.Department.Name, $"%{k}%")) ||
                (cgr.CaContractDraftVendor != null && cgr.CaContractDraftVendor.ContractDraft != null &&
                 cgr.CaContractDraftVendor.ContractDraft.Procurement != null &&
                 cgr.CaContractDraftVendor.ContractDraft.Procurement.SupplyMethod != null &&
                 EF.Functions.ILike(cgr.CaContractDraftVendor.ContractDraft.Procurement.SupplyMethod.Label, $"%{k}%")) ||
                (cgr.CaContractDraftVendor != null &&
                 EF.Functions.ILike(cgr.CaContractDraftVendor.Budget.ToString()!, $"%{k}%")) ||
                (cgr.CaContractDraftVendor != null && cgr.CaContractDraftVendor.ContractDraft != null &&
                 cgr.CaContractDraftVendor.ContractDraft.Procurement != null &&
                 EF.Functions.ILike(cgr.CaContractDraftVendor.ContractDraft.Procurement.BudgetYear.ToString()!, $"%{k}%")) ||
                (cgr.CaContractDraftVendor != null && cgr.CaContractDraftVendor.Vendor.VendorInfo != null && !string.IsNullOrEmpty(cgr.CaContractDraftVendor.Vendor.VendorInfo.EstablishmentName) &&
                 EF.Functions.ILike(cgr.CaContractDraftVendor.Vendor.VendorInfo.EstablishmentName, $"%{k}%")) ||
                (cgr.CaContractDraftVendor != null && cgr.CaContractDraftVendor.Vendor.VendorInfo != null && !string.IsNullOrEmpty(cgr.CaContractDraftVendor.Vendor.VendorInfo.PlaceName) &&
                 EF.Functions.ILike(cgr.CaContractDraftVendor.Vendor.VendorInfo.PlaceName, $"%{k}%")));
        }

        var contractGuaranteeReturnRaw = await contractGuaranteeReturnQuery.Select(cgr => new
        {
            Id = cgr.Id.Value,
            ContractDraftVendorId = cgr.ContractDraftVendorId.Value,
            ContractDraftId = cgr.CaContractDraftVendor != null && cgr.CaContractDraftVendor.ContractDraft != null
                                                           ? (Guid?)cgr.CaContractDraftVendor.ContractDraft.Id.Value
                                                           : null,
            ContractNumber = cgr.CaContractDraftVendor != null ? cgr.CaContractDraftVendor.ContractNumber : null,
            ContractName = cgr.CaContractDraftVendor != null ? cgr.CaContractDraftVendor.ContractName : null,
            Budget = cgr.CaContractDraftVendor != null ? (decimal?)cgr.CaContractDraftVendor.Budget : null,
            BudgetYear = cgr.CaContractDraftVendor != null && cgr.CaContractDraftVendor.ContractDraft != null && cgr.CaContractDraftVendor.ContractDraft.Procurement != null
                                                           ? (int?)cgr.CaContractDraftVendor.ContractDraft.Procurement.BudgetYear
                                                           : null,
            PlanId = cgr.CaContractDraftVendor != null && cgr.CaContractDraftVendor.ContractDraft != null && cgr.CaContractDraftVendor.ContractDraft.Procurement != null &&
                                                                cgr.CaContractDraftVendor.ContractDraft.Procurement.PlanId != null
                                                           ? (Guid?)cgr.CaContractDraftVendor.ContractDraft.Procurement.PlanId.Value
                                                           : null,
            PlanNumber = cgr.CaContractDraftVendor != null && cgr.CaContractDraftVendor.ContractDraft != null &&
                                                                    cgr.CaContractDraftVendor.ContractDraft.Procurement != null && cgr.CaContractDraftVendor.ContractDraft.Procurement.Plan != null
                                                           ? (string?)cgr.CaContractDraftVendor.ContractDraft.Procurement.Plan.PlanNumber.Value
                                                           : null,
            ProcurementId = cgr.CaContractDraftVendor != null && cgr.CaContractDraftVendor.ContractDraft != null &&
                                                                       cgr.CaContractDraftVendor.ContractDraft.Procurement != null
                                                           ? (Guid?)cgr.CaContractDraftVendor.ContractDraft.ProcurementId.Value
                                                           : null,
            ProcurementNumber = cgr.CaContractDraftVendor != null && cgr.CaContractDraftVendor.ContractDraft != null &&
                                                                           cgr.CaContractDraftVendor.ContractDraft.Procurement != null && cgr.CaContractDraftVendor.ContractDraft.Procurement.ProcurementNumber != null
                                                           ? (string?)cgr.CaContractDraftVendor.ContractDraft.Procurement.ProcurementNumber.Value
                                                           : null,
            DepartmentName = cgr.CaContractDraftVendor != null && cgr.CaContractDraftVendor.ContractDraft != null &&
                                                                        cgr.CaContractDraftVendor.ContractDraft.Procurement != null && cgr.CaContractDraftVendor.ContractDraft.Procurement.Department != null
                                                           ? cgr.CaContractDraftVendor.ContractDraft.Procurement.Department.Name
                                                           : null,
            SupplyMethod = cgr.CaContractDraftVendor != null && cgr.CaContractDraftVendor.ContractDraft != null &&
                                                                      cgr.CaContractDraftVendor.ContractDraft.Procurement != null && cgr.CaContractDraftVendor.ContractDraft.Procurement.SupplyMethod != null
                                                           ? cgr.CaContractDraftVendor.ContractDraft.Procurement.SupplyMethod.Label
                                                           : null,
            SupplyMethodType = cgr.CaContractDraftVendor != null && cgr.CaContractDraftVendor.ContractDraft != null &&
                                                                          cgr.CaContractDraftVendor.ContractDraft.Procurement != null && cgr.CaContractDraftVendor.ContractDraft.Procurement.SupplyMethodType != null
                                                           ? cgr.CaContractDraftVendor.ContractDraft.Procurement.SupplyMethodType.Label
                                                           : null,
            SupplyMethodSpecialType =
                                                           cgr.CaContractDraftVendor != null && cgr.CaContractDraftVendor.ContractDraft != null && cgr.CaContractDraftVendor.ContractDraft.Procurement != null &&
                                                           cgr.CaContractDraftVendor.ContractDraft.Procurement.SupplyMethodSpecialType != null
                                                               ? cgr.CaContractDraftVendor.ContractDraft.Procurement.SupplyMethodSpecialType.Label
                                                               : null,
            VendorEstablishmentName = cgr.CaContractDraftVendor != null ? cgr.CaContractDraftVendor.Vendor.VendorInfo.EstablishmentName : null,
            VendorPlaceName = cgr.CaContractDraftVendor != null ? cgr.CaContractDraftVendor.Vendor.VendorInfo.PlaceName : null,
            cgr.AuditInfo.CreatedAt,
            cgr.AuditInfo.LastModifiedAt,
        })
                                                   .ToListAsync(ct);

        var contractAmendmentQuery = this.dbContext.CamContractAmendments.Where(ca => !ca.IsDeleted).AsNoTracking();
        foreach (var kw in keywords)
        {
            var k = kw;
            contractAmendmentQuery = contractAmendmentQuery.Where(ca =>
                EF.Functions.ILike((string)ca.CamContractAmendmentNumber, $"%{k}%") ||
                (!string.IsNullOrWhiteSpace(ca.Remark) && EF.Functions.ILike(ca.Remark, $"%{k}%")) ||
                (ca.ContractDraftVendor != null && ca.ContractDraftVendor.ContractDraft != null && ca.ContractDraftVendor.ContractDraft.Procurement != null &&
                 ca.ContractDraftVendor.ContractDraft.Procurement.ProcurementNumber != null &&
                 EF.Functions.ILike((string)ca.ContractDraftVendor.ContractDraft.Procurement.ProcurementNumber.Value, $"%{k}%")) ||
                (ca.ContractDraftVendor != null && ca.ContractDraftVendor.ContractDraft != null && ca.ContractDraftVendor.ContractDraft.Procurement != null &&
                 EF.Functions.ILike(ca.ContractDraftVendor.ContractDraft.Procurement.Name, $"%{k}%")) ||
                (ca.ContractDraftVendor != null && ca.ContractDraftVendor.ContractDraft != null && ca.ContractDraftVendor.ContractDraft.Procurement != null &&
                 ca.ContractDraftVendor.ContractDraft.Procurement.Plan != null &&
                 EF.Functions.ILike((string)ca.ContractDraftVendor.ContractDraft.Procurement.Plan.PlanNumber, $"%{k}%")) ||
                (ca.ContractDraftVendor != null && ca.ContractDraftVendor.ContractDraft != null && ca.ContractDraftVendor.ContractDraft.Procurement != null &&
                 ca.ContractDraftVendor.ContractDraft.Procurement.Department != null &&
                 EF.Functions.ILike(ca.ContractDraftVendor.ContractDraft.Procurement.Department.Name, $"%{k}%")) ||
                (ca.ContractDraftVendor != null && ca.ContractDraftVendor.ContractDraft != null && ca.ContractDraftVendor.ContractDraft.Procurement != null &&
                 ca.ContractDraftVendor.ContractDraft.Procurement.SupplyMethod != null &&
                 EF.Functions.ILike(ca.ContractDraftVendor.ContractDraft.Procurement.SupplyMethod.Label, $"%{k}%")) ||
                (ca.ContractDraftVendor != null && ca.ContractDraftVendor.ContractDraft != null && ca.ContractDraftVendor.ContractDraft.Procurement != null &&
                 EF.Functions.ILike(ca.ContractDraftVendor.ContractDraft.Procurement.Budget.ToString()!, $"%{k}%")) ||
                (ca.ContractDraftVendor != null && ca.ContractDraftVendor.ContractDraft != null && ca.ContractDraftVendor.ContractDraft.Procurement != null &&
                 EF.Functions.ILike(ca.ContractDraftVendor.ContractDraft.Procurement.BudgetYear.ToString()!, $"%{k}%")) ||
                (ca.ContractDraftVendor != null && ca.ContractDraftVendor.Vendor.VendorInfo != null && !string.IsNullOrEmpty(ca.ContractDraftVendor.Vendor.VendorInfo.EstablishmentName) &&
                 EF.Functions.ILike(ca.ContractDraftVendor.Vendor.VendorInfo.EstablishmentName, $"%{k}%")) ||
                (ca.ContractDraftVendor != null && ca.ContractDraftVendor.Vendor.VendorInfo != null && !string.IsNullOrEmpty(ca.ContractDraftVendor.Vendor.VendorInfo.PlaceName) &&
                 EF.Functions.ILike(ca.ContractDraftVendor.Vendor.VendorInfo.PlaceName, $"%{k}%")));
        }

        var contractAmendmentRaw = await contractAmendmentQuery.Select(ca => new
        {
            Id = ca.Id.Value,
            CamNumber = (string)ca.CamContractAmendmentNumber,
            ca.Remark,
            ContractDraftId = ca.ContractDraftVendor != null && ca.ContractDraftVendor.ContractDraft != null
                                                     ? (Guid?)ca.ContractDraftVendor.ContractDraft.Id.Value
                                                     : null,
            PlanId = ca.ContractDraftVendor != null && ca.ContractDraftVendor.ContractDraft != null && ca.ContractDraftVendor.ContractDraft.Procurement != null &&
                                                          ca.ContractDraftVendor.ContractDraft.Procurement.PlanId != null
                                                     ? (Guid?)ca.ContractDraftVendor.ContractDraft.Procurement.PlanId.Value
                                                     : null,
            PlanNumber = ca.ContractDraftVendor != null && ca.ContractDraftVendor.ContractDraft != null && ca.ContractDraftVendor.ContractDraft.Procurement != null &&
                                                              ca.ContractDraftVendor.ContractDraft.Procurement.Plan != null
                                                     ? (string?)ca.ContractDraftVendor.ContractDraft.Procurement.Plan.PlanNumber.Value
                                                     : null,
            ProcurementId = ca.ContractDraftVendor != null && ca.ContractDraftVendor.ContractDraft != null && ca.ContractDraftVendor.ContractDraft.Procurement != null
                                                     ? (Guid?)ca.ContractDraftVendor.ContractDraft.ProcurementId.Value
                                                     : null,
            ProcurementNumber = ca.ContractDraftVendor != null && ca.ContractDraftVendor.ContractDraft != null && ca.ContractDraftVendor.ContractDraft.Procurement != null &&
                                                                     ca.ContractDraftVendor.ContractDraft.Procurement.ProcurementNumber != null
                                                     ? (string?)ca.ContractDraftVendor.ContractDraft.Procurement.ProcurementNumber.Value
                                                     : null,
            ContractNumber = ca.ContractDraftVendor != null ? ca.ContractDraftVendor.ContractNumber : null,
            ContractDraftVendorId = ca.ContractDraftVendor != null ? (Guid?)ca.ContractDraftVendor.Id.Value : null,
            Budget = ca.ContractDraftVendor != null && ca.ContractDraftVendor.ContractDraft != null && ca.ContractDraftVendor.ContractDraft.Procurement != null
                                                     ? (decimal?)ca.ContractDraftVendor.ContractDraft.Procurement.Budget
                                                     : null,
            BudgetYear = ca.ContractDraftVendor != null && ca.ContractDraftVendor.ContractDraft != null && ca.ContractDraftVendor.ContractDraft.Procurement != null
                                                     ? (int?)ca.ContractDraftVendor.ContractDraft.Procurement.BudgetYear
                                                     : null,
            DepartmentName = ca.ContractDraftVendor != null && ca.ContractDraftVendor.ContractDraft != null && ca.ContractDraftVendor.ContractDraft.Procurement != null &&
                                                                  ca.ContractDraftVendor.ContractDraft.Procurement.Department != null
                                                     ? ca.ContractDraftVendor.ContractDraft.Procurement.Department.Name
                                                     : null,
            SupplyMethod = ca.ContractDraftVendor != null && ca.ContractDraftVendor.ContractDraft != null && ca.ContractDraftVendor.ContractDraft.Procurement != null &&
                                                                ca.ContractDraftVendor.ContractDraft.Procurement.SupplyMethod != null
                                                     ? ca.ContractDraftVendor.ContractDraft.Procurement.SupplyMethod.Label
                                                     : null,
            SupplyMethodType = ca.ContractDraftVendor != null && ca.ContractDraftVendor.ContractDraft != null && ca.ContractDraftVendor.ContractDraft.Procurement != null &&
                                                                    ca.ContractDraftVendor.ContractDraft.Procurement.SupplyMethodType != null
                                                     ? ca.ContractDraftVendor.ContractDraft.Procurement.SupplyMethodType.Label
                                                     : null,
            SupplyMethodSpecialType =
                                                     ca.ContractDraftVendor != null && ca.ContractDraftVendor.ContractDraft != null && ca.ContractDraftVendor.ContractDraft.Procurement != null &&
                                                     ca.ContractDraftVendor.ContractDraft.Procurement.SupplyMethodSpecialType != null
                                                         ? ca.ContractDraftVendor.ContractDraft.Procurement.SupplyMethodSpecialType.Label
                                                         : null,
            VendorEstablishmentName = ca.ContractDraftVendor != null ? ca.ContractDraftVendor.Vendor.VendorInfo.EstablishmentName : null,
            VendorPlaceName = ca.ContractDraftVendor != null ? ca.ContractDraftVendor.Vendor.VendorInfo.PlaceName : null,
            ca.AuditInfo.CreatedAt,
            ca.AuditInfo.LastModifiedAt,
        })
                                             .ToListAsync(ct);

        var contractDraftVendorEditsQuery = this.dbContext.CaContractDraftVendorEdits.Where(e => !e.IsDeleted).AsNoTracking();
        foreach (var kw in keywords)
        {
            var k = kw;
            contractDraftVendorEditsQuery = contractDraftVendorEditsQuery.Where(e =>
                EF.Functions.ILike(e.ContractNumber, $"%{k}%") ||
                EF.Functions.ILike(e.ContractName, $"%{k}%") ||
                EF.Functions.ILike(e.PoNumber, $"%{k}%") ||
                (e.ContractDraftVendor != null && e.ContractDraftVendor.Vendor.VendorInfo != null && !string.IsNullOrEmpty(e.ContractDraftVendor.Vendor.VendorInfo.EstablishmentName) &&
                 EF.Functions.ILike(e.ContractDraftVendor.Vendor.VendorInfo.EstablishmentName, $"%{k}%")) ||
                (e.ContractDraftVendor != null && e.ContractDraftVendor.Vendor.VendorInfo != null && !string.IsNullOrEmpty(e.ContractDraftVendor.Vendor.VendorInfo.PlaceName) &&
                 EF.Functions.ILike(e.ContractDraftVendor.Vendor.VendorInfo.PlaceName, $"%{k}%")));
        }

        var contractDraftVendorEditsRaw = await contractDraftVendorEditsQuery.Select(e => new
        {
            Id = e.Id.Value,
            ContractDraftVendorId = e.ContractDraftVendorId.Value,
            e.ContractNumber,
            e.ContractName,
            Budget = (decimal?)e.Budget,
            e.ProcurementId,
            e.AuditInfo.CreatedAt,
            e.AuditInfo.LastModifiedAt,
        })
                                                    .ToListAsync(ct);

        var expenseDisbursementQuery = this.dbContext.PExpenseDisbursements.Where(ed => !ed.IsDeleted).AsNoTracking();
        foreach (var kw in keywords)
        {
            var k = kw;
            expenseDisbursementQuery = expenseDisbursementQuery.Where(ed =>
                !string.IsNullOrWhiteSpace(ed.AdvanceName) && EF.Functions.ILike(ed.AdvanceName, $"%{k}%"));
        }

        var expenseDisbursementRaw = await expenseDisbursementQuery.Select(ed => new
        {
            Id = ed.Id.Value,
            ed.AdvanceName,
            ed.AuditInfo.CreatedAt,
            ed.AuditInfo.LastModifiedAt,
        })
                                               .ToListAsync(ct);

        var deliveryAcceptanceQuery = this.dbContext.CmDeliveryAcceptances.Where(da => !da.IsDeleted).AsNoTracking();
        foreach (var kw in keywords)
        {
            var k = kw;
            deliveryAcceptanceQuery = deliveryAcceptanceQuery.Where(da =>
                da.Periods.Any(p => p.AcceptanceNumber != null && EF.Functions.ILike(p.AcceptanceNumber, $"%{k}%")) ||
                (da.SourceType == SourceType.Manual &&
                 ((da.Number != null && EF.Functions.ILike(da.Number, $"%{k}%")) ||
                  (da.Name != null && EF.Functions.ILike(da.Name, $"%{k}%")) ||
                  da.Periods.Any(p => p.AcceptanceNumber != null && EF.Functions.ILike(p.AcceptanceNumber, $"%{k}%")))));
        }

        var deliveryAcceptanceRaw = await deliveryAcceptanceQuery.Select(da => new
        {
            Id = da.Id.Value,
            da.SourceType,
            da.RefId,
            da.AuditInfo.CreatedAt,
            da.AuditInfo.LastModifiedAt,
            da.Number,
            da.Name,
            da.Budget,
            DepartmentName = da.Department != null ? da.Department.Name : null,
            SupplyMethod = da.SupplyMethod != null ? da.SupplyMethod.Label : null,
            SupplyMethodType = da.SupplyMethodType != null ? da.SupplyMethodType.Label : null,
            SupplyMethodSpecialType = da.SupplyMethodSpecialType != null ? da.SupplyMethodSpecialType.Label : null,
        })
                                              .ToListAsync(ct);

        if (hasSearch)
        {
            var matchedPlanRefIds = plansRaw.Select(p => p.Id).ToList();
            var matchedProcRefIds = procurementsRaw.Select(p => p.Id).ToList();

            foreach (var pa in planAnnouncementsRaw.Where(x => x.FirstPlanId != null))
            {
                matchedPlanRefIds.Add(pa.FirstPlanId!.Value);
            }

            var matchedDraftIds = contractAgreementRaw.Select(c => c.Id).ToList();
            var matchedVendorRefIds = matchedDraftIds.Count > 0
                ? await this.dbContext.CaContractDraftVendors
                            .Where(v => matchedDraftIds.Select(ContractDraftId.From).Contains(v.ContractDraft.Id))
                            .AsNoTracking()
                            .Select(v => v.Id.Value)
                            .ToListAsync(ct)
                : new List<Guid>();

            var matchedVendorEditRefIds = contractDraftVendorEditsRaw.Select(e => e.Id).ToList();

            if (matchedPlanRefIds.Count > 0 || matchedProcRefIds.Count > 0 || matchedVendorRefIds.Count > 0 || matchedVendorEditRefIds.Count > 0)
            {
                var existingDaIds = deliveryAcceptanceRaw.Select(x => x.Id).ToHashSet();
                var additionalDa = await this.dbContext.CmDeliveryAcceptances
                                             .Where(da => !da.IsDeleted)
                                             .Where(da =>
                                                 (da.SourceType == SourceType.Plan && matchedPlanRefIds.Contains((Guid)da.RefId)) ||
                                                 (da.SourceType == SourceType.Procurement && matchedProcRefIds.Contains((Guid)da.RefId)) ||
                                                 (da.SourceType == SourceType.ContractDraftVendor && matchedVendorRefIds.Contains((Guid)da.RefId)) ||
                                                 (da.SourceType == SourceType.ContractDraftVendorEdit && matchedVendorEditRefIds.Contains((Guid)da.RefId)))
                                             .AsNoTracking()
                                             .Select(da => new
                                             {
                                                 Id = da.Id.Value,
                                                 da.SourceType,
                                                 da.RefId,
                                                 da.AuditInfo.CreatedAt,
                                                 da.AuditInfo.LastModifiedAt,
                                                 da.Number,
                                                 da.Name,
                                                 da.Budget,
                                                 DepartmentName = da.Department != null ? da.Department.Name : null,
                                                 SupplyMethod = da.SupplyMethod != null ? da.SupplyMethod.Label : null,
                                                 SupplyMethodType = da.SupplyMethodType != null ? da.SupplyMethodType.Label : null,
                                                 SupplyMethodSpecialType = da.SupplyMethodSpecialType != null ? da.SupplyMethodSpecialType.Label : null,
                                             })
                                             .ToListAsync(ct);

                // Merge and deduplicate
                deliveryAcceptanceRaw = deliveryAcceptanceRaw
                                        .Concat(additionalDa.Where(x => !existingDaIds.Contains(x.Id)))
                                        .ToList();
            }
        }

        // Phase 2: Batch lookup PlanAnnouncements for all PlanIds
        var allPlanIds = new HashSet<Guid>();

        foreach (var p in procurementsRaw.Where(x => x.PlanId != null))
        {
            allPlanIds.Add(p.PlanId!.Value);
        }

        foreach (var c in contractAgreementRaw.Where(x => x.PlanId != null))
        {
            allPlanIds.Add(c.PlanId!.Value);
        }

        foreach (var ct2 in contractTerminationRaw.Where(x => x.PlanId != null))
        {
            allPlanIds.Add(ct2.PlanId!.Value);
        }

        foreach (var cgr in contractGuaranteeReturnRaw.Where(x => x.PlanId != null))
        {
            allPlanIds.Add(cgr.PlanId!.Value);
        }

        foreach (var ca in contractAmendmentRaw.Where(x => x.PlanId != null))
        {
            allPlanIds.Add(ca.PlanId!.Value);
        }

        var planAnnouncementMap = new Dictionary<Guid, (Guid PlanAnnouncementId, string PlanAnnouncementNumber)>();

        if (allPlanIds.Count > 0)
        {
            var planIdList = allPlanIds.Select(PlanId.From).ToList();
            var paLookup = await this.dbContext.PlanAnnouncementSelecteds
                                     .Where(pas => !pas.IsDeleted)
                                     .Where(pas => planIdList.Contains(pas.PlanId))
                                     .AsNoTracking()
                                     .Select(pas => new
                                     {
                                         PlanId = pas.PlanId.Value,
                                         PlanAnnouncementId = pas.PlanAnnouncementId.Value,
                                         PlanAnnouncementNumber = (string)pas.PlanAnnouncement.PlanAnnouncementNumber,
                                     })
                                     .ToListAsync(ct);

            foreach (var pa in paLookup)
            {
                planAnnouncementMap.TryAdd(pa.PlanId, (pa.PlanAnnouncementId, pa.PlanAnnouncementNumber));
            }
        }

        // Resolve ContractDraftVendorEdit data via ContractDraftVendor → ContractDraft → Procurement → Plan
        var cm007VendorIds = contractDraftVendorEditsRaw
                             .Select(e => e.ContractDraftVendorId)
                             .Distinct()
                             .ToList();
        var cm007VendorData = cm007VendorIds.Count > 0
            ? await this.dbContext.CaContractDraftVendors
                        .Where(v => cm007VendorIds.Select(ContractDraftVendorId.From).Contains(v.Id))
                        .AsNoTracking()
                        .Select(v => new
                        {
                            Id = v.Id.Value,
                            VendorContractNumber = v.ContractNumber,
                            ProcurementId = v.ContractDraft != null ? (Guid?)v.ContractDraft.ProcurementId.Value : null,
                            ProcurementNumber = v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.ProcurementNumber != null
                                ? (string?)v.ContractDraft.Procurement.ProcurementNumber.Value
                                : null,
                            ProcurementType = v.ContractDraft != null && v.ContractDraft.Procurement != null ? (ProcurementType?)v.ContractDraft.Procurement.Type : null,
                            PlanId = v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.PlanId != null ? (Guid?)v.ContractDraft.Procurement.PlanId.Value : null,
                            PlanNumber =
                                v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.Plan != null ? (string?)v.ContractDraft.Procurement.Plan.PlanNumber.Value : null,
                            DepartmentName = v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.Department != null
                                ? (string?)v.ContractDraft.Procurement.Department.Name
                                : null,
                            SupplyMethod = v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.SupplyMethod != null
                                ? (string?)v.ContractDraft.Procurement.SupplyMethod.Label
                                : null,
                            SupplyMethodType = v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.SupplyMethodType != null
                                ? (string?)v.ContractDraft.Procurement.SupplyMethodType.Label
                                : null,
                            SupplyMethodSpecialType = v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.SupplyMethodSpecialType != null
                                ? (string?)v.ContractDraft.Procurement.SupplyMethodSpecialType.Label
                                : null,
                            VendorEstablishmentName = v.Vendor.VendorInfo.EstablishmentName,
                            VendorPlaceName = v.Vendor.VendorInfo.PlaceName,
                        })
                        .ToListAsync(ct)
            : [];
        var cm007VendorDict = cm007VendorData.ToDictionary(x => x.Id);

        // Phase 3: Resolve DeliveryAcceptance chains
        var dapRefIdsByPlan = deliveryAcceptanceRaw
                              .Where(x => x.SourceType == SourceType.Plan && x.RefId.HasValue)
                              .Select(x => x.RefId!.Value)
                              .Distinct()
                              .ToList();

        var dapRefIdsByProcurement = deliveryAcceptanceRaw
                                     .Where(x => x.SourceType == SourceType.Procurement && x.RefId.HasValue)
                                     .Select(x => x.RefId!.Value)
                                     .Distinct()
                                     .ToList();

        var dapRefIdsByContractVendor = deliveryAcceptanceRaw
                                        .Where(x => x.SourceType == SourceType.ContractDraftVendor && x.RefId.HasValue)
                                        .Select(x => x.RefId!.Value)
                                        .Distinct()
                                        .ToList();

        var dapRefIdsByVendorEdit = deliveryAcceptanceRaw
                                    .Where(x => x.SourceType == SourceType.ContractDraftVendorEdit && x.RefId.HasValue)
                                    .Select(x => x.RefId!.Value)
                                    .Distinct()
                                    .ToList();

        var dapPlanLookup = dapRefIdsByPlan.Count > 0
            ? await this.dbContext.Plans
                        .Where(p => dapRefIdsByPlan.Select(PlanId.From).Contains(p.Id))
                        .AsNoTracking()
                        .Select(p => new DapPlanLookupItem(
                            p.Id.Value,
                            (string)p.PlanNumber,
                            p.Name,
                            (decimal?)p.Budget,
                            p.Department.Name,
                            p.SupplyMethod.Label,
                            p.SupplyMethodType != null ? p.SupplyMethodType.Label : null,
                            p.SupplyMethodSpecialType != null ? p.SupplyMethodSpecialType.Label : null))
                        .ToDictionaryAsync(x => x.Id, ct)
            : new Dictionary<Guid, DapPlanLookupItem>();

        var dapProcurementLookup = dapRefIdsByProcurement.Count > 0
            ? await this.dbContext.Procurements
                        .Where(p => dapRefIdsByProcurement.Select(ProcurementId.From).Contains(p.Id))
                        .AsNoTracking()
                        .Select(p => new DapProcurementLookupItem(
                            p.Id.Value,
                            p.PlanId != null ? (Guid?)p.PlanId.Value : null,
                            p.Plan != null ? (string?)p.Plan.PlanNumber.Value : null,
                            p.Plan != null ? p.Plan.Name : null,
                            p.ProcurementNumber != null ? (string?)p.ProcurementNumber.Value : null,
                            (decimal?)p.Budget,
                            p.Department != null ? p.Department.Name : null,
                            p.SupplyMethod != null ? p.SupplyMethod.Label : null,
                            p.SupplyMethodType != null ? p.SupplyMethodType.Label : null,
                            p.SupplyMethodSpecialType != null ? p.SupplyMethodSpecialType.Label : null))
                        .ToDictionaryAsync(x => x.Id, ct)
            : new Dictionary<Guid, DapProcurementLookupItem>();

        var dapContractVendorLookup = dapRefIdsByContractVendor.Count > 0
            ? await this.dbContext.CaContractDraftVendors
                .Where(v => dapRefIdsByContractVendor.Select(ContractDraftVendorId.From).Contains(v.Id))
                .AsNoTracking()
                .Select(v => new DapContractVendorLookupItem(
                    v.Id.Value,
                    v.ContractNumber,
                    v.ContractName,
                    v.ContractDraft != null ? (Guid?)v.ContractDraft.ProcurementId.Value : null,
                    v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.ProcurementNumber != null ? (string?)v.ContractDraft.Procurement.ProcurementNumber.Value : null,
                    v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.PlanId != null ? (Guid?)v.ContractDraft.Procurement.PlanId.Value : null,
                    v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.Plan != null ? (string?)v.ContractDraft.Procurement.Plan.PlanNumber.Value : null,
                    v.ContractDraft != null && v.ContractDraft.Procurement != null ? (decimal?)v.ContractDraft.Procurement.Budget : null,
                    v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.Department != null ? v.ContractDraft.Procurement.Department.Name : null,
                    v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.SupplyMethod != null ? v.ContractDraft.Procurement.SupplyMethod.Label : null,
                    v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.SupplyMethodType != null ? v.ContractDraft.Procurement.SupplyMethodType.Label : null,
                    v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.SupplyMethodSpecialType != null ? v.ContractDraft.Procurement.SupplyMethodSpecialType.Label : null,
                    v.Vendor.VendorInfo.EstablishmentName,
                    v.Vendor.VendorInfo.PlaceName))
                .ToDictionaryAsync(x => x.Id, ct)
            : new Dictionary<Guid, DapContractVendorLookupItem>();

        var dapVendorEditLookup = new Dictionary<Guid, DapVendorEditLookupItem>();

        if (dapRefIdsByVendorEdit.Count > 0)
        {
            var vendorEditBasicData = await this.dbContext.CaContractDraftVendorEdits
                                                .Where(e => dapRefIdsByVendorEdit.Select(ContractDraftVendorEditId.From).Contains(e.Id))
                                                .AsNoTracking()
                                                .Select(e => new
                                                {
                                                    Id = e.Id.Value,
                                                    e.ContractNumber,
                                                    e.ContractName,
                                                    Budget = (decimal?)e.Budget,
                                                    VendorId = e.ContractDraftVendorId.Value,
                                                })
                                                .ToListAsync(ct);

            var vendorEditVendorIds = vendorEditBasicData.Select(e => e.VendorId).Distinct().ToList();
            var vendorEditVendorLookup = await this.dbContext.CaContractDraftVendors
                                                   .Where(v => vendorEditVendorIds.Select(ContractDraftVendorId.From).Contains(v.Id))
                                                   .AsNoTracking()
                                                   .Select(v => new
                                                   {
                                                       Id = v.Id.Value,
                                                       ContractNumber = v.ContractNumber,
                                                       ProcurementId = v.ContractDraft != null ? (Guid?)v.ContractDraft.ProcurementId.Value : null,
                                                       ProcurementNumber = v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.ProcurementNumber != null
                                                           ? (string?)v.ContractDraft.Procurement.ProcurementNumber.Value
                                                           : null,
                                                       PlanId = v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.PlanId != null
                                                           ? (Guid?)v.ContractDraft.Procurement.PlanId.Value
                                                           : null,
                                                       PlanNumber = v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.Plan != null
                                                           ? (string?)v.ContractDraft.Procurement.Plan.PlanNumber.Value
                                                           : null,
                                                       DepartmentName = v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.Department != null
                                                           ? v.ContractDraft.Procurement.Department.Name
                                                           : null,
                                                       SupplyMethod = v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.SupplyMethod != null
                                                           ? v.ContractDraft.Procurement.SupplyMethod.Label
                                                           : null,
                                                       SupplyMethodType = v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.SupplyMethodType != null
                                                           ? v.ContractDraft.Procurement.SupplyMethodType.Label
                                                           : null,
                                                       SupplyMethodSpecialType = v.ContractDraft != null && v.ContractDraft.Procurement != null && v.ContractDraft.Procurement.SupplyMethodSpecialType != null
                                                           ? v.ContractDraft.Procurement.SupplyMethodSpecialType.Label
                                                           : null,
                                                       VendorEstablishmentName = v.Vendor.VendorInfo.EstablishmentName,
                                                       VendorPlaceName = v.Vendor.VendorInfo.PlaceName,
                                                   })
                                                   .ToDictionaryAsync(x => x.Id, ct);

            foreach (var e in vendorEditBasicData)
            {
                vendorEditVendorLookup.TryGetValue(e.VendorId, out var vendor);
                dapVendorEditLookup[e.Id] = new DapVendorEditLookupItem(
                    e.Id,
                    e.ContractNumber,
                    e.ContractName,
                    e.Budget,
                    e.VendorId,
                    vendor?.ContractNumber,
                    vendor?.ProcurementId,
                    vendor?.ProcurementNumber,
                    vendor?.PlanId,
                    vendor?.PlanNumber,
                    vendor?.DepartmentName,
                    vendor?.SupplyMethod,
                    vendor?.SupplyMethodType,
                    vendor?.SupplyMethodSpecialType,
                    vendor?.VendorEstablishmentName,
                    vendor?.VendorPlaceName);
            }
        }

        // Collect PlanIds from DAP lookups for PlanAnnouncement resolution
        foreach (var planId in dapPlanLookup.Keys)
        {
            allPlanIds.Add(planId);
        }

        foreach (var proc in dapProcurementLookup.Values)
        {
            if (proc.PlanId != null)
            {
                allPlanIds.Add((Guid)proc.PlanId);
            }
        }

        foreach (var cv in dapContractVendorLookup.Values)
        {
            if (cv.PlanId != null)
            {
                allPlanIds.Add((Guid)cv.PlanId);
            }
        }

        foreach (var vendor in cm007VendorDict.Values)
        {
            if (vendor.PlanId != null)
            {
                allPlanIds.Add((Guid)vendor.PlanId);
            }
        }

        foreach (var ve in dapVendorEditLookup.Values)
        {
            if (ve.PlanId != null)
            {
                allPlanIds.Add(ve.PlanId.Value);
            }
        }

        // Re-query PlanAnnouncement for any new PlanIds from DAP
        if (allPlanIds.Count > planAnnouncementMap.Count)
        {
            var newPlanIds = allPlanIds.Where(id => !planAnnouncementMap.ContainsKey(id)).Select(PlanId.From).ToList();

            if (newPlanIds.Count > 0)
            {
                var additionalPa = await this.dbContext.PlanAnnouncementSelecteds
                                             .Where(pas => !pas.IsDeleted)
                                             .Where(pas => newPlanIds.Contains(pas.PlanId))
                                             .AsNoTracking()
                                             .Select(pas => new
                                             {
                                                 PlanId = pas.PlanId.Value,
                                                 PlanAnnouncementId = pas.PlanAnnouncementId.Value,
                                                 PlanAnnouncementNumber = (string)pas.PlanAnnouncement.PlanAnnouncementNumber,
                                             })
                                             .ToListAsync(ct);

                foreach (var pa in additionalPa)
                {
                    planAnnouncementMap.TryAdd(pa.PlanId, (pa.PlanAnnouncementId, pa.PlanAnnouncementNumber));
                }
            }
        }

        // Phase 4: Build steps and map to response items
        List<WorkflowStepDto> BuildPlanChainSteps(Guid? planId, string? planNumber)
        {
            var steps = new List<WorkflowStepDto>();

            if (planId != null && planNumber != null)
            {
                steps.Add(new WorkflowStepDto(ProgramConstant.Plan.Name, planNumber, Fmt(ProgramConstant.Plan.Url, planId)));

                if (planAnnouncementMap.TryGetValue(planId.Value, out var pa))
                {
                    steps.Add(new WorkflowStepDto(ProgramConstant.PlanAnnouncement.Name, pa.PlanAnnouncementNumber, Fmt(ProgramConstant.PlanAnnouncement.Url, pa.PlanAnnouncementId)));
                }
            }

            return steps;
        }

        // Plans
        var plansItems = plansRaw.Select(p => new GetListAllProgramResponseItems(
            p.Id,
            p.PlanNumber,
            null,
            null,
            p.Name,
            p.Budget,
            p.BudgetYear,
            p.DepartmentName,
            p.SupplyMethod,
            p.SupplyMethodType,
            p.SupplyMethodSpecialType,
            null,
            "Plan",
            p.CreatedAt,
            p.LastModifiedAt,
            new List<WorkflowStepDto>
            {
                new(ProgramConstant.Plan.Name, p.PlanNumber, Fmt(ProgramConstant.Plan.Url, p.Id)),
            })).ToList();

        // PlanAnnouncements
        var planAnnouncementsItems = planAnnouncementsRaw.Select(pa =>
        {
            var steps = new List<WorkflowStepDto>();

            if (pa.FirstPlanId != null && pa.FirstPlanNumber != null)
            {
                steps.Add(new WorkflowStepDto(ProgramConstant.Plan.Name, pa.FirstPlanNumber, Fmt(ProgramConstant.Plan.Url, pa.FirstPlanId)));
            }

            steps.Add(new WorkflowStepDto(ProgramConstant.PlanAnnouncement.Name, pa.PlanAnnouncementNumber, Fmt(ProgramConstant.PlanAnnouncement.Url, pa.Id)));

            return new GetListAllProgramResponseItems(
                pa.Id,
                pa.PlanAnnouncementNumber,
                null,
                null,
                pa.Title,
                pa.Budget,
                pa.BudgetYear,
                pa.DepartmentName,
                pa.SupplyMethod,
                pa.SupplyMethodType,
                pa.SupplyMethodSpecialType,
                null,
                "PlanAnnouncement",
                pa.CreatedAt,
                pa.LastModifiedAt,
                steps);
        }).ToList();

        // Procurements
        var procurementsItems = procurementsRaw.Select(p =>
        {
            var steps = BuildPlanChainSteps(p.PlanId, p.PlanNumber);
            steps.Add(new WorkflowStepDto(ProgramConstant.Procurement.Name, p.ProcurementNumber ?? "-", Fmt(ProgramConstant.Procurement.Url, p.Id)));

            return new GetListAllProgramResponseItems(
                p.Id,
                p.PlanNumber,
                p.ProcurementNumber,
                p.ContractNumber,
                p.Name,
                p.Budget,
                p.BudgetYear,
                p.DepartmentName,
                p.SupplyMethod,
                p.SupplyMethodType,
                p.SupplyMethodSpecialType,
                null,
                "Procurement",
                p.CreatedAt,
                p.LastModifiedAt,
                steps);
        }).ToList();

        // PW119
        var pw119Items = pw119Raw.Select(pw => new GetListAllProgramResponseItems(
            pw.Id,
            pw.Pw119Number,
            null,
            null,
            pw.Subject,
            pw.Budget,
            pw.BudgetYear,
            pw.DepartmentName,
            pw.SupplyMethod,
            null,
            pw.SupplyMethodSpecialType,
            null,
            "PW119",
            pw.CreatedAt,
            pw.LastModifiedAt,
            new List<WorkflowStepDto>
            {
                new(ProgramConstant.W119.Name, pw.Pw119Number, Fmt(ProgramConstant.W119.Url, pw.Id)),
            })).ToList();

        // P79Clause2
        var p79Clause2Items = p79Clause2Raw.Select(p => new GetListAllProgramResponseItems(
            p.Id,
            p.P79Clause2Number,
            null,
            null,
            p.Subject,
            p.Budget,
            p.BudgetYear,
            p.DepartmentName,
            p.SupplyMethod,
            p.SupplyMethodType,
            p.SupplyMethodSpecialType,
            null,
            "P79Clause2",
            p.CreatedAt,
            p.LastModifiedAt,
            new List<WorkflowStepDto>
            {
                new(ProgramConstant.Urgent79Clause2.Name, p.P79Clause2Number, Fmt(ProgramConstant.Urgent79Clause2.Url, p.Id)),
            })).ToList();

        // PettyCash
        var pettyCashItems = pettyCashRaw.Select(p => new GetListAllProgramResponseItems(
            p.Id,
            p.PettyCashNumber,
            null,
            null,
            p.Subject,
            p.Budget,
            p.BudgetYear,
            p.DepartmentName,
            p.SupplyMethod,
            p.SupplyMethodType,
            p.SupplyMethodSpecialType,
            null,
            "PettyCash",
            p.CreatedAt,
            p.LastModifiedAt,
            new List<WorkflowStepDto>
            {
                new(ProgramConstant.PettyCash.Name, p.PettyCashNumber, Fmt(ProgramConstant.PettyCash.Url, p.Id)),
            })).ToList();

        // PettyCashReimbursement
        var pettyCashReimbursementItems = pettyCashReimbursementRaw.Select(p => new GetListAllProgramResponseItems(
            p.Id,
            p.Number,
            null,
            null,
            p.Subject,
            null,
            null,
            p.DepartmentName,
            null,
            null,
            null,
            null,
            "PettyCashReimbursement",
            p.CreatedAt,
            p.LastModifiedAt,
            new List<WorkflowStepDto>
            {
                new(ProgramConstant.PettyCashReimbursement.Name, p.Number, Fmt(ProgramConstant.PettyCashReimbursement.Url, p.Id)),
            })).ToList();

        // ContractAgreement
        var contractAgreementItems = contractAgreementRaw.Select(c =>
        {
            var steps = BuildPlanChainSteps(c.PlanId, c.PlanNumber);

            if (c.ProcurementNumber != null)
            {
                steps.Add(new WorkflowStepDto(ProgramConstant.Procurement.Name, c.ProcurementNumber, Fmt(ProgramConstant.Procurement.Url, c.ProcurementId)));
            }

            if (c.VendorInfos.Count > 0)
            {
                foreach (var vendor in c.VendorInfos)
                {
                    var vendorUrl = c.ProcurementType == ProcurementType.Procurement
                        ? Fmt(ProgramConstant.ContractDraft.VendorUrl, c.ProcurementId, vendor.VendorId)
                        : Fmt(ProgramConstant.PrincipalApprovalRental.Url, c.ProcurementId) + $"?vendorId={vendor.VendorId}";
                    steps.Add(new WorkflowStepDto(ProgramConstant.ContractDraft.Name, vendor.ContractNumber ?? "-", vendorUrl));
                }
            }
            else
            {
                var contractUrl = c.ProcurementType == ProcurementType.Procurement
                    ? Fmt(ProgramConstant.Procurement.Url, c.ProcurementId)
                    : Fmt(ProgramConstant.PrincipalApprovalRental.Url, c.ProcurementId);
                steps.Add(new WorkflowStepDto(ProgramConstant.ContractDraft.Name, "-", contractUrl));
            }

            return new GetListAllProgramResponseItems(
                c.Id,
                c.PlanNumber,
                c.ProcurementNumber,
                c.ContractNumber,
                c.Name,
                c.Budget,
                c.BudgetYear,
                c.DepartmentName,
                c.SupplyMethod,
                c.SupplyMethodType,
                c.SupplyMethodSpecialType,
                !string.IsNullOrWhiteSpace(c.VendorNames) ? c.VendorNames : (string.IsNullOrWhiteSpace(c.VendorPlaceNames) ? null : c.VendorPlaceNames),
                "ContractAgreement",
                c.CreatedAt,
                c.LastModifiedAt,
                steps);
        }).ToList();

        // ContractTermination
        var contractTerminationItems = contractTerminationRaw.Select(ct2 =>
        {
            var steps = BuildPlanChainSteps(ct2.PlanId, ct2.PlanNumber);

            if (ct2.ProcurementId != null && ct2.ProcurementNumber != null)
            {
                steps.Add(new WorkflowStepDto(ProgramConstant.Procurement.Name, ct2.ProcurementNumber, Fmt(ProgramConstant.Procurement.Url, ct2.ProcurementId)));
            }

            steps.Add(new WorkflowStepDto(ProgramConstant.ContractTermination.Name, ct2.ContractNumber ?? "-", Fmt(ProgramConstant.ContractTermination.Url, ct2.ContractDraftVendorId, ct2.Id)));

            return new GetListAllProgramResponseItems(
                ct2.Id,
                ct2.ContractNumber,
                null,
                null,
                ct2.ContractName,
                ct2.Budget,
                ct2.BudgetYear,
                ct2.DepartmentName,
                ct2.SupplyMethod,
                ct2.SupplyMethodType,
                ct2.SupplyMethodSpecialType,
                !string.IsNullOrWhiteSpace(ct2.VendorEstablishmentName) ? ct2.VendorEstablishmentName : (string.IsNullOrWhiteSpace(ct2.VendorPlaceName) ? null : ct2.VendorPlaceName),
                "ContractTermination",
                ct2.CreatedAt,
                ct2.LastModifiedAt,
                steps);
        }).ToList();

        // ContractGuaranteeReturn
        var contractGuaranteeReturnItems = contractGuaranteeReturnRaw.Select(cgr =>
        {
            var steps = BuildPlanChainSteps(cgr.PlanId, cgr.PlanNumber);

            if (cgr.ProcurementId != null && cgr.ProcurementNumber != null)
            {
                steps.Add(new WorkflowStepDto(ProgramConstant.Procurement.Name, cgr.ProcurementNumber, Fmt(ProgramConstant.Procurement.Url, cgr.ProcurementId)));
            }

            steps.Add(new WorkflowStepDto(ProgramConstant.ContractGuaranteeReturn.Name, cgr.ContractNumber ?? "-", Fmt(ProgramConstant.ContractGuaranteeReturn.Url, cgr.ContractDraftVendorId, cgr.Id)));

            return new GetListAllProgramResponseItems(
                cgr.Id,
                cgr.ContractNumber,
                null,
                null,
                cgr.ContractName,
                cgr.Budget,
                cgr.BudgetYear,
                cgr.DepartmentName,
                cgr.SupplyMethod,
                cgr.SupplyMethodType,
                cgr.SupplyMethodSpecialType,
                !string.IsNullOrWhiteSpace(cgr.VendorEstablishmentName) ? cgr.VendorEstablishmentName : (string.IsNullOrWhiteSpace(cgr.VendorPlaceName) ? null : cgr.VendorPlaceName),
                "ContractGuaranteeReturn",
                cgr.CreatedAt,
                cgr.LastModifiedAt,
                steps);
        }).ToList();

        // ContractAmendment
        var contractAmendmentItems = contractAmendmentRaw.Select(ca =>
        {
            var steps = BuildPlanChainSteps(ca.PlanId, ca.PlanNumber);

            if (ca.ProcurementId != null && ca.ProcurementNumber != null)
            {
                steps.Add(new WorkflowStepDto(ProgramConstant.Procurement.Name, ca.ProcurementNumber, Fmt(ProgramConstant.Procurement.Url, ca.ProcurementId)));
            }

            steps.Add(new WorkflowStepDto(ProgramConstant.ContractAmendment.Name, ca.CamNumber, Fmt(ProgramConstant.ContractAmendment.Url, ca.Id)));

            return new GetListAllProgramResponseItems(
                ca.Id,
                ca.CamNumber,
                null,
                null,
                ca.Remark,
                ca.Budget,
                ca.BudgetYear,
                ca.DepartmentName,
                ca.SupplyMethod,
                ca.SupplyMethodType,
                ca.SupplyMethodSpecialType,
                !string.IsNullOrWhiteSpace(ca.VendorEstablishmentName) ? ca.VendorEstablishmentName : (string.IsNullOrWhiteSpace(ca.VendorPlaceName) ? null : ca.VendorPlaceName),
                "ContractAmendment",
                ca.CreatedAt,
                ca.LastModifiedAt,
                steps);
        }).ToList();

        // ContractDraftVendorEdit (CM007)
        var contractDraftVendorEditItems = contractDraftVendorEditsRaw.Select(e =>
        {
            cm007VendorDict.TryGetValue(e.ContractDraftVendorId, out var vendor);
            var steps = BuildPlanChainSteps(vendor?.PlanId, vendor?.PlanNumber);

            if (vendor?.ProcurementId != null)
            {
                steps.Add(new WorkflowStepDto(ProgramConstant.Procurement.Name, vendor.ProcurementNumber ?? "-", Fmt(ProgramConstant.Procurement.Url, vendor.ProcurementId.Value)));
            }

            if (vendor?.VendorContractNumber != null)
            {
                var contractUrl = vendor.ProcurementType == ProcurementType.Procurement
                    ? Fmt(ProgramConstant.Procurement.Url, vendor.ProcurementId!.Value)
                    : Fmt(ProgramConstant.PrincipalApprovalRental.Url, vendor.ProcurementId!.Value);
                steps.Add(new WorkflowStepDto(ProgramConstant.ContractDraft.Name, vendor.VendorContractNumber, contractUrl));
            }

            steps.Add(new WorkflowStepDto(ProgramConstant.ContractDraftVendorEdit.Name, e.ContractNumber ?? "-", Fmt(ProgramConstant.ContractDraftVendorEdit.Url, e.Id)));

            return new GetListAllProgramResponseItems(
                e.Id,
                e.ContractNumber,
                null,
                null,
                e.ContractName,
                e.Budget,
                null,
                vendor?.DepartmentName,
                vendor?.SupplyMethod,
                vendor?.SupplyMethodType,
                vendor?.SupplyMethodSpecialType,
                !string.IsNullOrWhiteSpace(vendor?.VendorEstablishmentName) ? vendor.VendorEstablishmentName : (string.IsNullOrWhiteSpace(vendor?.VendorPlaceName) ? null : vendor.VendorPlaceName),
                "ContractDraftEditVendor",
                e.CreatedAt,
                e.LastModifiedAt,
                steps);
        }).ToList();

        // ExpenseDisbursement
        var expenseDisbursementItems = expenseDisbursementRaw.Select(ed => new GetListAllProgramResponseItems(
            ed.Id,
            ed.AdvanceName ?? "Expense Disbursement",
            null,
            null,
            ed.AdvanceName,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "ExpenseDisbursement",
            ed.CreatedAt,
            ed.LastModifiedAt,
            new List<WorkflowStepDto>
            {
                new(ProgramConstant.ExpenseDisbursement.Name, ed.AdvanceName ?? "Expense Disbursement", Fmt(ProgramConstant.ExpenseDisbursement.Url, ed.Id)),
            })).ToList();

        // DeliveryAcceptance
        var deliveryAcceptanceItems = deliveryAcceptanceRaw.Select(da =>
        {
            var steps = new List<WorkflowStepDto>();
            string? refNumber = null;
            decimal? budget = null;
            string? programName = null;
            string? departmentName = null;
            string? supplyMethod = null;
            string? supplyMethodType = null;
            string? supplyMethodSpecialType = null;
            string? vendorName = null;

            switch (da.SourceType)
            {
                case SourceType.Plan:
                    if (da.RefId.HasValue && dapPlanLookup.TryGetValue(da.RefId.Value, out var plan))
                    {
                        refNumber = plan.PlanNumber;
                        programName = plan.PlanName;
                        budget = plan.Budget;
                        departmentName = plan.DepartmentName;
                        supplyMethod = plan.SupplyMethod;
                        supplyMethodType = plan.SupplyMethodType;
                        supplyMethodSpecialType = plan.SupplyMethodSpecialType;
                        steps.AddRange(BuildPlanChainSteps(da.RefId.Value, plan.PlanNumber));
                    }

                    break;

                case SourceType.Procurement:
                    if (da.RefId.HasValue && dapProcurementLookup.TryGetValue(da.RefId.Value, out var proc))
                    {
                        refNumber = proc.ProcurementNumber;
                        programName = proc.PlanName;
                        budget = proc.Budget;
                        departmentName = proc.DepartmentName;
                        supplyMethod = proc.SupplyMethod;
                        supplyMethodType = proc.SupplyMethodType;
                        supplyMethodSpecialType = proc.SupplyMethodSpecialType;
                        steps.AddRange(BuildPlanChainSteps(proc.PlanId, proc.PlanNumber));
                        steps.Add(new WorkflowStepDto(ProgramConstant.Procurement.Name, proc.ProcurementNumber ?? "-", Fmt(ProgramConstant.Procurement.Url, da.RefId.Value)));
                    }

                    break;

                case SourceType.ContractDraftVendor:
                    if (da.RefId.HasValue && dapContractVendorLookup.TryGetValue(da.RefId.Value, out var cv))
                    {
                        refNumber = cv.ContractNumber;
                        programName = cv.ContractName;
                        budget = cv.Budget;
                        departmentName = cv.DepartmentName;
                        supplyMethod = cv.SupplyMethod;
                        supplyMethodType = cv.SupplyMethodType;
                        supplyMethodSpecialType = cv.SupplyMethodSpecialType;
                        vendorName = !string.IsNullOrWhiteSpace(cv.VendorEstablishmentName) ? cv.VendorEstablishmentName : cv.VendorPlaceName;
                        steps.AddRange(BuildPlanChainSteps(cv.PlanId, cv.PlanNumber));

                        if (cv.ProcurementId != null)
                        {
                            if (cv.ProcurementNumber != null)
                            {
                                steps.Add(new WorkflowStepDto(ProgramConstant.Procurement.Name, cv.ProcurementNumber, Fmt(ProgramConstant.Procurement.Url, cv.ProcurementId.Value)));
                            }

                            steps.Add(new WorkflowStepDto(ProgramConstant.ContractDraft.Name, cv.ContractNumber ?? "-", Fmt(ProgramConstant.Procurement.Url, cv.ProcurementId.Value)));
                        }
                    }

                    break;

                case SourceType.ContractDraftVendorEdit:
                    if (dapVendorEditLookup.TryGetValue((Guid)da.RefId, out var ve))
                    {
                        refNumber = ve.EditContractNumber;
                        programName = ve.EditContractName;
                        budget = ve.Budget;
                        departmentName = ve.DepartmentName;
                        supplyMethod = ve.SupplyMethod;
                        supplyMethodType = ve.SupplyMethodType;
                        supplyMethodSpecialType = ve.SupplyMethodSpecialType;
                        vendorName = !string.IsNullOrWhiteSpace(ve.VendorEstablishmentName) ? ve.VendorEstablishmentName : ve.VendorPlaceName;
                        steps.AddRange(BuildPlanChainSteps(ve.PlanId, ve.PlanNumber));

                        if (ve.ProcurementId != null)
                        {
                            if (ve.ProcurementNumber != null)
                            {
                                steps.Add(new WorkflowStepDto(ProgramConstant.Procurement.Name, ve.ProcurementNumber, Fmt(ProgramConstant.Procurement.Url, ve.ProcurementId.Value)));
                            }

                            steps.Add(new WorkflowStepDto(ProgramConstant.ContractDraft.Name, ve.VendorContractNumber ?? "-", Fmt(ProgramConstant.Procurement.Url, ve.ProcurementId.Value)));
                        }

                        steps.Add(new WorkflowStepDto(ProgramConstant.ContractDraftVendorEdit.Name, ve.EditContractNumber ?? "-", Fmt(ProgramConstant.ContractDraftVendorEdit.Url, da.RefId!.Value)));
                    }

                    break;

                case SourceType.Manual:
                    refNumber = da.Number;
                    programName = da.Name;
                    budget = da.Budget;
                    departmentName = da.DepartmentName;
                    supplyMethod = da.SupplyMethod;
                    supplyMethodType = da.SupplyMethodType;
                    supplyMethodSpecialType = da.SupplyMethodSpecialType;
                    break;
            }

            steps.Add(new WorkflowStepDto("ตรวจรับ (จพ.008)", refNumber ?? "-", Fmt(ProgramConstant.ContractAcceptance.Url, da.Id)));

            return new GetListAllProgramResponseItems(
                da.Id,
                refNumber ?? "-",
                null,
                null,
                programName,
                budget,
                null,
                departmentName,
                supplyMethod,
                supplyMethodType,
                supplyMethodSpecialType,
                vendorName,
                "DeliveryAcceptance",
                da.CreatedAt,
                da.LastModifiedAt,
                steps);
        }).ToList();

        // Phase 5: Post-filter — show only the deepest nodes in each chain

        // 5a. Procurement: remove if it has ContractDraft children in results
        var procIdsWithContractDraft = new HashSet<Guid>(
            contractAgreementRaw.Select(c => c.ProcurementId));
        var filteredProcurements = procurementsItems
                                   .Where(p => !procIdsWithContractDraft.Contains(p.Id))
                                   .ToList();

        // 5b. ContractDraft: remove if it has leaf children (Termination/GuaranteeReturn/Amendment/DAP)
        var contractDraftIdsWithLeaves = new HashSet<Guid>();

        foreach (var ct2 in contractTerminationRaw.Where(x => x.ContractDraftId != null))
        {
            contractDraftIdsWithLeaves.Add(ct2.ContractDraftId!.Value);
        }

        foreach (var cgr in contractGuaranteeReturnRaw.Where(x => x.ContractDraftId != null))
        {
            contractDraftIdsWithLeaves.Add(cgr.ContractDraftId!.Value);
        }

        foreach (var ca in contractAmendmentRaw.Where(x => x.ContractDraftId != null))
        {
            contractDraftIdsWithLeaves.Add(ca.ContractDraftId!.Value);
        }

        // DA with SourceType=ContractDraftVendor: resolve VendorId → ContractDraftId via lookup
        foreach (var da in deliveryAcceptanceRaw.Where(x => x.SourceType == SourceType.ContractDraftVendor && x.RefId.HasValue))
        {
            if (dapContractVendorLookup.TryGetValue(da.RefId!.Value, out var cvLookup) && cvLookup.ProcurementId != null)
            {
                // Find matching ContractDraft via ProcurementId
                var matchingDraft = contractAgreementRaw.FirstOrDefault(c => c.ProcurementId == cvLookup.ProcurementId.Value);

                if (matchingDraft != null)
                {
                    contractDraftIdsWithLeaves.Add(matchingDraft.Id);
                }
            }
        }

        // DA with SourceType=ContractDraftVendorEdit: mark ContractDraftVendorEdit as having DA children
        var vendorEditIdsWithDa = new HashSet<Guid>(
            deliveryAcceptanceRaw
                .Where(x => x.SourceType == SourceType.ContractDraftVendorEdit && x.RefId.HasValue)
                .Select(x => x.RefId!.Value));

        // Also mark the parent ContractDraft as having leaves via VendorEdit → Vendor → ContractDraft
        foreach (var da in deliveryAcceptanceRaw.Where(x => x.SourceType == SourceType.ContractDraftVendorEdit && x.RefId.HasValue))
        {
            if (dapVendorEditLookup.TryGetValue(da.RefId!.Value, out var veLookup) && veLookup.ProcurementId != null)
            {
                var matchingDraft = contractAgreementRaw.FirstOrDefault(c => c.ProcurementId == veLookup.ProcurementId.Value);

                if (matchingDraft != null)
                {
                    contractDraftIdsWithLeaves.Add(matchingDraft.Id);
                }
            }
        }

        // DA with SourceType=Procurement: this means Procurement has DA children → remove Procurement too
        foreach (var da in deliveryAcceptanceRaw.Where(x => x.SourceType == SourceType.Procurement && x.RefId.HasValue))
        {
            procIdsWithContractDraft.Add(da.RefId!.Value); // reuse set to also exclude Procurements with DA children
        }

        // Re-filter Procurements (now also excluding those with DAP children)
        filteredProcurements = procurementsItems
                               .Where(p => !procIdsWithContractDraft.Contains(p.Id))
                               .ToList();

        var filteredContractAgreement = contractAgreementItems
                                        .Where(c => !contractDraftIdsWithLeaves.Contains(c.Id))
                                        .ToList();

        var filteredContractDraftVendorEditItems = contractDraftVendorEditItems
                                                   .Where(e => !vendorEditIdsWithDa.Contains(e.Id))
                                                   .ToList();

        // 5d. Plan: hide if any deeper entity in the result already represents this Plan
        var planIdsRepresentedByChildren = new HashSet<Guid>();

        foreach (var p in procurementsRaw.Where(x => x.PlanId != null))
        {
            planIdsRepresentedByChildren.Add(p.PlanId!.Value);
        }

        foreach (var c in contractAgreementRaw.Where(x => x.PlanId != null))
        {
            planIdsRepresentedByChildren.Add(c.PlanId!.Value);
        }

        foreach (var ct2 in contractTerminationRaw.Where(x => x.PlanId != null))
        {
            planIdsRepresentedByChildren.Add(ct2.PlanId!.Value);
        }

        foreach (var cgr in contractGuaranteeReturnRaw.Where(x => x.PlanId != null))
        {
            planIdsRepresentedByChildren.Add(cgr.PlanId!.Value);
        }

        foreach (var ca in contractAmendmentRaw.Where(x => x.PlanId != null))
        {
            planIdsRepresentedByChildren.Add(ca.PlanId!.Value);
        }

        foreach (var da in deliveryAcceptanceRaw.Where(x => x.SourceType == SourceType.Plan && x.RefId.HasValue))
        {
            planIdsRepresentedByChildren.Add(da.RefId!.Value);
        }

        foreach (var pa in planAnnouncementsRaw.Where(x => x.FirstPlanId != null))
        {
            planIdsRepresentedByChildren.Add(pa.FirstPlanId!.Value);
        }

        var filteredPlans = plansItems
                            .Where(p => !planIdsRepresentedByChildren.Contains(p.Id))
                            .ToList();

        // 5e. PlanAnnouncement: hide if its first plan is already represented by a deeper entity
        var planAnnouncementIdsRepresentedByChildren = new HashSet<Guid>(
            planAnnouncementsRaw
                .Where(pa => pa.FirstPlanId != null && planIdsRepresentedByChildren.Contains(pa.FirstPlanId.Value))
                .Select(pa => pa.Id));

        var filteredPlanAnnouncements = planAnnouncementsItems
                                        .Where(pa => !planAnnouncementIdsRepresentedByChildren.Contains(pa.Id))
                                        .ToList();

        // 5f. Combine, sort, paginate
        var combinedItems = filteredPlans
                            .Concat(filteredPlanAnnouncements)
                            .Concat(filteredProcurements)
                            .Concat(filteredContractAgreement)
                            .Concat(contractTerminationItems)
                            .Concat(contractGuaranteeReturnItems)
                            .Concat(contractAmendmentItems)
                            .Concat(filteredContractDraftVendorEditItems)
                            .Concat(deliveryAcceptanceItems)
                            .Concat(pw119Items)
                            .Concat(p79Clause2Items)
                            .Concat(pettyCashItems)
                            .Concat(pettyCashReimbursementItems)
                            .Concat(expenseDisbursementItems)
                            .OrderByDescending(x => x.LastModifiedDate ?? x.CreatedDate)
                            .ThenByDescending(x => x.CreatedDate)
                            .ThenBy(x => x.ProgramName)
                            .ToList();

        var totalCount = combinedItems.Count;
        var skip = Math.Max(0, (req.PageNumber - 1) * req.PageSize);
        var pageItems = combinedItems.Skip(skip).Take(req.PageSize).ToList();

        var result = new PaginatedQueryResult<GetListAllProgramResponseItems>(
            pageItems,
            totalCount);

        return TypedResults.Ok<PaginatedQueryResult<GetListAllProgramResponseItems>>(result);
    }
}