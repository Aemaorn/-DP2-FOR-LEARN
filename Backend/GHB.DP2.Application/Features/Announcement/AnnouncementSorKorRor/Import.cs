namespace GHB.DP2.Application.Features.Announcement.AnnouncementSorKorRor;

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

public class ImportAnnouncementSorKorRorRowRequest
{
    public long? OldId { get; init; }

    public int? Year { get; init; }

    public int? Month { get; init; }

    public decimal? Amount { get; init; }

    public string? DepartmentTypeCode { get; init; }

    public string? DocumentUrl { get; init; }
}

public record ImportAnnouncementSorKorRorRowResult(
    int RowIndex,
    string ErrorMessage);

public record ImportAnnouncementSorKorRorSkippedResult(
    int RowIndex,
    int? Year,
    int? Month,
    string? DepartmentTypeCode);

public record ImportAnnouncementSorKorRorResponse(
    int TotalRows,
    int SuccessCount,
    int FailedCount,
    int SkippedCount,
    IReadOnlyList<ImportAnnouncementSorKorRorRowResult> FailedRows,
    IReadOnlyList<ImportAnnouncementSorKorRorSkippedResult> SkippedRows);

public class ImportAnnouncementSorKorRorEndpoint
    : EndpointBase<List<ImportAnnouncementSorKorRorRowRequest>, Ok<ImportAnnouncementSorKorRorResponse>>
{
    private readonly Dp2DbContext dbContext;

    public ImportAnnouncementSorKorRorEndpoint(
        ILogger<ImportAnnouncementSorKorRorEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("announcement-sor-kor-ror/import");
        this.Description(b => b
            .WithTags("AnnouncementSorKorRor")
            .WithName("ImportAnnouncementSorKorRor")
            .AllowAnonymous()
            .Accepts<List<ImportAnnouncementSorKorRorRowRequest>>("application/json")
            .Produces<ImportAnnouncementSorKorRorResponse>(StatusCodes.Status200OK));
    }

    protected override async ValueTask<Ok<ImportAnnouncementSorKorRorResponse>> HandleRequestAsync(
        List<ImportAnnouncementSorKorRorRowRequest> req,
        CancellationToken ct)
    {
        var result = await AnnouncementSorKorRorImportHelper.ProcessAsync(req, this.dbContext, ct);
        return TypedResults.Ok(result);
    }
}

public class ImportAnnouncementSorKorRorFileRequest
{
    public IFormFile File { get; init; } = null!;
}

public class ImportAnnouncementSorKorRorFileEndpoint
    : EndpointBase<ImportAnnouncementSorKorRorFileRequest, Ok<ImportAnnouncementSorKorRorResponse>>
{
    private readonly Dp2DbContext dbContext;

    public ImportAnnouncementSorKorRorFileEndpoint(
        ILogger<ImportAnnouncementSorKorRorFileEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("announcement-sor-kor-ror/import-file");
        this.AllowFileUploads();
        this.Description(b => b
            .WithTags("AnnouncementSorKorRor")
            .WithName("ImportAnnouncementSorKorRorFile")
            .AllowAnonymous()
            .Accepts<ImportAnnouncementSorKorRorFileRequest>("multipart/form-data")
            .Produces<ImportAnnouncementSorKorRorResponse>(StatusCodes.Status200OK));
    }

    protected override async ValueTask<Ok<ImportAnnouncementSorKorRorResponse>> HandleRequestAsync(
        ImportAnnouncementSorKorRorFileRequest req,
        CancellationToken ct)
    {
        var ext = Path.GetExtension(req.File.FileName).ToLowerInvariant();
        var rows = ext == ".csv"
            ? AnnouncementSorKorRorImportHelper.ParseCsv(req.File)
            : AnnouncementSorKorRorImportHelper.ParseExcel(req.File);

        var result = await AnnouncementSorKorRorImportHelper.ProcessAsync(rows, this.dbContext, ct);
        return TypedResults.Ok(result);
    }
}

internal static class AnnouncementSorKorRorImportHelper
{
    internal static async Task<ImportAnnouncementSorKorRorResponse> ProcessAsync(
        List<ImportAnnouncementSorKorRorRowRequest> rows,
        Dp2DbContext dbContext,
        CancellationToken ct)
    {
        var departmentTypeMap = await dbContext.SuParameters
            .Where(p => p.GroupCode == GroupCode.From(ParameterGroupConstant.AssignDepartment) && p.IsActive)
            .Select(p => new { p.Label, CodeValue = p.Code.Value })
            .ToDictionaryAsync(p => p.Label, p => p.CodeValue, StringComparer.OrdinalIgnoreCase, ct);

        var failedRows = new List<ImportAnnouncementSorKorRorRowResult>();
        var skippedRows = new List<ImportAnnouncementSorKorRorSkippedResult>();
        var entitiesToAdd = new List<Domain.AnnouncementInfo.AnnouncementSorKorRor>();
        var entitiesToUpdate = new List<Domain.AnnouncementInfo.AnnouncementSorKorRor>();

        var existingByOldId = await dbContext.AnnouncementSorKorRors
            .Where(a => a.OldId != null)
            .ToDictionaryAsync(a => a.OldId!.Value, ct);

        var existingKeys = await dbContext.AnnouncementSorKorRors
            .Where(a => !a.IsDeleted && a.Year != null && a.Month != null && a.DepartmentTypeCode != null)
            .Select(a => new { a.Year, a.Month, TypeCode = (string?)a.DepartmentTypeCode!.Value })
            .ToListAsync(ct);

        var duplicateKeys = existingKeys
            .Select(a => (a.Year!.Value, a.Month!.Value, a.TypeCode!))
            .ToHashSet();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowIndex = i + 2;

            try
            {
                var oldIdAsInt = row.OldId is long l && l is >= int.MinValue and <= int.MaxValue ? (int?)l : null;

                departmentTypeMap.TryGetValue(row.DepartmentTypeCode ?? string.Empty, out var departmentTypeCode);

                if (oldIdAsInt.HasValue && existingByOldId.TryGetValue(oldIdAsInt.Value, out var existing))
                {
                    existing.ImportUpdate(
                        year: row.Year,
                        month: row.Month,
                        amount: row.Amount,
                        departmentTypeCode: departmentTypeCode,
                        documentUrl: row.DocumentUrl);
                    existing.AddActivity(new ActivityInfo(
                        ActivityLogActionTypeConstant.Update,
                        "นำเข้าข้อมูลประกาศ สขร. (อัปเดต)",
                        existing.IsActive?.ToString() ?? string.Empty,
                        Remark: "นำเข้าข้อมูลประกาศ สขร. (อัปเดต)"));
                    entitiesToUpdate.Add(existing);
                }
                else
                {
                    if (row.Year.HasValue && row.Month.HasValue && departmentTypeCode is not null
                        && duplicateKeys.Contains((row.Year.Value, row.Month.Value, departmentTypeCode)))
                    {
                        skippedRows.Add(new ImportAnnouncementSorKorRorSkippedResult(
                            rowIndex,
                            row.Year,
                            row.Month,
                            departmentTypeCode));
                        continue;
                    }

                    var entity = Domain.AnnouncementInfo.AnnouncementSorKorRor.Import(
                        oldId: oldIdAsInt,
                        year: row.Year,
                        month: row.Month,
                        amount: row.Amount,
                        departmentTypeCode: departmentTypeCode,
                        documentUrl: row.DocumentUrl);
                    entity.AddActivity(new ActivityInfo(
                        ActivityLogActionTypeConstant.Create,
                        "นำเข้าข้อมูลประกาศ สขร.",
                        entity.IsActive?.ToString() ?? string.Empty,
                        Remark: "นำเข้าข้อมูลประกาศ สขร."));
                    entitiesToAdd.Add(entity);

                    if (row.Year.HasValue && row.Month.HasValue && departmentTypeCode is not null)
                    {
                        duplicateKeys.Add((row.Year.Value, row.Month.Value, departmentTypeCode));
                    }
                }
            }
            catch (Exception ex)
            {
                failedRows.Add(new ImportAnnouncementSorKorRorRowResult(rowIndex, ex.Message));
            }
        }

        const int chunkSize = 500;
        foreach (var chunk in entitiesToAdd.Chunk(chunkSize))
        {
            dbContext.AnnouncementSorKorRors.AddRange(chunk);
            await dbContext.SaveChangesAsync(ct);
            dbContext.ChangeTracker.Clear();
        }

        if (entitiesToUpdate.Count > 0)
        {
            await dbContext.SaveChangesAsync(ct);
        }

        var successCount = entitiesToAdd.Count + entitiesToUpdate.Count;
        return new ImportAnnouncementSorKorRorResponse(
            TotalRows: rows.Count,
            SuccessCount: successCount,
            FailedCount: failedRows.Count,
            SkippedCount: skippedRows.Count,
            FailedRows: failedRows,
            SkippedRows: skippedRows);
    }

    internal static List<ImportAnnouncementSorKorRorRowRequest> ParseCsv(IFormFile file)
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
        var rows = new List<ImportAnnouncementSorKorRorRowRequest>();

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

    internal static List<ImportAnnouncementSorKorRorRowRequest> ParseExcel(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        var headers = worksheet.Row(1).Cells()
            .Where(c => !c.IsEmpty())
            .ToDictionary(c => c.Address.ColumnNumber, c => c.GetString().Trim());

        var rows = new List<ImportAnnouncementSorKorRorRowRequest>();
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

    private static ImportAnnouncementSorKorRorRowRequest MapCellsToRequest(Dictionary<string, string?> cells)
    {
        static string? Get(Dictionary<string, string?> d, string key) =>
            d.TryGetValue(key, out var v) ? v : null;

        return new ImportAnnouncementSorKorRorRowRequest
        {
            OldId = long.TryParse(Get(cells, "OldId"), out var oldId) ? oldId : null,
            Year = int.TryParse(Get(cells, "ปี"), out var year) ? year : null,
            Month = int.TryParse(Get(cells, "เดือน"), out var month) ? month : null,
            Amount = decimal.TryParse(Get(cells, "จำนวน (ฉบับ)"), out var amount) ? amount : null,
            DepartmentTypeCode = Get(cells, "ประเภทหน่วยงาน"),
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
