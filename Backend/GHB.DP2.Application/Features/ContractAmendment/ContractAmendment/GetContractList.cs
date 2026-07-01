namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetContractListRequest(
    int PageNumber,
    int PageSize,
    string? Keyword);

public record GetContractListResponse(
    Guid ContractDraftVendorId,
    string ContractNumber,
    string PoNumber,
    string ContractName,
    string EntrepreneurCode,
    string EntrepreneurName,
    string? EntrepreneurEmail,
    DateTimeOffset? ContractSignedDate,
    decimal Budget,
    string? ContractTypeCode,
    string? ContractTypeLabel,
    ContractDraftVendorStatus Status,
    string? ContractTemplate,
    int? DeliveryLeadTime,
    ParameterCode? DeliveryLeadTimeTypeCode,
    string? DeliveryLeadTimeTypeLabel,
    DateTimeOffset? DeliveryDate);

public class GetContractListEndpoint : EndpointBase<GetContractListRequest, Ok<PaginatedQueryResult<GetContractListResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetContractListEndpoint(
        Dp2DbContext dbContext,
        ILogger<GetContractListEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x =>
            x.WithTags(nameof(ContractAmendment))
             .WithName("GetContractListDialog")
             .Produces<Ok<PaginatedQueryResult<GetContractListResponse>>>());

        this.Get("contract-amendments/contract-list");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetContractListResponse>>> HandleRequestAsync(GetContractListRequest req, CancellationToken ct)
    {
        var contractDraftVendorIds = await this.dbContext.CamContractAmendments
                                               .Select(s => s.ContractDraftVendorId)
                                               .ToListAsync(ct);

        var query = this.dbContext
                        .CaContractDraftVendors
                        .Where(w => w.Status == ContractDraftVendorStatus.Approved && !contractDraftVendorIds.Contains(w.Id) && w.ContractSignedDate.HasValue)
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.Keyword),
                            x => EF.Functions.ILike(x.ContractName, $"%{req.Keyword}%") ||
                                 EF.Functions.ILike(x.ContractNumber, $"%{req.Keyword}%"))
                        .OrderByDescending(x => x.ContractSignedDate);

        var paginated =
            await PaginatedList<CaContractDraftVendor>
                .CreateAsync(
                    query,
                    req.PageNumber,
                    req.PageSize,
                    ct);

        var res =
            paginated.ToResult(da =>
            {
                var vendor = MapSuVendorByType(da.ContractInvitationVendors, da.ContractInvitationVendors.ContractInvitation.Procurement.Type);

                return new GetContractListResponse(
                    da.Id.Value,
                    da.ContractNumber,
                    da.PoNumber,
                    da.ContractName,
                    vendor != null ? vendor.SapVendorNumber : string.Empty,
                    vendor != null ? vendor.EstablishmentName : string.Empty,
                    vendor != null ? vendor.Email : string.Empty,
                    da.ContractSignedDate,
                    da.Budget,
                    da.ContractTypeCode?.Value,
                    da.ContractType?.Label,
                    da.Status,
                    da.Template?.Label,
                    da.Delivery.LeadTime,
                    da.Delivery.LeadTimeTypeCode,
                    da.Delivery?.LeadTimeType?.Label,
                    da.Delivery?.Date);
            });

        return TypedResults.Ok(res);
    }

    private static SuVendor? MapSuVendorByType(CaContractInvitationVendors entity, ProcurementType type)
    {
        return type == ProcurementType.Procurement ? entity.PurchaseOrderApprovalContract.Entrepreneur?.SuVendor : entity.PurchaseOrderApprovalContract.PrincipleApprovalRentalEntrepreneurs?.Vendor;
    }
}