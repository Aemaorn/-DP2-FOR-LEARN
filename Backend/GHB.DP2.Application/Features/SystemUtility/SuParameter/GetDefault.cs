namespace GHB.DP2.Application.Features.SystemUtility.SuParameter;

using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

public class GetDefaultParameterRequest
{
    public Guid Id { get; init; }

    public Guid? ParentId { get; init; }
}

public record GetDefaultParameterResponse(
    int NextSequence,
    string? NextCode);

public class GetDefaultParameter : EndpointBase<GetDefaultParameterRequest, Ok<GetDefaultParameterResponse>>
{
    private static readonly Regex CodePattern = new(@"^([A-Za-z]+)(\d{3})$", RegexOptions.Compiled);

    private static readonly Regex ChildRunningPattern = new(@"(\d{3})$", RegexOptions.Compiled);

    private readonly Dp2DbContext dbContext;

    public GetDefaultParameter(
        Dp2DbContext dbContext,
        ILogger<GetDefaultParameter> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(static x => x.WithTags("SuParameter"));
        this.Get("/st/st006/default/{Id:guid}");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Ok<GetDefaultParameterResponse>> HandleRequestAsync(GetDefaultParameterRequest req, CancellationToken ct)
    {
        if (req.ParentId.HasValue)
        {
            return await this.HandleChildAsync(req.ParentId.Value, ct);
        }

        var groupId = GroupId.From(req.Id);

        var query = this.dbContext.SuParameters
            .IgnoreQueryFilters()
            .Where(x => x.ParentId == null)
            .Where(x => x.Group.ParentId == groupId || (x.Group.ParentId == null && x.Group.Id == groupId));

        var maxSequence = await query.MaxAsync(x => (int?)x.Sequence, ct) ?? 0;

        var codes = await query
            .Select(x => x.Code.Value)
            .ToListAsync(ct);

        var parsed = codes
            .Select(c =>
            {
                var match = CodePattern.Match(c);

                if (!match.Success)
                {
                    return default((string Prefix, int Running)?);
                }

                return ((string Prefix, int Running)?)(match.Groups[1].Value, int.Parse(match.Groups[2].Value));
            })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToList();

        string? nextCode = null;

        if (parsed.Count > 0)
        {
            var max = parsed.MaxBy(x => x.Running);
            nextCode = $"{max.Prefix}{max.Running + 1:D3}";
        }

        return TypedResults.Ok(new GetDefaultParameterResponse(maxSequence + 1, nextCode));
    }

    private async Task<Ok<GetDefaultParameterResponse>> HandleChildAsync(Guid parentId, CancellationToken ct)
    {
        var parent = await this.dbContext.SuParameters
            .IgnoreQueryFilters()
            .Where(x => x.Id == ParameterId.From(parentId))
            .Select(x => new { x.Code })
            .FirstOrDefaultAsync(ct);

        if (parent is null)
        {
            return TypedResults.Ok(new GetDefaultParameterResponse(1, null));
        }

        var childrenQuery = this.dbContext.SuParameters
            .IgnoreQueryFilters()
            .Where(x => x.ParentId == ParameterId.From(parentId));

        var maxSequence = await childrenQuery.MaxAsync(x => (int?)x.Sequence, ct) ?? 0;

        var childCodes = await childrenQuery
            .Select(x => x.Code.Value)
            .ToListAsync(ct);

        var parentCode = parent.Code.Value;
        var maxRunning = childCodes
            .Where(c => c.StartsWith(parentCode))
            .Select(c => ChildRunningPattern.Match(c[parentCode.Length..]))
            .Where(m => m.Success)
            .Select(m => int.Parse(m.Groups[1].Value))
            .DefaultIfEmpty(0)
            .Max();

        var nextCode = $"{parentCode}{maxRunning + 1:D3}";

        return TypedResults.Ok(new GetDefaultParameterResponse(maxSequence + 1, nextCode));
    }
}
