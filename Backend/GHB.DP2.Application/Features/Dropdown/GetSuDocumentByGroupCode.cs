namespace GHB.DP2.Application.Features.Dropdown;

using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetSuDocumentByGroupCode
{
    public IEnumerable<string> GroupCode { get; init; }

    public string? SupplyMethodCode { get; init; }

    public decimal? Budget { get; init; }

    public bool? IsJorPorComment { get; init; }

    public bool? IsCancel { get; init; }

    public bool? IsChange { get; init; }
}

public record GetSuDocumentByGroupCodeResponse(
    Guid Id,
    string Value,
    string Label);

public class GetSuDocumentByGroupCodeEndpoint : EndpointBase<GetSuDocumentByGroupCode, Ok<List<GetSuDocumentByGroupCodeResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetSuDocumentByGroupCodeEndpoint(Dp2DbContext dbContext, ILogger<GetSuDocumentByGroupCodeEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Dropdown"));
        this.Get("/dropdown/document-templates");
    }

    protected override async ValueTask<Ok<List<GetSuDocumentByGroupCodeResponse>>> HandleRequestAsync(
        GetSuDocumentByGroupCode req,
        CancellationToken ct)
    {
        var query = this.dbContext.SuDocumentTemplates
                        .Where(w =>
                            req.GroupCode.Contains(w.Group) &&
                            w.IsActive)
                        .WhereIfTrue(
                            !req.IsJorPorComment.HasValue,
                            x => x.AdditionalInfo == null)
                        .WhereIfTrue(
                            req.IsJorPorComment.HasValue,
                            x =>
                                EF.Functions.JsonExists(x.AdditionalInfo!, nameof(SuDocumentTemplate.IsJorPorComment)) &&
                                x.AdditionalInfo!.RootElement
                                 .GetProperty(nameof(SuDocumentTemplate.IsJorPorComment))
                                 .GetBoolean() == req.IsJorPorComment)
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.SupplyMethodCode),
                            w => w.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                        .WhereIfTrue(
                            req.IsChange.HasValue,
                            x => x.IsChange == req.IsChange)
                        .WhereIfTrue(
                            !req.IsChange.HasValue,
                            x => x.IsChange == false || x.IsChange == null)
                        .WhereIfTrue(
                            req.IsCancel.HasValue,
                            x => x.IsCancel == req.IsCancel)
                        .WhereIfTrue(
                            !req.IsCancel.HasValue,
                            x => x.IsCancel == false || x.IsCancel == null)
                        .WhereIfTrue(
                            req.Budget is not null,
                            w => w.BudgetForDocument.Min <= req.Budget &&
                                 (w.BudgetForDocument.Max >= req.Budget || w.BudgetForDocument.Max == null));

        var result = await query
                           .OrderBy(s => s.Group)
                           .ThenBy(o => o.Code)
                           .Select(s => new GetSuDocumentByGroupCodeResponse(
                               s.Id.Value,
                               s.Code,
                               s.Name))
                                .AsNoTracking()
                                .ToListAsync(ct);

        return TypedResults.Ok(result);
    }
}