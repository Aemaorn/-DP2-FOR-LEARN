namespace GHB.DP2.Application.Features.Dashboard;

using GHB.DP2.Application.Common;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public record ExportExcelPriceSummaryRequest(
    int? BudgetYear,
    string? Keyword,
    string? DepartmentId,
    string? SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode,
    int? Month,
    int? Quarter);

public class ExportExcelPriceSummaryEndpoint : Endpoint<ExportExcelPriceSummaryRequest>
{
    private readonly Dp2DbContext dbContext;

    public ExportExcelPriceSummaryEndpoint(Dp2DbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(builder =>
            builder.WithTags("Dashboard")
                   .ProducesProblem(StatusCodes.Status500InternalServerError));
        this.Get("dashboard/price-summary/export-excel");
    }

    public override async Task HandleAsync(ExportExcelPriceSummaryRequest req, CancellationToken ct)
    {
        var items = await this.GetRowsDataAsync(req, ct);
        var criteriaText = await this.BuildCriteriaTextAsync(req, ct);

        using var stream = new MemoryStream();

        var colWidths = new[] { 8d, 20d, 40d, 20d, 20d, 25d, 15d, 20d, 20d, 20d, 20d, 25d, 20d, 25d, 30d };

        var cellStyle = new
        {
            title = 2u,
            header = 14u,
            normal = 3u,
            normalWrap = 17u,
            number2 = 5u,
            center = 7u,
        };

        var yearText = req.BudgetYear.HasValue ? $" ปีงบประมาณ {req.BudgetYear.Value}" : string.Empty;

        using var excelDocument = ExportExcel.Create(stream)
            .AddSheet("สรุปรายละเอียดราคา", colWidths, 3)
            .RowStyled(
                ($"สรุปรายละเอียดผู้ประกอบการเสนอราคา{yearText}", cellStyle.title))
            .Merge("A1:O1")
            .RowStyled(
                (criteriaText, cellStyle.title))
            .Merge("A2:O2")
            .RowStyled(
                ("ลำดับ", cellStyle.header),
                ("เลขที่", cellStyle.header),
                ("ชื่อโครงการ", cellStyle.header),
                ("วิธีจัดซื้อจัดจ้าง", cellStyle.header),
                ("วิธีการ", cellStyle.header),
                ("ผู้ประกอบการ", cellStyle.header),
                ("วันที่อนุมัติ", cellStyle.header),
                ("งบประมาณ", cellStyle.header),
                ("ราคากลาง", cellStyle.header),
                ("ราคารวมที่เสนอ", cellStyle.header),
                ("ราคารวมที่ตกลง ซื้อ/จ้าง/เช่า", cellStyle.header),
                ("เทียบงบประมาณ", cellStyle.header),
                ("เทียบราคากลาง", cellStyle.header),
                ("เทียบราคาที่เสนอ", cellStyle.header),
                ("เหตุผลการคัดเลือก / หมายเหตุ", cellStyle.header));

        if (items.Any())
        {
            var index = 1;

            foreach (var r in items)
            {
                var budgetLabel = r.Budget > 0
                    ? $"{(r.IsUnderBudget ? "ต่ำกว่า" : "สูงกว่า")} {r.BudgetDiff:#,##0.00}\n({r.BudgetDiffPercent}%)"
                    : "-";
                var medianLabel = r.MedianPrice > 0
                    ? $"{(r.IsUnderMedianPrice ? "ต่ำกว่า" : "สูงกว่า")} {r.MedianPriceDiff:#,##0.00}\n({r.MedianPriceDiffPercent}%)"
                    : "-";
                var offeredLabel = r.TotalOfferedPrice > 0
                    ? $"{(r.IsUnderOfferedPrice ? "ต่ำกว่า" : "สูงกว่า")} {r.OfferedPriceDiff:#,##0.00}\n({r.OfferedPriceDiffPercent}%)"
                    : "-";
                var approvedDateLabel = r.ApprovedDate.HasValue
                    ? r.ApprovedDate.Value.ToString("dd/MM/yyyy")
                    : "-";
                var reasonRemarkLabel = string.Join(" / ", new[] { r.SelectionReasonName, r.Remark }
                    .Where(s => !string.IsNullOrEmpty(s)));

                excelDocument.RowStyled(
                    (index, cellStyle.center),
                    (r.ProcurementNumber, cellStyle.normal),
                    (r.ProjectName, cellStyle.normal),
                    (r.SupplyMethodName, cellStyle.normal),
                    (r.SupplyMethodSpecialTypeName ?? string.Empty, cellStyle.normal),
                    (r.VendorName, cellStyle.normal),
                    (approvedDateLabel, cellStyle.center),
                    (r.Budget, cellStyle.number2),
                    (r.MedianPrice, cellStyle.number2),
                    (r.TotalOfferedPrice, cellStyle.number2),
                    (r.TotalAgreedPrice, cellStyle.number2),
                    (budgetLabel, cellStyle.normalWrap),
                    (medianLabel, cellStyle.normalWrap),
                    (offeredLabel, cellStyle.normalWrap),
                    (reasonRemarkLabel, cellStyle.normal));

                index++;
            }
        }
        else
        {
            excelDocument.RowStyled(("ไม่พบข้อมูล", cellStyle.center))
                         .Merge("A4:O4");
        }

        excelDocument.Finish();

        var fileName = $"สรุปรายละเอียดราคาจัดซื้อจัดจ้าง_{DateTimeOffset.Now:yyyyMMddHHmmss}.xlsx";
        var content = stream.ToArray();

        await this.SendBytesAsync(
            content,
            fileName,
            contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            cancellation: ct);
    }

    private async Task<string> BuildCriteriaTextAsync(ExportExcelPriceSummaryRequest req, CancellationToken ct)
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

    private async Task<List<PriceSummaryResponse>> GetRowsDataAsync(ExportExcelPriceSummaryRequest req, CancellationToken ct)
    {
        var filter = new PriceSummaryFilter(req.BudgetYear, req.Keyword, req.SupplyMethodCode, req.SupplyMethodSpecialTypeCode, req.Month, req.Quarter, req.DepartmentId);
        var query = await PriceSummaryQuery.BuildAsync(this.dbContext, filter, ct);

        var rawItems = await query
            .OrderByDescending(po => po.Procurement.Plan.PlanNumber)
            .Include(po => po.Procurement)
            .ThenInclude(p => p.SupplyMethod)
            .Include(po => po.Procurement)
            .ThenInclude(p => p.SupplyMethodSpecialType)
            .Include(po => po.Procurement)
            .ThenInclude(p => p.PurchaseRequisitions)
            .Include(po => po.Entrepreneurs)
            .ThenInclude(e => e.PJp006PriceDetails)
            .Include(po => po.Entrepreneurs)
            .ThenInclude(e => e.SuVendor)
            .Include(po => po.Acceptors)
            .Include(po => po.Procurement)
            .ThenInclude(p => p.Plan)
            .AsSplitQuery()
            .ToListAsync(ct);

        var selectionReasonCodes = rawItems
            .SelectMany(po => po.Entrepreneurs.Where(e => e.IsWinner && e.SelectionReasonCode != null))
            .Select(e => ParameterCode.From(e.SelectionReasonCode!))
            .Distinct()
            .ToList();

        var selectionReasonMap = selectionReasonCodes.Count > 0
            ? await this.dbContext.SuParameters
                .AsNoTracking()
                .Where(p => selectionReasonCodes.Contains(p.Code))
                .Select(p => new { p.Code, p.Label })
                .ToDictionaryAsync(k => k.Code, v => v.Label, ct)
            : new Dictionary<ParameterCode, string>();

        return rawItems
            .SelectMany(po => po.Entrepreneurs
                .Where(e => e.IsWinner && e.PJp006PriceDetails.Any())
                .Select(e => GetPriceSummaryEndpoint.MapToResponse(po, e, selectionReasonMap)))
            .ToList();
    }
}
