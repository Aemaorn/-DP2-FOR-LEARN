namespace GHB.DP2.Application.Features.Raws.District;

using GHB.DP2.Domain.Raws;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UpdateDistrictRequest
{
    public string Id { get; init; }

    public string NameTh { get; init; }

    public string? NameEn { get; init; }
}

public class UpdateDistrict : EndpointBase<UpdateDistrictRequest, Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateDistrict(Dp2DbContext dbContext, ILogger<UpdateDistrict> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("District"));
        this.Put("/st/st012/{Id}");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(UpdateDistrictRequest req, CancellationToken ct)
    {
        // Province is intentionally not accepted here — it is locked once a district is created.
        var data = await this.dbContext.RawDistricts
                             .SingleOrDefaultAsync(w => w.Id == DistrictId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลอำเภอ/เขต");
        }

        data.Update(req.NameTh, req.NameEn);

        this.dbContext.RawDistricts.Update(data);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
