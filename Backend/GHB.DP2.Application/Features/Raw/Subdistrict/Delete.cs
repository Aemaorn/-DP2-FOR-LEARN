namespace GHB.DP2.Application.Features.Raw.Subdistrict;

using GHB.DP2.Domain.Raws;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class DeleteSubdistrictRequest
{
    public string Id { get; init; }
}

public class DeleteSubdistrict : EndpointBase<DeleteSubdistrictRequest, Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteSubdistrict(Dp2DbContext dbContext, ILogger<DeleteSubdistrict> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Subdistrict"));
        this.Delete("/st/st013/{Id}");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(DeleteSubdistrictRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.RawSubDistricts
                             .SingleOrDefaultAsync(w => w.Id == SubDistrictId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลตำบล/แขวง");
        }

        this.dbContext.RawSubDistricts.Remove(data);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
