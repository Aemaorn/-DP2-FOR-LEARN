namespace GHB.DP2.Application.Features.Procurement.Jp006;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetListWinnerRequest(
    int PageNumber,
    int PageSize,
    Guid ProcurementId,
    Guid Jp006Id,
    string? Keyword,
    string? Type);

public record WinnerResponse(
    Guid Id,
    string Type,
    string TaxId,
    string Name,
    decimal AgreedPrice,
    string? Email);

public class GetListWinnerEndpoint : EndpointBase<GetListWinnerRequest, Results<Ok<PaginatedQueryResult<WinnerResponse>>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetListWinnerEndpoint(Dp2DbContext dbContext, ILogger<GetListWinnerEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(Jp006))
             .WithName("GetListWinner")
             .Produces<Ok>());
        this.Get("procurement/{ProcurementId:guid}/jp006/{Jp006Id:guid}/GetListWinner");
    }

    protected override async ValueTask<Results<Ok<PaginatedQueryResult<WinnerResponse>>, NotFound<string>>> HandleRequestAsync(GetListWinnerRequest req, CancellationToken ct)
    {
        var entrepreneurs = this.dbContext.PJp006Entrepreneurs
            .Where(x => x.PPurchaseOrder.ProcurementId == ProcurementId.From(req.ProcurementId) && x.PPurchaseOrder.Id == PurchaseOrderId.From(req.Jp006Id) && x.IsWinner)
            .WhereIfTrue(!string.IsNullOrEmpty(req.Keyword), x =>
                EF.Functions.ILike(x.SuVendor.EstablishmentName, $"%{req.Keyword}%") ||
                EF.Functions.ILike(x.SuVendor.TaxpayerIdentificationNo, $"%{req.Keyword}%"))
            .WhereIfTrue(!string.IsNullOrEmpty(req.Type), x => x.SuVendor.Type.ToString() == req.Type);

        var paginated = await PaginatedList<PPurchaseOrderEntrepreneur>
            .CreateAsync(entrepreneurs.AsNoTracking(), req.PageNumber, req.PageSize, ct);

        var data = paginated.ToResult(p => new WinnerResponse(
            p.Id.Value,
            p.SuVendor.Type.ToString(),
            p.SuVendor.TaxpayerIdentificationNo,
            p.SuVendor.EstablishmentName,
            p.PJp006PriceDetails.Sum(s => s.AgreedPrice * s.ParcelQuantity),
            p.SuVendor.Email));

        return TypedResults.Ok(data);
    }
}