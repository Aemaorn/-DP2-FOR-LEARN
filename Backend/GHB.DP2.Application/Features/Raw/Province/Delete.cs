namespace GHB.DP2.Application.Features.Raws.Province;

using GHB.DP2.Domain.Raws;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class DeleteProvinceRequest
{
    public string Id { get; init; }
}

public class DeleteProvince : EndpointBase<DeleteProvinceRequest, Results<NoContent, NotFound<string>, Conflict<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteProvince(Dp2DbContext dbContext, ILogger<DeleteProvince> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Province"));
        this.Delete("/st/st011/{Id}");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>, Conflict<string>>> HandleRequestAsync(DeleteProvinceRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.RawProvinces
                             .SingleOrDefaultAsync(w => w.Id == ProvinceId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลจังหวัด");
        }

        var anyDistrictUsed = await this.dbContext.RawDistricts
                                        .AnyAsync(w => w.ProvinceCode == data.Code, ct);

        if (anyDistrictUsed)
        {
            return TypedResults.Conflict("จังหวัดนี้มีอำเภอ/เขตผูกอยู่ ไม่สามารถลบได้");
        }

        this.dbContext.RawProvinces.Remove(data);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
