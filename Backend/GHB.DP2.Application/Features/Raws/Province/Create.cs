namespace GHB.DP2.Application.Features.Raws.Province;

using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ConflictWithReason = Microsoft.AspNetCore.Http.HttpResults.Conflict<string>;

public class CreateProvinceRequest
{
    public string Code { get; init; }

    public string NameTh { get; init; }

    public string? NameEn { get; init; }
}

public record CreateProvinceResponse(string Id);

public class CreateProvince : EndpointBase<CreateProvinceRequest, Results<Created<CreateProvinceResponse>, ConflictWithReason>>
{
    private readonly Dp2DbContext dbContext;

    public CreateProvince(Dp2DbContext dbContext, ILogger<CreateProvince> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Province"));
        this.Post("/st/st011");
    }

    protected override async ValueTask<Results<Created<CreateProvinceResponse>, ConflictWithReason>> HandleRequestAsync(CreateProvinceRequest req, CancellationToken ct)
    {
        var isCodeExist = await this.dbContext.RawProvinces
                                    .AnyAsync(w => w.Code == req.Code, ct);

        if (isCodeExist)
        {
            return TypedResults.Conflict("รหัสจังหวัดซ้ำ");
        }

        var maxSequence = await this.dbContext.RawProvinces
                                    .Select(s => (int?)s.Sequence)
                                    .MaxAsync(ct) ?? 0;

        var province = Domain.Raws.RawProvinces.Create(
            req.Code,
            req.NameTh,
            req.NameEn,
            maxSequence + 1);

        this.dbContext.RawProvinces.Add(province);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, new CreateProvinceResponse(province.Id.Value));
    }
}
