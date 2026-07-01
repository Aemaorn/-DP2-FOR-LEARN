namespace GHB.DP2.Application.Features.SystemUtility.SuVendorShareholders;

using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeleteSuVendorShareholdersRequest(Guid VendorId);

public class DeleteSuVendorShareholders : EndpointBase<DeleteSuVendorShareholdersRequest, Results<NoContent, NotFound>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteSuVendorShareholders(ILogger<DeleteSuVendorShareholders> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuVendorShareholder"));
        this.Delete("/st/vendor/{VendorId:guid}/shareholders");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Results<NoContent, NotFound>> HandleRequestAsync(DeleteSuVendorShareholdersRequest req, CancellationToken ct)
    {
        var vendorId = SuVendorId.From(req.VendorId);

        var vendorExists = await this.dbContext.SuVendors.AnyAsync(v => v.Id == vendorId, ct);
        if (!vendorExists)
        {
            return TypedResults.NotFound();
        }

        await this.dbContext.SuVendorShareholdersList
            .Where(x => x.VendorId == vendorId)
            .ExecuteDeleteAsync(ct);

        return TypedResults.NoContent();
    }
}
