namespace GHB.DP2.Application.Features.Raw.Subdistrict;

using GHB.DP2.Domain.Raws;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetSubdistrictByIdRequest
{
    public string Id { get; init; }
}

public record GetSubdistrictByIdResponse(
    string Id,
    string Code,
    string NameTh,
    string? NameEn,
    string? ZipCode,
    string? DistrictCode);

public class GetSubdistrictById : EndpointBase<GetSubdistrictByIdRequest, Results<Ok<GetSubdistrictByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetSubdistrictById(Dp2DbContext dbContext, ILogger<GetSubdistrictById> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Subdistrict"));
        this.Get("/st/st013/{Id}");
    }

    protected override async ValueTask<Results<Ok<GetSubdistrictByIdResponse>, NotFound<string>>> HandleRequestAsync(GetSubdistrictByIdRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.RawSubDistricts
                             .AsNoTracking()
                             .SingleOrDefaultAsync(w => w.Id == SubDistrictId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลตำบล/แขวง");
        }

        return TypedResults.Ok(new GetSubdistrictByIdResponse(
            data.Id.Value,
            data.Code,
            data.NameTh,
            data.NameEn,
            data.ZipCode,
            data.DistrictCode));
    }
}
