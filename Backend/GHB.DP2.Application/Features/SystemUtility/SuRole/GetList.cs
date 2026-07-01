namespace GHB.DP2.Application.Features.SystemUtility.SuRole;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.Services;

public class GetListRoleRequest
{
    public string? Keyword { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public record GetListRoleResponse(
    string Code,
    string Name,
    bool IsActive);

public class GetListRole :
    SecureEndpointBase<GetListRoleRequest,
                       Ok<PaginatedQueryResult<GetListRoleResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetListRole(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionService,
        ILogger<GetListRole> logger)
        : base(permissionService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuRole"));
        this.Get("/st/st004");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetListRoleResponse>>> HandleRequestAsync(GetListRoleRequest req, CancellationToken ct)
    {
        var roleQuery = this.dbContext.SuRoles
                            .WhereIfTrue(
                                !string.IsNullOrWhiteSpace(req.Keyword),
                                x => EF.Functions.ILike((string)x.Code, $"%{req.Keyword}%") || EF.Functions.ILike(x.Name, $"%{req.Keyword}%"))
                            .AsNoTracking()
                            .OrderByDescending(o => o.AuditInfo.CreatedAt);

        var paginated =
            await PaginatedList<Domain.SystemUtility.SuRole>
                .CreateAsync(
                    roleQuery,
                    req.PageNumber,
                    req.PageSize,
                    ct);

        var res = paginated.ToResult(static x =>
            new GetListRoleResponse(
                x.Code.Value,
                x.Name,
                x.IsActive));

        return TypedResults.Ok(res);
    }
}