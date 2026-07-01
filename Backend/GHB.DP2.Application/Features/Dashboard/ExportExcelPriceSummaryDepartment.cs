namespace GHB.DP2.Application.Features.Dashboard;

using GHB.DP2.Application.Common;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public record ExportExcelPriceSummaryDepartmentRequest(
    int? BudgetYear,
    string? Keyword,
    string? DepartmentId,
    string? SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode,
    int? Month,
    int? Quarter);

public class ExportExcelPriceSummaryDepartmentEndpoint : Endpoint<ExportExcelPriceSummaryDepartmentRequest>
{
    private readonly Dp2DbContext dbContext;

    public ExportExcelPriceSummaryDepartmentEndpoint(Dp2DbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(builder =>
            builder.WithTags("Dashboard")
                   .ProducesProblem(StatusCodes.Status500InternalServerError));
        this.Get("dashboard/price-summary/department/export-excel");
    }

    public override async Task HandleAsync(ExportExcelPriceSummaryDepartmentRequest req, CancellationToken ct)
    {
        var items = await this.GetRowsDataAsync(req, ct);
        var criteriaText = await this.BuildCriteriaTextAsync(req, ct);

        using var stream = new MemoryStream();

        var colWidths = new[] { 40d, 20d, 25d, 25d, 25d };

        var cellStyle = new
        {
            title = 2u,
            header = 14u,
            normal = 3u,
            number2 = 5u,
            center = 7u,
        };

        var yearText = req.BudgetYear.HasValue ? $" ปีงบประมาณ {req.BudgetYear.Value}" : string.Empty;

        using var excelDocument = ExportExcel.Create(stream)
            .AddSheet("สรุปผลตามฝ่าย", colWidths, 3)
            .RowStyled(
                ($"สรุปผลตามฝ่าย{yearText}", cellStyle.title))
            .Merge("A1:E1")
            .RowStyled(
                (criteriaText, cellStyle.title))
            .Merge("A2:E2")
            .RowStyled(
                ("ฝ่าย", cellStyle.header),
                ("จำนวนโครงการ", cellStyle.header),
                ("งบประมาณรวม", cellStyle.header),
                ("ราคากลางรวม", cellStyle.header),
                ("ราคาที่ตกลงรวม", cellStyle.header));

        if (items.Any())
        {
            foreach (var r in items)
            {
                excelDocument.RowStyled(
                    (r.DepartmentName, cellStyle.normal),
                    (r.ProjectCount, cellStyle.center),
                    (r.TotalBudget, cellStyle.number2),
                    (r.TotalMedianPrice, cellStyle.number2),
                    (r.TotalAgreedPrice, cellStyle.number2));
            }
        }
        else
        {
            excelDocument.RowStyled(("ไม่พบข้อมูล", cellStyle.center))
                         .Merge("A4:E4");
        }

        excelDocument.Finish();

        var fileName = $"สรุปผลตามฝ่าย_{DateTimeOffset.Now:yyyyMMddHHmmss}.xlsx";
        var content = stream.ToArray();

        await this.SendBytesAsync(
            content,
            fileName,
            contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            cancellation: ct);
    }

    private async Task<string> BuildCriteriaTextAsync(ExportExcelPriceSummaryDepartmentRequest req, CancellationToken ct)
    {
        var parts = new List<string>();

        if (req.BudgetYear.HasValue)
        {
            parts.Add($"ปีงบประมาณ: {req.BudgetYear.Value}");
        }

        if (!string.IsNullOrEmpty(req.Keyword))
        {
            parts.Add($"คำค้นหา: {req.Keyword}");
        }

        if (!string.IsNullOrEmpty(req.SupplyMethodCode))
        {
            var code = ParameterCode.From(req.SupplyMethodCode);
            var label = await this.dbContext.SuParameters
                .AsNoTracking()
                .Where(p => p.Code == code)
                .Select(p => p.Label)
                .FirstOrDefaultAsync(ct);
            parts.Add($"วิธีจัดซื้อจัดจ้าง: {label ?? req.SupplyMethodCode}");
        }

        if (!string.IsNullOrEmpty(req.SupplyMethodSpecialTypeCode))
        {
            var code = ParameterCode.From(req.SupplyMethodSpecialTypeCode);
            var label = await this.dbContext.SuParameters
                .AsNoTracking()
                .Where(p => p.Code == code)
                .Select(p => p.Label)
                .FirstOrDefaultAsync(ct);
            parts.Add($"วิธีการ: {label ?? req.SupplyMethodSpecialTypeCode}");
        }

        if (req.Quarter.HasValue)
        {
            var quarterLabels = new[] { "ไตรมาส 1", "ไตรมาส 2", "ไตรมาส 3", "ไตรมาส 4" };
            parts.Add($"ไตรมาส: {quarterLabels[req.Quarter.Value - 1]}");
        }

        if (req.Month.HasValue)
        {
            var monthLabels = new[]
            {
                "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน",
                "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม",
            };
            parts.Add($"เดือน: {monthLabels[req.Month.Value - 1]}");
        }

        return parts.Count > 0 ? string.Join("  |  ", parts) : string.Empty;
    }

    private async Task<List<PriceSummaryDepartmentItem>> GetRowsDataAsync(ExportExcelPriceSummaryDepartmentRequest req, CancellationToken ct)
    {
        var filter = new PriceSummaryFilter(req.BudgetYear, req.Keyword, req.SupplyMethodCode, req.SupplyMethodSpecialTypeCode, req.Month, req.Quarter, req.DepartmentId);
        var query = await PriceSummaryQuery.BuildAsync(this.dbContext, filter, ct);

        var rawItems = await query
            .Include(po => po.Procurement)
            .ThenInclude(p => p.Department)
            .Include(po => po.Procurement)
            .ThenInclude(p => p.PurchaseRequisitions)
            .Include(po => po.Entrepreneurs.Where(e => e.IsWinner))
            .ThenInclude(e => e.PJp006PriceDetails)
            .AsSplitQuery()
            .ToListAsync(ct);

        return rawItems
            .GroupBy(po => po.Procurement.Department?.Name ?? "ไม่ระบุฝ่าย")
            .Select(g =>
            {
                var totalBudget = g.Sum(po => po.Procurement.Budget ?? 0m);
                var totalMedianPrice = g.Sum(po =>
                    po.Procurement.PurchaseRequisitions
                        .Select(pr => pr.MedianPriceAmount)
                        .FirstOrDefault() ?? 0m);
                var totalAgreedPrice = g.Sum(po =>
                    po.Entrepreneurs
                        .Where(e => e.IsWinner)
                        .SelectMany(e => e.PJp006PriceDetails)
                        .Sum(pd => pd.AgreedPrice * pd.ParcelQuantity));

                return new PriceSummaryDepartmentItem(
                    DepartmentName: g.Key,
                    ProjectCount: g.Count(),
                    TotalBudget: totalBudget,
                    TotalMedianPrice: totalMedianPrice,
                    TotalAgreedPrice: totalAgreedPrice);
            })
            .OrderBy(i => i.DepartmentName)
            .ToList();
    }
}
