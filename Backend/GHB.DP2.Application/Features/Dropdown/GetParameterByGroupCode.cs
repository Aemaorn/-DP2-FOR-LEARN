namespace GHB.DP2.Application.Features.Dropdown;

using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetParameterByGroupCodeRequest
{
    public string GroupCode { get; init; }

    public string? OptionGroupCode { get; init; }

    public string? ParentCode { get; init; }

    public Guid? ParentId { get; init; }

    public bool IncludeChildren { get; init; }
}

public record GetParameterByGroupCodeResponse(
    string Value,
    string Label,
    Guid? Id = null,
    IReadOnlyList<GetParameterByGroupCodeResponse>? Children = null,
    IReadOnlyList<string>? ValueKeys = null);

public class GetParameterByGroupCode : EndpointBase<GetParameterByGroupCodeRequest, Ok<List<GetParameterByGroupCodeResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetParameterByGroupCode(Dp2DbContext dbContext, ILogger<GetParameterByGroupCode> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Dropdown"));
        this.Get("/dropdown/parameter");
    }

    protected override async ValueTask<Ok<List<GetParameterByGroupCodeResponse>>> HandleRequestAsync(
        GetParameterByGroupCodeRequest req,
        CancellationToken ct)
    {
        var query = this.dbContext.SuParameters
                        .Where(w => (
                                        (string)w.GroupCode == req.GroupCode ||
                                        (req.OptionGroupCode != null && (string)w.GroupCode == req.OptionGroupCode)
                                    ) && w.IsActive)
                        .WhereIfTrue(
                            !req.ParentCode.IsNullOrEmpty(),
                            w => (string)w.Code == req.ParentCode)
                        .WhereIfTrue(
                            req.ParentCode.IsNullOrEmpty() && !req.ParentId.HasValue,
                            w => w.ParentId == null)
                        .WhereIfTrue(
                            req.ParentId.HasValue,
                            w => (Guid?)w.ParentId == req.ParentId);

        if (!req.ParentCode.IsNullOrEmpty())
        {
            query = query.SelectMany(p => p.Children);
        }

        if (req.IncludeChildren)
        {
            var parents = await query.Include(p => p.Children)
                                     .OrderBy(s => s.Sequence)
                                     .AsNoTracking()
                                     .ToListAsync(ct);

            var nested = parents.Select(p =>
            {
                var children = p.Children
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Sequence)
                    .Select(c => new GetParameterByGroupCodeResponse(
                        c.Code.Value,
                        c.Label,
                        c.Id.Value,
                        null,
                        ExtractValueKeys(c.Values)))
                    .ToList();

                return new GetParameterByGroupCodeResponse(
                    p.Code.Value,
                    p.Label,
                    p.Id.Value,
                    children.Count > 0 ? children : null,
                    ExtractValueKeys(p.Values));
            }).ToList();

            return TypedResults.Ok(nested);
        }

        var entities = await query.OrderBy(s => s.Sequence)
                                  .AsNoTracking()
                                  .ToListAsync(ct);

        var result = entities.Select(s => new GetParameterByGroupCodeResponse(
                                 s.Code.Value,
                                 s.Label,
                                 s.Id.Value,
                                 null,
                                 ExtractValueKeys(s.Values)))
                             .ToList();

        return TypedResults.Ok(result);
    }

    private static IReadOnlyList<string>? ExtractValueKeys(Dictionary<string, ParameterValue>? values)
    {
        if (values is null || values.Count == 0)
        {
            return null;
        }

        return values.OrderBy(v => v.Value.Sequence).Select(v => v.Key).ToList();
    }
}