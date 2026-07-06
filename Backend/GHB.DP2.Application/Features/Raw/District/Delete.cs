namespace GHB.DP2.Application.Features.Raws.District;

using GHB.DP2.Domain.Raws;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class DeleteDistrictRequest
{
    public string Id { get; init; }
}

public class DeleteDistrict : EndpointBase<DeleteDistrictRequest, Results<NoContent, NotFound<string>, Conflict<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteDistrict(Dp2DbContext dbContext, ILogger<DeleteDistrict> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("District"));
        this.Delete("/st/st012/{Id}");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>, Conflict<string>>> HandleRequestAsync(DeleteDistrictRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.RawDistricts
                             .SingleOrDefaultAsync(w => w.Id == DistrictId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลอำเภอ/เขต");
        }

        var anySubDistrictUsed = await this.dbContext.RawSubDistricts
                                           .AnyAsync(w => w.DistrictCode == data.Code, ct);

        if (anySubDistrictUsed)
        {
            return TypedResults.Conflict("อำเภอ/เขตนี้มีตำบล/แขวงผูกอยู่ ไม่สามารถลบได้");
        }

        this.dbContext.RawDistricts.Remove(data);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
