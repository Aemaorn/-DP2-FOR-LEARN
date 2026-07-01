namespace GHB.DP2.Application.Features.Dropdown;

using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetDistrictsRequest(
    string? ProvinceCode);

public record GetDistrictsResponse(
    string Value,
    string Label);

public class GetDistricts : EndpointBase<GetDistrictsRequest, Ok<IAsyncEnumerable<GetDistrictsResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetDistricts(Dp2DbContext dbContext, ILogger<GetRoleCode> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Dropdown"));
        this.Get("/dropdown/districts");
    }

    protected override ValueTask<Ok<IAsyncEnumerable<GetDistrictsResponse>>> HandleRequestAsync(GetDistrictsRequest req, CancellationToken ct)
    {
        var query = this.dbContext.RawDistricts
                        .Where(r => r.ProvinceCode == req.ProvinceCode && r.IsActive)
                        .Select(r => new GetDistrictsResponse(
                            r.Code,
                            r.NameTh))
                        .AsNoTracking()
                        .AsAsyncEnumerable();

        return ValueTask.FromResult(TypedResults.Ok(query));
    }
}
