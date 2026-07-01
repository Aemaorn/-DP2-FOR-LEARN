namespace GHB.DP2.Application.Features.SystemUtility.SuVendor;

using System.Text;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Services.Coi;
using GHB.DP2.Infrastructure.Services.Watchlist;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetVendorCheckRequest
{
    public Guid VendorId { get; init; }

    public CheckType CheckType { get; init; }
}

public record GetVendorCheckResponse(
    QualificationResult Result,
    string Remark);

public class GetVendorCheck : SecureEndpointBase<GetVendorCheckRequest, Results<Ok<GetVendorCheckResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IWatchlistService watchlistService;
    private readonly ICoiService coiService;

    public GetVendorCheck(
        Dp2DbContext dbContext,
        ILogger<GetVendorCheck> logger,
        IWatchlistService watchlistService,
        ICoiService coiService,
        IPermissionValidationService permissionValidationService)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
        this.watchlistService = watchlistService;
        this.coiService = coiService;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuVendor"));
        this.Get("/st/st003/{VendorId:guid}/check-qualification");
    }

    protected override async ValueTask<Results<Ok<GetVendorCheckResponse>, NotFound<string>>>
        HandleRequestAsync(
            GetVendorCheckRequest req,
            CancellationToken ct)
    {
        var vendorExisting =
            await this.dbContext.SuVendors
                      .FirstOrDefaultAsync(vendor => vendor.Id == SuVendorId.From(req.VendorId), ct);

        if (vendorExisting is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลคู่ค้า");
        }

        var checkAsync =
            req.CheckType == CheckType.COI
                ? GetCoiByNameAsync(vendorExisting, ct)
                : GetWatchlistByTaxIdAsync(vendorExisting, ct);

        var checkResult = await checkAsync;

        var checkResultResponse =
            checkResult.Result
                ? QualificationResult.Pass
                : QualificationResult.Fail;

        var response =
            new GetVendorCheckResponse(
                checkResultResponse,
                checkResult.Remark);

        return TypedResults.Ok(response);

        async Task<(bool Result, string Remark)> GetCoiByNameAsync(SuVendor suVendor, CancellationToken cancellationToken)
        {
            var coiResult = await this.coiService.GetCoiByNameAsync(suVendor.EstablishmentName, cancellationToken);

            var coiInfos = coiResult.ToArray();

            var remarkSting = new StringBuilder();

            if (!coiInfos.Any())
            {
                remarkSting.Append("ไม่พบข้อมูล COI");

                return (true, remarkSting.ToString());
            }

            foreach (var coiInfo in coiInfos)
            {
                remarkSting.AppendLine($"พบข้อมูล COI: {coiInfo.EmployeeName}, ตำแหน่ง: {coiInfo.PositionName}, แผนก: {coiInfo.DivisionName}, มี ความสัมพันธ์: {coiInfo.RelationName}");
            }

            return (false, remarkSting.ToString());
        }

        async Task<(bool Result, string Remark)> GetWatchlistByTaxIdAsync(SuVendor suVendor, CancellationToken cancellationToken)
        {
            var isJuristic = suVendor.Type == SuVendorType.JuristicPerson;

            var watchlistResult =
                await this.watchlistService.SearchWatchlistAsync(
                    isJuristic,
                    string.Empty,
                    string.Empty,
                    suVendor.TaxpayerIdentificationNo,
                    cancellationToken);

            var watchlistInfos = watchlistResult.ToArray();
            var watchlist = watchlistInfos.FirstOrDefault();
            var remarkSting = new StringBuilder();

            if (watchlist?.Details is null)
            {
                remarkSting.Append("ไม่พบข้อมูล Watchlist");

                return (true, remarkSting.ToString());
            }

            foreach (var watchlistInfo in watchlist.Details ?? [])
            {
                remarkSting.AppendLine($"{watchlistInfo.Reason}, ");
            }

            return (false, remarkSting.ToString());
        }
    }
}