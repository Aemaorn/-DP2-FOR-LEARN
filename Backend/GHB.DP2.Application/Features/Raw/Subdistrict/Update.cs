namespace GHB.DP2.Application.Features.Raw.Subdistrict;

using GHB.DP2.Domain.Raws;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UpdateSubdistrictRequest
{
    public string Id { get; init; }

    public string NameTh { get; init; }

    public string? NameEn { get; init; }

    public string? ZipCode { get; init; }
}

public class UpdateSubdistrict : EndpointBase<UpdateSubdistrictRequest, Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateSubdistrict(Dp2DbContext dbContext, ILogger<UpdateSubdistrict> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Subdistrict"));
        this.Put("/st/st013/{Id}");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(UpdateSubdistrictRequest req, CancellationToken ct)
    {
        // Province/district are intentionally not accepted here — locked once a subdistrict is created.
        var data = await this.dbContext.RawSubDistricts
                             .SingleOrDefaultAsync(w => w.Id == SubDistrictId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลตำบล/แขวง");
        }

        data.Update(req.NameTh, req.NameEn, req.ZipCode);

        this.dbContext.RawSubDistricts.Update(data);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
