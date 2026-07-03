namespace GHB.DP2.Application.Features.Raws.District;

using GHB.DP2.Domain.Raws;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetDistrictByIdRequest
{
    public string Id { get; init; }
}

public record GetDistrictByIdResponse(
    string Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? ProvinceCode);

public class GetDistrictById : EndpointBase<GetDistrictByIdRequest, Results<Ok<GetDistrictByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetDistrictById(Dp2DbContext dbContext, ILogger<GetDistrictById> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("District"));
        this.Get("/st/st012/{Id}");
    }

    protected override async ValueTask<Results<Ok<GetDistrictByIdResponse>, NotFound<string>>> HandleRequestAsync(GetDistrictByIdRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.RawDistricts
                             .AsNoTracking()
                             .SingleOrDefaultAsync(w => w.Id == DistrictId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลอำเภอ/เขต");
        }

        return TypedResults.Ok(new GetDistrictByIdResponse(
            data.Id.Value,
            data.Code,
            data.NameTh,
            data.NameEn,
            data.ProvinceCode));
    }
}
