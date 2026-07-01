namespace GHB.DP2.Application.Features.SystemUtility.SuVendor;

using GHB.DP2.Application.Features.Coi;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GHB.DP2.Infrastructure.Services.Coi;
using GHB.DP2.Infrastructure.Services.Watchlist;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class CheckHistorySuVendorItem
{
    public string? TaxpayerIdentificationNo { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public bool? IsDirector { get; init; }

    public bool? IsShareholder { get; init; }

    public bool IsJuristic { get; init; }
}

public class CheckHistorySuVendorRequest
{
    public Guid? VendorId { get; init; }

    public CheckType CheckType { get; init; }

    public List<CheckHistorySuVendorItem> Items { get; init; } = [];
}

public record CheckHistorySuVendorItemResponse(
    string? Name,
    string? TaxpayerIdentificationNo,
    bool Result,
    string Remark,
    string? CheckTime,
    string? CheckerEmployeeCode,
    string? EmployeeName,
    string? Position);

public class CheckHistorySuVendor : EndpointBase<CheckHistorySuVendorRequest, Results<Ok<List<CheckHistorySuVendorItemResponse>>, Ok<SearchCoiResult>>>
{
    private readonly IWatchlistService watchlistService;
    private readonly ICoiService coiService;
    private readonly Dp2DbContext dbContext;

    public CheckHistorySuVendor(
        ILogger<CheckHistorySuVendor> logger,
        IWatchlistService watchlistService,
        ICoiService coiService,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.watchlistService = watchlistService;
        this.coiService = coiService;
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuVendor"));
        this.Post("/st/st003/check-history/lookup");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Results<Ok<List<CheckHistorySuVendorItemResponse>>, Ok<SearchCoiResult>>> HandleRequestAsync(CheckHistorySuVendorRequest req, CancellationToken ct)
    {
        var checkerEmployeeCode = await GetCheckerEmployeeCodeAsync(ct);
        var thaiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
        var checkTime = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, thaiTimeZone).ToString("dd/MM/yyyy HH:mm");

        if (req.VendorId.HasValue)
        {
            var vendorId = SuVendorId.From(req.VendorId.Value);

            var vendorExists = await this.dbContext.SuVendors.AnyAsync(v => v.Id == vendorId, ct);

            if (vendorExists && req.Items.Any(item => !string.IsNullOrWhiteSpace(item.FirstName)))
            {
                await this.dbContext.SuVendorShareholdersList
                    .Where(x => x.VendorId == vendorId)
                    .ExecuteDeleteAsync(ct);

                var newShareholders = req.Items
                    .Select((item, i) => SuVendorShareholders.Create(
                        vendorId,
                        i + 1,
                        item.FirstName,
                        item.LastName,
                        item.IsDirector,
                        item.IsShareholder,
                        item.IsJuristic));

                await this.dbContext.SuVendorShareholdersList.AddRangeAsync(newShareholders, ct);
                await this.dbContext.SaveChangesAsync(ct);
            }
        }

        try
        {
            var tasks = req.Items.Select(item =>
                req.CheckType == CheckType.COI
                    ? GetCoiByNameAsync(item, ct)
                    : GetWatchlistByTaxIdAsync(item, ct));

            var results = await Task.WhenAll(tasks);

            return TypedResults.Ok(results.SelectMany(r => r).ToList());
        }
        catch (WatchlistException ex) when (ex.ApiMessage is not null)
        {
            // Business-level error from the Watchlist system (e.g. invalid name/id).
            // Surface the original message so the user knows what to fix.
            this.Logger.LogWarning(ex, "Watchlist returned a business error: {ApiMessage}", ex.ApiMessage);

            return TypedResults.Ok(
                new SearchCoiResult(
                    QualificationResult.UnKnow,
                    ex.ApiMessage));
        }
        catch (Exception)
        {
            // Technical failure (timeout/network/etc.) — let the client show its
            // generic "cannot connect, please try again" message.
            return TypedResults.Ok(
                new SearchCoiResult(
                    QualificationResult.UnKnow,
                    string.Empty));
        }

        async Task<string?> GetCheckerEmployeeCodeAsync(CancellationToken cancellationToken)
        {
            var userIdClaim = this.HttpContext.User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userIdGuid))
            {
                return null;
            }

            var user = await this.dbContext.SuUsers
                .FirstOrDefaultAsync(u => u.Id == UserId.From(userIdGuid), cancellationToken);

            return user == null ? null : user.EmployeeCode.ToString();
        }

        async Task<IEnumerable<CheckHistorySuVendorItemResponse>> GetCoiByNameAsync(CheckHistorySuVendorItem item, CancellationToken cancellationToken)
        {
            var resolvedName = $"{item.FirstName} {item.LastName}".Trim();
            var hasName = !string.IsNullOrWhiteSpace(resolvedName);
            var hasSsn = !string.IsNullOrWhiteSpace(item.TaxpayerIdentificationNo);

            CoiInfo[] coiInfos;

            if (hasName && hasSsn)
            {
                // กรอกครบทั้งคู่: ไล่ค้นหาแบบ fallback Name_SSN -> SSN -> Name
                coiInfos = (await this.coiService.GetCoiByNameSsnAsync(resolvedName!, item.TaxpayerIdentificationNo!, cancellationToken)).ToArray();

                if (coiInfos.Length == 0)
                {
                    coiInfos = (await this.coiService.GetCoiBySsnAsync(item.TaxpayerIdentificationNo!, cancellationToken)).ToArray();
                }

                if (coiInfos.Length == 0)
                {
                    coiInfos = (await this.coiService.GetCoiByNameAsync(resolvedName!, cancellationToken)).ToArray();
                }
            }
            else
            {
                // กรอกอย่างใดอย่างหนึ่ง: วิ่งเส้นปกติเส้นเดียว
                var coiTask = (hasName, hasSsn) switch
                {
                    (true, false) => this.coiService.GetCoiByNameAsync(resolvedName!, cancellationToken),
                    (false, true) => this.coiService.GetCoiBySsnAsync(item.TaxpayerIdentificationNo!, cancellationToken),
                    _ => Task.FromResult(Enumerable.Empty<CoiInfo>()),
                };

                coiInfos = (await coiTask).ToArray();
            }

            if (!coiInfos.Any())
            {
                return
                [
                    new CheckHistorySuVendorItemResponse(
                        Name: resolvedName,
                        TaxpayerIdentificationNo: item.TaxpayerIdentificationNo,
                        Result: true,
                        Remark: "ไม่พบความสัมพันธ์",
                        CheckTime: checkTime,
                        CheckerEmployeeCode: checkerEmployeeCode,
                        EmployeeName: null,
                        Position: null),
                ];
            }

            return coiInfos.Select(coiInfo => new CheckHistorySuVendorItemResponse(
                Name: resolvedName,
                TaxpayerIdentificationNo: item.TaxpayerIdentificationNo,
                Result: false,
                Remark: $"พบความสัมพันธ์: {coiInfo.RelationName}",
                CheckTime: checkTime,
                CheckerEmployeeCode: checkerEmployeeCode,
                EmployeeName: coiInfo.EmployeeName,
                Position: coiInfo.PositionName));
        }

        async Task<IEnumerable<CheckHistorySuVendorItemResponse>> GetWatchlistByTaxIdAsync(CheckHistorySuVendorItem item, CancellationToken cancellationToken)
        {
            var displayName = $"{item.FirstName} {item.LastName}".Trim();
            var firstName = item.FirstName ?? string.Empty;
            var lastName = item.LastName ?? string.Empty;
            var ssn = item.TaxpayerIdentificationNo ?? string.Empty;

            var hasName = !string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName);
            var hasSsn = !string.IsNullOrWhiteSpace(ssn);

            WatchlistInfo? watchlist;

            try
            {
                if (hasName && hasSsn)
                {
                    // กรอกครบทั้งคู่: ไล่ค้นหาแบบ fallback (ชื่อ+เลขบัตร) -> (เลขบัตรอย่างเดียว) -> (ชื่ออย่างเดียว)
                    watchlist = await SearchAsync(firstName, lastName, ssn);
                    watchlist ??= await SearchAsync(string.Empty, string.Empty, ssn);
                    watchlist ??= await SearchAsync(firstName, lastName, string.Empty);
                }
                else
                {
                    // กรอกอย่างใดอย่างหนึ่ง: วิ่งเส้นปกติ
                    watchlist = await SearchAsync(firstName, lastName, ssn);
                }
            }
            catch (WatchlistException ex)
            {
                // Error ระดับ record เดียวจากระบบ Watchlist (เช่น HTTP 400) — ไม่ทำให้ทั้ง batch ล่มเป็น UnKnow
                // record อื่น ๆ ยังคืนผลได้ตามปกติ
                this.Logger.LogWarning(ex, "Watchlist returned an error for a single record: {ApiMessage}", ex.ApiMessage);

                // มี message จากระบบ Watchlist (business error เช่น HTTP 400) → แสดง message นั้น
                // ไม่มี message (network/timeout) → แสดงข้อความเชื่อมต่อไม่สำเร็จ เพื่อไม่ให้สับสนกับ "ไม่พบข้อมูล" (ผ่าน)
                var remark = string.IsNullOrWhiteSpace(ex.ApiMessage)
                    ? "ไม่สามารถเชื่อมต่อระบบ Watchlist ได้ กรุณาลองใหม่อีกครั้ง"
                    : ex.ApiMessage!;

                return
                [
                    new CheckHistorySuVendorItemResponse(
                        Name: displayName,
                        TaxpayerIdentificationNo: item.TaxpayerIdentificationNo,
                        Result: false,
                        Remark: remark,
                        CheckTime: checkTime,
                        CheckerEmployeeCode: checkerEmployeeCode,
                        EmployeeName: null,
                        Position: null),
                ];
            }

            if (watchlist?.Details is null)
            {
                return
                [
                    new CheckHistorySuVendorItemResponse(
                        Name: displayName,
                        TaxpayerIdentificationNo: item.TaxpayerIdentificationNo,
                        Result: true,
                        Remark: "ไม่พบข้อมูล",
                        CheckTime: checkTime,
                        CheckerEmployeeCode: checkerEmployeeCode,
                        EmployeeName: null,
                        Position: null),
                ];
            }

            return watchlist.Details.Select(detail => new CheckHistorySuVendorItemResponse(
                Name: displayName,
                TaxpayerIdentificationNo: item.TaxpayerIdentificationNo,
                Result: false,
                Remark: detail.Reason,
                CheckTime: checkTime,
                CheckerEmployeeCode: checkerEmployeeCode,
                EmployeeName: null,
                Position: null));

            async Task<WatchlistInfo?> SearchAsync(string fn, string ln, string id)
            {
                var result = await this.watchlistService.SearchWatchlistAsync(
                    isJuristic: item.IsJuristic,
                    firstName: fn,
                    lastName: ln,
                    idNumber: id,
                    cancellationToken);

                // "เจอข้อมูล" = มีรายการที่มี Details; ถ้าไม่มีให้คืน null เพื่อให้ fallback ไปขั้นถัดไป
                return result.FirstOrDefault(w => w.Details is not null);
            }
        }
    }
}
