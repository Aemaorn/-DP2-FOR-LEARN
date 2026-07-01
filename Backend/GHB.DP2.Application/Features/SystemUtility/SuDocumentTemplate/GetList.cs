namespace GHB.DP2.Application.Features.SystemUtility.SuDocumentTemplate;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Services;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetListSuDocumentTemplateRequest
{
    public string? Group { get; init; }

    public string? Name { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public record GetListSuDocumentTemplateResponse(
    Guid Id,
    string Group,
    string Code,
    string Name,
    bool IsActive);

public class GetListSuDocumentTemplate : SecureEndpointBase<GetListSuDocumentTemplateRequest, Ok<PaginatedQueryResult<GetListSuDocumentTemplateResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetListSuDocumentTemplate(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<GetListSuDocumentTemplate> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuDocumentTemplate"));
        this.Get("/st/st007");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetListSuDocumentTemplateResponse>>> HandleRequestAsync(GetListSuDocumentTemplateRequest req, CancellationToken ct)
    {
        var listData = this.dbContext.SuDocumentTemplates
                           .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Group), x => x.Group == req.Group)
                           .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Name), x =>
                               EF.Functions.ILike(x.Name, $"%{req.Name}%") ||
                               EF.Functions.ILike(x.Code, $"%{req.Name}%"));

        var paginated =
            await PaginatedList<GHB.DP2.Domain.SystemUtility.SuDocumentTemplate>
                .CreateAsync(
                    listData,
                    req.PageNumber,
                    req.PageSize,
                    ct);

        var result = paginated.ToResult(static x => new GetListSuDocumentTemplateResponse(
            x.Id.Value,
            x.Group,
            x.Code,
            x.Name,
            x.IsActive));

        return TypedResults.Ok(result);
    }
}