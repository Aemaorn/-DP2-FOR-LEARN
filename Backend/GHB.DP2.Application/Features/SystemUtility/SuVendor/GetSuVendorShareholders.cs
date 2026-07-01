namespace GHB.DP2.Application.Features.SystemUtility.SuVendor;

using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetSuVendorShareholdersRequest(Guid VendorId);

public record GetSuVendorShareholdersItemResponse(
    string? FirstName,
    string? LastName,
    bool? IsJuristic,
    bool? IsDirector,
    bool? IsShareholder);

public class GetSuVendorShareholders : EndpointBase<GetSuVendorShareholdersRequest, Ok<IEnumerable<GetSuVendorShareholdersItemResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetSuVendorShareholders(
        Dp2DbContext dbContext,
        ILogger<GetSuVendorShareholders> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuVendorShareholder"));
        this.Get("/su/vendor/{VendorId:guid}/shareholders");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Ok<IEnumerable<GetSuVendorShareholdersItemResponse>>> HandleRequestAsync(GetSuVendorShareholdersRequest req, CancellationToken ct)
    {
        var vendorId = SuVendorId.From(req.VendorId);

        var shareholders = await this.dbContext.SuVendorShareholdersList
            .Where(x => x.VendorId == vendorId)
            .OrderBy(x => x.Sequence)
            .Select(x => new GetSuVendorShareholdersItemResponse(x.FirstName, x.LastName, x.IsJuristic, x.IsDirector, x.IsShareholder))
            .ToListAsync(ct);

        return TypedResults.Ok(shareholders.AsEnumerable());
    }
}
