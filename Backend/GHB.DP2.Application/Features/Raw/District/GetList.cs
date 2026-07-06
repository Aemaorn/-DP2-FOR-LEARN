namespace GHB.DP2.Application.Features.Raw.District;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetListDistrictRequest
{
    public string? Keyword { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public record GetListDistrictResponse(
    string Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? ProvinceCode);

public class GetListDistrict :
    EndpointBase<GetListDistrictRequest,
                Ok<PaginatedQueryResult<GetListDistrictResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetListDistrict(Dp2DbContext dbContext, ILogger<GetListDistrict> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("District"));
        this.Get("/st/st012");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetListDistrictResponse>>> HandleRequestAsync(GetListDistrictRequest req, CancellationToken ct)
    {
        var query = this.dbContext.RawDistricts
                        .Where(w => w.IsActive)
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.Keyword),
                            x => EF.Functions.ILike(x.Code, $"%{req.Keyword}%") ||
                                 EF.Functions.ILike(x.NameTh, $"%{req.Keyword}%") ||
                                 (x.NameEn != null && EF.Functions.ILike(x.NameEn, $"%{req.Keyword}%")))
                        .AsNoTracking()
                        .OrderBy(o => o.Sequence);

        var paginated =
            await PaginatedList<Domain.Raws.RawDistrict>
                .CreateAsync(
                    query,
                    req.PageNumber,
                    req.PageSize,
                    ct);

        var res = paginated.ToResult(static x =>
            new GetListDistrictResponse(
                x.Id.Value,
                x.Code,
                x.NameTh,
                x.NameEn,
                x.ProvinceCode));

        return TypedResults.Ok(res);
    }
}
