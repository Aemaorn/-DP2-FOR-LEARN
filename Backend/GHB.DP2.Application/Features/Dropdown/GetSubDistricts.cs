namespace GHB.DP2.Application.Features.Dropdown;

using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetSubDistrictsRequest(
    string? DistrictCode);

public record GetSubDistrictsResponse(
    string Value,
    string Label);

public class GetSubDistricts : EndpointBase<GetSubDistrictsRequest, Ok<IAsyncEnumerable<GetSubDistrictsResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetSubDistricts(Dp2DbContext dbContext, ILogger<GetRoleCode> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Dropdown"));
        this.Get("/dropdown/subDistricts");
    }

    protected override ValueTask<Ok<IAsyncEnumerable<GetSubDistrictsResponse>>> HandleRequestAsync(GetSubDistrictsRequest req, CancellationToken ct)
    {
        var query = this.dbContext.RawSubDistricts
                        .Where(r => r.DistrictCode == req.DistrictCode && r.IsActive)
                        .Select(r => new GetSubDistrictsResponse(
                            r.Code,
                            r.NameTh))
                        .AsNoTracking()
                        .AsAsyncEnumerable();

        return ValueTask.FromResult(TypedResults.Ok(query));
    }
}
