namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptance;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetPlanAndContractVendorListRequest(
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

public record GetPlanAndContractVendorListResponse(
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

public class GetPlanAndContractVendorList
    : EndpointBase<GetPlanAndContractVendorListRequest, Ok<PaginatedQueryResult<GetPlanAndContractVendorListResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetPlanAndContractVendorList(
        Dp2DbContext dbContext,
        ILogger<GetPlanAndContractVendorList> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/DeliveryAcceptance")
             .Produces<Ok>());
        this.Get("delivery-acceptance/plan-and-contract-vendor");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetPlanAndContractVendorListResponse>>> HandleRequestAsync(
        GetPlanAndContractVendorListRequest req,
        CancellationToken ct)
    {
        var userBusinessUnitIdValue = await this.dbContext.SuUsers
                                                .Where(u => u.Id == UserId.From(req.UserId))
                                                .Select(u => u.Employee.View != null ? (string?)u.Employee.View.BusinessUnitId : null)
                                                .FirstOrDefaultAsync(ct);

        var planQuery = this.dbContext.Plans
                            .Include(x => x.Department)
                            .Include(x => x.SupplyMethod)
                            .Include(x => x.SupplyMethodType)
                            .Include(x => x.SupplyMethodSpecialType)
                            .Where(p => !p.IsDeleted && p.IsActive)
                            .WhereIfTrue(
                                !string.IsNullOrWhiteSpace(userBusinessUnitIdValue),
                                p => p.DepartmentId == BusinessUnitId.From(userBusinessUnitIdValue!))
                            .Where(p => p.Status == PlanStatus.Announcement || p.Status == PlanStatus.ApprovePlan)
                            .Where(p => !this.dbContext.CmDeliveryAcceptances
                                             .Any(da => da.SourceType == SourceType.Plan && da.RefId == (Guid)p.Id))
                            .Where(p => !this.dbContext.Procurements
                                             .Any(proc => proc.PlanId == p.Id
                                                          && !proc.IsDeleted))
                            .Where(p => !this.dbContext.PPurchaseOrderApprovals
                                             .Where(poa => poa.Procurement.PlanId == p.Id)
                                             .Any(poa => this.dbContext.CmDeliveryAcceptances
                                                              .Any(da => da.SourceType == SourceType.Procurement && da.RefId == (Guid)poa.Id)))
                            .Where(p => !this.dbContext.CaContractDraftVendors
                                             .Where(cdv => cdv.ContractDraft.Procurement.PlanId == p.Id)
                                             .Any(cdv => this.dbContext.CmDeliveryAcceptances
                                                              .Any(da => da.SourceType == SourceType.ContractDraftVendor && da.RefId == (Guid)cdv.Id)))
                            .Where(p => !this.dbContext.CaContractDraftVendorEdits
                                             .Where(cdve => cdve.ContractDraftVendor.ContractDraft.Procurement.PlanId == p.Id)
                                             .Any(cdve => this.dbContext.CmDeliveryAcceptances
                                                               .Any(da => da.SourceType == SourceType.ContractDraftVendorEdit && da.RefId == (Guid)cdve.Id)))
                            .WhereIfTrue(
                                !string.IsNullOrWhiteSpace(req.Keyword),
                                x =>
                                    EF.Functions.ILike(x.Name, $"%{req.Keyword}%") ||
                                    EF.Functions.ILike((string)x.PlanNumber, $"%{req.Keyword}%"))
                            .WhereIfTrue(
                                req.BudgetYear.HasValue,
                                x => x.BudgetYear == req.BudgetYear)
                            .WhereIfTrue(
                                !string.IsNullOrWhiteSpace(req.DepartmentCode),
                                x => x.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                            .WhereIfTrue(
                                !string.IsNullOrWhiteSpace(req.SupplyMethodCode),
                                x => x.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                            .WhereIfTrue(
                                !string.IsNullOrWhiteSpace(req.SupplyMethodTypeCode),
                                x => x.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!))
                            .WhereIfTrue(
                                !string.IsNullOrWhiteSpace(req.SupplyMethodSpecialTypeCode),
                                x => x.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
                            .AsSplitQuery()
                            .Select(p => new GetPlanAndContractVendorListResponse(
                                p.Id.Value,
                                p.PlanNumber.Value,
                                p.Department.Name,
                                p.Type,
                                null,
                                null,
                                null,
                                null,
                                null,
                                p.Name,
                                null,
                                null,
                                null,
                                null,
                                null,
                                p.SupplyMethod.Label,
                                p.SupplyMethodType != null ? p.SupplyMethodType.Label : string.Empty,
                                p.SupplyMethodSpecialType != null ? p.SupplyMethodSpecialType.Label : string.Empty,
                                p.BudgetYear,
                                p.Budget,
                                SourceType.Plan,
                                p.IsStock,
                                p.AuditInfo.CreatedAt,
                                null));

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
                                      .Where(v => v.ContractStatus != ContractStatus.Cancel)
                                      .Where(v => !v.ContractDraft.IsDeleted)
                                      .Where(v => !v.ContractDraft.Procurement.IsDeleted)
                                      .Where(v => v.ContractDraft.Procurement.Plan == null || !v.ContractDraft.Procurement.Plan.IsDeleted)
                                      .Where(v =>
                                          this.dbContext.PPurchaseOrderApprovalCommittees
                                              .Any(c => c.PPurchaseOrderApproval.ProcurementId == v.ContractDraft.ProcurementId
                                                        && c.SuUserId == UserId.From(req.UserId))
                                          ||
                                          this.dbContext.PJp005Committees
                                              .Any(c => c.PJp005.ProcurementId == v.ContractDraft.ProcurementId
                                                        && c.GroupType == PJp005CommitteeGroupType.InspectionCommittee
                                                        && c.SuUserId == UserId.From(req.UserId))
                                          ||
                                          this.dbContext.PPrincipleApprovals
                                              .Any(pa => pa.ProcurementId == v.ContractDraft.ProcurementId
                                                         && pa.PrincipleApprovalCommittees.Any(c =>
                                                             c.GroupType == CommitteeGroupType.AcceptanceCommittee
                                                             && c.SuUserId == UserId.From(req.UserId))))
                                      .Where(v => !this.dbContext.CmDeliveryAcceptances
                                                       .Any(da => da.SourceType == SourceType.ContractDraftVendor && da.RefId == (Guid)v.Id))
                                      .Where(v => !this.dbContext.CmDeliveryAcceptances
                                                       .Any(da => da.SourceType == SourceType.Plan
                                                                  && v.ContractDraft.Procurement.Plan != null
                                                                  && da.RefId == (Guid)v.ContractDraft.Procurement.Plan.Id))
                                      .Where(v => !this.dbContext.PPurchaseOrderApprovals
                                                       .Where(poa => poa.ProcurementId == v.ContractDraft.ProcurementId)
                                                       .Any(poa => this.dbContext.CmDeliveryAcceptances
                                                                        .Any(da => da.SourceType == SourceType.Procurement && da.RefId == (Guid)poa.Id)))
                                      .Where(v => !this.dbContext.CaContractDraftVendorEdits
                                                       .Where(cdve => cdve.ContractDraftVendorId == v.Id)
                                                       .Any(cdve => this.dbContext.CmDeliveryAcceptances
                                                                         .Any(da => da.SourceType == SourceType.ContractDraftVendorEdit && da.RefId == (Guid)cdve.Id)))
                                      .WhereIfTrue(
                                          !string.IsNullOrWhiteSpace(req.Keyword),
                                          x =>
                                              EF.Functions.ILike(x.ContractDraft.Procurement.Name, $"%{req.Keyword}%") ||
                                              (x.ContractDraft.Procurement.Plan != null && EF.Functions.ILike(x.ContractDraft.Procurement.Plan.Name, $"%{req.Keyword}%")) ||
                                              (x.ContractDraft.Procurement.Plan != null && EF.Functions.ILike((string)x.ContractDraft.Procurement.Plan.PlanNumber, $"%{req.Keyword}%")) ||
                                              (x.ContractDraft.Procurement.ProcurementNumber != null && EF.Functions.ILike((string)x.ContractDraft.Procurement.ProcurementNumber, $"%{req.Keyword}%")) ||
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
                                      .Select(v => new GetPlanAndContractVendorListResponse(
                                          v.Id.Value,
                                          v.ContractDraft.Procurement.Plan != null ? v.ContractDraft.Procurement.Plan.PlanNumber.Value : v.ContractDraft.Procurement.ProcurementNumber != null ? (string)v.ContractDraft.Procurement.ProcurementNumber.Value : string.Empty,
                                          v.ContractDraft.Procurement.Department.Name,
                                          v.ContractDraft.Procurement.Plan != null ? v.ContractDraft.Procurement.Plan.Type : (PlanType?)null,
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
                                          v.ContractDraft.Procurement.Plan != null ? v.ContractDraft.Procurement.Plan.BudgetYear : (int?)null,
                                          v.ContractDraft.Procurement.Plan != null ? v.ContractDraft.Procurement.Plan.Budget : (decimal?)null,
                                          SourceType.ContractDraftVendor,
                                          v.ContractDraft.Procurement.Plan != null && v.ContractDraft.Procurement.Plan.IsStock,
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
                                   .Where(poa => !this.dbContext.CmDeliveryAcceptances
                                                      .Any(da => da.SourceType == SourceType.Procurement && da.RefId == (Guid)poa.Id))
                                   .Where(poa => !this.dbContext.CaContractDraftVendors
                                                      .Any(cdv => cdv.ContractDraft.ProcurementId == poa.ProcurementId
                                                               && !cdv.IsDeleted
                                                               && cdv.Status == ContractDraftVendorStatus.Approved
                                                               && cdv.ContractStatus != ContractStatus.Cancel
                                                               && !cdv.ContractDraft.IsDeleted))
                                   .Where(poa => !this.dbContext.CmDeliveryAcceptances
                                                      .Any(da => da.SourceType == SourceType.Plan
                                                                 && poa.Procurement.Plan != null
                                                                 && da.RefId == (Guid)poa.Procurement.Plan.Id))
                                   .Where(poa => !this.dbContext.CaContractDraftVendors
                                                      .Where(cdv => cdv.ContractDraft.ProcurementId == poa.ProcurementId)
                                                      .Any(cdv => this.dbContext.CmDeliveryAcceptances
                                                                       .Any(da => da.SourceType == SourceType.ContractDraftVendor && da.RefId == (Guid)cdv.Id)))
                                   .Where(poa => !this.dbContext.CaContractDraftVendorEdits
                                                      .Where(cdve => cdve.ContractDraftVendor.ContractDraft.ProcurementId == poa.ProcurementId)
                                                      .Any(cdve => this.dbContext.CmDeliveryAcceptances
                                                                        .Any(da => da.SourceType == SourceType.ContractDraftVendorEdit && da.RefId == (Guid)cdve.Id)))
                                   .Where(v =>
                                       this.dbContext.PPurchaseOrderApprovalCommittees
                                           .Any(c => c.PPurchaseOrderApproval.ProcurementId == v.ProcurementId
                                                     && c.SuUserId == UserId.From(req.UserId))
                                       ||
                                       this.dbContext.PJp005Committees
                                           .Any(c => c.PJp005.ProcurementId == v.ProcurementId
                                                     && c.GroupType == PJp005CommitteeGroupType.InspectionCommittee
                                                     && c.SuUserId == UserId.From(req.UserId))
                                       ||
                                       this.dbContext.PPrincipleApprovals
                                           .Any(pa => pa.ProcurementId == v.ProcurementId
                                                      && pa.PrincipleApprovalCommittees.Any(c =>
                                                          c.GroupType == CommitteeGroupType.AcceptanceCommittee
                                                          && c.SuUserId == UserId.From(req.UserId))))
                                   .WhereIfTrue(
                                       !string.IsNullOrWhiteSpace(req.Keyword),
                                       poa =>
                                           EF.Functions.ILike(poa.Procurement.Name, $"%{req.Keyword}%") ||
                                           (poa.Procurement.Plan != null && EF.Functions.ILike((string)poa.Procurement.Plan.PlanNumber, $"%{req.Keyword}%")) ||
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
                                   .Select(poa => new GetPlanAndContractVendorListResponse(
                                       poa.Id.Value,
                                       poa.Procurement.PlanId.HasValue ? poa.Procurement.Plan.PlanNumber.Value : poa.Procurement.ProcurementNumber != null ? (string)poa.Procurement.ProcurementNumber.Value : string.Empty,
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

        var vendorEditQuery = this.dbContext.CaContractDraftVendorEdits
                                  .Where(ve => !ve.IsDeleted)
                                  .Where(ve => ve.Status == ContractDraftVendorEditStatus.Approved)
                                  .Where(ve => !ve.ContractDraftVendor.IsDeleted)
                                  .Where(ve => !ve.ContractDraftVendor.ContractDraft.IsDeleted)
                                  .Where(ve => !ve.ContractDraftVendor.ContractDraft.Procurement.IsDeleted)
                                  .Where(ve => !this.dbContext.CmDeliveryAcceptances
                                                   .Where(da => da.SourceType == SourceType.ContractDraftVendorEdit)
                                                   .Any(d => d.RefId == (Guid)ve.Id))
                                  .Where(ve =>
                                      this.dbContext.PPurchaseOrderApprovalCommittees
                                          .Any(c => c.PPurchaseOrderApproval.ProcurementId == ve.ContractDraftVendor.ContractDraft.ProcurementId
                                                    && c.SuUserId == UserId.From(req.UserId))
                                      ||
                                      this.dbContext.PJp005Committees
                                          .Any(c => c.PJp005.ProcurementId == ve.ContractDraftVendor.ContractDraft.ProcurementId
                                                    && c.GroupType == PJp005CommitteeGroupType.InspectionCommittee
                                                    && c.SuUserId == UserId.From(req.UserId))
                                      ||
                                      this.dbContext.PPrincipleApprovals
                                          .Any(pa => pa.ProcurementId == ve.ContractDraftVendor.ContractDraft.ProcurementId
                                                     && pa.PrincipleApprovalCommittees.Any(c =>
                                                         c.GroupType == CommitteeGroupType.AcceptanceCommittee
                                                         && c.SuUserId == UserId.From(req.UserId))))
                                  .WhereIfTrue(
                                      !string.IsNullOrWhiteSpace(req.Keyword),
                                      ve =>
                                          EF.Functions.ILike(ve.ContractName!, $"%{req.Keyword}%") ||
                                          EF.Functions.ILike(ve.ContractNumber!, $"%{req.Keyword}%") ||
                                          EF.Functions.ILike(ve.PoNumber!, $"%{req.Keyword}%") ||
                                          (ve.ContractDraftVendor.ContractDraft.Procurement.Plan != null && EF.Functions.ILike(ve.ContractDraftVendor.ContractDraft.Procurement.Plan.Name, $"%{req.Keyword}%")) ||
                                          (ve.ContractDraftVendor.ContractDraft.Procurement.Plan != null && EF.Functions.ILike((string)ve.ContractDraftVendor.ContractDraft.Procurement.Plan.PlanNumber, $"%{req.Keyword}%")) ||
                                          (ve.ContractDraftVendor.ContractDraft.Procurement.ProcurementNumber != null && EF.Functions.ILike((string)ve.ContractDraftVendor.ContractDraft.Procurement.ProcurementNumber, $"%{req.Keyword}%")))
                                  .WhereIfTrue(
                                      req.BudgetYear.HasValue,
                                      ve => ve.ContractDraftVendor.ContractDraft.Procurement.BudgetYear == req.BudgetYear)
                                  .WhereIfTrue(
                                      !string.IsNullOrWhiteSpace(req.DepartmentCode),
                                      ve => ve.ContractDraftVendor.ContractDraft.Procurement.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                                  .WhereIfTrue(
                                      !string.IsNullOrWhiteSpace(req.SupplyMethodCode),
                                      ve => ve.ContractDraftVendor.ContractDraft.Procurement.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                                  .WhereIfTrue(
                                      !string.IsNullOrWhiteSpace(req.SupplyMethodTypeCode),
                                      ve => ve.ContractDraftVendor.ContractDraft.Procurement.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!))
                                  .WhereIfTrue(
                                      !string.IsNullOrWhiteSpace(req.SupplyMethodSpecialTypeCode),
                                      ve => ve.ContractDraftVendor.ContractDraft.Procurement.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
                                  .AsSplitQuery()
                                  .Select(ve => new GetPlanAndContractVendorListResponse(
                                      ve.Id.Value,
                                      ve.ContractDraftVendor.ContractDraft.Procurement.Plan != null ? ve.ContractDraftVendor.ContractDraft.Procurement.Plan.PlanNumber.Value : ve.ContractDraftVendor.ContractDraft.Procurement.ProcurementNumber != null ? (string)ve.ContractDraftVendor.ContractDraft.Procurement.ProcurementNumber.Value : string.Empty,
                                      ve.ContractDraftVendor.ContractDraft.Procurement.Department.Name,
                                      ve.ContractDraftVendor.ContractDraft.Procurement.Plan != null ? ve.ContractDraftVendor.ContractDraft.Procurement.Plan.Type : (PlanType?)null,
                                      $"{ve.Vendor.VendorInfo.SapVendorNumber} : {ve.Vendor.VendorInfo.EstablishmentName}",
                                      ve.Email,
                                      ve.ContractNumber,
                                      ve.PoNumber,
                                      ve.Budget,
                                      ve.ContractName,
                                      ve.ContractType != null ? ve.ContractType.Label : null,
                                      ve.Template != null ? ve.Template.Label : null,
                                      ve.ContractSignedDate,
                                      ve.PeriodConditionType != null ? ve.PeriodConditionType.Label : null,
                                      ve.EndDate,
                                      ve.ContractDraftVendor.ContractDraft.Procurement.SupplyMethod.Label,
                                      ve.ContractDraftVendor.ContractDraft.Procurement.SupplyMethodType != null ? ve.ContractDraftVendor.ContractDraft.Procurement.SupplyMethodType.Label : string.Empty,
                                      ve.ContractDraftVendor.ContractDraft.Procurement.SupplyMethodSpecialType != null ? ve.ContractDraftVendor.ContractDraft.Procurement.SupplyMethodSpecialType.Label : string.Empty,
                                      ve.ContractDraftVendor.ContractDraft.Procurement.Plan != null ? ve.ContractDraftVendor.ContractDraft.Procurement.Plan.BudgetYear : (int?)null,
                                      ve.ContractDraftVendor.ContractDraft.Procurement.Plan != null ? ve.ContractDraftVendor.ContractDraft.Procurement.Plan.Budget : (decimal?)null,
                                      SourceType.ContractDraftVendorEdit,
                                      ve.ContractDraftVendor.ContractDraft.Procurement.Plan != null && ve.ContractDraftVendor.ContractDraft.Procurement.Plan.IsStock,
                                      ve.AuditInfo.CreatedAt,
                                      ve.ContractDraftVendor.ContractDraft.ProcurementId.Value));

        var planData = !req.SourceType.HasValue || req.SourceType == SourceType.Plan
            ? await planQuery.ToListAsync(ct)
            : new List<GetPlanAndContractVendorListResponse>();
        var contractVendorData = !req.SourceType.HasValue || req.SourceType == SourceType.ContractDraftVendor
            ? await contractVendorQuery.ToListAsync(ct)
            : new List<GetPlanAndContractVendorListResponse>();
        var procurementData = !req.SourceType.HasValue || req.SourceType == SourceType.Procurement
            ? await procurementQuery.ToListAsync(ct)
            : new List<GetPlanAndContractVendorListResponse>();
        var vendorEditData = !req.SourceType.HasValue || req.SourceType == SourceType.ContractDraftVendorEdit
            ? await vendorEditQuery.ToListAsync(ct)
            : new List<GetPlanAndContractVendorListResponse>();

        var mergedData = planData
                         .Concat(contractVendorData)
                         .Concat(procurementData)
                         .Concat(vendorEditData)
                         .OrderByDescending(x => x.CreatedAt)
                         .ToList();

        var totalRecords = mergedData.Count;
        var paginatedData = mergedData
                            .Skip((req.PageNumber - 1) * req.PageSize)
                            .Take(req.PageSize)
                            .ToList();

        return TypedResults.Ok(new PaginatedQueryResult<GetPlanAndContractVendorListResponse>(paginatedData, totalRecords));
    }
}