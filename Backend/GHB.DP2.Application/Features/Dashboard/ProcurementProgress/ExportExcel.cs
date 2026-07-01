namespace GHB.DP2.Application.Features.Dashboard.ProcurementProgress;

using GHB.DP2.Application.Common;
using GHB.DP2.Domain.Dashboard;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public record ExportExcelProcurementProgressRequest(
    string? Keyword,
    string? SupplyMethodSpecialTypeLabel,
    ProcurementProgressStatus? Status,
    DateFilterType? DateType,
    DateOnly? DateFrom,
    DateOnly? DateTo);

public class ExportExcelProcurementProgressEndpoint : Endpoint<ExportExcelProcurementProgressRequest>
{
    private readonly Dp2DbContext dbContext;

    public ExportExcelProcurementProgressEndpoint(Dp2DbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(builder =>
            builder.WithTags("Dashboard")
                   .ProducesProblem(StatusCodes.Status500InternalServerError));
        this.Get("dashboard/procurement-progress/export-excel");
    }

    public override async Task HandleAsync(ExportExcelProcurementProgressRequest req, CancellationToken ct)
    {
        var items = await this.GetDataAsync(req, ct);

        using var stream = new MemoryStream();

        var colWidths = new[] { 6d, 40d, 20d, 18d, 28d, 28d, 34d, 18d, 14d };

        var cellStyle = new
        {
            title = 2u,
            header = 14u,
            normal = 3u,
            number2 = 5u,
            center = 7u,
        };

        using var excelDocument = ExportExcel.Create(stream)
            .AddSheet("ติดตามสถานะ", colWidths, 3)
            .RowStyled(
                ("ติดตามสถานะการออกใบสั่งจ้างและการลงนามสัญญา", cellStyle.title))
            .Merge("A1:I1")
            .RowStyled(
                (string.Empty, cellStyle.title))
            .Merge("A2:I2")
            .RowStyled(
                ("ลำดับ", cellStyle.header),
                ("ชื่อโครงการ/แผนงาน (Project Name)", cellStyle.header),
                ("วิธีการจัดจ้าง (Procurement Method)", cellStyle.header),
                ("วงเงินจัดจ้าง (Budget) บาท", cellStyle.header),
                ("วันที่อนุมัติจัดจ้าง\n(SLA เริ่มต้น)", cellStyle.header),
                ("วันที่ออกใบสั่งซื้อ/สั่งจ้าง\n(SLA 2 วันทำการนับจากวันอนุมัติ)", cellStyle.header),
                ("วันที่ออกหนังสือแจ้งเตรียมเอกสาร\n(SLA 2 วันทำการนับจากออกใบสั่งจ้าง)", cellStyle.header),
                ("วันที่ลงนามสัญญา\n(SLA 5 วันทำการนับจากได้รับเอกสารครบถ้วน)", cellStyle.header),
                ("รวมระยะเวลาดำเนินการ", cellStyle.header));

        if (items.Any())
        {
            var index = 1;
            foreach (var r in items)
            {
                var poDays = CalcWorkingDaysExclusive(r.PlanDate, r.PurchaseOrderDate);
                var poText = r.PurchaseOrderDate.HasValue
                    ? $"{FormatDateTh(r.PurchaseOrderDate.Value)}\n({poDays} วันทำการ)"
                    : "-";

                var docDays = CalcWorkingDaysExclusive(r.PurchaseOrderDate, r.DocPrepareNotifyDate);
                var docText = r.DocPrepareNotifyDate.HasValue
                    ? $"{FormatDateTh(r.DocPrepareNotifyDate.Value)}\n({docDays} วันทำการ)"
                    : "-";

                var contractDays = CalcWorkingDaysInclusive(r.DocPrepareNotifyDate, r.ContractDate);
                var contractText = r.ContractDate.HasValue
                    ? $"{FormatDateTh(r.ContractDate.Value)}\n({contractDays} วันทำการ)"
                    : "-";

                var totalDays = CalcWorkingDaysInclusive(r.PlanDate, r.ContractDate);
                var totalCell = BuildTotalDaysRuns(totalDays);

                excelDocument.RowStyled(
                    (index, cellStyle.center),
                    (r.ProjectName, cellStyle.normal),
                    (r.SupplyMethodLabel, cellStyle.normal),
                    (r.Budget, cellStyle.number2),
                    (r.PlanDate.HasValue ? FormatDateTh(r.PlanDate.Value) : "-", cellStyle.normal),
                    (poText, cellStyle.normal),
                    (docText, cellStyle.normal),
                    (contractText, cellStyle.normal),
                    (totalCell, cellStyle.center));

                index++;
            }

            // Summary row
            excelDocument.RowStyled(
                ("รวม", cellStyle.center),
                ($"{items.Count} โครงการ", cellStyle.normal),
                (string.Empty, cellStyle.normal),
                (string.Empty, cellStyle.normal),
                (string.Empty, cellStyle.normal),
                (string.Empty, cellStyle.normal),
                (string.Empty, cellStyle.normal),
                (string.Empty, cellStyle.normal),
                (string.Empty, cellStyle.normal));
        }
        else
        {
            excelDocument.RowStyled(("ไม่พบข้อมูล", cellStyle.center))
                         .Merge("A4:I4");
        }

        excelDocument.Finish();

        var fileName = $"ติดตามสถานะการออกใบสั่งจ้างและการลงนามสัญญา_{DateTimeOffset.Now:yyyyMMddHHmmss}.xlsx";
        var content = stream.ToArray();

        await this.SendBytesAsync(
            content,
            fileName,
            contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            cancellation: ct);
    }

    private async Task<List<ProcurementProgressExportRow>> GetDataAsync(ExportExcelProcurementProgressRequest req, CancellationToken ct)
    {
        var targetStatuses = new[] { PlanStatus.ApprovePlan, PlanStatus.Announcement };

        var plansQuery = this.dbContext.Plans
            .Where(p => p.IsActive && !p.IsDeleted && targetStatuses.Contains(p.Status))
            .WhereIfTrue(
                !string.IsNullOrWhiteSpace(req.Keyword),
                p => EF.Functions.ILike(p.Name, $"%{req.Keyword}%") ||
                     EF.Functions.ILike((string)p.PlanNumber, $"%{req.Keyword}%"))
            .WhereIfTrue(
                !string.IsNullOrWhiteSpace(req.SupplyMethodSpecialTypeLabel),
                p => p.SupplyMethodSpecialType != null &&
                     EF.Functions.ILike(p.SupplyMethodSpecialType.Label, $"%{req.SupplyMethodSpecialTypeLabel}%"));

        var status = req.Status;
        var dateType = req.DateType;
        var dateFrom = req.DateFrom;
        var dateTo = req.DateTo;
        var hasDateFilter = dateType.HasValue && (dateFrom.HasValue || dateTo.HasValue);

        var rows = await plansQuery
            .GroupJoin(
                this.dbContext.ProcurementProgressSummaries,
                p => p.Id,
                s => s.PlanId,
                (p, sGroup) => new { p, sGroup })
            .SelectMany(
                x => x.sGroup.DefaultIfEmpty(),
                (x, s) => new { x.p, s })
            .Where(x => !status.HasValue || (x.s != null && x.s.Status == status.Value))
            .Where(x => !hasDateFilter
                || (dateType == DateFilterType.PlanDate && x.s != null
                    && (!dateFrom.HasValue || x.s.PlanDate >= dateFrom.Value)
                    && (!dateTo.HasValue || x.s.PlanDate <= dateTo.Value))
                || (dateType == DateFilterType.PurchaseOrderDate && x.s != null
                    && (!dateFrom.HasValue || x.s.PurchaseOrderDate >= dateFrom.Value)
                    && (!dateTo.HasValue || x.s.PurchaseOrderDate <= dateTo.Value))
                || (dateType == DateFilterType.DocPrepareNotifyDate && x.s != null
                    && (!dateFrom.HasValue || x.s.DocPrepareNotifyDate >= dateFrom.Value)
                    && (!dateTo.HasValue || x.s.DocPrepareNotifyDate <= dateTo.Value))
                || (dateType == DateFilterType.ContractDate && x.s != null
                    && (!dateFrom.HasValue || x.s.ContractDate >= dateFrom.Value)
                    && (!dateTo.HasValue || x.s.ContractDate <= dateTo.Value)))
            .OrderByDescending(x => x.p.AuditInfo.LastModifiedAt ?? x.p.AuditInfo.CreatedAt)
            .Select(x => new ProcurementProgressExportRow(
                x.p.Name,
                x.p.SupplyMethodSpecialType != null ? x.p.SupplyMethodSpecialType.Label : x.p.SupplyMethod.Label,
                x.p.Budget,
                x.s != null ? x.s.PlanDate : null,
                x.s != null ? x.s.PurchaseOrderDate : null,
                x.s != null ? x.s.DocPrepareNotifyDate : null,
                x.s != null ? x.s.ContractDate : null))
            .ToListAsync(ct);

        return rows;
    }

    private static string FormatDateTh(DateOnly date)
    {
        var thYear = date.Year + 543;
        var monthNames = new[] { "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค." };
        return $"{date.Day} {monthNames[date.Month - 1]} {thYear}";
    }

    // Returns null when either date is missing or count is 0 (matches frontend calcDays).
    // Negative when to < from (matches frontend sign logic).
    private static int? CalcWorkingDaysExclusive(DateOnly? from, DateOnly? to)
    {
        if (!from.HasValue || !to.HasValue)
        {
            return null;
        }

        var sign = to.Value >= from.Value ? 1 : -1;
        var count = CountWorkingDays(from.Value, to.Value, includeStart: false);
        return count == 0 ? null : sign * count;
    }

    // Returns null when either date is missing or count is 0 (matches frontend calcDaysInclusive).
    private static int? CalcWorkingDaysInclusive(DateOnly? from, DateOnly? to)
    {
        if (!from.HasValue || !to.HasValue)
        {
            return null;
        }

        var count = CountWorkingDays(from.Value, to.Value, includeStart: true);
        return count == 0 ? null : count;
    }

    // Builds a coloured RichTextRun[] for the total days column.
    // Matches frontend status thresholds: ≤10 → emerald, ≤15 → amber, >15 → rose
    private static RichTextRun[] BuildTotalDaysRuns(int? days)
    {
        if (!days.HasValue)
        {
            return [new RichTextRun("-")];
        }

        var color = days.Value <= 10 ? "FF10B981" // emerald-500
                  : days.Value <= 15 ? "FFF59E0B" // amber-500
                  : "FFF43F5E";   // rose-500

        return [new RichTextRun($"{days.Value} วันทำการ", color)];
    }

    private static int CountWorkingDays(DateOnly from, DateOnly to, bool includeStart)
    {
        var (start, end) = from <= to ? (from, to) : (to, from);
        var cur = includeStart ? start : start.AddDays(1);
        var count = 0;
        while (cur <= end)
        {
            var dow = cur.DayOfWeek;
            if (dow != DayOfWeek.Saturday && dow != DayOfWeek.Sunday)
            {
                count++;
            }

            cur = cur.AddDays(1);
        }

        return count;
    }
}

public record ProcurementProgressExportRow(
    string ProjectName,
    string SupplyMethodLabel,
    decimal Budget,
    DateOnly? PlanDate,
    DateOnly? PurchaseOrderDate,
    DateOnly? DocPrepareNotifyDate,
    DateOnly? ContractDate);
