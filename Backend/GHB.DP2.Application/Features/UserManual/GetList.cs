namespace GHB.DP2.Application.Features.UserManual;

using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetUserManualListResponse(
    Guid Id,
    string Code,
    string Name);

public class GetUserManualList : EndpointBase<Ok<List<GetUserManualListResponse>>>
{
    private const string UserManualGroup = "UserManual";

    private readonly Dp2DbContext dbContext;

    public GetUserManualList(
        Dp2DbContext dbContext,
        ILogger<GetUserManualList> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("UserManual"));
        this.Get("/user-manuals");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Ok<List<GetUserManualListResponse>>> HandleRequestAsync(CancellationToken ct)
    {
        var data = await this.dbContext.SuDocumentTemplates
                             .AsNoTracking()
                             .Where(x => x.Group == UserManualGroup && x.IsActive)
                             .OrderBy(x => x.Name)
                             .Select(x => new GetUserManualListResponse(
                                 x.Id.Value,
                                 x.Code,
                                 x.Name))
                             .ToListAsync(ct);

        return TypedResults.Ok(data);
    }
}
