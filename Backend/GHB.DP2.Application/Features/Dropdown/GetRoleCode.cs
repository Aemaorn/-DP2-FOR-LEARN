namespace GHB.DP2.Application.Features.Dropdown;

using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetRoleCodeResponse(
    string Value,
    string Label);

public class GetRoleCode : EndpointBase<Ok<IAsyncEnumerable<GetRoleCodeResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetRoleCode(Dp2DbContext dbContext, ILogger<GetRoleCode> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Dropdown"));
        this.Get("/dropdown/rolecode");
    }

    protected override ValueTask<Ok<IAsyncEnumerable<GetRoleCodeResponse>>> HandleRequestAsync(CancellationToken ct)
    {
        var query = this.dbContext.SuRoles
                        .Where(role => role.IsActive)
                        .Select(role => new GetRoleCodeResponse(
                            role.Code.Value,
                            role.Name))
                        .AsNoTracking()
                        .AsAsyncEnumerable();

        return ValueTask.FromResult(TypedResults.Ok(query));
    }
}