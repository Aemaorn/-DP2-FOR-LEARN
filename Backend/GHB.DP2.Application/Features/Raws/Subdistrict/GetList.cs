namespace GHB.DP2.Application.Features.Raws.Subdistrict;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetListSubdistrictRequest
{
    public string? Keyword { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public record GetListSubdistrictResponse(
    string Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? ZipCode,
    string? DistrictCode);

public class GetListSubdistrict :
    EndpointBase<GetListSubdistrictRequest,
                Ok<PaginatedQueryResult<GetListSubdistrictResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetListSubdistrict(Dp2DbContext dbContext, ILogger<GetListSubdistrict> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Subdistrict"));
        this.Get("/st/st013");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetListSubdistrictResponse>>> HandleRequestAsync(GetListSubdistrictRequest req, CancellationToken ct)
    {
        var query = this.dbContext.RawSubDistricts
                        .Where(w => w.IsActive)
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.Keyword),
                            x => EF.Functions.ILike(x.Code, $"%{req.Keyword}%") ||
                                 EF.Functions.ILike(x.NameTh, $"%{req.Keyword}%") ||
                                 (x.NameEn != null && EF.Functions.ILike(x.NameEn, $"%{req.Keyword}%")))
                        .AsNoTracking()
                        .OrderBy(o => o.Sequence);

        var paginated =
            await PaginatedList<Domain.Raws.RawSubDistrict>
                .CreateAsync(
                    query,
                    req.PageNumber,
                    req.PageSize,
                    ct);

        var res = paginated.ToResult(static x =>
            new GetListSubdistrictResponse(
                x.Id.Value,
                x.Code,
                x.NameTh,
                x.NameEn,
                x.ZipCode,
                x.DistrictCode));

        return TypedResults.Ok(res);
    }
}
