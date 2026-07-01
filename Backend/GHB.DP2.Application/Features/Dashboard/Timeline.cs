namespace GHB.DP2.Application.Features.Dashboard;

using System.Globalization;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetProcurementTimelineRequest(string ProcurementId, int? BudgetYear);

public record ProcurementHeader(
    string ProcurementId,
    int BudgetYear,
    string DepartmentId,
    string DepartmentName,
    decimal Budget,
    string SupplyMethodCode,
    string SupplyMethodName,
    string? Subject);

public record TimelineTask(
    string Code,
    string Name,
    DateOnly? ActualStart,
    DateOnly? ActualEnd,
    int? StartIndex = null,
    int? EndIndex = null);

public record TimelinePhase(string Code, string Name, IEnumerable<TimelineTask> Tasks);

public record GetProcurementTimelineResponse(
    ProcurementHeader Header,
    IEnumerable<string> AxisMonths,
    DateOnly AsOfDate,
    IEnumerable<TimelinePhase> Phases);

public class GetProcurementTimelineEndpoint : EndpointBase<GetProcurementTimelineRequest, Ok<GetProcurementTimelineResponse>>
{
    private readonly Dp2DbContext dbContext;

    public GetProcurementTimelineEndpoint(ILogger<GetProcurementTimelineEndpoint> logger, Dp2DbContext dbContext)
        : base(logger)
        => this.dbContext = dbContext;

    public override void Configure()
    {
        this.Get("dashboard/procurements/{procurementId}/timeline");
        this.Options(o => o.WithTags("Dashboard"));
    }

    protected override async ValueTask<Ok<GetProcurementTimelineResponse>> HandleRequestAsync(GetProcurementTimelineRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(req.ProcurementId, out var guid))
        {
            throw new ArgumentException("Invalid procurement id format", nameof(req.ProcurementId));
        }

        var procurementId = ProcurementId.From(guid);

        var p = await this.dbContext.Procurements
                          .AsNoTracking()
                          .Where(x => x.Id == procurementId)
                          .Select(x => new
                          {
                              x.Id,
                              x.PlanId,
                              BudgetYear = x.BudgetYear ?? 0,
                              Budget = x.Budget ?? 0m,
                              DepartmentId = x.DepartmentId.Value,
                              DepartmentName = x.Department.Name,
                              x.SupplyMethodCode,
                              Subject = x.Name,
                          })
                          .FirstOrDefaultAsync(ct);

        if (p is null)
        {
            throw new InvalidOperationException("Procurement not found.");
        }

        var year = req.BudgetYear ?? p.BudgetYear;
        var supplyLabel = await this.dbContext.SuParameters.AsNoTracking()
                                    .Where(s => s.Code == p.SupplyMethodCode)
                                    .Select(s => s.Label)
                                    .FirstOrDefaultAsync(ct) ?? p.SupplyMethodCode.Value;

        var header = new ProcurementHeader(
            p.Id.Value.ToString(), year, p.DepartmentId, p.DepartmentName, p.Budget, p.SupplyMethodCode.Value, supplyLabel, p.Subject);

        var (planDraftStart, planDraftEnd, planAnnStart, planAnnEnd) = await this.GetPlanDateRangesAsync(p.PlanId?.Value, ct);

        var phases = await this.CreateTimelinePhasesAsync(procurementId, planDraftStart, planDraftEnd, planAnnStart, planAnnEnd, ct);

        var (axis, phasesWithIdx) = this.CalculateAxisAndIndexMapping(phases, year);

        var resp = new GetProcurementTimelineResponse(header, axis, DateOnly.FromDateTime(DateTime.UtcNow), phasesWithIdx);

        return TypedResults.Ok(resp);
    }

    private async ValueTask<(DateOnly? Start, DateOnly? End)> GetAppointRange(ProcurementId procurementId, CancellationToken ct)
    {
        return Range(await this.dbContext.PpAppoints.AsNoTracking()
                               .Where(a => !a.IsDeleted && a.ProcurementId == procurementId)
                               .Select(a => new ValueTuple<DateTimeOffset?, DateTimeOffset?>(a.AuditInfo.CreatedAt, a.AuditInfo.LastModifiedAt))
                               .ToListAsync(ct));
    }

    private async ValueTask<(DateOnly? Start, DateOnly? End)> GetTorDraftsRange(ProcurementId procurementId, CancellationToken ct)
    {
        return Range(await this.dbContext.PpTorDrafts.AsNoTracking()
                               .Where(t => !t.IsDeleted && t.ProcurementId == procurementId)
                               .Select(t => new ValueTuple<DateTimeOffset?, DateTimeOffset?>(t.AuditInfo.CreatedAt, t.AuditInfo.LastModifiedAt))
                               .ToListAsync(ct));
    }

    private async ValueTask<(DateOnly? Start, DateOnly? End)> GetMedianPricesRange(ProcurementId procurementId, CancellationToken ct)
    {
        return Range(await this.dbContext.PpMedianPrices.AsNoTracking()
                               .Where(m => !m.IsDeleted && m.ProcurementId == procurementId)
                               .Select(m => new ValueTuple<DateTimeOffset?, DateTimeOffset?>(m.AuditInfo.CreatedAt, m.AuditInfo.LastModifiedAt))
                               .ToListAsync(ct));
    }

    private async ValueTask<(DateOnly? Start, DateOnly? End)> GetJp005Range(ProcurementId procurementId, CancellationToken ct)
    {
        return Range(await this.dbContext.PJp005S.AsNoTracking()
                               .Where(j => !j.IsDeleted && j.ProcurementId == procurementId)
                               .Select(j => new ValueTuple<DateTimeOffset?, DateTimeOffset?>(j.AuditInfo.CreatedAt, j.AuditInfo.LastModifiedAt))
                               .ToListAsync(ct));
    }

    private async ValueTask<(DateOnly? Start, DateOnly? End)> GetInviteRange(ProcurementId procurementId, CancellationToken ct)
    {
        return Range(await this.dbContext.PInvites.AsNoTracking()
                               .Where(i => !i.IsDeleted && i.ProcurementId == procurementId)
                               .Select(i => new ValueTuple<DateTimeOffset?, DateTimeOffset?>(i.AuditInfo.CreatedAt, i.AuditInfo.LastModifiedAt))
                               .ToListAsync(ct));
    }

    private async ValueTask<(DateOnly? Start, DateOnly? End)> GetJp006Range(ProcurementId procurementId, CancellationToken ct)
    {
        return Range(await this.dbContext.PJp006S.AsNoTracking()
                               .Where(o => !o.IsDeleted && o.ProcurementId == procurementId)
                               .Select(o => new ValueTuple<DateTimeOffset?, DateTimeOffset?>(o.AuditInfo.CreatedAt, o.AuditInfo.LastModifiedAt))
                               .ToListAsync(ct));
    }

    private async ValueTask<(DateOnly? Start, DateOnly? End)> GetPurchaseOrderApprovalRange(ProcurementId procurementId, CancellationToken ct)
    {
        return Range(await this.dbContext.PPurchaseOrderApprovals.AsNoTracking()
                               .Where(a => !a.IsDeleted && a.ProcurementId == procurementId)
                               .Select(a => new ValueTuple<DateTimeOffset?, DateTimeOffset?>(a.AuditInfo.CreatedAt, a.AuditInfo.LastModifiedAt))
                               .ToListAsync(ct));
    }

    private async ValueTask<(DateOnly? Start, DateOnly? End)> GetCaContractInvitationRange(ProcurementId procurementId, CancellationToken ct)
    {
        return Range(await this.dbContext.CaContractInvitations.AsNoTracking()
                               .Where(ci => !ci.IsDeleted && ci.ProcurementId == procurementId)
                               .Select(ci => new ValueTuple<DateTimeOffset?, DateTimeOffset?>(ci.AuditInfo.CreatedAt, ci.AuditInfo.LastModifiedAt))
                               .ToListAsync(ct));
    }

    private async ValueTask<(DateOnly? Start, DateOnly? End)> GetCaContractDraftRange(ProcurementId procurementId, CancellationToken ct)
    {
        return Range(await this.dbContext.CaContractDrafts.AsNoTracking()
                               .Where(cd => !cd.IsDeleted && cd.ProcurementId == procurementId)
                               .Select(cd => new ValueTuple<DateTimeOffset?, DateTimeOffset?>(cd.AuditInfo.CreatedAt, cd.AuditInfo.LastModifiedAt))
                               .ToListAsync(ct));
    }

    private async ValueTask<(DateOnly? Start, DateOnly? End)> GetCmDeliveryAcceptanceRange(CancellationToken ct)
    {
        return Range(await this.dbContext.CmDeliveryAcceptances.AsNoTracking()
                               .Where(c => !c.IsDeleted)
                               .Select(c => new ValueTuple<DateTimeOffset?, DateTimeOffset?>(c.AuditInfo.CreatedAt, c.AuditInfo.LastModifiedAt))
                               .ToListAsync(ct));
    }

    private async ValueTask<(DateOnly? Start, DateOnly? End)> GetCmContractGuaranteeReturnRange(ProcurementId procurementId, CancellationToken ct)
    {
        return Range(await this.dbContext.CmContractGuaranteeReturns.AsNoTracking()
                               .Where(r => !r.IsDeleted && r.CaContractDraftVendor.ContractDraft.ProcurementId == procurementId)
                               .Select(r => new ValueTuple<DateTimeOffset?, DateTimeOffset?>(r.AuditInfo.CreatedAt, r.AuditInfo.LastModifiedAt))
                               .ToListAsync(ct));
    }

    private static DateOnly? AsDate(DateTimeOffset? dto) => dto.HasValue ? DateOnly.FromDateTime(dto.Value.UtcDateTime) : null;

    private static (DateOnly? Start, DateOnly? End) Range(IEnumerable<(DateTimeOffset? Created, DateTimeOffset? Modified)> rows)
    {
        var list = rows.Where(r => r.Created.HasValue || r.Modified.HasValue).ToList();

        if (list.Count == 0)
        {
            return (null, null);
        }

        var start = list.Min(r => r.Created ?? r.Modified);
        var end = list.Max(r => r.Modified ?? r.Created);

        return (AsDate(start), AsDate(end));
    }

    private async ValueTask<(DateOnly? PlanDraftStart, DateOnly? PlanDraftEnd, DateOnly? PlanAnnStart, DateOnly? PlanAnnEnd)> GetPlanDateRangesAsync(
        Guid? planId,
        CancellationToken ct)
    {
        if (!planId.HasValue)
        {
            return (null, null, null, null);
        }

        var planIdDomain = PlanId.From(planId.Value);
        var planRangeTuple = await this.dbContext.Plans.AsNoTracking()
            .Where(x => x.Id == planIdDomain)
            .Select(x => new ValueTuple<DateTimeOffset?, DateTimeOffset?>(x.AuditInfo.CreatedAt, x.AuditInfo.LastModifiedAt))
            .FirstOrDefaultAsync(ct);

        DateOnly? planDraftStart = null, planDraftEnd = null;
        if (planRangeTuple != default)
        {
            planDraftStart = AsDate(planRangeTuple.Item1);
            planDraftEnd = AsDate(planRangeTuple.Item2 ?? planRangeTuple.Item1);
        }

        var planAnnMeta = await this.dbContext.PlanAnnouncementSelecteds.AsNoTracking()
            .Where(a => a.PlanId == planIdDomain)
            .Select(a => new { a.AuditInfo.CreatedAt, a.AuditInfo.LastModifiedAt })
            .FirstOrDefaultAsync(ct);

        DateOnly? planAnnStart = null, planAnnEnd = null;
        if (planAnnMeta != null)
        {
            planAnnStart = AsDate(planAnnMeta.CreatedAt);
            planAnnEnd = AsDate(planAnnMeta.LastModifiedAt ?? planAnnMeta.CreatedAt);
        }

        return (planDraftStart, planDraftEnd, planAnnStart, planAnnEnd);
    }

    private async ValueTask<TimelinePhase[]> CreateTimelinePhasesAsync(
        ProcurementId procurementId,
        DateOnly? planDraftStart,
        DateOnly? planDraftEnd,
        DateOnly? planAnnStart,
        DateOnly? planAnnEnd,
        CancellationToken ct)
    {
        var appointRange = await this.GetAppointRange(procurementId, ct);
        var torRange = await this.GetTorDraftsRange(procurementId, ct);
        var medianRange = await this.GetMedianPricesRange(procurementId, ct);
        var jp05Range = await this.GetJp005Range(procurementId, ct);
        var inviteRange = await this.GetInviteRange(procurementId, ct);
        var poRange = await this.GetJp006Range(procurementId, ct);
        var poAppRange = await this.GetPurchaseOrderApprovalRange(procurementId, ct);
        var caInviteRange = await this.GetCaContractInvitationRange(procurementId, ct);
        var caDraftRange = await this.GetCaContractDraftRange(procurementId, ct);
        var deliveryRange = await this.GetCmDeliveryAcceptanceRange(ct);
        var guaranteeRange = await this.GetCmContractGuaranteeReturnRange(procurementId, ct);

        TimelineTask T(string code, string name, (DateOnly? Start, DateOnly? End) rng) => new(code, name, rng.Start, rng.End);

        var phasePlan = new TimelinePhase("PH_PLAN", "รายการจัดซื้อจัดจ้าง", new[]
        {
            new TimelineTask("PLAN_DRAFT", "จัดทำร่างแผนจัดซื้อจัดจ้าง", planDraftStart, planDraftEnd),
            new TimelineTask("PLAN_ANNOUNCE", "ประกาศเผยแพร่แผน", planAnnStart, planAnnEnd),
        });

        var phasePre = new TimelinePhase("PH_PRE", "Pre-Procurement", new[]
        {
            T("PRE_APPOINT", "ขอแต่งตั้งบุคคล/คกก.TOR/ราคากลาง", appointRange),
            T("PRE_TOR", "จัดทำร่างขอบเขตงาน (TOR)", torRange),
            T("PRE_BENCH", "กำหนดราคากลาง", medianRange),
            new TimelineTask("PRE_INFO", "การแจ้งข้อมูลเบื้องต้น (พง.004)", null, null),
        });

        var phaseProc = new TimelinePhase("PH_PROC", "Procurement", new[]
        {
            T("PROC_REQREP", "จัดทำรายงานขอซื้อจ้าง (จพ.005)", jp05Range),
            T("PROC_INVITE", "จัดทำหนังสือเชิญชวนผู้ประกอบการ", inviteRange),
            T("PROC_EVALAP", "จัดทำรายงานผลฯ และขออนุมัติสั่งซื้อ/จ้าง (จพ.006)", poRange),
            T("PROC_AWARD", "อนุมัติผลสั่งซื้อ/จ้าง/เช่า และแจ้งทำสัญญา", poAppRange),
        });

        var phaseCa = new TimelinePhase("PH_CA", "Contract Agreement", new[]
        {
            T("CA_INVITE", "เชิญชวนทำสัญญา", caInviteRange),
            T("CA_DRAFT", "ร่างข้อมูลใบสั่ง/สัญญา", caDraftRange),
        });

        var phaseCm = new TimelinePhase("PH_CM", "Contract Management", new[]
        {
            T("CM_PROGRESS", "บันทึกส่งมอบและตรวจรับ และตั้งหนี้ตั้งเบิก", deliveryRange),
            T("CM_RETURN", "คืนหลักประกันสัญญา", guaranteeRange),
        });

        return new[] { phasePlan, phasePre, phaseProc, phaseCa, phaseCm };
    }

    private (List<string> Axis, List<TimelinePhase> PhasesWithIndex) CalculateAxisAndIndexMapping(
        TimelinePhase[] phases,
        int year)
    {
        var allDates = phases.SelectMany(ph => ph.Tasks)
            .SelectMany(t => new[] { t.ActualStart, t.ActualEnd })
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();

        var spanStart = allDates.Count > 0 ? allDates.Min() : new DateOnly(year, 1, 1);
        var spanEnd = allDates.Count > 0 ? allDates.Max() : new DateOnly(year, 12, 31);
        var axis = BuildAxisMonths(spanStart, spanEnd).ToList();

        var axisStartMonth = new DateOnly(spanStart.Year, spanStart.Month, 1);
        var phasesWithIdx = phases.Select(ph => this.MapPhaseToMonthIndices(ph, axisStartMonth, axis.Count))
            .ToList();

        return (axis, phasesWithIdx);
    }

    private TimelinePhase MapPhaseToMonthIndices(
        TimelinePhase phase,
        DateOnly axisStartMonth,
        int axisCount)
    {
        var tasksWithIndices = phase.Tasks.Select(t => this.MapTaskToMonthIndices(t, axisStartMonth, axisCount))
            .ToList();

        return new TimelinePhase(phase.Code, phase.Name, tasksWithIndices);
    }

    private TimelineTask MapTaskToMonthIndices(
        TimelineTask task,
        DateOnly axisStartMonth,
        int axisCount)
    {
        int? startIndex = null;
        int? endIndex = null;

        if (task.ActualStart.HasValue)
        {
            var startMonth = new DateOnly(task.ActualStart.Value.Year, task.ActualStart.Value.Month, 1);
            var monthDiff = CalculateMonthDifference(startMonth, axisStartMonth);
            startIndex = ClampToAxisRange(monthDiff, axisCount);
        }

        if (task.ActualEnd.HasValue)
        {
            var endMonth = new DateOnly(task.ActualEnd.Value.Year, task.ActualEnd.Value.Month, 1);
            var monthDiff = CalculateMonthDifference(endMonth, axisStartMonth);
            endIndex = ClampToAxisRange(monthDiff, axisCount);
        }

        return new TimelineTask(task.Code, task.Name, task.ActualStart, task.ActualEnd, startIndex, endIndex);
    }

    private static int CalculateMonthDifference(DateOnly a, DateOnly b)
        => ((a.Year - b.Year) * 12) + (a.Month - b.Month);

    private static int? ClampToAxisRange(int monthDiff, int axisCount)
    {
        if (monthDiff < 0 || monthDiff >= axisCount)
        {
            return null;
        }

        return monthDiff;
    }

    private static IEnumerable<string> BuildAxisMonths(DateOnly start, DateOnly end)
    {
        var thShort = new[] { "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค." };
        var s = new DateOnly(start.Year, start.Month, 1);
        var e = new DateOnly(end.Year, end.Month, 1);
        var labels = new List<string>();

        for (var cur = s; cur <= e; cur = cur.AddMonths(1))
        {
            var be = ((cur.Year + 543) % 100).ToString("00", CultureInfo.InvariantCulture);
            labels.Add($"{thShort[cur.Month - 1]} {be}");
        }

        // Pad forward to ensure 12 months total
        var last = e;

        while (labels.Count < 12)
        {
            last = last.AddMonths(1);
            var be = ((last.Year + 543) % 100).ToString("00", CultureInfo.InvariantCulture);
            labels.Add($"{thShort[last.Month - 1]} {be}");
        }

        return labels;
    }
}