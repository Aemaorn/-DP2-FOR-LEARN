namespace GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition;

using System.IdentityModel.Tokens.Jwt;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetDeliveryAcceptanceDialogRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    int PageNumber,
    int PageSize,
    string? Keyword,
    int? BudgetYear,
    string? DepartmentCode,
    string? SupplyMethodCode,
    string? SupplyMethodTypeCode,
    string? SupplyMethodSpecialTypeCode
);

public record GetDeliveryAcceptanceDialogListResponse(
    ContractDraftVendorId Id,
    PlanId PlanId,
    string DpNumber,
    string TaxId,
    string EntrepreneurName,
    string EntrepreneurEmail,
    string ContractNumber,
    string PoNumber,
    decimal Budget,
    string ContractName,
    string? ContractType,
    string? ContractTemplate,
    DateTimeOffset? ContractSignedDate,
    int? DeliveryLeadTime,
    ParameterCode? DeliveryLeadTimeTypeCode,
    string? DeliveryLeadTimeTypeLabel,
    DateTimeOffset? DeliveryDate
);

public class GetDeliveryAcceptanceDialogHandler : EndpointBase<GetDeliveryAcceptanceDialogRequest, Ok<PaginatedQueryResult<GetDeliveryAcceptanceDialogListResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetDeliveryAcceptanceDialogHandler(
        Dp2DbContext dbContext,
        ILogger<GetDeliveryAcceptanceDialogHandler> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment/CertificateRequisition")
             .Produces<Ok>());
        this.Get("certificate-requisition/delivery-acceptance-dialog");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetDeliveryAcceptanceDialogListResponse>>> HandleRequestAsync(GetDeliveryAcceptanceDialogRequest req, CancellationToken ct)
    {
        var approvedRefIdGuids = await this.dbContext.CmDeliveryAcceptances
                                           .Where(da => da.SourceType == SourceType.ContractDraftVendor &&
                                                        da.RefId.HasValue &&
                                                        !da.Periods.Any(p => p.AccountStatus != CmDeliveryAcceptancePeriodAccountStatus.Paid))
                                           .Select(da => da.RefId!.Value)
                                           .ToListAsync(ct);

        var approvedVendorIds = approvedRefIdGuids
                                .Select(ContractDraftVendorId.From)
                                .ToList();

        var query = this.dbContext.CaContractDraftVendors
                        .Include(c => c.ContractDraft)
                        .ThenInclude(cd => cd.Procurement)
                        .ThenInclude(p => p.Plan)
                        .Include(c => c.Vendor)
                        .ThenInclude(v => v.VendorInfo)
                        .Include(c => c.ContractType)
                        .Include(c => c.Template)
                        .Include(c => c.ContractInvitationVendors)
                        .Include(c => c.Delivery)
                        .Include(c => c.DraftTermsConditions)
                        .ThenInclude(c => c.Guarantee)
                        .Where(c => !c.IsDeleted && approvedVendorIds.Contains(c.Id))
                        .Where(p => !this.dbContext.CamCertificateRequisitions
                                         .Any(d => d.ContractDraftVendorId == p.Id))
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
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.Keyword),
                            c =>
                                EF.Functions.ILike(c.ContractName, $"%{req.Keyword}%") ||
                                EF.Functions.ILike(c.ContractNumber, $"%{req.Keyword}%") ||
                                EF.Functions.ILike(c.ContractInvitationVendors.PoNumber, $"%{req.Keyword}%") ||
                                EF.Functions.ILike(c.Email, $"{req.Keyword}%") ||
                                EF.Functions.ILike((string)c.ContractDraft.Procurement.Plan.PlanNumber, $"%{req.Keyword}%"))
                        .WhereIfTrue(
                            req.BudgetYear.HasValue,
                            c => c.ContractDraft.Procurement.BudgetYear == req.BudgetYear.Value)
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.DepartmentCode),
                            c => c.ContractDraft.Procurement.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.SupplyMethodCode),
                            x => x.ContractDraft.Procurement.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.SupplyMethodTypeCode),
                            x => x.ContractDraft.Procurement.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.SupplyMethodSpecialTypeCode),
                            x => x.ContractDraft.Procurement.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
                        .AsSplitQuery();

        var paginator =
            await PaginatedList<CaContractDraftVendor>
                .CreateAsync(
                    query,
                    req.PageNumber,
                    req.PageSize,
                    ct);

        var result = paginator.ToResult(
            v => new GetDeliveryAcceptanceDialogListResponse(
                v.Id,
                v.ContractDraft.Procurement.Plan.Id,
                v.ContractDraft?.Procurement?.Plan?.PlanNumber.Value ?? string.Empty,
                v.Vendor.VendorInfo.TaxpayerIdentificationNo,
                v.Vendor.VendorInfo.EstablishmentName,
                v.Email,
                v.ContractNumber,
                v.ContractInvitationVendors.PoNumber,
                v.Budget,
                v.ContractName,
                v.ContractType?.Label,
                v.Template?.Label,
                v.ContractSignedDate,
                v.Delivery.LeadTime,
                v.Delivery.LeadTimeTypeCode,
                v.Delivery?.LeadTimeType?.Label,
                v.Delivery?.Date));

        return TypedResults.Ok(result);
    }
}