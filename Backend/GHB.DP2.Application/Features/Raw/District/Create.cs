namespace GHB.DP2.Application.Features.Raw.District;

using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ConflictWithReason = Microsoft.AspNetCore.Http.HttpResults.Conflict<string>;

public class CreateDistrictRequest
{
    public string Code { get; init; }

    public string NameTh { get; init; }

    public string? NameEn { get; init; }

    public string ProvinceCode { get; init; }
}

public record CreateDistrictResponse(string Id);

public class CreateDistrict : EndpointBase<CreateDistrictRequest, Results<Created<CreateDistrictResponse>, ConflictWithReason, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CreateDistrict(Dp2DbContext dbContext, ILogger<CreateDistrict> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("District"));
        this.Post("/st/st012");
    }

    protected override async ValueTask<Results<Created<CreateDistrictResponse>, ConflictWithReason, NotFound<string>>> HandleRequestAsync(CreateDistrictRequest req, CancellationToken ct)
    {
        var provinceExist = await this.dbContext.RawProvinces
                                      .AnyAsync(w => w.Code == req.ProvinceCode, ct);

        if (!provinceExist)
        {
            return TypedResults.NotFound("ไม่พบจังหวัดที่เลือก");
        }

        var isCodeExist = await this.dbContext.RawDistricts
                                    .AnyAsync(w => w.Code == req.Code, ct);

        if (isCodeExist)
        {
            return TypedResults.Conflict("รหัสอำเภอ/เขตซ้ำ");
        }

        var maxSequence = await this.dbContext.RawDistricts
                                    .Select(s => (int?)s.Sequence)
                                    .MaxAsync(ct) ?? 0;

        var district = Domain.Raws.RawDistrict.Create(
            req.ProvinceCode,
            req.Code,
            req.NameTh,
            req.NameEn,
            maxSequence + 1);

        this.dbContext.RawDistricts.Add(district);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, new CreateDistrictResponse(district.Id.Value));
    }
}
