namespace GHB.DP2.Application.Features.Announcement.AnnouncementInfo;

using ClosedXML.Excel;
using GHB.DP2.Application.Constants;
using GHB.DP2.Domain.AnnouncementInfo;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using System.Text;

public class ImportAnnouncementInfoRowRequest
{
    public long? OldId { get; init; }

    public string? AnnouncementName { get; init; }

    public string? PostContent { get; init; }

    /// <summary>publish / draft</summary>
    public string? Status { get; init; }

    public string? AnnouncementDate { get; init; }

    public string? LastModifiedAt { get; init; }

    public string? CreatedBy { get; init; }

    /// <summary>Thai label → mapped to category code via CategoryMap</summary>
    public string? AnnouncementCategoryCode { get; init; }

    /// <summary>"80" → SMethod004, "60" → SMethod002</summary>
    public string? SupplyMethodCode { get; init; }

    public decimal? BudgetAmount { get; init; }

    public string? AnnouncementTitle { get; init; }

    public string? Email { get; init; }

    public string? Description { get; init; }

    public string? EndDate { get; init; }

    public string? DocumentUrl { get; init; }

    public decimal? ReferencePrice { get; init; }

    /// <summary>เลขเดือน 1–12 (รวมกับ BudgetYear เพื่อสร้างวันที่)</summary>
    public int? ExpectedDate { get; init; }

    public int? BudgetYear { get; init; }

    public string? StartDate { get; init; }
}

public record ImportAnnouncementInfoRowResult(
    int RowIndex,
    string? AnnouncementName,
    string ErrorMessage);

public record ImportAnnouncementInfoResponse(
    int TotalRows,
    int SuccessCount,
    int FailedCount,
    IReadOnlyList<ImportAnnouncementInfoRowResult> FailedRows);

public class ImportAnnouncementInfoEndpoint
    : EndpointBase<List<ImportAnnouncementInfoRowRequest>, Ok<ImportAnnouncementInfoResponse>>
{
    private readonly Dp2DbContext dbContext;

    public ImportAnnouncementInfoEndpoint(
        ILogger<ImportAnnouncementInfoEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("announcement-info/import");
        this.Description(b => b
            .WithTags("AnnouncementInfo")
            .WithName("ImportAnnouncementInfo")
            .AllowAnonymous()
            .Accepts<List<ImportAnnouncementInfoRowRequest>>("application/json")
            .Produces<ImportAnnouncementInfoResponse>(StatusCodes.Status200OK));
    }

    protected override async ValueTask<Ok<ImportAnnouncementInfoResponse>> HandleRequestAsync(
        List<ImportAnnouncementInfoRowRequest> req,
        CancellationToken ct)
    {
        var result = await AnnouncementInfoImportHelper.ProcessAsync(req, this.dbContext, ct);
        return TypedResults.Ok(result);
    }
}

public class ImportAnnouncementInfoFileRequest
{
    public IFormFile File { get; init; } = null!;
}

public class ImportAnnouncementInfoFileEndpoint
    : EndpointBase<ImportAnnouncementInfoFileRequest, Ok<ImportAnnouncementInfoResponse>>
{
    private readonly Dp2DbContext dbContext;

    public ImportAnnouncementInfoFileEndpoint(
        ILogger<ImportAnnouncementInfoFileEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("announcement-info/import-file");
        this.AllowFileUploads();
        this.Description(b => b
            .WithTags("AnnouncementInfo")
            .WithName("ImportAnnouncementInfoFile")
            .AllowAnonymous()
            .Accepts<ImportAnnouncementInfoFileRequest>("multipart/form-data")
            .Produces<ImportAnnouncementInfoResponse>(StatusCodes.Status200OK));
    }

    protected override async ValueTask<Ok<ImportAnnouncementInfoResponse>> HandleRequestAsync(
        ImportAnnouncementInfoFileRequest req,
        CancellationToken ct)
    {
        var ext = Path.GetExtension(req.File.FileName).ToLowerInvariant();
        var rows = ext == ".csv"
            ? AnnouncementInfoImportHelper.ParseCsv(req.File)
            : AnnouncementInfoImportHelper.ParseExcel(req.File);

        var result = await AnnouncementInfoImportHelper.ProcessAsync(rows, this.dbContext, ct);
        return TypedResults.Ok(result);
    }
}

internal static class AnnouncementInfoImportHelper
{
    internal static async Task<ImportAnnouncementInfoResponse> ProcessAsync(
        List<ImportAnnouncementInfoRowRequest> rows,
        Dp2DbContext dbContext,
        CancellationToken ct)
    {
        var categoryMap = await dbContext.SuParameters
            .Where(p => p.GroupCode == GroupCode.From(ParameterGroupConstant.AnnCategory) && p.IsActive)
            .Select(p => new { p.Label, CodeValue = p.Code.Value })
            .ToDictionaryAsync(p => p.Label, p => p.CodeValue, StringComparer.OrdinalIgnoreCase, ct);

        var supplyMethodMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["80"] = SupplyMethodConstant.Eighty,
            ["60"] = SupplyMethodConstant.Sixty,
        };

        var failedRows = new List<ImportAnnouncementInfoRowResult>();
        var entitiesToAdd = new List<Domain.AnnouncementInfo.AnnouncementInfo>();
        var entitiesToUpdate = new List<Domain.AnnouncementInfo.AnnouncementInfo>();

        var existingByOldId = await dbContext.AnnouncementInfos
            .Where(a => a.OldId != null)
            .ToDictionaryAsync(a => a.OldId!.Value, ct);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowIndex = i + 2;

            try
            {
                var oldIdAsInt = row.OldId is long l && l is >= int.MinValue and <= int.MaxValue ? (int?)l : null;

                if (oldIdAsInt.HasValue && existingByOldId.TryGetValue(oldIdAsInt.Value, out var existing))
                {
                    UpdateEntity(existing, row, categoryMap, supplyMethodMap);
                    existing.AddActivity(new ActivityInfo(
                        ActivityLogActionTypeConstant.Update,
                        "นำเข้าข้อมูลประกาศ (อัปเดต)",
                        nameof(AnnouncementInfoStatus.Publish),
                        Remark: "นำเข้าข้อมูลประกาศ (อัปเดต)"));
                    entitiesToUpdate.Add(existing);
                }
                else
                {
                    var entity = MapToEntity(row, categoryMap, supplyMethodMap);
                    entity.AddActivity(new ActivityInfo(
                        ActivityLogActionTypeConstant.Create,
                        "นำเข้าข้อมูลประกาศ",
                        nameof(AnnouncementInfoStatus.Publish),
                        Remark: "นำเข้าข้อมูลประกาศ"));
                    entitiesToAdd.Add(entity);
                }
            }
            catch (Exception ex)
            {
                failedRows.Add(new ImportAnnouncementInfoRowResult(rowIndex, row.AnnouncementName, ex.Message));
            }
        }

        const int chunkSize = 500;
        foreach (var chunk in entitiesToAdd.Chunk(chunkSize))
        {
            dbContext.AnnouncementInfos.AddRange(chunk);
            await dbContext.SaveChangesAsync(ct);
            dbContext.ChangeTracker.Clear();
        }

        if (entitiesToUpdate.Count > 0)
        {
            await dbContext.SaveChangesAsync(ct);
        }

        var successCount = entitiesToAdd.Count + entitiesToUpdate.Count;
        return new ImportAnnouncementInfoResponse(
            TotalRows: rows.Count,
            SuccessCount: successCount,
            FailedCount: failedRows.Count,
            FailedRows: failedRows);
    }

    internal static List<ImportAnnouncementInfoRowRequest> ParseCsv(IFormFile file)
    {
        using var reader = new StreamReader(
            file.OpenReadStream(),
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);

        var headerLine = reader.ReadLine();
        if (headerLine == null)
        {
            return [];
        }

        var headers = SplitCsvLine(headerLine).Select(h => h.Trim()).ToList();
        var rows = new List<ImportAnnouncementInfoRowRequest>();

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = SplitCsvLine(line);
            var cells = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                var val = i < values.Count ? values[i].Trim() : null;
                cells[headers[i]] = string.IsNullOrEmpty(val) ? null : val;
            }

            rows.Add(MapCellsToRequest(cells));
        }

        return rows;
    }

    internal static List<ImportAnnouncementInfoRowRequest> ParseExcel(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        var headers = worksheet.Row(1).Cells()
            .Where(c => !c.IsEmpty())
            .ToDictionary(c => c.Address.ColumnNumber, c => c.GetString().Trim());

        var rows = new List<ImportAnnouncementInfoRowRequest>();
        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            var cells = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (colNum, header) in headers)
            {
                var cell = row.Cell(colNum);
                var value = cell.IsEmpty() ? null : cell.GetString().Trim();
                cells[header] = string.IsNullOrEmpty(value) ? null : value;
            }

            rows.Add(MapCellsToRequest(cells));
        }

        return rows;
    }

    internal static Domain.AnnouncementInfo.AnnouncementInfo MapToEntity(
        ImportAnnouncementInfoRowRequest row,
        Dictionary<string, string> categoryMap,
        Dictionary<string, string> supplyMethodMap)
    {
        var status = string.Equals(row.Status, "publish", StringComparison.OrdinalIgnoreCase)
            ? AnnouncementInfoStatus.Publish
            : AnnouncementInfoStatus.Publish;

        supplyMethodMap.TryGetValue(row.SupplyMethodCode ?? string.Empty, out var supplyMethodCode);
        categoryMap.TryGetValue(row.AnnouncementCategoryCode ?? string.Empty, out var categoryCode);

        return Domain.AnnouncementInfo.AnnouncementInfo.Import(
            oldId: row.OldId is long l && l is >= int.MinValue and <= int.MaxValue ? (int?)l : null,
            announcementTitle: row.AnnouncementTitle,
            announcementName: row.AnnouncementName,
            announcementDate: ParseDate(row.AnnouncementDate),
            budgetAmount: row.BudgetAmount,
            announcementCategoryCode: categoryCode,
            status: status,
            supplyMethodCode: supplyMethodCode,
            email: row.Email,
            documentUrl: row.DocumentUrl,
            budgetYear: row.BudgetYear,
            remark: row.PostContent,
            description: row.Description,
            expectedDate: BuildExpectedDate(row.ExpectedDate, row.BudgetYear),
            referencePrice: row.ReferencePrice,
            startDate: ParseDate(row.StartDate),
            endDate: ParseDate(row.EndDate));
    }

    internal static void UpdateEntity(
        Domain.AnnouncementInfo.AnnouncementInfo entity,
        ImportAnnouncementInfoRowRequest row,
        Dictionary<string, string> categoryMap,
        Dictionary<string, string> supplyMethodMap)
    {
        var status = string.Equals(row.Status, "publish", StringComparison.OrdinalIgnoreCase)
            ? AnnouncementInfoStatus.Publish
            : AnnouncementInfoStatus.Publish;

        supplyMethodMap.TryGetValue(row.SupplyMethodCode ?? string.Empty, out var supplyMethodCode);
        categoryMap.TryGetValue(row.AnnouncementCategoryCode ?? string.Empty, out var categoryCode);

        entity.ImportUpdate(
            announcementTitle: row.AnnouncementTitle,
            announcementName: row.AnnouncementName,
            announcementDate: ParseDate(row.AnnouncementDate),
            budgetAmount: row.BudgetAmount,
            announcementCategoryCode: categoryCode,
            status: status,
            supplyMethodCode: supplyMethodCode,
            email: row.Email,
            documentUrl: row.DocumentUrl,
            budgetYear: row.BudgetYear,
            remark: row.PostContent,
            description: row.Description,
            expectedDate: BuildExpectedDate(row.ExpectedDate, row.BudgetYear),
            referencePrice: row.ReferencePrice,
            startDate: ParseDate(row.StartDate),
            endDate: ParseDate(row.EndDate));
    }

    internal static DateTimeOffset? BuildExpectedDate(int? month, int? buddhistYear)
    {
        if (month is null || buddhistYear is null)
        {
            return null;
        }

        var ceYear = buddhistYear.Value - 543;
        if (month.Value < 1 || month.Value > 12 || ceYear < 1)
        {
            return null;
        }

        return new DateTimeOffset(ceYear, month.Value, 1, 0, 0, 0, TimeSpan.FromHours(7));
    }

    internal static DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(value, out var result) ? result : null;
    }

    private static ImportAnnouncementInfoRowRequest MapCellsToRequest(Dictionary<string, string?> cells)
    {
        static string? Get(Dictionary<string, string?> d, string key) =>
            d.TryGetValue(key, out var v) ? v : null;

        return new ImportAnnouncementInfoRowRequest
        {
            OldId = long.TryParse(Get(cells, "OldId"), out var oldId) ? oldId : null,
            AnnouncementName = Get(cells, "ประกาศ"),
            PostContent = Get(cells, "post_content"),
            Status = Get(cells, "สถานะ"),
            AnnouncementDate = Get(cells, "วันที่เผยแพร่"),
            LastModifiedAt = Get(cells, "วันที่แก้ไข"),
            CreatedBy = Get(cells, "ชื่อคนสร้าง"),
            AnnouncementCategoryCode = Get(cells, "ประเภทประกาศ"),
            SupplyMethodCode = Get(cells, "อ้างอิง"),
            BudgetAmount = decimal.TryParse(Get(cells, "วงเงินงบประมาณ"), out var budget) ? budget : null,
            AnnouncementTitle = Get(cells, "หัวข้อประกาศ"),
            Email = Get(cells, "อีเมล"),
            Description = Get(cells, "รายละเอียด"),
            DocumentUrl = Get(cells, "ไฟล์แนบ"),
            ReferencePrice = decimal.TryParse(Get(cells, "ราคากลางอ้างอิง"), out var refPrice) ? refPrice : null,
            ExpectedDate = int.TryParse(Get(cells, "คาดว่าจะประกาศ(เดือน/ปี)"), out var expDate) ? expDate : null,
            BudgetYear = int.TryParse(Get(cells, "ปีงบประมาณ"), out var year) ? year : null,
            StartDate = Get(cells, "วันที่เริ่มต้นประชาพิจารณ์"),
        };
    }

    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                fields.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(ch);
            }
        }

        fields.Add(sb.ToString());
        return fields;
    }
}
