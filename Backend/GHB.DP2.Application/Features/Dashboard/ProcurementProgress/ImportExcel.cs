namespace GHB.DP2.Application.Features.Dashboard.ProcurementProgress;

using ClosedXML.Excel;
using GHB.DP2.Application.Contracts;
using GHB.DP2.Domain.Dashboard;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

public record ImportProcurementProgressExcelRequestDto(IFormFile File) : IHasFile;

public class ImportProcurementProgressExcelRequest
{
    [FromForm]
    public ImportProcurementProgressExcelRequestDto Request { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }
}

public record ImportProcurementProgressExcelResponse(
    int Imported,
    int Skipped,
    List<string> SkippedPlanNumbers);

public class ImportProcurementProgressExcel
    : EndpointBase<ImportProcurementProgressExcelRequest, Ok<ImportProcurementProgressExcelResponse>>
{
    private readonly Dp2DbContext dbContext;

    public ImportProcurementProgressExcel(Dp2DbContext dbContext, ILogger<ImportProcurementProgressExcel> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Dashboard")
             .WithName("ImportProcurementProgressExcel")
             .Produces<Ok>());
        this.Post("dashboard/procurement-progress/import-excel");
        this.AllowFileUploads();
    }

    protected override async ValueTask<Ok<ImportProcurementProgressExcelResponse>> HandleRequestAsync(
        ImportProcurementProgressExcelRequest req,
        CancellationToken ct)
    {
        var rows = ReadExcelRows(req.Request.File);

        var planNumbers = rows.Select(r => r.PlanNumber).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();

        var plans = await this.dbContext.Plans
            .Where(p => p.IsActive && !p.IsDeleted && planNumbers.Contains((string)p.PlanNumber))
            .ToDictionaryAsync(p => p.PlanNumber.Value, ct);

        var planIds = plans.Values.Select(p => p.Id).ToList();

        var existingSummaries = await this.dbContext.ProcurementProgressSummaries
            .Where(s => planIds.Contains(s.PlanId))
            .ToDictionaryAsync(s => s.PlanId, ct);

        var userName = "System";
        var userId = UserId.From(req.UserId);
        var user = await this.dbContext.SuUsers
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user?.Employee is not null)
        {
            userName = user.Employee.View?.FullName ?? userName;
        }

        var imported = 0;
        var skippedPlanNumbers = new List<string>();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.PlanNumber))
            {
                continue;
            }

            if (!plans.TryGetValue(row.PlanNumber, out var plan))
            {
                skippedPlanNumbers.Add(row.PlanNumber);
                continue;
            }

            if (existingSummaries.TryGetValue(plan.Id, out var existing))
            {
                existing.Update(row.PlanDate, row.PurchaseOrderDate, row.DocPrepareNotifyDate, row.ContractDate, existing.Status);
                existing.Update(req.UserId, userName);
            }
            else
            {
                var summary = ProcurementProgressSummary.Create(
                    plan.Id,
                    row.PlanDate,
                    row.PurchaseOrderDate,
                    row.DocPrepareNotifyDate,
                    row.ContractDate);
                summary.Update(req.UserId, userName);
                await this.dbContext.ProcurementProgressSummaries.AddAsync(summary, ct);
                existingSummaries[plan.Id] = summary;
            }

            imported++;
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new ImportProcurementProgressExcelResponse(
            imported,
            skippedPlanNumbers.Count,
            skippedPlanNumbers));
    }

    private static List<ImportExcelRow> ReadExcelRows(IFormFile file)
    {
        var result = new List<ImportExcelRow>();

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = sheet.Row(rowNum);

            var planNumber = row.Cell(1).GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(planNumber))
            {
                continue;
            }

            result.Add(new ImportExcelRow(
                planNumber,
                ParseDate(row.Cell(4)),
                ParseDate(row.Cell(5)),
                ParseDate(row.Cell(6)),
                ParseDate(row.Cell(7))));
        }

        return result;
    }

    private static DateOnly? ParseDate(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.DataType == XLDataType.DateTime)
        {
            return DateOnly.FromDateTime(cell.GetDateTime());
        }

        var str = cell.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(str))
        {
            return null;
        }

        if (DateOnly.TryParseExact(str, ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy"], null, System.Globalization.DateTimeStyles.None, out var d))
        {
            return d;
        }

        return null;
    }
}

internal record ImportExcelRow(
    string PlanNumber,
    DateOnly? PlanDate,
    DateOnly? PurchaseOrderDate,
    DateOnly? DocPrepareNotifyDate,
    DateOnly? ContractDate);
