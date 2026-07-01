namespace GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetListEntrepreneurRequest(
    int PageNumber,
    int PageSize,
    Guid ProcurementId,
    Guid PrincipleApprovalRentalId,
    string? Keyword,
    string? Type);

public record WinnerResponse(
    Guid Id,
    string Type,
    string TaxId,
    string Name,
    decimal AgreedPrice,
    string? Email);

public class GetListEntrepreneurEndpoint : EndpointBase<GetListEntrepreneurRequest, Results<Ok<PaginatedQueryResult<WinnerResponse>>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetListEntrepreneurEndpoint(Dp2DbContext dbContext, ILogger<GetListEntrepreneurEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/PrincipleApprovalRental")
             .WithName("GetListEntrepreneur")
             .Produces<Ok>());
        this.Get("procurement/{procurementId:guid}/principle-approval-rental/{principleApprovalRentalId:guid}/entrepreneurs");
    }

    protected override async ValueTask<Results<Ok<PaginatedQueryResult<WinnerResponse>>, NotFound<string>>> HandleRequestAsync(GetListEntrepreneurRequest req, CancellationToken ct)
    {
        var entrepreneurs = this.dbContext.PPrincipleApprovalRentalEntrepreneurs
                                .Include(x => x.Vendor)
                                .Where(x => x.PPrincipleApprovalRental.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                                            x.PPrincipleApprovalRental.Id == PPrincipleApprovalRentalId.From(req.PrincipleApprovalRentalId))
                                .WhereIfTrue(!string.IsNullOrEmpty(req.Keyword), x =>
                                    EF.Functions.ILike(x.Vendor.EstablishmentName, $"%{req.Keyword}%") ||
                                    EF.Functions.ILike(x.Vendor.TaxpayerIdentificationNo, $"%{req.Keyword}%"))
                                .WhereIfTrue(!string.IsNullOrEmpty(req.Type), x => x.Vendor.Type.ToString() == req.Type);

        var paginated = await PaginatedList<PPrincipleApprovalRentalEntrepreneurs>
            .CreateAsync(entrepreneurs.AsNoTracking(), req.PageNumber, req.PageSize, ct);

        var data = paginated.ToResult(p => new WinnerResponse(
            p.Id.Value,
            p.Vendor.Type.ToString(),
            p.Vendor.TaxpayerIdentificationNo,
            p.Vendor.EstablishmentName,
            p.EntrepreneursPriceDetails.Sum(x => x.AgreedPrice * x.ParcelQuantity),
            p.Vendor.Email));

        return TypedResults.Ok(data);
    }
}