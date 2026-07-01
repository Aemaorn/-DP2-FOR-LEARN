namespace GHB.DP2.Application.Features.Plan.Plan;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Common;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public record ExportExcelPlanRequest(
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
    PlanStatus? Status,
    bool? IsChange,
    bool? IsCancel,
    WorkProcess WorkProcess = WorkProcess.InProcess);

public record ExportExcelPlanDto(
    string Key,
    string RefNo,
    string DepartmentName,
    string Name,
    decimal Budget,
    DateTimeOffset ExpectingAt,
    string SupplyMethod,
    string? SubSupplyMethod);

public class ExportExcelPlanEndpoint : Endpoint<ExportExcelPlanRequest>
{
    private readonly Dp2DbContext dbContext;

    public ExportExcelPlanEndpoint(Dp2DbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(builder =>
            builder.WithTags(nameof(Plan))
                   .ProducesProblem(StatusCodes.Status404NotFound)
                   .ProducesProblem(StatusCodes.Status500InternalServerError));
        this.Get("plan/export-plan");
    }

    public override async Task HandleAsync(ExportExcelPlanRequest req, CancellationToken ct)
    {
        using var stream = new MemoryStream();

        var colWidths = new[] { 10d, 0d, 20d, 35d, 60d, 25d, 30d, 35d, 40d };

        var cellStyle =
            new
            {
                header = 2u,
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
                           ("ลำดับที่", cellStyle.header),
                           ("Key", cellStyle.header),
                           ("Ref.No", cellStyle.header),
                           ("ชื่อฝ่าย", cellStyle.header),
                           ("ชื่อโครงการ", cellStyle.header),
                           ("งบประมาณโครงการ (บาท)", cellStyle.header),
                           ("ประมาณช่วงเวลาจัดซื้อจัดจ้าง", cellStyle.header),
                           ("วิธีจัดหา", cellStyle.header),
                           (string.Empty, cellStyle.header),
                           ("วัยที่ประกาศแผน", cellStyle.header),
                           ("Activity", cellStyle.header))
                       .Merge("G1:H1");

        var rowData = await GetRowsData(ct);

        if (rowData.Any())
        {
            var index = 1;

            foreach (var r in rowData)
            {
                excelDocument.RowStyled(
                    (index, cellStyle.center),
                    (r.Key, cellStyle.center),
                    (r.RefNo, cellStyle.center),
                    (r.DepartmentName, cellStyle.normal),
                    (r.Name, cellStyle.normal),
                    (r.Budget, cellStyle.number2),
                    (r.ExpectingAt.ToString("dd/MM/yyyy"), cellStyle.date),
                    (r.SupplyMethod, cellStyle.normal),
                    (r.SubSupplyMethod ?? string.Empty, cellStyle.normal),
                    (string.Empty, cellStyle.normal),
                    (string.Empty, cellStyle.normal));
                index++;
            }
        }
        else
        {
            excelDocument.RowStyled(("ไม่พบข้อมูล", cellStyle.center))
                         .Merge("A2:H2");
        }

        excelDocument.Finish();

        var fileName = $"รายงานรายการจัดซื้อจัดจ้าง_{DateTimeOffset.Now:yyyyMMddHHmmss}.xlsx";

        var content = stream.ToArray();

        await this.SendBytesAsync(
            content,
            fileName,
            contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            cancellation: ct);

        return;

        async Task<List<ExportExcelPlanDto>> GetRowsData(CancellationToken ct)
        {
            var statusMap = new Dictionary<WorkProcess, List<PlanStatus>>
            {
                { WorkProcess.InProcess, [PlanStatus.DraftPlan, PlanStatus.RejectPlan, PlanStatus.EditPlan] },
                { WorkProcess.Related, [PlanStatus.WaitingApprovePlan, PlanStatus.Assigned, PlanStatus.DraftRecordDocument, PlanStatus.WaitingAcceptor, PlanStatus.WaitingAnnouncement] },
                { WorkProcess.Completed, [PlanStatus.Announcement, PlanStatus.CancelPlan, PlanStatus.ApprovePlan] },
            };

            var workProcessStatus = statusMap.TryGetValue(req.WorkProcess, value: out var value)
                ? value
                : [];

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
                            .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), x => EF.Functions.ILike(x.Name, $"%{req.Keyword}%") || EF.Functions.ILike((string)x.PlanNumber, $"%{req.Keyword}%"))
                            .WhereIfTrue(!req.Type.IsNull(), x => x.Type == req.Type)
                            .WhereIfTrue(!string.IsNullOrWhiteSpace(req.DepartmentCode), x => x.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                            .WhereIfTrue(!req.BudgetYear.IsNull(), x => x.BudgetYear == req.BudgetYear)
                            .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodCode), x => x.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                            .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodTypeCode), x => x.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!))
                            .WhereIfTrue(req.IsChange is true, x => x.IsChange)
                            .WhereIfTrue(req.IsCancel is true, x => x.IsCancel)
                            .WhereIfTrue(!string.IsNullOrEmpty(req.SupplyMethodSpecialTypeCode), x => x.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!));

            query = req.WorkProcess switch
            {
                WorkProcess.InProcess => GetInprogressPlans(query, viewUser, userIds),
                WorkProcess.Related => GetRelatedPlans(query, workProcessStatus, userIds),
                WorkProcess.Completed => GetCompletedPlans(query, workProcessStatus, userIds),
                _ => query,
            };

            query = query.OrderByDescending(o => o.AuditInfo.LastModifiedAt ?? o.AuditInfo.CreatedAt);

            var paaginatedQuery =
                await query.WhereIfTrue(
                               !req.Status.IsNull(),
                               x => x.Status == req.Status)
                           .Select(v => new ExportExcelPlanDto(
                               v.Id.Value.ToString(),
                               v.PlanNumber.Value,
                               v.Department.Name,
                               v.Name,
                               v.Budget,
                               v.ExpectingProcurementAt,
                               v.SupplyMethod.Label,
                               v.SupplyMethodSpecialType != null ? v.SupplyMethodSpecialType.Label : null))
                           .ToListAsync(ct);

            return paaginatedQuery;
        }
    }

    private static IQueryable<Plan> GetInprogressPlans(IQueryable<Plan> query, RawEmployeeView viewUser, IEnumerable<UserId> userIdObj)
    {
        return query.Where(p =>
            (
                (
                    p.Status == PlanStatus.DraftPlan ||
                    p.Status == PlanStatus.RejectPlan ||
                    p.Status == PlanStatus.EditPlan
                ) &&
                p.DepartmentId == viewUser.BusinessUnitId
            ) ||
            (
                (
                    p.Status == PlanStatus.WaitingApprovePlan ||
                    p.Status == PlanStatus.WaitingAcceptor
                ) &&
                p.Acceptors.Any(a => a.IsCurrent && userIdObj.Contains(a.UserId) && a.Status == AcceptorStatus.Pending)
            ) ||

            // InYear > 500,000
            (
                (
                    p.Status == PlanStatus.WaitingAssign ||
                    p.Status == PlanStatus.WaitingAnnouncement
                ) &&
                p.Assignees.Any(a => userIdObj.Contains(a.UserId) && a.Type == AssigneeType.Director)
            ) ||
            (
                p.Status == PlanStatus.DraftRecordDocument &&
                p.Assignees.Any(a => userIdObj.Contains(a.UserId) && a.Type == AssigneeType.Assignee)
            ));
    }

    private static IQueryable<Plan> GetRelatedPlans(IQueryable<Plan> query, List<PlanStatus> workProcessStatus, IEnumerable<UserId> userIdObj)
    {
        return query.Where(p =>
            (
                workProcessStatus.Contains(p.Status) &&
                p.Acceptors.Any(a =>
                    (
                        !a.IsCurrent && userIdObj.Contains(a.UserId) &&
                        (a.Status == AcceptorStatus.Approved ||
                         a.Status == AcceptorStatus.Rejected)
                    ) ||
                    userIdObj.Select(u => u.Value).Contains(p.AuditInfo.CreatedBy))
            ) ||
            (
                p.Status == PlanStatus.Assigned &&
                p.Assignees.Any(a => userIdObj.Contains(a.UserId) && a.Type == AssigneeType.Director)));
    }

    private static IQueryable<Plan> GetCompletedPlans(IQueryable<Plan> query, List<PlanStatus> workProcessStatus, IEnumerable<UserId> userIdObj)
    {
        return query.Where(p =>
            p.Acceptors.Any(a =>
                userIdObj.Contains(a.UserId) &&
                a.Status == AcceptorStatus.Approved) ||
            workProcessStatus.Contains(p.Status));
    }
}