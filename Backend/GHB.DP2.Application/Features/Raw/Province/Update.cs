namespace GHB.DP2.Application.Features.Raw.Province;

using GHB.DP2.Domain.Raws;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UpdateProvinceRequest
{
    public string Id { get; init; }

    public string NameTh { get; init; }

    public string? NameEn { get; init; }
}

public class UpdateProvince : EndpointBase<UpdateProvinceRequest, Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateProvince(Dp2DbContext dbContext, ILogger<UpdateProvince> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Province"));
        this.Put("/st/st011/{Id}");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(UpdateProvinceRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.RawProvinces
                             .SingleOrDefaultAsync(w => w.Id == ProvinceId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลจังหวัด");
        }

        data.Update(req.NameTh, req.NameEn);

        this.dbContext.RawProvinces.Update(data);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
