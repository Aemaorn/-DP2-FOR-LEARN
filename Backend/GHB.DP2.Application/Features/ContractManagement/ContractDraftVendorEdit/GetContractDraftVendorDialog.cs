namespace GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit;

using System.IdentityModel.Tokens.Jwt;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetContractDraftVendorDialogRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    int PageNumber,
    int PageSize,
    string? Keyword,
    int? BudgetYear,
    string? DepartmentCode,
    string? SupplyMethodCode,
    string? SupplyMethodTypeCode,
    string? SupplyMethodSpecialTypeCode);

public class GetContractDraftVendorDialogEndpoint
    : EndpointBase<GetContractDraftVendorDialogRequest, Ok<PaginatedQueryResult<ContractDraftVendorDialogItemResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetContractDraftVendorDialogEndpoint(
        Dp2DbContext dbContext,
        ILogger<GetContractDraftVendorDialogEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractDraftVendorEdit")
             .Produces<Ok>());
        this.Get("contract/contract-draft-vendor-edit/Dialog");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<ContractDraftVendorDialogItemResponse>>> HandleRequestAsync(
        GetContractDraftVendorDialogRequest req, CancellationToken ct)
    {
        var query = this.dbContext.CaContractDraftVendors
                        .Include(c => c.ContractDraft)
                        .ThenInclude(cd => cd.Procurement)
                        .ThenInclude(p => p.Department)
                        .Include(c => c.ContractDraft)
                        .ThenInclude(cd => cd.Procurement)
                        .ThenInclude(p => p.SupplyMethod)
                        .Include(c => c.Vendor)
                        .ThenInclude(v => v.VendorInfo)
                        .Include(c => c.ContractType)
                        .Include(c => c.Template)
                        .Where(c => !c.IsDeleted)
                        .Where(c => c.Status == ContractDraftVendorStatus.Approved)
                        .Where(c => !this.dbContext.CaContractDraftVendorEdits
                            .Any(e => e.ContractDraftVendorId == c.Id && !e.IsDeleted
                                && e.Status != ContractDraftVendorEditStatus.Approved))
                        .Where(c =>
                            this.dbContext.PJp005S
                                .Any(jp005 => jp005.ProcurementId == c.ContractDraft.ProcurementId
                                    && !jp005.IsDeleted
                                    && jp005.Committees.Any(committee =>
                                        (committee.GroupType == PJp005CommitteeGroupType.InspectionCommittee
                                        || committee.GroupType == PJp005CommitteeGroupType.MaintenanceInspectionCommittee
                                        || committee.GroupType == PJp005CommitteeGroupType.ConstructionSupervisor)
                                        && committee.SuUserId == UserId.From(req.UserId)))
                            || this.dbContext.PPurchaseOrderApprovals
                                .Any(poa => poa.ProcurementId == c.ContractDraft.ProcurementId
                                    && !poa.IsDeleted
                                    && (poa.Committees.Any(committee =>
                                            committee.SuUserId == UserId.From(req.UserId))
                                        || poa.Assignees.Any(assignee =>
                                            assignee.Type == AssigneeType.Assignee
                                            && assignee.UserId == UserId.From(req.UserId)
                                            && !assignee.IsDeleted))))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.Keyword),
                            c =>
                                EF.Functions.ILike(c.ContractName, $"%{req.Keyword}%") ||
                                EF.Functions.ILike(c.ContractNumber, $"%{req.Keyword}%") ||
                                EF.Functions.ILike(c.PoNumber, $"%{req.Keyword}%") ||
                                EF.Functions.ILike(c.Vendor.VendorInfo.EstablishmentName, $"%{req.Keyword}%"))
                        .WhereIfTrue(
                            req.BudgetYear.HasValue,
                            c => c.ContractDraft.Procurement.BudgetYear == req.BudgetYear!.Value)
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.DepartmentCode),
                            c => c.ContractDraft.Procurement.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.SupplyMethodCode),
                            c => c.ContractDraft.Procurement.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.SupplyMethodTypeCode),
                            c => c.ContractDraft.Procurement.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.SupplyMethodSpecialTypeCode),
                            c => c.ContractDraft.Procurement.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
                        .OrderByDescending(c => c.AuditInfo.CreatedAt)
                        .AsSplitQuery();

        var paginator =
            await PaginatedList<CaContractDraftVendor>
                .CreateAsync((IQueryable<CaContractDraftVendor>)query, req.PageNumber, req.PageSize, ct);

        var result = paginator.ToResult(
            v => new ContractDraftVendorDialogItemResponse(
                v.Id,
                v.ContractDraftNumber.Value,
                v.ContractNumber,
                v.PoNumber,
                v.ContractName,
                v.Vendor.VendorInfo.EstablishmentName,
                v.Vendor.VendorInfo.TaxpayerIdentificationNo,
                v.Budget,
                v.ContractSignedDate,
                v.ContractEndDate,
                v.Template?.Label ?? null,
                v.ContractDraft?.Procurement?.SupplyMethod?.Label,
                v.ContractDraft?.Procurement?.Department?.Name));

        return TypedResults.Ok(result);
    }
}
