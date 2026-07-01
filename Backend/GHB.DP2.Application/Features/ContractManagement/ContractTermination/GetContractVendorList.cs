namespace GHB.DP2.Application.Features.ContractManagement.ContractTermination;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetContractVendorListRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    int PageNumber,
    int PageSize,
    string? Keyword,
    int? BudgetYear,
    string? DepartmentCode,
    string? SupplyMethodCode,
    string? SupplyMethodTypeCode,
    string? SupplyMethodSpecialTypeCode,
    SourceType? SourceType);

public record GetContractVendorListResponse(
    Guid Id,
    string PlanCode,
    string DepartmentName,
    PlanType? PlanType,
    string? VendorName,
    string? VendorEmail,
    string? ContractNumber,
    string? PoNumber,
    decimal? ContractBudget,
    string? Name,
    string? ContractTypeName,
    string? TemplateName,
    DateTimeOffset? ContractDate,
    string? Period,
    DateTimeOffset? DeliveryDate,
    string? SupplyMethod,
    string? SupplyMethodType,
    string? SupplyMethodSpecialType,
    int? BudgetYear,
    decimal? Budget,
    SourceType SourceType,
    bool IsStock,
    DateTimeOffset CreatedAt,
    Guid? ProcurementId);

public class GetContractVendorList
    : EndpointBase<GetContractVendorListRequest, Ok<PaginatedQueryResult<GetContractVendorListResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetContractVendorList(
        Dp2DbContext dbContext,
        ILogger<GetContractVendorList> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractTermination")
             .Produces<Ok>());
        this.Get("contract-termination/contract-vendor");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetContractVendorListResponse>>> HandleRequestAsync(
        GetContractVendorListRequest req,
        CancellationToken ct)
    {
        var contractVendorQuery = this.dbContext.CaContractDraftVendors
                                      .Include(x => x.ContractInvitationVendors)
                                      .Include(x => x.Vendor)
                                      .ThenInclude(x => x.VendorInfo)
                                      .Include(x => x.ContractType)
                                      .Include(x => x.Template)
                                      .Include(x => x.PeriodConditionType)
                                      .Include(x => x.ContractDraft)
                                      .ThenInclude(x => x.Procurement)
                                      .ThenInclude(x => x.Plan)
                                      .Include(x => x.ContractDraft)
                                      .ThenInclude(x => x.Procurement)
                                      .ThenInclude(x => x.Department)
                                      .Include(x => x.ContractDraft)
                                      .ThenInclude(x => x.Procurement)
                                      .ThenInclude(x => x.SupplyMethod)
                                      .Include(x => x.ContractDraft)
                                      .ThenInclude(x => x.Procurement)
                                      .ThenInclude(x => x.SupplyMethodType)
                                      .Include(x => x.ContractDraft)
                                      .ThenInclude(x => x.Procurement)
                                      .ThenInclude(x => x.SupplyMethodSpecialType)
                                      .AsSplitQuery()
                                      .Where(v => !v.IsDeleted)
                                      .Where(v => v.Status == ContractDraftVendorStatus.Approved)
                                      .Where(v => !v.ContractDraft.IsDeleted)
                                      .Where(v => !v.ContractDraft.Procurement.IsDeleted)
                                      .Where(v => !v.ContractDraft.Procurement.Plan.IsDeleted)
                                      .Where(v =>
                                          this.dbContext.PPurchaseOrderApprovalCommittees
                                              .Any(c => c.PPurchaseOrderApproval.ProcurementId == v.ContractDraft.ProcurementId
                                                        && c.SuUserId == UserId.From(req.UserId))
                                          ||
                                          this.dbContext.PJp005Committees
                                              .Any(c => c.PJp005.ProcurementId == v.ContractDraft.ProcurementId
                                                        && (c.GroupType == PJp005CommitteeGroupType.InspectionCommittee
                                                        || c.GroupType == PJp005CommitteeGroupType.MaintenanceInspectionCommittee
                                                        || c.GroupType == PJp005CommitteeGroupType.ConstructionSupervisor)
                                                        && c.SuUserId == UserId.From(req.UserId)))
                                      .Where(v => !this.dbContext.CmContractTerminations
                                                       .Any(t => t.ContractDraftVendorId == v.Id))
                                      .WhereIfTrue(
                                          !string.IsNullOrWhiteSpace(req.Keyword),
                                          x =>
                                              EF.Functions.ILike(x.ContractDraft.Procurement.Name, $"%{req.Keyword}%") ||
                                              EF.Functions.ILike(x.ContractDraft.Procurement.Plan!.Name, $"%{req.Keyword}%") ||
                                              EF.Functions.ILike((string)x.ContractDraft.Procurement.Plan!.PlanNumber, $"%{req.Keyword}%") ||
                                              EF.Functions.ILike(x.ContractName!, $"%{req.Keyword}%") ||
                                              EF.Functions.ILike(x.ContractNumber!, $"%{req.Keyword}%") ||
                                              EF.Functions.ILike(x.PoNumber!, $"%{req.Keyword}%") ||
                                              EF.Functions.ILike(x.Vendor.VendorInfo.EstablishmentName, $"%{req.Keyword}%") ||
                                              EF.Functions.ILike(x.Vendor.VendorInfo.SapVendorNumber, $"%{req.Keyword}%"))
                                      .WhereIfTrue(
                                          req.BudgetYear.HasValue,
                                          x => x.ContractDraft.Procurement.BudgetYear == req.BudgetYear)
                                      .WhereIfTrue(
                                          !string.IsNullOrWhiteSpace(req.DepartmentCode),
                                          x => x.ContractDraft.Procurement.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                                      .WhereIfTrue(
                                          !string.IsNullOrWhiteSpace(req.SupplyMethodCode),
                                          x => x.ContractDraft.Procurement.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                                      .WhereIfTrue(
                                          !string.IsNullOrWhiteSpace(req.SupplyMethodTypeCode),
                                          x => x.ContractDraft.Procurement.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!))
                                      .WhereIfTrue(
                                          !string.IsNullOrWhiteSpace(req.SupplyMethodSpecialTypeCode),
                                          x => x.ContractDraft.Procurement.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
                                      .AsSplitQuery()
                                      .Select(v => new GetContractVendorListResponse(
                                          v.Id.Value,
                                          v.ContractDraft.Procurement.Plan.PlanNumber.Value,
                                          v.ContractDraft.Procurement.Department.Name,
                                          v.ContractDraft.Procurement.Plan.Type,
                                          $"{v.Vendor.VendorInfo.SapVendorNumber} : {v.Vendor.VendorInfo.EstablishmentName}",
                                          v.ContractInvitationVendors.Email,
                                          v.ContractNumber,
                                          v.PoNumber,
                                          v.ContractInvitationVendors.AgreedPrice,
                                          v.ContractName,
                                          v.ContractType != null ? v.ContractType.Label : null,
                                          v.Template != null ? v.Template.Label : null,
                                          v.ContractSignedDate,
                                          v.PeriodConditionType != null ? v.PeriodConditionType.Label : null,
                                          v.EndDate,
                                          v.ContractDraft.Procurement.SupplyMethod.Label,
                                          v.ContractDraft.Procurement.SupplyMethodType != null ? v.ContractDraft.Procurement.SupplyMethodType.Label : string.Empty,
                                          v.ContractDraft.Procurement.SupplyMethodSpecialType != null ? v.ContractDraft.Procurement.SupplyMethodSpecialType.Label : string.Empty,
                                          v.ContractDraft.Procurement.Plan.BudgetYear,
                                          v.ContractDraft.Procurement.Plan.Budget,
                                          SourceType.ContractDraftVendor,
                                          v.ContractDraft.Procurement.Plan.IsStock,
                                          v.AuditInfo.CreatedAt,
                                          v.ContractDraft.ProcurementId.Value));

        var procurementQuery = this.dbContext.PPurchaseOrderApprovals
                                   .Include(poa => poa.Procurement)
                                   .ThenInclude(p => p.Plan)
                                   .Include(poa => poa.Procurement)
                                   .ThenInclude(p => p.Department)
                                   .Include(poa => poa.Procurement)
                                   .ThenInclude(p => p.SupplyMethod)
                                   .Include(poa => poa.Procurement)
                                   .ThenInclude(p => p.SupplyMethodType)
                                   .Include(poa => poa.Procurement)
                                   .ThenInclude(p => p.SupplyMethodSpecialType)
                                   .Where(poa => poa.ContractType == "CType002")
                                   .Where(poa => poa.Status == PurchaseOrderApprovalStatus.Assigned)
                                   .Where(poa => !poa.Procurement.IsDeleted && !poa.Procurement.IsCancelled)
                                   .Where(poa => poa.Procurement.Status == ProcurementStatus.Completed)
                                   .Where(poa => poa.Procurement.PlanId != null)
                                   .Where(poa => !this.dbContext.CmContractTerminations
                                                      .Any(t => t.CaContractDraftVendor.ContractDraft.ProcurementId == poa.ProcurementId))
                                   .Where(v =>
                                       this.dbContext.PPurchaseOrderApprovalCommittees
                                           .Any(c => c.PPurchaseOrderApproval.ProcurementId == v.ProcurementId
                                                     && c.SuUserId == UserId.From(req.UserId))
                                       ||
                                       this.dbContext.PJp005Committees
                                           .Any(c => c.PJp005.ProcurementId == v.ProcurementId
                                                     && (c.GroupType == PJp005CommitteeGroupType.InspectionCommittee
                                                     || c.GroupType == PJp005CommitteeGroupType.MaintenanceInspectionCommittee
                                                     || c.GroupType == PJp005CommitteeGroupType.ConstructionSupervisor)
                                                     && c.SuUserId == UserId.From(req.UserId)))
                                   .WhereIfTrue(
                                       !string.IsNullOrWhiteSpace(req.Keyword),
                                       poa =>
                                           EF.Functions.ILike(poa.Procurement.Name, $"%{req.Keyword}%") ||
                                           EF.Functions.ILike((string)poa.Procurement.Plan.PlanNumber, $"%{req.Keyword}%") ||
                                           ((poa.Procurement.ProcurementNumber.HasValue && EF.Functions.ILike((string)poa.Procurement.ProcurementNumber, $"%{req.Keyword}%")) ||
                                            poa.Contracts.Any(c => EF.Functions.ILike(c.PoNumber, $"%{req.Keyword}%"))))
                                   .WhereIfTrue(
                                       req.BudgetYear.HasValue,
                                       poa => poa.Procurement.BudgetYear == req.BudgetYear)
                                   .WhereIfTrue(
                                       !string.IsNullOrWhiteSpace(req.DepartmentCode),
                                       poa => poa.Procurement.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                                   .WhereIfTrue(
                                       !string.IsNullOrWhiteSpace(req.SupplyMethodCode),
                                       poa => poa.Procurement.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                                   .WhereIfTrue(
                                       !string.IsNullOrWhiteSpace(req.SupplyMethodTypeCode),
                                       poa => poa.Procurement.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!))
                                   .WhereIfTrue(
                                       !string.IsNullOrWhiteSpace(req.SupplyMethodSpecialTypeCode),
                                       poa => poa.Procurement.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
                                   .AsSplitQuery()
                                   .Select(poa => new GetContractVendorListResponse(
                                       poa.Id.Value,
                                       poa.Procurement.PlanId.HasValue ? poa.Procurement.Plan.PlanNumber.Value : string.Empty,
                                       poa.Procurement.Department.Name,
                                       poa.Procurement.PlanId.HasValue ? poa.Procurement.Plan.Type : null,
                                       null,
                                       null,
                                       poa.Procurement.ProcurementNumber.HasValue ? poa.Procurement.ProcurementNumber.Value.Value : string.Empty,
                                       string.Join(", ", poa.Contracts.Select(c => c.PoNumber)),
                                       null,
                                       poa.Procurement.Name,
                                       null,
                                       null,
                                       null,
                                       null,
                                       null,
                                       poa.Procurement.SupplyMethod.Label,
                                       poa.Procurement.SupplyMethodType != null ? poa.Procurement.SupplyMethodType.Label : string.Empty,
                                       poa.Procurement.SupplyMethodSpecialType != null ? poa.Procurement.SupplyMethodSpecialType.Label : string.Empty,
                                       poa.Procurement.BudgetYear,
                                       poa.Procurement.PlanId.HasValue ? poa.Procurement.Plan.Budget : null,
                                       SourceType.Procurement,
                                       poa.Procurement.PlanId.HasValue && poa.Procurement.Plan.IsStock,
                                       poa.AuditInfo.CreatedAt,
                                       poa.ProcurementId.Value));

        var contractVendorData = !req.SourceType.HasValue || req.SourceType == SourceType.ContractDraftVendor
            ? await contractVendorQuery.ToListAsync(ct)
            : new List<GetContractVendorListResponse>();
        var procurementData = !req.SourceType.HasValue || req.SourceType == SourceType.Procurement
            ? await procurementQuery.ToListAsync(ct)
            : new List<GetContractVendorListResponse>();

        var mergedData = contractVendorData
                         .Concat(procurementData)
                         .OrderByDescending(x => x.CreatedAt)
                         .ToList();

        var totalRecords = mergedData.Count;
        var paginatedData = mergedData
                            .Skip((req.PageNumber - 1) * req.PageSize)
                            .Take(req.PageSize)
                            .ToList();

        return TypedResults.Ok(new PaginatedQueryResult<GetContractVendorListResponse>(paginatedData, totalRecords));
    }
}
