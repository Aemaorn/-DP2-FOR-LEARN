namespace GHB.DP2.Application.Features.Raw.Province;

using GHB.DP2.Domain.Raws;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetProvinceByIdRequest
{
    public string Id { get; init; }
}

public record GetProvinceByIdResponse(
    string Id,
    string Code,
    string NameTh,
    string? NameEn);

public class GetProvinceById : EndpointBase<GetProvinceByIdRequest, Results<Ok<GetProvinceByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetProvinceById(Dp2DbContext dbContext, ILogger<GetProvinceById> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Province"));
        this.Get("/st/st011/{Id}");
    }

    protected override async ValueTask<Results<Ok<GetProvinceByIdResponse>, NotFound<string>>> HandleRequestAsync(GetProvinceByIdRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.RawProvinces
                             .AsNoTracking()
                             .SingleOrDefaultAsync(w => w.Id == ProvinceId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลจังหวัด");
        }

        return TypedResults.Ok(new GetProvinceByIdResponse(
            data.Id.Value,
            data.Code,
            data.NameTh,
            data.NameEn));
    }
}
