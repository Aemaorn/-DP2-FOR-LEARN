namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptance;

using System.IdentityModel.Tokens.Jwt;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptance.Abstract;
using GHB.DP2.Domain.Common; // add for AcceptorStatus, AcceptorType
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;

public record GetDeliveryAcceptanceListRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    int PageNumber,
    int PageSize,
    string? Keyword,
    string? DepartmentCode,
    string? ContractType,
    string? ContractTypeCode,
    DateTimeOffset? ContractDateStart,
    DateTimeOffset? ContractDateEnd,
    string? SupplyMethodCode,
    string? SupplyMethodTypeCode,
    string? SupplyMethodSpecialTypeCode,
    CmDeliveryAcceptanceStatus? Status,
    WorkProcess WorkProcess = WorkProcess.InProcess
);

public record GetDeliveryAcceptanceListResponse(
    CmDeliveryAcceptanceId Id,
    CmDeliveryAcceptanceStatus Status,
    string? RefNumber,
    Guid? RefId,
    string? PlanNumber,
    Guid? PlanId,
    string? PoNumber,
    string Name,
    string? VendorName,
    decimal Budget,
    string DepartmentName,
    string SupplyMethod,
    string? SupplyMethodType,
    string? SupplyMethodSpecialType,
    PeriodRef[] PeriodRefs,
    SourceType SourceType,
    ProcurementType? ProcessType,
    bool IsCanDelete,
    IReadOnlyCollection<string> GLAccounts);

public record PeriodRef(
    CmDeliveryAcceptancePeriodId PeriodId,
    string? AcceptanceNumber);

public record GetStatusCount(
    int All,
    int InProgress,
    int Completed);

public record GetDeliveryAcceptanceListResult(
    GetStatusCount StatusCount,
    PaginatedQueryResult<GetDeliveryAcceptanceListResponse> Data
);

public class GetDeliveryAcceptanceList
    : DeliveryAcceptanceEndpointBase<
        GetDeliveryAcceptanceListRequest,
        Ok<GetDeliveryAcceptanceListResult>>
{
    private new readonly Dp2DbContext dbContext;

    public GetDeliveryAcceptanceList(
        Dp2DbContext dbContext,
        ILogger<GetDeliveryAcceptanceList> logger)
        : base(dbContext, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/DeliveryAcceptance")
             .WithName("GetCmDeliveryAcceptanceList")
             .Produces<Ok>());
        this.Get("delivery-acceptance");
    }

    protected override async ValueTask<Ok<GetDeliveryAcceptanceListResult>> HandleRequestAsync(
        GetDeliveryAcceptanceListRequest req,
        CancellationToken ct)
    {
        var userId = UserId.From(req.UserId);

        var query = this.BuildBaseQuery(req);
        query = await this.ApplyWorkProcessFilterAsync(query, req.WorkProcess, userId, ct);
        query = query.OrderByDescending(o => o.AuditInfo.LastModifiedAt ?? o.AuditInfo.CreatedAt);

        var paginatedQuery = ApplyStatusFilter(query, req.Status);
        var paginated = await PaginatedList<CmDeliveryAcceptance>
            .CreateAsync(paginatedQuery, req.PageNumber, req.PageSize, ct);

        var statusCount = await GetStatusCountAsync(query, ct);
        var data = await paginated.ToResultAsync(MapTest);

        return TypedResults.Ok(new GetDeliveryAcceptanceListResult(statusCount, data));

        async Task<GetDeliveryAcceptanceListResponse> MapTest(CmDeliveryAcceptance cmDeliveryAcceptance)
        {
            GetDeliveryAcceptanceListResponse result = default!;

            var glAccounts = cmDeliveryAcceptance.Periods
                                                 .SelectMany(p => p.Budgets ?? [])
                                                 .Where(b => b.AccountNo != null)
                                                 .Select(b => b.AccountNo.Label)
                                                 .Distinct()
                                                 .ToList();

            if (cmDeliveryAcceptance.SourceType == SourceType.Plan)
            {
                var planData = await this.dbContext.Plans
                                         .Include(plan => plan.Department)
                                         .Include(plan => plan.SupplyMethod)
                                         .Include(plan => plan.SupplyMethodType)
                                         .Include(plan => plan.SupplyMethodSpecialType)
                                         .FirstOrDefaultAsync(p => p.Id == PlanId.From(cmDeliveryAcceptance.RefId!.Value), ct);

                if (planData == null)
                {
                    this.ThrowError("ไม่พบข้อมูลแผน", StatusCodes.Status404NotFound);
                }

                result = new GetDeliveryAcceptanceListResponse(
                    cmDeliveryAcceptance.Id,
                    cmDeliveryAcceptance.Status,
                    planData.PlanNumber.Value,
                    cmDeliveryAcceptance.RefId,
                    planData.PlanNumber.Value,
                    cmDeliveryAcceptance.RefId,
                    null,
                    planData.Name,
                    null,
                    planData.Budget,
                    planData.Department?.Name ?? string.Empty,
                    planData.SupplyMethod.Label,
                    planData.SupplyMethodType?.Label,
                    planData.SupplyMethodSpecialType?.Label,
                    cmDeliveryAcceptance.Periods
                                        .Select(p => new PeriodRef(
                                            p.Id,
                                            p.AcceptanceNumber ?? string.Empty))
                                        .ToArray(),
                    cmDeliveryAcceptance.SourceType,
                    null,
                    !cmDeliveryAcceptance.Periods.Any(),
                    glAccounts);
            }
            else if (cmDeliveryAcceptance.SourceType == SourceType.ContractDraftVendor)
            {
                var contractDraftVendor = await this.dbContext.CaContractDraftVendors
                                                    .Include(caContractDraftVendor => caContractDraftVendor.ContractInvitationVendors)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.ContractDraft)
                                                    .ThenInclude(caContractDraft => caContractDraft.Procurement)
                                                    .ThenInclude(procurement => procurement.Plan)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.ContractDraft)
                                                    .ThenInclude(caContractDraft => caContractDraft.Procurement)
                                                    .ThenInclude(procurement => procurement.Department)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.ContractDraft)
                                                    .ThenInclude(caContractDraft => caContractDraft.Procurement)
                                                    .ThenInclude(procurement => procurement.SupplyMethod)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.ContractDraft)
                                                    .ThenInclude(caContractDraft => caContractDraft.Procurement)
                                                    .ThenInclude(procurement => procurement.SupplyMethodType)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.ContractDraft)
                                                    .ThenInclude(caContractDraft => caContractDraft.Procurement)
                                                    .ThenInclude(procurement => procurement.SupplyMethodSpecialType).Include(caContractDraftVendor => caContractDraftVendor.Vendor)
                                                    .ThenInclude(vendor => vendor.VendorInfo)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.ContractType).Include(caContractDraftVendor => caContractDraftVendor.Template)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.PeriodConditionType)
                                                    .AsSplitQuery()
                                                    .FirstOrDefaultAsync(p => p.Id == ContractDraftVendorId.From(cmDeliveryAcceptance.RefId!.Value), ct);

                if (contractDraftVendor == null)
                {
                    this.ThrowError("ไม่พบข้อมูล", StatusCodes.Status404NotFound);
                }

                var vendorInfo = contractDraftVendor.Vendor.VendorInfo;
                var vendorName = string.IsNullOrWhiteSpace(vendorInfo?.EstablishmentName) ? $"{vendorInfo?.SapVendorNumber}" : $"{vendorInfo?.SapVendorNumber} : {vendorInfo?.EstablishmentName}";

                result = new GetDeliveryAcceptanceListResponse(
                    cmDeliveryAcceptance.Id,
                    cmDeliveryAcceptance.Status,
                    contractDraftVendor.ContractNumber,
                    contractDraftVendor.ContractDraft.ProcurementId.Value,
                    contractDraftVendor.ContractDraft.Procurement.Plan?.PlanNumber.Value,
                    contractDraftVendor.ContractDraft.Procurement.Plan?.Id.Value,
                    contractDraftVendor.PoNumber,
                    contractDraftVendor.ContractName,
                    vendorName,
                    contractDraftVendor.Budget,
                    contractDraftVendor.ContractDraft.Procurement.Department?.Name ?? string.Empty,
                    contractDraftVendor.ContractDraft.Procurement.SupplyMethod.Label,
                    contractDraftVendor.ContractDraft.Procurement.SupplyMethodType?.Label,
                    contractDraftVendor.ContractDraft.Procurement.SupplyMethodSpecialType?.Label,
                    cmDeliveryAcceptance.Periods
                                        .Select(p => new PeriodRef(
                                            p.Id,
                                            p.AcceptanceNumber ?? string.Empty))
                                        .ToArray(),
                    cmDeliveryAcceptance.SourceType,
                    contractDraftVendor.ContractDraft.Procurement.Type,
                    !cmDeliveryAcceptance.Periods.Any(),
                    glAccounts);
            }
            else if (cmDeliveryAcceptance.SourceType == SourceType.Procurement)
            {
                var poaData = await this.dbContext.PPurchaseOrderApprovals
                                        .Include(poa => poa.Procurement).ThenInclude(p => p.Plan)
                                        .Include(poa => poa.Procurement).ThenInclude(p => p.Department)
                                        .Include(poa => poa.Procurement).ThenInclude(p => p.SupplyMethod)
                                        .Include(poa => poa.Procurement).ThenInclude(p => p.SupplyMethodType)
                                        .Include(poa => poa.Procurement).ThenInclude(p => p.SupplyMethodSpecialType)
                                        .AsSplitQuery()
                                        .FirstOrDefaultAsync(poa => poa.Id == PurchaseOrderApprovalId.From(cmDeliveryAcceptance.RefId!.Value), ct);

                if (poaData == null)
                {
                    this.ThrowError("ไม่พบข้อมูลใบสั่งซื้อ/จ้าง/เช่า", StatusCodes.Status404NotFound);
                }

                var procurementData = poaData.Procurement;

                result = new GetDeliveryAcceptanceListResponse(
                    cmDeliveryAcceptance.Id,
                    cmDeliveryAcceptance.Status,
                    (string?)procurementData.ProcurementNumber.Value,
                    procurementData.Id.Value,
                    procurementData.Plan?.PlanNumber.Value,
                    procurementData.Plan?.Id.Value,
                    null,
                    procurementData.Name,
                    null,
                    procurementData.Plan?.Budget ?? 0,
                    procurementData.Department?.Name ?? string.Empty,
                    procurementData.SupplyMethod.Label,
                    procurementData.SupplyMethodType?.Label,
                    procurementData.SupplyMethodSpecialType?.Label,
                    cmDeliveryAcceptance.Periods
                                        .Select(p => new PeriodRef(
                                            p.Id,
                                            p.AcceptanceNumber ?? string.Empty))
                                        .ToArray(),
                    cmDeliveryAcceptance.SourceType,
                    procurementData.Type,
                    !cmDeliveryAcceptance.Periods.Any(),
                    glAccounts);
            }
            else if (cmDeliveryAcceptance.SourceType == SourceType.ContractDraftVendorEdit)
            {
                var vendorEdit = await this.dbContext.CaContractDraftVendorEdits
                    .Include(ve => ve.Vendor).ThenInclude(v => v.VendorInfo)
                    .FirstOrDefaultAsync(ve => ve.Id == ContractDraftVendorEditId.From(cmDeliveryAcceptance.RefId!.Value), ct);

                if (vendorEdit == null)
                {
                    this.ThrowError("ไม่พบข้อมูลบันทึกต่อท้ายสัญญา", StatusCodes.Status404NotFound);
                }

                var contractDraftVendor = await this.dbContext.CaContractDraftVendors
                    .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.Plan)
                    .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.Department)
                    .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.SupplyMethod)
                    .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.SupplyMethodType)
                    .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.SupplyMethodSpecialType)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(v => v.Id == vendorEdit.ContractDraftVendorId, ct);

                if (contractDraftVendor == null)
                {
                    this.ThrowError("ไม่พบข้อมูลสัญญาต้นฉบับ", StatusCodes.Status404NotFound);
                }

                var vendorInfo = vendorEdit.Vendor.VendorInfo;
                var vendorName = string.IsNullOrWhiteSpace(vendorInfo?.EstablishmentName) ? $"{vendorInfo?.SapVendorNumber}" : $"{vendorInfo?.SapVendorNumber} : {vendorInfo?.EstablishmentName}";
                var procurement = contractDraftVendor.ContractDraft.Procurement;

                result = new GetDeliveryAcceptanceListResponse(
                    cmDeliveryAcceptance.Id,
                    cmDeliveryAcceptance.Status,
                    vendorEdit.ContractNumber,
                    procurement.Id.Value,
                    procurement.Plan?.PlanNumber.Value,
                    procurement.Plan?.Id.Value,
                    vendorEdit.PoNumber,
                    vendorEdit.ContractName,
                    vendorName,
                    vendorEdit.Budget,
                    procurement.Department?.Name ?? string.Empty,
                    procurement.SupplyMethod.Label,
                    procurement.SupplyMethodType?.Label,
                    procurement.SupplyMethodSpecialType?.Label,
                    cmDeliveryAcceptance.Periods
                        .Select(p => new PeriodRef(p.Id, p.AcceptanceNumber ?? string.Empty))
                        .ToArray(),
                    cmDeliveryAcceptance.SourceType,
                    procurement.Type,
                    !cmDeliveryAcceptance.Periods.Any(),
                    glAccounts);
            }
            else if (cmDeliveryAcceptance.SourceType == SourceType.Manual)
            {
                result = new GetDeliveryAcceptanceListResponse(
                    cmDeliveryAcceptance.Id,
                    cmDeliveryAcceptance.Status,
                    cmDeliveryAcceptance.Number,
                    null,
                    null,
                    null,
                    null,
                    cmDeliveryAcceptance.Name ?? string.Empty,
                    null,
                    cmDeliveryAcceptance.Budget ?? 0,
                    cmDeliveryAcceptance.Department?.Name ?? string.Empty,
                    cmDeliveryAcceptance.SupplyMethod?.Label ?? string.Empty,
                    cmDeliveryAcceptance.SupplyMethodType?.Label,
                    cmDeliveryAcceptance.SupplyMethodSpecialType?.Label,
                    cmDeliveryAcceptance.Periods
                                        .Select(p => new PeriodRef(
                                            p.Id,
                                            p.AcceptanceNumber ?? string.Empty))
                                        .ToArray(),
                    cmDeliveryAcceptance.SourceType,
                    null,
                    !cmDeliveryAcceptance.Periods.Any(),
                    glAccounts);
            }
            else
            {
                this.ThrowError("SourceType ไม่ถูกต้อง", StatusCodes.Status400BadRequest);
            }

            return result;
        }
    }

    private IQueryable<CmDeliveryAcceptance> BuildBaseQuery(GetDeliveryAcceptanceListRequest req)
    {
        return this.dbContext.CmDeliveryAcceptances
                   .Include(c => c.Periods)
                       .ThenInclude(p => p.Budgets)
                       .ThenInclude(b => b.AccountNo)
                   .Include(c => c.Department)
                   .Include(c => c.SupplyMethod)
                   .Include(c => c.SupplyMethodType)
                   .Include(c => c.SupplyMethodSpecialType)
                   .WhereIfTrue(
                       !req.ContractType.IsNull(),
                       x => x.ContractType == req.ContractType)
                   .WhereIfTrue(
                       !string.IsNullOrWhiteSpace(req.Keyword),
                       x =>
                           this.dbContext.Plans.Any(p =>
                               x.RefId == (Guid)p.Id &&
                               x.SourceType == SourceType.Plan &&
                               (EF.Functions.ILike(p.Name, $"%{req.Keyword}%") ||
                                EF.Functions.ILike((string)p.PlanNumber, $"%{req.Keyword}%"))) ||
                           this.dbContext.CaContractDraftVendors.Any(v =>
                               x.RefId == (Guid)v.Id &&
                               x.SourceType == SourceType.ContractDraftVendor &&
                               (EF.Functions.ILike(v.PoNumber!, $"%{req.Keyword}%") ||
                                EF.Functions.ILike(v.ContractNumber!, $"%{req.Keyword}%") ||
                                EF.Functions.ILike(v.ContractName!, $"%{req.Keyword}%") ||
                                (v.ContractDraft.Procurement.ProcurementNumber != null && EF.Functions.ILike((string)v.ContractDraft.Procurement.ProcurementNumber, $"%{req.Keyword}%")) ||
                                (v.ContractDraft.Procurement.Plan != null && EF.Functions.ILike(v.ContractDraft.Procurement.Plan.Name, $"%{req.Keyword}%")) ||
                                (v.ContractDraft.Procurement.Plan != null && EF.Functions.ILike((string)v.ContractDraft.Procurement.Plan.PlanNumber, $"%{req.Keyword}%")))) ||
                           this.dbContext.PPurchaseOrderApprovals.Any(poa =>
                               x.RefId == (Guid)poa.Id &&
                               x.SourceType == SourceType.Procurement &&
                               (EF.Functions.ILike(poa.Procurement.Name, $"%{req.Keyword}%") ||
                                EF.Functions.ILike((string)poa.Procurement.ProcurementNumber!, $"%{req.Keyword}%") ||
                                (poa.Procurement.Plan != null && EF.Functions.ILike((string)poa.Procurement.Plan.PlanNumber, $"%{req.Keyword}%")))) ||
                           this.dbContext.CaContractDraftVendorEdits.Any(ve =>
                               x.RefId == (Guid)ve.Id &&
                               x.SourceType == SourceType.ContractDraftVendorEdit &&
                               (EF.Functions.ILike(ve.PoNumber!, $"%{req.Keyword}%") ||
                                EF.Functions.ILike(ve.ContractNumber!, $"%{req.Keyword}%") ||
                                EF.Functions.ILike(ve.ContractName!, $"%{req.Keyword}%"))) ||
                           (x.SourceType == SourceType.Manual &&
                            ((x.Number != null && EF.Functions.ILike(x.Number, $"%{req.Keyword}%")) ||
                             (x.Name != null && EF.Functions.ILike(x.Name, $"%{req.Keyword}%")))) ||
                           x.Periods.Any(p => p.Budgets.Any(b => EF.Functions.ILike(b.AccountNo.Label, $"%{req.Keyword}%"))))
                   .WhereIfTrue(
                       !string.IsNullOrWhiteSpace(req.DepartmentCode),
                       x =>
                           this.dbContext.Plans.Any(p =>
                               x.RefId == (Guid)p.Id &&
                               x.SourceType == SourceType.Plan &&
                               p.DepartmentId == BusinessUnitId.From(req.DepartmentCode!)) ||
                           this.dbContext.CaContractDraftVendors.Any(v =>
                               x.RefId == (Guid)v.Id &&
                               x.SourceType == SourceType.ContractDraftVendor &&
                               v.ContractDraft.Procurement.DepartmentId == BusinessUnitId.From(req.DepartmentCode!)) ||
                           this.dbContext.PPurchaseOrderApprovals.Any(poa =>
                               x.RefId == (Guid)poa.Id &&
                               x.SourceType == SourceType.Procurement &&
                               poa.Procurement.DepartmentId == BusinessUnitId.From(req.DepartmentCode!)) ||
                           this.dbContext.CaContractDraftVendorEdits.Any(ve =>
                               x.RefId == (Guid)ve.Id &&
                               x.SourceType == SourceType.ContractDraftVendorEdit &&
                               this.dbContext.CaContractDraftVendors.Any(v =>
                                   v.Id == ve.ContractDraftVendorId &&
                                   v.ContractDraft.Procurement.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))) ||
                           (x.SourceType == SourceType.Manual &&
                            x.DepartmentId == BusinessUnitId.From(req.DepartmentCode!)))
                   .WhereIfTrue(
                       !string.IsNullOrWhiteSpace(req.ContractTypeCode),
                       x =>
                           this.dbContext.CaContractDraftVendors.Any(v =>
                               x.RefId == (Guid)v.Id &&
                               x.SourceType == SourceType.ContractDraftVendor &&
                               v.ContractTypeCode == ParameterCode.From(req.ContractTypeCode!)) ||
                           this.dbContext.CaContractDraftVendorEdits.Any(ve =>
                               x.RefId == (Guid)ve.Id &&
                               x.SourceType == SourceType.ContractDraftVendorEdit &&
                               ve.ContractTypeCode == ParameterCode.From(req.ContractTypeCode!)))
                   .WhereIfTrue(
                       req.ContractDateStart.HasValue,
                       x =>
                           this.dbContext.CaContractDraftVendors.Any(v =>
                               x.RefId == (Guid)v.Id &&
                               x.SourceType == SourceType.ContractDraftVendor &&
                               v.ContractSignedDate >= req.ContractDateStart) ||
                           this.dbContext.CaContractDraftVendorEdits.Any(ve =>
                               x.RefId == (Guid)ve.Id &&
                               x.SourceType == SourceType.ContractDraftVendorEdit &&
                               ve.ContractSignedDate >= req.ContractDateStart))
                   .WhereIfTrue(
                       req.ContractDateEnd.HasValue,
                       x =>
                           this.dbContext.CaContractDraftVendors.Any(v =>
                               x.RefId == (Guid)v.Id &&
                               x.SourceType == SourceType.ContractDraftVendor &&
                               v.ContractSignedDate <= req.ContractDateEnd) ||
                           this.dbContext.CaContractDraftVendorEdits.Any(ve =>
                               x.RefId == (Guid)ve.Id &&
                               x.SourceType == SourceType.ContractDraftVendorEdit &&
                               ve.ContractSignedDate <= req.ContractDateEnd))
                   .WhereIfTrue(
                       !string.IsNullOrWhiteSpace(req.SupplyMethodCode),
                       x =>
                           this.dbContext.Plans.Any(p =>
                               x.RefId == (Guid)p.Id &&
                               x.SourceType == SourceType.Plan &&
                               p.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!)) ||
                           this.dbContext.CaContractDraftVendors.Any(v =>
                               x.RefId == (Guid)v.Id &&
                               x.SourceType == SourceType.ContractDraftVendor &&
                               v.ContractDraft.Procurement.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!)) ||
                           this.dbContext.PPurchaseOrderApprovals.Any(poa =>
                               x.RefId == (Guid)poa.Id &&
                               x.SourceType == SourceType.Procurement &&
                               poa.Procurement.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!)) ||
                           this.dbContext.CaContractDraftVendorEdits.Any(ve =>
                               x.RefId == (Guid)ve.Id &&
                               x.SourceType == SourceType.ContractDraftVendorEdit &&
                               this.dbContext.CaContractDraftVendors.Any(v =>
                                   v.Id == ve.ContractDraftVendorId &&
                                   v.ContractDraft.Procurement.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))) ||
                           (x.SourceType == SourceType.Manual &&
                            x.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!)))
                   .WhereIfTrue(
                       !string.IsNullOrWhiteSpace(req.SupplyMethodTypeCode),
                       x =>
                           this.dbContext.Plans.Any(p =>
                               x.RefId == (Guid)p.Id &&
                               x.SourceType == SourceType.Plan &&
                               p.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!)) ||
                           this.dbContext.CaContractDraftVendors.Any(v =>
                               x.RefId == (Guid)v.Id &&
                               x.SourceType == SourceType.ContractDraftVendor &&
                               v.ContractDraft.Procurement.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!)) ||
                           this.dbContext.PPurchaseOrderApprovals.Any(poa =>
                               x.RefId == (Guid)poa.Id &&
                               x.SourceType == SourceType.Procurement &&
                               poa.Procurement.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!)) ||
                           this.dbContext.CaContractDraftVendorEdits.Any(ve =>
                               x.RefId == (Guid)ve.Id &&
                               x.SourceType == SourceType.ContractDraftVendorEdit &&
                               this.dbContext.CaContractDraftVendors.Any(v =>
                                   v.Id == ve.ContractDraftVendorId &&
                                   v.ContractDraft.Procurement.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!))) ||
                           (x.SourceType == SourceType.Manual &&
                            x.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!)))
                   .WhereIfTrue(
                       !string.IsNullOrWhiteSpace(req.SupplyMethodSpecialTypeCode),
                       x =>
                           this.dbContext.Plans.Any(p =>
                               x.RefId == (Guid)p.Id &&
                               x.SourceType == SourceType.Plan &&
                               p.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!)) ||
                           this.dbContext.CaContractDraftVendors.Any(v =>
                               x.RefId == (Guid)v.Id &&
                               x.SourceType == SourceType.ContractDraftVendor &&
                               v.ContractDraft.Procurement.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!)) ||
                           this.dbContext.PPurchaseOrderApprovals.Any(poa =>
                               x.RefId == (Guid)poa.Id &&
                               x.SourceType == SourceType.Procurement &&
                               poa.Procurement.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!)) ||
                           this.dbContext.CaContractDraftVendorEdits.Any(ve =>
                               x.RefId == (Guid)ve.Id &&
                               x.SourceType == SourceType.ContractDraftVendorEdit &&
                               this.dbContext.CaContractDraftVendors.Any(v =>
                                   v.Id == ve.ContractDraftVendorId &&
                                   v.ContractDraft.Procurement.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))) ||
                           (x.SourceType == SourceType.Manual &&
                            x.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!)))
                   .AsSplitQuery();
    }

    private async Task<IQueryable<CmDeliveryAcceptance>> ApplyWorkProcessFilterAsync(
        IQueryable<CmDeliveryAcceptance> query,
        WorkProcess workProcess,
        UserId userId,
        CancellationToken ct)
    {
        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .ThenInclude(e => e.View)
                             .FirstOrDefaultAsync(u => u.Id == userId, ct);

        var userBusinessUnitCode = user?.Employee?.PrimaryBusinessUnit?.BusinessUnitCode;

        return workProcess switch
        {
            WorkProcess.InProcess => ApplyInProcessFilter(query, userId, userBusinessUnitCode),
            WorkProcess.Related => ApplyRelatedFilter(query, userId, userBusinessUnitCode),
            WorkProcess.Completed => ApplyCompletedFilter(query, userId, userBusinessUnitCode),
            _ => query,
        };
    }

    private static IQueryable<CmDeliveryAcceptance> ApplyInProcessFilter(
        IQueryable<CmDeliveryAcceptance> query,
        UserId userId,
        string? userBusinessUnitCode)
    {
        return query.Where(x =>
            x.Status == CmDeliveryAcceptanceStatus.InProgress &&
            (
                x.Periods.Any(p =>
                    (p.Status == CmDeliveryAcceptancePeriodStatus.Draft ||
                     p.Status == CmDeliveryAcceptancePeriodStatus.Edit ||
                     p.Status == CmDeliveryAcceptancePeriodStatus.Rejected) &&
                    (p.Acceptors.Any(a => a.UserId == userId &&
                                          a.Type == AcceptorType.AcceptanceCommittee) ||
                     p.AuditInfo.CreatedBy == userId.Value)) ||
                x.Periods.Any(p =>
                    p.Status == CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval &&
                    p.Acceptors.Any(a => a.UserId == userId && a.Status == AcceptorStatus.Pending &&
                                         a.Type == AcceptorType.AcceptanceCommittee && a.IsCurrent)) ||
                x.Periods.Any(p =>
                    p.Status == CmDeliveryAcceptancePeriodStatus.WaitingAcceptance &&
                    p.Acceptors.Any(a => a.UserId == userId &&
                                         a.Status == AcceptorStatus.Pending &&
                                         a.Type == AcceptorType.Approver && a.IsCurrent)) ||
                x.Periods.Any(td => td.Status == CmDeliveryAcceptancePeriodStatus.WaitingAssign &&
                                    td.Assignees.Any(a => a.UserId == userId)) ||
                x.Periods.Any(td => td.Status == CmDeliveryAcceptancePeriodStatus.WaitingComment &&
                                    td.Assignees.Any(a => (a.UserId == userId) ||
                                                          td.Status == CmDeliveryAcceptancePeriodStatus.RejectToAssignee)) ||
                x.Periods.Any(p =>
                    p.Status == CmDeliveryAcceptancePeriodStatus.Approved &&
                    p.AccountStatus == CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval &&
                    p.Acceptors.Any(a =>
                        a.Type == AcceptorType.Accounting &&
                        a.UserId == userId &&
                        a.IsActive &&
                        !a.IsDeleted &&
                        (
                            p.Acceptors
                             .Where(acc => acc.Type == AcceptorType.Accounting && acc.IsActive && !acc.IsDeleted)
                             .All(acc => acc.Status == AcceptorStatus.Pending) ||
                            a.IsCurrent
                        ))) ||
                (userBusinessUnitCode == ExpenseDisbursementConstant.DefaultDirector.BusinessUnitCode &&
                 x.Periods.Any(p =>
                     p.Status == CmDeliveryAcceptancePeriodStatus.Approved &&
                     p.AccountStatus == CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate))));
    }

    private static IQueryable<CmDeliveryAcceptance> ApplyRelatedFilter(
        IQueryable<CmDeliveryAcceptance> query,
        UserId userId,
        string? userBusinessUnitCode)
    {
        return query.Where(x =>
            x.Periods.Any(p =>
                x.Periods.Any(p =>
                    p.Status == CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval &&
                    p.Acceptors.Any(a => a.UserId == userId && a.Status == AcceptorStatus.Pending &&
                                         a.Type == AcceptorType.AcceptanceCommittee && !a.IsCurrent))) ||
            x.Periods.Any(p =>
                p.Status == CmDeliveryAcceptancePeriodStatus.WaitingAcceptance &&
                p.Acceptors.Any(a => a.UserId == userId &&
                                     a.Status == AcceptorStatus.Pending &&
                                     a.Type == AcceptorType.Approver && !a.IsCurrent)) ||
            x.Periods.Any(td => td.Status == CmDeliveryAcceptancePeriodStatus.WaitingComment &&
                                td.Assignees.Any(a => a.UserId == userId)) ||
            x.Periods.Any(td => td.Status == CmDeliveryAcceptancePeriodStatus.WaitingAcceptance &&
                                td.Assignees.Any(a => (a.UserId == userId) ||
                                                      td.Status == CmDeliveryAcceptancePeriodStatus.RejectToAssignee)) ||
            x.Periods.Any(p =>
                p.Status == CmDeliveryAcceptancePeriodStatus.Approved &&
                p.AccountStatus == CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval &&
                p.Acceptors.Any(a =>
                    a.Type == AcceptorType.Accounting &&
                    a.UserId == userId &&
                    a.IsActive &&
                    !a.IsDeleted &&
                    (a.Status == AcceptorStatus.Approved || a.Status == AcceptorStatus.Rejected) &&
                    !a.IsCurrent)) ||
            (userBusinessUnitCode == ExpenseDisbursementConstant.DefaultDirector.BusinessUnitCode &&
             x.Periods.Any(p =>
                 p.Status == CmDeliveryAcceptancePeriodStatus.Approved &&
                 p.AccountStatus == CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate &&
                 p.Acceptors.Any(a =>
                     a.Type == AcceptorType.Accounting &&
                     a.UserId == userId &&
                     a.IsActive &&
                     !a.IsDeleted &&
                     a.Status == AcceptorStatus.Approved))));
    }

    private static IQueryable<CmDeliveryAcceptance> ApplyCompletedFilter(
        IQueryable<CmDeliveryAcceptance> query,
        UserId userId,
        string? userBusinessUnitCode)
    {
        return query.Where(x =>
            x.Status == CmDeliveryAcceptanceStatus.Completed ||
            x.Periods.Any(p =>
                p.Status == CmDeliveryAcceptancePeriodStatus.Approved &&
                p.AccountStatus == CmDeliveryAcceptancePeriodAccountStatus.Paid &&
                (
                    p.Acceptors.Any(a =>
                        a.Type == AcceptorType.Accounting &&
                        a.UserId == userId &&
                        a.IsActive &&
                        !a.IsDeleted) ||
                    userBusinessUnitCode == ExpenseDisbursementConstant.DefaultDirector.BusinessUnitCode
                )));
    }

    private static IQueryable<CmDeliveryAcceptance> ApplyStatusFilter(
        IQueryable<CmDeliveryAcceptance> query,
        CmDeliveryAcceptanceStatus? status)
    {
        return query.WhereIfTrue(!status.IsNull(), p => p.Status == status);
    }

    private static async ValueTask<GetStatusCount> GetStatusCountAsync(
        IQueryable<CmDeliveryAcceptance> query,
        CancellationToken ct)
    {
        var statusCounts = await query
                                 .GroupBy(x => x.Status)
                                 .Select(g => new { Status = g.Key, Count = g.Count() })
                                 .ToListAsync(ct);

        var totalCount = statusCounts.Sum(x => x.Count);
        var inProgressCount = statusCounts.FirstOrDefault(x => x.Status == CmDeliveryAcceptanceStatus.InProgress)?.Count ?? 0;
        var completedCount = statusCounts.FirstOrDefault(x => x.Status == CmDeliveryAcceptanceStatus.Completed)?.Count ?? 0;

        return new GetStatusCount(totalCount, inProgressCount, completedCount);
    }
}