namespace GHB.DP2.Application.Features.SystemUtility.SuParameter;

using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetParentGroupsResponse(
    GroupId Id,
    string Group,
    string Label);

public class GetParentGroups : EndpointBase<Ok<IAsyncEnumerable<GetParentGroupsResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetParentGroups(
        Dp2DbContext dbContext,
        ILogger<GetParentGroups> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(static x => x.WithTags("SuParameter"));
        this.Get("/st/st006/group");
        this.AllowAnonymous();
    }

    protected override ValueTask<Ok<IAsyncEnumerable<GetParentGroupsResponse>>> HandleRequestAsync(CancellationToken ct)
    {
        var query = this.dbContext.SuParameterGroups
            .AsNoTracking()
            .Where(g => g.ParentId == null)
            .Select(g => new GetParentGroupsResponse(
                g.Id,
                g.Code.Value,
                g.Label))
            .AsAsyncEnumerable();

        return ValueTask.FromResult(TypedResults.Ok(query));
    }
}
