namespace GHB.DP2.Application.Features.SystemUtility.SuVendor;

using System.Text;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Services.Coi;
using GHB.DP2.Infrastructure.Services.Watchlist;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class CreateVendorCheckHistoryRequest
{
    public Guid VendorId { get; init; }

    public CheckType CheckType { get; init; }

    public bool Result { get; init; }

    public string? Remark { get; init; }
}

public record CreateVendorCheckHistoryResponse(
    Guid Id,
    bool Result,
    string Remark);

public class CreateVendorCheckHistory : SecureEndpointBase<CreateVendorCheckHistoryRequest, Results<Ok<CreateVendorCheckHistoryResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IWatchlistService watchlistService;
    private readonly ICoiService coiService;

    public CreateVendorCheckHistory(
        Dp2DbContext dbContext,
        ILogger<CreateVendorCheckHistory> logger,
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
        this.Post("/st/st003/{VendorId:guid}/check-history");
    }

    protected override async ValueTask<Results<Ok<CreateVendorCheckHistoryResponse>, NotFound<string>>> HandleRequestAsync(CreateVendorCheckHistoryRequest req, CancellationToken ct)
    {
        var vendor = await this.dbContext.SuVendors
                               .FirstOrDefaultAsync(x => x.Id == SuVendorId.From(req.VendorId), ct);

        if (vendor is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลคู่ค้า");
        }

        var checkAsync =
            req.CheckType == CheckType.COI
                ? GetCoiByNameAsync(vendor, ct)
                : GetWatchlistByTaxIdAsync(vendor, ct);

        var checkResult = await checkAsync;

        var history = SuVendorCheckHistory.Create(
            vendor,
            req.CheckType,
            checkResult.Result,
            checkResult.Remark);

        this.dbContext.SuVendorCheckHistories.Add(history);

        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        var response = new CreateVendorCheckHistoryResponse(history.Id.Value, checkResult.Result, checkResult.Remark);

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