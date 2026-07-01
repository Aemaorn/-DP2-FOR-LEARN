namespace GHB.DP2.Application.Features.Announcement.AnnouncementReport;

using ClosedXML.Excel;
using GHB.DP2.Application.Constants;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

public class ImportAnnouncementReportRowRequest
{
    public long? OldId { get; init; }

    public int? Year { get; init; }

    public string? Discretion { get; init; }

    public string? AnnouncementReportTypeCode { get; init; }

    public string? DocumentUrl { get; init; }
}

public record ImportAnnouncementReportRowResult(
    int RowIndex,
    string? Discretion,
    string ErrorMessage);

public record ImportAnnouncementReportSkippedResult(
    int RowIndex,
    string? Discretion,
    int? Year,
    string? AnnouncementReportTypeCode);

public record ImportAnnouncementReportResponse(
    int TotalRows,
    int SuccessCount,
    int FailedCount,
    int SkippedCount,
    IReadOnlyList<ImportAnnouncementReportRowResult> FailedRows,
    IReadOnlyList<ImportAnnouncementReportSkippedResult> SkippedRows);

public class ImportAnnouncementReportEndpoint
    : EndpointBase<List<ImportAnnouncementReportRowRequest>, Ok<ImportAnnouncementReportResponse>>
{
    private readonly Dp2DbContext dbContext;

    public ImportAnnouncementReportEndpoint(
        ILogger<ImportAnnouncementReportEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("announcement-report/import");
        this.Description(b => b
            .WithTags("AnnouncementReport")
            .WithName("ImportAnnouncementReport")
            .AllowAnonymous()
            .Accepts<List<ImportAnnouncementReportRowRequest>>("application/json")
            .Produces<ImportAnnouncementReportResponse>(StatusCodes.Status200OK));
    }

    protected override async ValueTask<Ok<ImportAnnouncementReportResponse>> HandleRequestAsync(
        List<ImportAnnouncementReportRowRequest> req,
        CancellationToken ct)
    {
        var result = await AnnouncementReportImportHelper.ProcessAsync(req, this.dbContext, ct);
        return TypedResults.Ok(result);
    }
}

public class ImportAnnouncementReportFileRequest
{
    public IFormFile File { get; init; } = null!;
}

public class ImportAnnouncementReportFileEndpoint
    : EndpointBase<ImportAnnouncementReportFileRequest, Ok<ImportAnnouncementReportResponse>>
{
    private readonly Dp2DbContext dbContext;

    public ImportAnnouncementReportFileEndpoint(
        ILogger<ImportAnnouncementReportFileEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("announcement-report/import-file");
        this.AllowFileUploads();
        this.Description(b => b
            .WithTags("AnnouncementReport")
            .WithName("ImportAnnouncementReportFile")
            .AllowAnonymous()
            .Accepts<ImportAnnouncementReportFileRequest>("multipart/form-data")
            .Produces<ImportAnnouncementReportResponse>(StatusCodes.Status200OK));
    }

    protected override async ValueTask<Ok<ImportAnnouncementReportResponse>> HandleRequestAsync(
        ImportAnnouncementReportFileRequest req,
        CancellationToken ct)
    {
        var ext = Path.GetExtension(req.File.FileName).ToLowerInvariant();
        var rows = ext == ".csv"
            ? AnnouncementReportImportHelper.ParseCsv(req.File)
            : AnnouncementReportImportHelper.ParseExcel(req.File);

        var result = await AnnouncementReportImportHelper.ProcessAsync(rows, this.dbContext, ct);
        return TypedResults.Ok(result);
    }
}

internal static class AnnouncementReportImportHelper
{
    internal static async Task<ImportAnnouncementReportResponse> ProcessAsync(
        List<ImportAnnouncementReportRowRequest> rows,
        Dp2DbContext dbContext,
        CancellationToken ct)
    {
        var reportTypeMap = await dbContext.SuParameters
            .Where(p => p.GroupCode == GroupCode.From(ParameterGroupConstant.AnnReportType) && p.IsActive)
            .Select(p => new { p.Label, CodeValue = p.Code.Value })
            .ToDictionaryAsync(p => p.Label, p => p.CodeValue, StringComparer.OrdinalIgnoreCase, ct);

        var failedRows = new List<ImportAnnouncementReportRowResult>();
        var skippedRows = new List<ImportAnnouncementReportSkippedResult>();
        var entitiesToAdd = new List<Domain.AnnouncementInfo.AnnouncementReport>();
        var entitiesToUpdate = new List<Domain.AnnouncementInfo.AnnouncementReport>();

        var existingByOldId = await dbContext.AnnouncementReports
            .Where(a => a.OldId != null)
            .ToDictionaryAsync(a => a.OldId!.Value, ct);

        var existingYearTypeCodes = await dbContext.AnnouncementReports
            .Where(a => !a.IsDeleted && a.Year != null && a.AnnouncementReportTypeCode != null)
            .Select(a => new { a.Year, TypeCode = (string?)a.AnnouncementReportTypeCode!.Value })
            .ToListAsync(ct);

        var duplicateKeys = existingYearTypeCodes
            .Select(a => (a.Year!.Value, a.TypeCode!))
            .ToHashSet();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowIndex = i + 2;

            try
            {
                var oldIdAsInt = row.OldId is long l && l is >= int.MinValue and <= int.MaxValue ? (int?)l : null;

                reportTypeMap.TryGetValue(row.AnnouncementReportTypeCode ?? string.Empty, out var reportTypeCode);

                if (oldIdAsInt.HasValue && existingByOldId.TryGetValue(oldIdAsInt.Value, out var existing))
                {
                    existing.ImportUpdate(
                        year: row.Year,
                        discretion: row.Discretion,
                        reportTypeCode: reportTypeCode,
                        documentUrl: row.DocumentUrl);
                    existing.AddActivity(new ActivityInfo(
                        ActivityLogActionTypeConstant.Update,
                        "นำเข้าข้อมูลรายงานประกาศ (อัปเดต)",
                        existing.IsActive?.ToString() ?? string.Empty,
                        Remark: "นำเข้าข้อมูลรายงานประกาศ (อัปเดต)"));
                    entitiesToUpdate.Add(existing);
                }
                else
                {
                    if (row.Year.HasValue && reportTypeCode is not null
                        && duplicateKeys.Contains((row.Year.Value, reportTypeCode)))
                    {
                        skippedRows.Add(new ImportAnnouncementReportSkippedResult(
                            rowIndex,
                            row.Discretion,
                            row.Year,
                            reportTypeCode));
                        continue;
                    }

                    var entity = Domain.AnnouncementInfo.AnnouncementReport.Import(
                        oldId: oldIdAsInt,
                        year: row.Year,
                        discretion: row.Discretion,
                        reportTypeCode: reportTypeCode,
                        documentUrl: row.DocumentUrl);
                    entity.AddActivity(new ActivityInfo(
                        ActivityLogActionTypeConstant.Create,
                        "นำเข้าข้อมูลรายงานประกาศ",
                        entity.IsActive?.ToString() ?? string.Empty,
                        Remark: "นำเข้าข้อมูลรายงานประกาศ"));
                    entitiesToAdd.Add(entity);

                    if (row.Year.HasValue && reportTypeCode is not null)
                    {
                        duplicateKeys.Add((row.Year.Value, reportTypeCode));
                    }
                }
            }
            catch (Exception ex)
            {
                failedRows.Add(new ImportAnnouncementReportRowResult(rowIndex, row.Discretion, ex.Message));
            }
        }

        const int chunkSize = 500;
        foreach (var chunk in entitiesToAdd.Chunk(chunkSize))
        {
            dbContext.AnnouncementReports.AddRange(chunk);
            await dbContext.SaveChangesAsync(ct);
            dbContext.ChangeTracker.Clear();
        }

        if (entitiesToUpdate.Count > 0)
        {
            await dbContext.SaveChangesAsync(ct);
        }

        var successCount = entitiesToAdd.Count + entitiesToUpdate.Count;
        return new ImportAnnouncementReportResponse(
            TotalRows: rows.Count,
            SuccessCount: successCount,
            FailedCount: failedRows.Count,
            SkippedCount: skippedRows.Count,
            FailedRows: failedRows,
            SkippedRows: skippedRows);
    }

    internal static List<ImportAnnouncementReportRowRequest> ParseCsv(IFormFile file)
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
        var rows = new List<ImportAnnouncementReportRowRequest>();

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

    internal static List<ImportAnnouncementReportRowRequest> ParseExcel(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        var headers = worksheet.Row(1).Cells()
            .Where(c => !c.IsEmpty())
            .ToDictionary(c => c.Address.ColumnNumber, c => c.GetString().Trim());

        var rows = new List<ImportAnnouncementReportRowRequest>();
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

    private static ImportAnnouncementReportRowRequest MapCellsToRequest(Dictionary<string, string?> cells)
    {
        static string? Get(Dictionary<string, string?> d, string key) =>
            d.TryGetValue(key, out var v) ? v : null;

        return new ImportAnnouncementReportRowRequest
        {
            OldId = long.TryParse(Get(cells, "OldId"), out var oldId) ? oldId : null,
            Year = int.TryParse(Get(cells, "ปี"), out var year) ? year : null,
            Discretion = Get(cells, "รายละเอียด"),
            AnnouncementReportTypeCode = Get(cells, "ประเภทรายงาน"),
            DocumentUrl = Get(cells, "เอกสารแนบ"),
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
