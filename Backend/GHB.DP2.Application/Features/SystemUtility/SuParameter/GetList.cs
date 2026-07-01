namespace GHB.DP2.Application.Features.SystemUtility.SuParameter;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetParameterListRequest
{
    public string? Group { get; init; }

    public string? SubGroup { get; init; }

    public Guid? ParentId { get; init; }

    public string? Parameter { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public record GetParameterListResponse(
    Guid Id,
    string Group,
    string? SubGroup,
    Guid? ParentId,
    string Parameter,
    int Sequence);

public class GetParameterList :
    SecureEndpointBase<GetParameterListRequest,
                       Ok<PaginatedQueryResult<GetParameterListResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetParameterList(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<GetParameterList> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuParameter"));
        this.Get("/st/st006");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetParameterListResponse>>> HandleRequestAsync(GetParameterListRequest req, CancellationToken ct)
    {
        var query = this.dbContext.SuParameters
                        .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Group), x => x.GroupCode == GroupCode.From(req.Group!))
                        .WhereIfTrue(!string.IsNullOrWhiteSpace(req.SubGroup), x => x.Group.Code == GroupCode.From(req.SubGroup!))
                        .WhereIfTrue(req.ParentId.HasValue, x => x.ParentId == ParameterId.From(req.ParentId!.Value))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.Parameter),
                            x =>
                                x.Code == ParameterCode.From(req.Parameter!) || EF.Functions.ILike(x.Label, $"%{req.Parameter}%"))
                        .OrderByDescending(o => o.AuditInfo.CreatedAt);

        var paginated = await PaginatedList<GHB.DP2.Domain.SystemUtility.SuParameter>.CreateAsync(query, req.PageNumber, req.PageSize, ct);

        var result = paginated.ToResult(static x => new GetParameterListResponse(
            x.Id.Value,
            x.Group.Parent?.Label ?? x.Group.Label,
            x.Group.Parent != null ? x.Group.Label : null,
            x.ParentId?.Value,
            x.Label,
            x.Sequence));

        return TypedResults.Ok(result);
    }
}