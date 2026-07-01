namespace GHB.DP2.Application.Features.Procurement.P79Clause2;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.WorkList;
using GHB.DP2.Application.Features.WorkList.Programs;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.P79Clause2;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public record ExportExcelP79Clause2Request(
    string? Keyword,
    string? DepartmentCode,
    int? BudgetYear,
    int? Quarter,
    string? SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode,
    P79Clause2Status? Status,
    WorkProcess? WorkProcess,
    DateTimeOffset? ActionAtFrom,
    DateTimeOffset? ActionAtTo,
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId) : IProcurementBaseQuery
{
    public string? SupplyMethodTypeCode { get; }

    public bool? IsMd { get; }

    public bool? IsPendingDepartment => false;
}

public class ExportExcelP79Clause2Endpoint : Endpoint<ExportExcelP79Clause2Request>
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;
    private readonly ProcurementProgram procurementProgram = new();

    public ExportExcelP79Clause2Endpoint(Dp2DbContext dbContext, IOperationService operationService)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Options(builder =>
            builder.WithTags("P79Clause2")
                   .ProducesProblem(StatusCodes.Status500InternalServerError));
        this.Get("p79Clause2/export-excel");
    }

    public override async Task HandleAsync(ExportExcelP79Clause2Request req, CancellationToken ct)
    {
        var departmentName = "ธนาคารอาคารสงเคราะห์";
        var rowData = await this.GetRowsDataAsync(req, ct);

        using var stream = new MemoryStream();

        var colWidths = new[] { 8d, 35d, 50d, 25d, 20d, 35d, 20d };

        var cellStyle = new
        {
            title = 2u,
            header = 14u,
            normal = 3u,
            number2 = 5u,
            date = 6u,
            center = 7u,
        };

        var quarterText = BuildQuarterText(req.Quarter, req.BudgetYear);

        // Header rows (3) + table header (2 rows) = freeze 5 rows
        using var excelDocument = ExportExcel.Create(stream)
            .AddSheet("รายงานประกาศผู้ชนะรายไตรมาส", colWidths, 5)
            .RowStyled(
                ("รายละเอียดแนบท้ายประกาศผลผู้ชนะการจัดซื้อจัดจ้างหรือผู้ได้รับการคัดเลือก และสาระสำคัญของสัญญาหรือข้อตกลงเป็นหนังสือ", cellStyle.title))
            .Merge("A1:G1")
            .RowStyled((quarterText, cellStyle.title))
            .Merge("A2:G2")
            .RowStyled((departmentName, cellStyle.title))
            .Merge("A3:G3")
            .RowStyled(
                ("ลำดับที่\n(๑)", cellStyle.header),
                ("ชื่อผู้ประกอบการ\n(๒)", cellStyle.header),
                ("รายการพัสดุที่จัดซื้อจัดจ้าง\n(๓)", cellStyle.header),
                ("จำนวนเงินรวมที่จัดซื้อจัดจ้าง\n(๔)", cellStyle.header),
                ("เอกสารอ้างอิง (๕)", cellStyle.header),
                (string.Empty, cellStyle.header),
                ("เหตุผลสนับสนุน\n(๖)", cellStyle.header))
            .Merges("A4:A5", "B4:B5", "C4:C5", "D4:D5", "E4:F4", "G4:G5")
            .RowStyled(
                (string.Empty, cellStyle.header),
                (string.Empty, cellStyle.header),
                (string.Empty, cellStyle.header),
                (string.Empty, cellStyle.header),
                ("วันที่", cellStyle.header),
                ("เลขที่", cellStyle.header),
                (string.Empty, cellStyle.header));

        if (rowData.Any())
        {
            var index = 1;

            foreach (var r in rowData)
            {
                var thaiDate = $"{r.P79Clause2Date.Day}/{r.P79Clause2Date.Month}/{r.P79Clause2Date.Year + 543}";

                excelDocument.RowStyled(
                    (index, cellStyle.center),
                    (r.VendorName, cellStyle.normal),
                    (r.Items, cellStyle.normal),
                    (r.TotalPriceVat, cellStyle.number2),
                    (thaiDate, cellStyle.center),
                    (r.P79Clause2Number, cellStyle.center),
                    (string.Empty, cellStyle.normal));
                index++;
            }
        }
        else
        {
            excelDocument.RowStyled(("ไม่พบข้อมูล", cellStyle.center))
                         .Merge("A6:G6");
        }

        excelDocument.Finish();

        var fileName = $"รายงานประกาศผู้ชนะรายไตรมาส_{DateTimeOffset.Now:yyyyMMddHHmmss}.xlsx";
        var content = stream.ToArray();

        await this.SendBytesAsync(
            content,
            fileName,
            contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            cancellation: ct);
    }

    private async Task<UserContext?> GetUserContextAsync(UserId userId, CancellationToken ct)
    {
        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .ThenInclude(e => e.Positions)
                             .ThenInclude(p => p.BusinessUnit)
                             .Include(u => u.Employee)
                             .ThenInclude(e => e.View)
                             .Include(e => e.Delegatees)
                             .ThenInclude(e => e.SuDelegator)
                             .ThenInclude(suDelegator => suDelegator.SuUser)
                             .ThenInclude(suUser => suUser.Employee)
                             .ThenInclude(rawEmployee => rawEmployee.View)
                             .FirstOrDefaultAsync(u => u.Id == userId, ct);

        var viewUser = user?.Employee?.View;

        if (viewUser is null)
        {
            return null;
        }

        var effectiveUserIds = new List<EffectiveUserId>();

        if (user?.EffectiveDelegator is { SuUserId: var uid })
        {
            effectiveUserIds.Add(new EffectiveUserId(uid, true));
        }

        effectiveUserIds.Add(new EffectiveUserId(user!.Id, false));

        return new UserContext(
            user!,
            viewUser,
            user!.Employee?.PrimaryBusinessUnit?.Id,
            user.Employee?.IsJorPor ?? false,
            user.Employee?.PrimaryOrganizationLevel,
            effectiveUserIds);
    }

    private static string BuildQuarterText(int? quarter, int? budgetYear)
    {
        if (!quarter.HasValue)
        {
            return "ประจำไตรมาสที่ .... (เดือน .................. พ.ศ. ........ ถึง เดือน .................. พ.ศ. ........)";
        }

        var (startMonthName, endMonthName) = quarter.Value switch
        {
            1 => ("มกราคม", "มีนาคม"),
            2 => ("เมษายน", "มิถุนายน"),
            3 => ("กรกฎาคม", "กันยายน"),
            4 => ("ตุลาคม", "ธันวาคม"),
            _ => ("มกราคม", "ธันวาคม"),
        };

        var yearText = budgetYear.HasValue ? budgetYear.Value.ToString() : "........";

        return $"ประจำไตรมาสที่ {quarter} (เดือน {startMonthName} พ.ศ. {yearText} ถึง เดือน {endMonthName} พ.ศ. {yearText})";
    }

    private async Task<List<ExportExcelP79Clause2Row>> GetRowsDataAsync(ExportExcelP79Clause2Request req, CancellationToken ct)
    {
        IQueryable<P79Clause2> query;

        switch (req.WorkProcess)
        {
            case WorkProcess.InProcess:
                var userContext = await this.GetUserContextAsync(UserId.From(req.UserId), ct);
                var segmentAccountingMembers = await this.operationService.GetSegmentAccountingMembersAsync(ct);
                var isSegmentAccountingMember = userContext != null
                    && ProcurementProgram.IsSegmentAccountingMember(segmentAccountingMembers, userContext);
                query = userContext != null
                    ? this.procurementProgram.BuildP79Clause2BaseQuery(this.dbContext, req, userContext, isSegmentAccountingMember)
                    : this.dbContext.P79Clause2s.AsNoTracking();
                break;

            case WorkProcess.Related:
                var relatedUserId = UserId.From(req.UserId);
                query = this.dbContext.P79Clause2s
                    .Where(x =>
                        ((x.Status != P79Clause2Status.Draft || x.Status != P79Clause2Status.Rejected) && x.AuditInfo.CreatedBy == req.UserId) ||
                        (x.Status == P79Clause2Status.WaitingApproval && x.Acceptors.Any(ac =>
                            !ac.IsCurrent && ac.UserId == relatedUserId &&
                            (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected))));
                break;

            case WorkProcess.Completed:
                var completedUserId = UserId.From(req.UserId);
                query = this.dbContext.P79Clause2s
                    .Where(x =>
                        (x.Status != P79Clause2Status.Paid && x.AuditInfo.CreatedBy == req.UserId) ||
                        (x.Status == P79Clause2Status.Paid && x.Acceptors.Any(ac => ac.UserId == completedUserId)));
                break;

            default:
                query = this.dbContext.P79Clause2s.AsNoTracking();
                break;
        }

        query = query
            .Include(p => p.Vendors)
            .ThenInclude(v => v.VendorParcels)
            .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), x => EF.Functions.ILike(x.Subject, $"%{req.Keyword}%") || EF.Functions.ILike((string)x.P79Clause2Number, $"%{req.Keyword}%"))
            .WhereIfTrue(!string.IsNullOrWhiteSpace(req.DepartmentCode), x => x.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
            .WhereIfTrue(req.BudgetYear.HasValue, x => x.BudgetYear == req.BudgetYear)
            .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodCode), x => x.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
            .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodSpecialTypeCode), x => x.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
            .WhereIfTrue(req.Status.HasValue, x => x.Status == req.Status)
            .WhereIfTrue(req.ActionAtFrom.HasValue, x => x.Status == P79Clause2Status.Approved && (x.AuditInfo.LastModifiedAt ?? x.AuditInfo.CreatedAt) >= req.ActionAtFrom!.Value)
            .WhereIfTrue(req.ActionAtTo.HasValue, x => x.Status == P79Clause2Status.Approved && (x.AuditInfo.LastModifiedAt ?? x.AuditInfo.CreatedAt) <= req.ActionAtTo!.Value);

        if (req.Quarter.HasValue)
        {
            var (startMonth, endMonth) = req.Quarter.Value switch
            {
                1 => (1, 3),
                2 => (4, 6),
                3 => (7, 9),
                4 => (10, 12),
                _ => (1, 12),
            };

            query = query.Where(x => x.P79Clause2Date.Month >= startMonth && x.P79Clause2Date.Month <= endMonth);

            if (req.BudgetYear.HasValue)
            {
                var ceYear = req.BudgetYear.Value - 543;
                query = query.Where(x => x.P79Clause2Date.Year == ceYear);
            }
        }

        query = query.OrderBy(o => o.P79Clause2Date);

        var p79List = await query.ToListAsync(ct);

        var rows = p79List
            .SelectMany(p79 =>
                p79.Vendors
                   .OrderBy(v => v.Sequence)
                   .Where(v => v.VendorParcels.Sum(p => p.TotalPriceVat) <= 100000)
                   .Select(v => new ExportExcelP79Clause2Row(
                       v.VendorName,
                       string.Join(", ", v.VendorParcels.OrderBy(p => p.Sequence).Select(p => p.Item)),
                       v.VendorParcels.Sum(p => p.TotalPriceVat),
                       p79.P79Clause2Date,
                       p79.P79Clause2Number.Value)))
            .ToList();

        return rows;
    }
}

public record ExportExcelP79Clause2Row(
    string VendorName,
    string Items,
    decimal TotalPriceVat,
    DateTimeOffset P79Clause2Date,
    string P79Clause2Number);
