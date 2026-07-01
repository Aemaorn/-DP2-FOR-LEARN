namespace GHB.DP2.Application.Features.SystemUtility.SuParameter;

using GHB.DP2.Application.Features.SystemUtility.SuParameter.DTO;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class RequestParameterById
{
    public Guid Id { get; init; }
}

public record ResponseParameterById(
    Guid Id,
    string Group,
    string? SubGroup,
    Guid? ParentId,
    string Code,
    string Name,
    int Sequence,
    IEnumerable<ParameterKeyValue> Values,
    bool IsActive
);

public class GetParameterById :
    SecureEndpointBase<RequestParameterById,
                       Results<Ok<ResponseParameterById>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetParameterById(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<GetParameterById> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuParameter"));
        this.Get("/st/st006/{Id:guid}");
    }

    protected override async ValueTask<Results<Ok<ResponseParameterById>, NotFound<string>>> HandleRequestAsync(RequestParameterById req, CancellationToken ct)
    {
        var data = await this.dbContext
                             .SuParameters
                             .Include(suParameter => suParameter.Group!)
                             .ThenInclude(suParameterGroup => suParameterGroup.Parent)
                             .AsNoTracking()
                             .SingleOrDefaultAsync(x => x.Id == ParameterId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound($"Parameter with Id {req.Id} not found");
        }

        var response = new ResponseParameterById(
            data.Id.Value,
            data.Group.Parent?.Code.Value ?? data.GroupCode.Value,
            data.Group.Parent != null ? data.GroupCode.Value : null,
            data.ParentId?.Value,
            data.Code.Value,
            data.Label,
            data.Sequence,
            data.Values
                .OrderBy(d => d.Value.Sequence)
                .Select(x => new ParameterKeyValue
                {
                    Key = x.Key,
                    Value = new ParameterKeyValue.ParameterValues(x.Value.Sequence, x.Value.Value),
                }),
            data.IsActive);

        return TypedResults.Ok(response);
    }
}
