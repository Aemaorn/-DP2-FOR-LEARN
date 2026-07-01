namespace GHB.DP2.Application.Features.Raw;

using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetPositionsByBusinessUnitResponse(
    string BusinessUnitId,
    string PositionId,
    string Label);

public class GetPositionsByBusinessUnit(
    Dp2DbContext dbContext,
    ILogger<GetPositionsByBusinessUnit> logger)
    : EndpointBase<EmptyRequest, Ok<List<GetPositionsByBusinessUnitResponse>>>(logger)
{
    public override void Configure()
    {
        this.Options(x => x.WithTags("Raw"));
        this.Get("/positions-by-business-unit");
    }

    protected override async ValueTask<Ok<List<GetPositionsByBusinessUnitResponse>>> HandleRequestAsync(
        EmptyRequest req,
        CancellationToken ct)
    {
        var raw = await dbContext.RawEmployeePositions
                                 .Where(r => r.Acting == EmployeeConstant.Acting.Primary)
                                 .Select(r => new
                                 {
                                     r.BusinessUnitId,
                                     r.PositionId,
                                     PositionName = r.Position.Name,
                                 })
                                 .Distinct()
                                 .OrderBy(r => r.PositionName)
                                 .AsNoTracking()
                                 .ToListAsync(ct);

        var result = raw.Select(r => new GetPositionsByBusinessUnitResponse(
                             r.BusinessUnitId.Value,
                             r.PositionId.Value,
                             r.PositionName))
                        .ToList();

        return TypedResults.Ok(result);
    }
}
