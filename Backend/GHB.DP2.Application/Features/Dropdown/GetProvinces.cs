namespace GHB.DP2.Application.Features.Dropdown;

using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetProvincesResponse(
    string Value,
    string Label);

public class GetProvinces : EndpointBase<Ok<IAsyncEnumerable<GetProvincesResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetProvinces(Dp2DbContext dbContext, ILogger<GetRoleCode> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Dropdown"));
        this.Get("/dropdown/provinces");
    }

    protected override ValueTask<Ok<IAsyncEnumerable<GetProvincesResponse>>> HandleRequestAsync(CancellationToken ct)
    {
        var query = this.dbContext.RawProvinces
                        .Where(role => role.IsActive)
                        .Select(role => new GetProvincesResponse(
                            role.Code,
                            role.NameTh))
                        .AsNoTracking()
                        .AsAsyncEnumerable();

        return ValueTask.FromResult(TypedResults.Ok(query));
    }
}
