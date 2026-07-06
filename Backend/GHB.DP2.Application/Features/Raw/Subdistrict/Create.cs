namespace GHB.DP2.Application.Features.Raws.Subdistrict;

using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ConflictWithReason = Microsoft.AspNetCore.Http.HttpResults.Conflict<string>;

public class CreateSubdistrictRequest
{
    public string Code { get; init; }

    public string NameTh { get; init; }

    public string? NameEn { get; init; }

    public string? ZipCode { get; init; }

    public string DistrictCode { get; init; }
}

public record CreateSubdistrictResponse(string Id);

public class CreateSubdistrict : EndpointBase<CreateSubdistrictRequest, Results<Created<CreateSubdistrictResponse>, ConflictWithReason, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CreateSubdistrict(Dp2DbContext dbContext, ILogger<CreateSubdistrict> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Subdistrict"));
        this.Post("/st/st013");
    }

    protected override async ValueTask<Results<Created<CreateSubdistrictResponse>, ConflictWithReason, NotFound<string>>> HandleRequestAsync(CreateSubdistrictRequest req, CancellationToken ct)
    {
        var districtExist = await this.dbContext.RawDistricts
                                      .AnyAsync(w => w.Code == req.DistrictCode, ct);

        if (!districtExist)
        {
            return TypedResults.NotFound("ไม่พบอำเภอ/เขตที่เลือก");
        }

        var isCodeExist = await this.dbContext.RawSubDistricts
                                    .AnyAsync(w => w.Code == req.Code, ct);

        if (isCodeExist)
        {
            return TypedResults.Conflict("รหัสตำบล/แขวงซ้ำ");
        }

        var maxSequence = await this.dbContext.RawSubDistricts
                                    .Select(s => (int?)s.Sequence)
                                    .MaxAsync(ct) ?? 0;

        var subdistrict = Domain.Raws.RawSubDistrict.Create(
            req.DistrictCode,
            req.Code,
            req.NameTh,
            req.NameEn,
            req.ZipCode,
            maxSequence + 1);

        this.dbContext.RawSubDistricts.Add(subdistrict);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, new CreateSubdistrictResponse(subdistrict.Id.Value));
    }
}
