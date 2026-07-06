namespace GHB.DP2.Application.Features.Raws.Province;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetListProvinceRequest
{
    public string? Keyword { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public record GetListProvinceResponse(
    string Id,
    string Code,
    string NameTh,
    string? NameEn);

public class GetListProvince :
    EndpointBase<GetListProvinceRequest,
                Ok<PaginatedQueryResult<GetListProvinceResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetListProvince(Dp2DbContext dbContext, ILogger<GetListProvince> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Province"));
        this.Get("/st/st011");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetListProvinceResponse>>> HandleRequestAsync(GetListProvinceRequest req, CancellationToken ct)
    {
        var query = this.dbContext.RawProvinces
                        .Where(w => w.IsActive)
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.Keyword),
                            x => EF.Functions.ILike(x.Code, $"%{req.Keyword}%") ||
                                 EF.Functions.ILike(x.NameTh, $"%{req.Keyword}%") ||
                                 (x.NameEn != null && EF.Functions.ILike(x.NameEn, $"%{req.Keyword}%")))
                        .AsNoTracking()
                        .OrderBy(o => o.Sequence);

        var paginated =
            await PaginatedList<Domain.Raws.RawProvinces>
                .CreateAsync(
                    query,
                    req.PageNumber,
                    req.PageSize,
                    ct);

        var res = paginated.ToResult(static x =>
            new GetListProvinceResponse(
                x.Id.Value,
                x.Code,
                x.NameTh,
                x.NameEn));

        return TypedResults.Ok(res);
    }
}
