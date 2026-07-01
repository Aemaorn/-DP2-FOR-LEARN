namespace GHB.DP2.Application.Features.SystemUtility.SuVendorShareholders;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record GetShareHolderRequest(Guid VendorId);

public class GetGetShareHolderByVendorId : EndpointBase<GetShareHolderRequest, Results<Ok<IEnumerable<SuVendorShareholdersDto>>, NotFound<string>>>
{
    private readonly ISuVendorShareholdersService suVendorShareholdersService;

    public GetGetShareHolderByVendorId(
        ISuVendorShareholdersService suVendorShareholdersService,
        ILogger<GetGetShareHolderByVendorId> logger)
        : base(logger)
    {
        this.suVendorShareholdersService = suVendorShareholdersService;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuVendorShareholder"));
        this.Get("/st/vendor/{VendorId:guid}/shareholder");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Results<Ok<IEnumerable<SuVendorShareholdersDto>>, NotFound<string>>> HandleRequestAsync(GetShareHolderRequest req, CancellationToken ct)
    {
        var response = await this.suVendorShareholdersService.GetDefaultShareholdersByVendorId(req.VendorId, ct);

        return TypedResults.Ok(response);
    }
}