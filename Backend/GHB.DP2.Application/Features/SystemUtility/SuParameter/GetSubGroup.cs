namespace GHB.DP2.Application.Features.SystemUtility.SuParameter;

using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetSubGroupRequest
{
    public Guid Id { get; init; }
}

public record GetSubGroupResponse(
    GroupId Id,
    string Code,
    string Label
);

public class GetSubGroups :
    EndpointBase<GetSubGroupRequest,
                 Results<Ok<IAsyncEnumerable<GetSubGroupResponse>>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetSubGroups(
        Dp2DbContext dbContext,
        ILogger<GetSubGroups> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(static x => x.WithTags("SuParameter"));
        this.Get("/st/st006/subgroup/{Id:guid}");
        this.AllowAnonymous();
    }

    protected override ValueTask<Results<Ok<IAsyncEnumerable<GetSubGroupResponse>>, NotFound<string>>> HandleRequestAsync(GetSubGroupRequest req, CancellationToken ct)
    {
        var childrenQuery = this.dbContext.SuParameterGroups
            .AsNoTracking()
            .Where(g => g.ParentId == GroupId.From(req.Id))
            .Select(g => new GetSubGroupResponse(
                g.Id,
                g.Code.Value,
                g.Label))
            .AsAsyncEnumerable();

        return ValueTask.FromResult<Results<Ok<IAsyncEnumerable<GetSubGroupResponse>>, NotFound<string>>>(
            TypedResults.Ok(childrenQuery));
    }
}
