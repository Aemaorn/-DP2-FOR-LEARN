namespace GHB.DP2.Application.Features.Raw;

using GHB.DP2.Application.Services;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetRawBusinessPositionRequest
{
    public string EmployeeCode { get; init; }
}

public record GetRawBusinessPositionResponse(
    PositionId PositionId,
    BusinessUnitId BusinessUnitId,
    string Label);

public class GetRawBusinessPosition(
    Dp2DbContext dbContext,
    IPermissionValidationService permissionValidationService,
    ILogger<GetRawBusinessPosition> logger)
    : SecureEndpointBase<GetRawBusinessPositionRequest,
                Ok<List<GetRawBusinessPositionResponse>>>(
                    permissionValidationService, logger)
{
    public override void Configure()
    {
        this.Options(x => x.WithTags("Raw"));
        this.Get("/business-unit-position/{EmployeeCode}");
    }

    protected override async ValueTask<Ok<List<GetRawBusinessPositionResponse>>> HandleRequestAsync(
        GetRawBusinessPositionRequest req,
        CancellationToken ct)
    {
        var query = await dbContext.RawEmployeePositions
                                   .Include(r => r.Position)
                                   .Include(r => r.BusinessUnit)
                                   .Where(r => r.EmployeeCode == EmployeeCode.From(req.EmployeeCode))
                                   .OrderBy(r =>
                                       r.Acting == EmployeeConstant.Acting.Primary ? 0 :
                                       r.Acting == EmployeeConstant.Acting.ActingPosition ? 1 :
                                       r.Acting == EmployeeConstant.Acting.Temporary ? 2 : 3)
                                   .Select(r => new GetRawBusinessPositionResponse(
                                       r.PositionId,
                                       r.BusinessUnitId,
                                       $"{r.Position.Name} {r.BusinessUnit.Name}"))
                                   .AsNoTracking()
                                   .ToListAsync(ct);

        return TypedResults.Ok(query);
    }
}