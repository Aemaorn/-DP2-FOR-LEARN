namespace GHB.DP2.Application.Features.Plan.Plan;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Constants;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public record ExportExcelEGPRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    int PageNumber,
    int PageSize,
    string? Keyword,
    PlanType? Type,
    string? DepartmentCode,
    int? BudgetYear,
    string? SupplyMethodCode,
    string? SupplyMethodTypeCode,
    string? SupplyMethodSpecialTypeCode,
    bool? IsChange,
    bool? IsCancel);

public record ExportExcelEGPDto(
    int BudgetYear,
    string Name,
    DateTimeOffset ExpectingAt,
    decimal Budget);

public class ExportExcelEGPEndpoint : Endpoint<ExportExcelEGPRequest>
{
    private readonly Dp2DbContext dbContext;

    public ExportExcelEGPEndpoint(Dp2DbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(builder =>
            builder.WithTags(nameof(Plan))
                   .ProducesProblem(StatusCodes.Status404NotFound)
                   .ProducesProblem(StatusCodes.Status500InternalServerError));
        this.Get("plan/export-egp");
    }

    public override async Task HandleAsync(ExportExcelEGPRequest req, CancellationToken ct)
    {
        using var stream = new MemoryStream();

        var colWidths = new[] { 15d, 60d, 35d, 25d };

        var cellStyle =
            new
            {
                headerGrayRed = 10u,
                headerLightBlue = 11u,
                headerDarkBlue = 12u,
                headerYellow = 13u,
                normal = 3u,
                number2 = 5u,
                date = 6u,
                center = 7u,
            };

        using var excelDocument =
            ExportExcel.Create(stream)
                       .AddSheet(
                           "ข้อมูลแผนจัดซื้อจัดจ้าง",
                           colWidths,
                           1)
                       .RowStyled(
                           ("รหัสพนักงานแผนก", cellStyle.headerGrayRed),
                           ("ปีงบประมาณ", cellStyle.headerGrayRed),
                           ("ชื่อโครงการที่จะจัดซื้อจัดจ้าง", cellStyle.headerGrayRed),
                           ("เดือน/ปีที่คาดว่าจะประกาศจัดซื้อจัดจ้าง", cellStyle.headerGrayRed),
                           ("เงินงบประมาณตามพ.ร.บ.รายจ่ายประจำปี", cellStyle.headerLightBlue),
                           ("ประเภทเงินงบประมาณตามพ.ร.บ.รายจ่ายประจำปี", cellStyle.headerLightBlue),
                           ("เงินนอกงบประมาณ", cellStyle.headerDarkBlue),
                           ("ประเภทเงินนอกงบประมาณ", cellStyle.headerDarkBlue),
                           ("งบประมาณหน่วยงาน", cellStyle.headerYellow));

        var rowData = await GetRowsData(ct);

        if (rowData.Any())
        {
            foreach (var r in rowData)
            {
                excelDocument.RowStyled(
                    (string.Empty, cellStyle.normal),
                    (r.BudgetYear, cellStyle.center),
                    (r.Name, cellStyle.normal),
                    (r.ExpectingAt.ToString("MM/yyy"), cellStyle.date),
                    (string.Empty, cellStyle.number2),
                    (string.Empty, cellStyle.number2),
                    (string.Empty, cellStyle.number2),
                    (string.Empty, cellStyle.number2),
                    (r.Budget, cellStyle.number2));
            }
        }
        else
        {
            excelDocument.RowStyled(("ไม่พบข้อมูล", cellStyle.center))
                         .Merge("A2:F2");
        }

        excelDocument.Finish();

        var fileName = $"รายงาน e-GP_{DateTimeOffset.Now:yyyyMMddHHmmss}.xlsx";

        var content = stream.ToArray();

        await this.SendBytesAsync(
            content,
            fileName,
            contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            cancellation: ct);

        return;

        async Task<List<ExportExcelEGPDto>> GetRowsData(CancellationToken ct)
        {
            var userIdObj = UserId.From(req.UserId);

            var user = await this.dbContext.SuUsers
                                 .Include(u => u.Employee)
                                 .ThenInclude(e => e.Positions)
                                 .ThenInclude(p => p.BusinessUnit)
                                 .Include(suUser => suUser.Employee)
                                 .ThenInclude(rawEmployee => rawEmployee.View)
                                 .Include(suUser => suUser.Delegatees)
                                 .ThenInclude(suDelegatee => suDelegatee.SuDelegator)
                                 .FirstOrDefaultAsync(u => u.Id == userIdObj, ct);

            var viewUser = user?.Employee?.View;

            if (viewUser is null)
            {
                return null!;
            }

            var userDelegatee = user!.Delegatees.Select(d => d.SuDelegator.SuUserId);

            List<UserId> userIds = [.. userDelegatee, user.Id];

            var query = this.dbContext.Plans
                            .Where(p => p.IsActive)
                            .Where(p => p.SupplyMethodCode == ParameterCode.From(SupplyMethodConstant.Sixty))
                            .Where(p => p.Status == PlanStatus.ApprovePlan || p.Status == PlanStatus.WaitingApprovePlan)
                            .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), x => EF.Functions.ILike(x.Name, $"%{req.Keyword}%") || EF.Functions.ILike((string)x.PlanNumber, $"%{req.Keyword}%"))
                            .WhereIfTrue(!req.Type.IsNull(), x => x.Type == req.Type)
                            .WhereIfTrue(!string.IsNullOrWhiteSpace(req.DepartmentCode), x => x.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                            .WhereIfTrue(!req.BudgetYear.IsNull(), x => x.BudgetYear == req.BudgetYear)
                            .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodTypeCode), x => x.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!))
                            .WhereIfTrue(req.IsChange is true, x => x.IsChange)
                            .WhereIfTrue(req.IsCancel is true, x => x.IsCancel)
                            .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodSpecialTypeCode), x => x.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!));

            query = query.OrderByDescending(o => o.AuditInfo.LastModifiedAt ?? o.AuditInfo.CreatedAt);

            var paaginatedQuery =
                await query
                      .Select(v => new ExportExcelEGPDto(
                          v.BudgetYear,
                          v.Name,
                          v.ExpectingProcurementAt,
                          v.Budget))
                      .ToListAsync(ct);

            return paaginatedQuery;
        }
    }
}