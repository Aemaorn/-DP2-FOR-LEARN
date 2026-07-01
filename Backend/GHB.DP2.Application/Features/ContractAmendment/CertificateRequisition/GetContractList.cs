namespace GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetContractDeliveryAcceptanceListRequest(
    int PageNumber,
    int PageSize,
    string? Keyword);

public record GetContractDeliveryAcceptanceListItemResponse(
    ContractDraftVendorId ContractDraftVendorId,
    string ContractNumber,
    string PoNumber,
    DateTimeOffset? ContractSignedDate,
    string EntrepreneurCode,
    string EntrepreneurName,
    string ContractName,
    decimal Budget,
    ParameterCode? ContractTypeCode,
    string? ContractTypeLabel);

public class GetContractDeliveryAcceptanceListEndpoint
    : EndpointBase<GetContractDeliveryAcceptanceListRequest, Ok<PaginatedQueryResult<GetContractDeliveryAcceptanceListItemResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetContractDeliveryAcceptanceListEndpoint(
        Dp2DbContext dbContext,
        ILogger<GetContractDeliveryAcceptanceListEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment/CertificateRequisition")
             .WithName("GetContracts")
             .Produces<PaginatedQueryResult<GetContractDeliveryAcceptanceListItemResponse>>());
        this.Get("contracts");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetContractDeliveryAcceptanceListItemResponse>>>
        HandleRequestAsync(
            GetContractDeliveryAcceptanceListRequest req,
            CancellationToken ct)
    {
        var keyword = $"%{req.Keyword}%";

        var query =
            this.dbContext.CaContractDraftVendors
                .Include(cv => cv.Vendor)
                .Where(cv =>
                    cv.Status == ContractDraftVendorStatus.Approved &&
                    cv.DeliveryAcceptances.Any(da => da.Status == CmDeliveryAcceptanceStatus.Completed) &&
                    !cv.CamCertificateRequisitions.Any())
                .WhereIfTrue(
                    !string.IsNullOrWhiteSpace(req.Keyword),
                    cv =>
                        EF.Functions.ILike(cv.PoNumber, keyword) ||
                        EF.Functions.ILike(cv.ContractName, keyword) ||
                        EF.Functions.ILike(cv.Vendor.VendorInfo.SapVendorNumber, keyword) ||
                        EF.Functions.ILike(cv.Vendor.VendorInfo.EstablishmentName, keyword));

        var paginated =
            await PaginatedList<CaContractDraftVendor>
                .CreateAsync(
                    query,
                    req.PageNumber,
                    req.PageSize,
                    ct);

        var result =
            paginated.ToResult(
                p =>
                {
                    var vendor =
                        p.ContractInvitationVendors
                         .PurchaseOrderApprovalContract
                         .Entrepreneur?.SuVendor;

                    var result =
                        new GetContractDeliveryAcceptanceListItemResponse(
                            p.Id,
                            p.ContractNumber,
                            p.PoNumber,
                            p.ContractSignedDate,
                            vendor != null ? vendor.SapVendorNumber : string.Empty,
                            vendor != null ? vendor.EstablishmentName : string.Empty,
                            p.ContractName,
                            p.Budget,
                            p.ContractTypeCode,
                            p.ContractType?.Label);

                    return result;
                });

        return TypedResults.Ok(result);
    }
}