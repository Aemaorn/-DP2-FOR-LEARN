namespace GHB.DP2.Application.Features.ContractManagement;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetContractListRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    string? DepartmentCode,
    DateTimeOffset? ContractSignedDateFrom,
    DateTimeOffset? ContractSignedDateTo,
    ContractDraftVendorStatus? Status);

public record GetContractListResponse(
    ContractDraftVendorId ContractDraftVendorId,
    string ContractNumber,
    string PoNumber,
    string ContractName,
    string EntrepreneurCode,
    string EntrepreneurName,
    string? EntrepreneurEmail,
    DateTimeOffset? ContractSignedDate,
    decimal Budget,
    ParameterCode? ContractTypeCode,
    string? ContractTypeLabel,
    ContractDraftVendorStatus Status,
    string? ContractTemplate,
    int? DeliveryLeadTime,
    ParameterCode? DeliveryLeadTimeTypeCode,
    string? DeliveryLeadTimeTypeLabel,
    DateTimeOffset? DeliveryDate);

public record GetStatusCount(
    int All,
    int InProgress,
    int Completed);

public record GetContractResult(
    GetStatusCount StatusCount,
    PaginatedQueryResult<GetContractListResponse> Data
);

public class GetContractDisbursementApprovalListEndpoint : EndpointBase<GetContractListRequest, Ok<GetContractResult>>
{
    private readonly Dp2DbContext dbContext;

    public GetContractDisbursementApprovalListEndpoint(
        Dp2DbContext dbContext,
        ILogger<GetContractDisbursementApprovalListEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement")
             .WithName("GetContractList")
             .Produces<Ok>());
        this.Get("contract");
    }

    protected override async ValueTask<Ok<GetContractResult>> HandleRequestAsync(
        GetContractListRequest req,
        CancellationToken ct)
    {
        var query =
            this.dbContext.CaContractDraftVendors
                .WhereIfTrue(
                    !string.IsNullOrWhiteSpace(req.Keyword),
                    x => EF.Functions.ILike(x.ContractName, $"%{req.Keyword}%") ||
                         EF.Functions.ILike(x.ContractNumber, $"%{req.Keyword}%"))
                .WhereIfTrue(
                    !string.IsNullOrWhiteSpace(req.DepartmentCode),
                    x =>
                        x.ContractDraft
                         .Procurement.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                .WhereIfTrue(
                    req.ContractSignedDateFrom != null,
                    x =>
                        x.ContractSignedDate != null &&
                        x.ContractSignedDate >= req.ContractSignedDateFrom)
                .WhereIfTrue(
                    req.ContractSignedDateTo != null,
                    x =>
                        x.ContractSignedDate != null &&
                        x.ContractSignedDate <= req.ContractSignedDateTo);

        var paginatedQuery =
            query.WhereIfTrue(
                !req.Status.IsNull(),
                p => p.Status == req.Status);

        var paginated =
            await PaginatedList<CaContractDraftVendor>
                .CreateAsync(
                    paginatedQuery,
                    req.PageNumber,
                    req.PageSize,
                    ct);

        var result = await query.ToListAsync(ct);

        var statusCount =
            new GetStatusCount(
                result.Count,
                result.Count(s => s.Status == ContractDraftVendorStatus.Draft || s.Status == ContractDraftVendorStatus.Pending),
                result.Count(s => s.Status == ContractDraftVendorStatus.Approved));

        var data =
            paginated.ToResult(da =>
            {
                var vendor = MapSuVendorByType(da.ContractInvitationVendors, da.ContractInvitationVendors.ContractInvitation.Procurement.Type);

                return
                    new GetContractListResponse(
                        da.Id,
                        da.ContractNumber,
                        da.PoNumber,
                        da.ContractName,
                        vendor != null ? vendor.SapVendorNumber : string.Empty,
                        vendor != null ? vendor.EstablishmentName : string.Empty,
                        vendor != null ? vendor.Email : string.Empty,
                        da.ContractSignedDate,
                        da.Budget,
                        da.ContractTypeCode,
                        da.ContractType?.Label,
                        da.Status,
                        da.Template?.Label,
                        da.Delivery.LeadTime,
                        da.Delivery.LeadTimeTypeCode,
                        da.Delivery?.LeadTimeType?.Label,
                        da.Delivery?.Date);
            });

        return
            TypedResults.Ok(
                new GetContractResult(
                    statusCount,
                    data));
    }

    private static SuVendor? MapSuVendorByType(CaContractInvitationVendors entity, ProcurementType type)
    {
        return type == ProcurementType.Procurement ? entity.PurchaseOrderApprovalContract.Entrepreneur?.SuVendor : entity.PurchaseOrderApprovalContract.PrincipleApprovalRentalEntrepreneurs?.Vendor;
    }
}