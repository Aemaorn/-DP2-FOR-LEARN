namespace GHB.DP2.Application.Features.Procurement.Procurement;

using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Linq;
using GHB.DP2.Application.Common;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Application.Extensions;

public record ExportExcelProcurementRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    int PageNumber,
    int PageSize,
    string? Keyword,
    string? DepartmentCode,
    int? BudgetYear,
    string? SupplyMethodCode,
    string? SupplyMethodTypeCode,
    string? SupplyMethodSpecialTypeCode,
    ProcurementStep? Step,
    ProcurementStatus? Status,
    ProcurementType ProcurementType,
    string? RentTypeCode,
    WorkProcess? WorkProcess);

public record ExportExcelProcurementDto(
    string Key,
    string? RefNo,
    string? ProcurementNumber,
    string? Name,
    decimal? Budget,
    string? Type,
    string? DepartmentName,
    string? SupplyMethod,
    string? SupplyMethodType,
    string? SupplyMethodSpecialType,
    string? ProcessType,
    string? Status);

public class ExportExcelProcurementEndpoint : Endpoint<ExportExcelProcurementRequest>
{
    private readonly Dp2DbContext dbContext;

    public ExportExcelProcurementEndpoint(Dp2DbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(builder =>
            builder.WithTags(nameof(Plan))
                   .ProducesProblem(StatusCodes.Status404NotFound)
                   .ProducesProblem(StatusCodes.Status500InternalServerError));
        this.Get("procurement/export-procurement");
    }

    public override async Task HandleAsync(ExportExcelProcurementRequest req, CancellationToken ct)
    {
        using var stream = new MemoryStream();

        var colWidths = new[] { 10d, 20d, 20d, 20d, 120d, 70, 60d, 60d, 60d, 60d, 60d };

        var cellStyle =
            new
            {
                header = 2u,
                normal = 3u,
                number2 = 5u,
                center = 7u,
            };

        using var excelDocument =
            ExportExcel.Create(stream)
                   .AddSheet(
                       "ข้อมูลการจัดซื้อจัดจ้าง",
                       colWidths,
                       1)
                   .HideColumns(2)
                   .RowStyled(
                       ("ลำดับที่", cellStyle.header),
                       ("Key", cellStyle.header),
                       ("เลขที่จัดซื้อจัดจ้าง", cellStyle.header),
                       ("Ref.No", cellStyle.header),
                       ("ชื่อโครงการ", cellStyle.header),
                       ("วงเงินงบประมาณ (บาท)", cellStyle.header),
                       ("ประเภทแผน", cellStyle.header),
                       ("ชื่อฝ่าย", cellStyle.header),
                       ("วิธีจัดหา", cellStyle.header),
                       ("ขั้นตอน", cellStyle.header),
                       ("สถานะ", cellStyle.header));

        var rowData = await GetRowsData(ct);

        if (rowData.Any())
        {
            var index = 1;

            foreach (var r in rowData)
            {
                excelDocument.RowStyled(
                    (index, cellStyle.center),
                    (r.Key, cellStyle.center),
                    (r.RefNo ?? string.Empty, cellStyle.center),
                    (r.ProcurementNumber ?? string.Empty, cellStyle.center),
                    (r.Name ?? string.Empty, cellStyle.normal),
                    (r.Budget ?? 0, cellStyle.number2),
                    (r.Type ?? string.Empty, cellStyle.normal),
                    (r.DepartmentName ?? string.Empty, cellStyle.normal),
                    (string.Format("{0}: {1}", r.SupplyMethod ?? string.Empty, r.SupplyMethodSpecialType ?? string.Empty), cellStyle.normal),
                    (r.ProcessType ?? string.Empty, cellStyle.normal),
                    (r.Status ?? string.Empty, cellStyle.normal));
                index++;
            }
        }
        else
        {
            excelDocument.RowStyled(("ไม่พบข้อมูล", cellStyle.center))
                         .Merge("A2:K2");
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

        async Task<List<ExportExcelProcurementDto>> GetRowsData(CancellationToken ct)
        {
            var (userPrimaryDepartmentId, isJorPor) = await this.GetUserContextAsync(req.UserId, ct);
            var baseQuery = this.BuildFilterQuery(req, userPrimaryDepartmentId, isJorPor);

            var paginatedQuery = baseQuery.WhereIfTrue(req.Step.HasValue, x => x.Step == req.Step);

            var list = await paginatedQuery.ToListAsync(ct);
            var data = list.Select(this.MapToProcurementResponse).ToList();

            return data;
        }
    }

    private static readonly Dictionary<ProcessType, string> ProcessTypeMap = new()
    {
        { ProcessType.Appoint, "ขอแต่งตั้งบุคลล/คกก." },
        { ProcessType.TorDraft, "ร่างขอบเขตงาน" },
        { ProcessType.MedianPrice, "กำหนดราคากลาง" },
        { ProcessType.PurchaseRequisition, "จพ.004" },
        { ProcessType.Jp005, "จพ.005" },
        { ProcessType.Invite, "เชิญชวนผู้ประกอบการ" },
        { ProcessType.PurchaseOrder, "จพ.006" },
        { ProcessType.PurchaseOrderApproval, "อนุมัติใบสั่ง/แจ้งทำสัญญา" },
        { ProcessType.ContractInvitation, "เชิญชวนทำสัญญา" },
        { ProcessType.ContractDraft, "ร่างสัญญา" },
    };

    private static readonly Dictionary<ProcurementStatus, string> ProcurementStatusTypeMap = new()
    {
        { ProcurementStatus.Cancelled, "ยกเลิกรายการ" },
        { ProcurementStatus.Draft, "แบบร่าง" },
        { ProcurementStatus.InProgress, "กำลังดำเนินการ" },
        { ProcurementStatus.Completed, "ดำเนินการแล้วเสร็จ" },
    };

    private ExportExcelProcurementDto MapToProcurementResponse(Procurement x)
    {
        return new ExportExcelProcurementDto(
            x.Id.Value.ToString(),
            x.ProcurementNumber?.Value ?? string.Empty,
            ProcurementNumber: x.Plan?.PlanNumber == null ? string.Empty : x.Plan.PlanNumber.Value.ToString(),
            Name: x.Name ?? string.Empty,
            Budget: x.Budget ?? 0,
            Type: x.Plan == null ? string.Empty : x.Plan.Type == PlanType.InYearPlan ? "แผนระหว่างปี" : "แผนรวมปี",
            DepartmentName: x.Department?.Name ?? string.Empty,
            SupplyMethod: x.SupplyMethod?.Label ?? string.Empty,
            SupplyMethodType: x.SupplyMethodType?.Label ?? string.Empty,
            SupplyMethodSpecialType: x.SupplyMethodSpecialType?.Label ?? string.Empty,
            ProcessType: ProcessTypeMap.ContainsKey(x.ProcessType) ? ProcessTypeMap[x.ProcessType] : x.ProcessType.ToString(),
            Status: ProcurementStatusTypeMap.ContainsKey(x.Status) ? ProcurementStatusTypeMap[x.Status] : x.Status.ToString());
    }

    private IQueryable<Procurement> BuildFilterQuery(ExportExcelProcurementRequest req, BusinessUnitId? userPrimaryDepartmentId = null, bool isJorPor = false)
    {
        var userId = UserId.From(req.UserId);
        var query = this.dbContext.Procurements
                        .AsNoTracking()
                        .Include(p => p.Plan)
                        .Include(p => p.Department)
                        .Include(p => p.SupplyMethod)
                        .Include(p => p.PrincipleApprovals)
                        .ThenInclude(p => p.RentTypeCodeInfo)
                        .Where(x => x.Type == req.ProcurementType);

        query = this.ApplyWorkProcessFilter(query, req.WorkProcess, userId, userPrimaryDepartmentId, isJorPor);
        query = ApplyRemainingFilters(query, req);

        return query.OrderByDescending(o => o.AuditInfo.LastModifiedAt != null ? o.AuditInfo.LastModifiedAt : o.AuditInfo.CreatedAt);
    }

    private async Task<(BusinessUnitId? UserPrimaryDepartmentId, bool IsJorPor)> GetUserContextAsync(Guid userId, CancellationToken ct)
    {
        var userIdObj = UserId.From(userId);
        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .ThenInclude(e => e.Positions)
                             .ThenInclude(p => p.BusinessUnit)
                             .FirstOrDefaultAsync(u => u.Id == userIdObj, ct);

        if (user?.Employee == null)
        {
            return (null, false);
        }

        return (user.Employee.PrimaryBusinessUnit?.Id, user.Employee.IsJorPor);
    }

    private static Expression<Func<Procurement, bool>> HasCurrentAppointAcceptor(UserId userId)
    {
        return x => x.Appoints.Any(a => a.Acceptors.Any(ac => ac.IsCurrent && ac.UserId == userId));
    }

    private static Expression<Func<Procurement, bool>> HasCurrentTorDraftAcceptor(UserId userId)
    {
        return x => x.TorDrafts.Any(td => td.PpTorDraftAcceptors.Any(ac => ac.IsCurrent && ac.UserId == userId));
    }

    private static Expression<Func<Procurement, bool>> HasCurrentMedianPriceAcceptor(UserId userId)
    {
        return x => x.MedianPrices.Any(mp => mp.Acceptors.Any(ac => ac.IsCurrent && ac.UserId == userId));
    }

    private static Expression<Func<Procurement, bool>> HasCurrentPurchaseRequisitionAcceptor(UserId userId)
    {
        return x => x.PurchaseRequisitions.Any(rq => rq.Acceptors.Any(ac => ac.IsCurrent && ac.UserId == userId));
    }

    private static Expression<Func<Procurement, bool>> HasTorDraftAssigneeAccess(UserId userId)
    {
        return x => x.TorDrafts.Any(td =>
            (td.Status == TorDraftStatus.WaitingAssign && td.Assignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Director)) ||
            (td.Status == TorDraftStatus.WaitingComment && td.Assignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Assignee)));
    }

    private static Expression<Func<Procurement, bool>> HasMedianPriceAssigneeAccess(UserId userId)
    {
        return x => x.MedianPrices.Any(mp =>
            ((mp.Status == MedianPriceStatus.WaitingAssign || mp.Status == MedianPriceStatus.RejectToAssignee) &&
             mp.Assignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Director)) ||
            ((mp.Status == MedianPriceStatus.WaitingComment || mp.Status == MedianPriceStatus.RejectToAssignee) &&
             mp.Assignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Assignee)));
    }

    private static Expression<Func<Procurement, bool>> HasPurchaseRequisitionAssigneeAccess(UserId userId)
    {
        return x => x.PurchaseRequisitions.Any(pr =>
            pr.Status == PurchaseRequisitionStatus.WaitingAssign &&
            pr.Assignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Director));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedAppointHistory(UserId userId)
    {
        return x => x.Appoints.Any(a => a.Acceptors.Any(ac =>
            !ac.IsCurrent &&
            ac.UserId == userId &&
            (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected) &&
            x.ProcessType == ProcessType.Appoint));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedTorDraftHistory(UserId userId)
    {
        return x => x.TorDrafts.Any(td => td.PpTorDraftAcceptors.Any(ac =>
            !ac.IsCurrent &&
            ac.UserId == userId &&
            (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected) &&
            x.ProcessType == ProcessType.TorDraft));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedMedianPriceHistory(UserId userId)
    {
        return x => x.MedianPrices.Any(mp => mp.Acceptors.Any(ac =>
            !ac.IsCurrent &&
            ac.UserId == userId &&
            (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected) &&
            x.ProcessType == ProcessType.MedianPrice));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedPurchaseRequisitionHistory(UserId userId)
    {
        return x => x.PurchaseRequisitions.Any(rq => rq.Acceptors.Any(ac =>
            !ac.IsCurrent &&
            ac.UserId == userId &&
            (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected) &&
            x.ProcessType == ProcessType.PurchaseRequisition));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedTorDraftAssigneeHistory(UserId userId)
    {
        return x => x.TorDrafts.Any(td =>
            ((td.Status == TorDraftStatus.WaitingComment &&
              td.Assignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Director && x.ProcessType == ProcessType.TorDraft)) ||
             (td.Status == TorDraftStatus.WaitingApproval &&
              td.Assignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Assignee && x.ProcessType == ProcessType.TorDraft))));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedMedianPriceAssigneeHistory(UserId userId)
    {
        return x => x.MedianPrices.Any(mp =>
            (((mp.Status == MedianPriceStatus.WaitingAssign || mp.Status == MedianPriceStatus.RejectToAssignee) &&
              mp.Assignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Director && x.ProcessType == ProcessType.MedianPrice)) ||
             ((mp.Status == MedianPriceStatus.WaitingComment || mp.Status == MedianPriceStatus.RejectToAssignee) &&
              mp.Assignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Assignee && x.ProcessType == ProcessType.MedianPrice))));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedPurchaseRequisitionAssigneeHistory(UserId userId)
    {
        return x => x.PurchaseRequisitions.Any(pr =>
            pr.Status == PurchaseRequisitionStatus.Approved &&
            pr.Assignees.Any(a => a.UserId == userId && x.ProcessType == ProcessType.PurchaseRequisition));
    }

    private IQueryable<Procurement> ApplyWorkProcessFilter(IQueryable<Procurement> query, WorkProcess? workProcess, UserId userId, BusinessUnitId? userPrimaryDepartmentId, bool isJorPor)
    {
        return workProcess switch
        {
            WorkProcess.InProcess => this.ApplyInProcessFilter(query, userId, userPrimaryDepartmentId, isJorPor),
            WorkProcess.Related => this.ApplyRelatedFilter(query, userId),
            WorkProcess.Completed => ApplyCompletedFilter(query, userId),
            _ => query,
        };
    }

    private IQueryable<Procurement> ApplyInProcessFilter(IQueryable<Procurement> query, UserId userId, BusinessUnitId? userPrimaryDepartmentId, bool isJorPor)
    {
        var expr = IsInProcessPreProcurement(userId, userPrimaryDepartmentId, isJorPor)
                       .Or(IsInProcessProcurement(userId, isJorPor))
                       .Or(IsInProcessContractAgreement(userId));

        return query.Where(expr);
    }

    private static Expression<Func<Procurement, bool>> IsInProcessPreProcurement(UserId userId, BusinessUnitId? userPrimaryDepartmentId, bool isJorPor)
    {
        Expression<Func<Procurement, bool>> stepCheck = x => x.Step == ProcurementStep.PreProcurement;

        return stepCheck
               .And(HasCurrentAcceptorRole(userId))
               .Or(HasMedianPriceCommitteeAccess(userId))
               .Or(HasPurchaseRequisitionAccess(userPrimaryDepartmentId, isJorPor))
               .Or(HasPrincipleApprovalAccess(userId))
               .Or(HasPrincipleApprovalRentalAccess(userId))
               .Or(HasAssigneeAccess(userId));
    }

    private static Expression<Func<Procurement, bool>> IsInProcessProcurement(UserId userId, bool isJorPor)
    {
        Expression<Func<Procurement, bool>> stepCheck = x => x.Step == ProcurementStep.Procurement;

        return stepCheck
               .And(HasProcurementAcceptorRole(userId))
               .Or(HasJp005Access(userId))
               .Or(HasInviteAccess(userId))
               .Or(HasPurchaseOrderAccess(userId))
               .Or(HasPurchaseOrderApprovalAccess(userId, isJorPor))
               .Or(HasProcurementAssigneeAccess(userId))
               .Or(HasPrincipleApprovalProcurementAccess(userId))
               .Or(HasPrincipleApprovalRentalProcurementAccess(userId))
               .Or(HasTorDraftProcurementAccess(userId));
    }

    private static Expression<Func<Procurement, bool>> IsInProcessContractAgreement(UserId userId)
    {
        Expression<Func<Procurement, bool>> stepCheck = x => x.Step == ProcurementStep.ContractAgreement;

        return stepCheck
               .And(HasContractInvitationAccess(userId))
               .Or(HasContractDraftAccess(userId));
    }

    public static Expression<Func<Procurement, bool>> HasCurrentAcceptorRole(UserId userId)
    {
        return HasCurrentAppointAcceptor(userId)
                   .Or(HasCurrentTorDraftAcceptor(userId))
                   .Or(HasCurrentMedianPriceAcceptor(userId))
                   .Or(HasCurrentPurchaseRequisitionAcceptor(userId));
    }

    private static Expression<Func<Procurement, bool>> HasMedianPriceCommitteeAccess(UserId userId)
    {
        return x => x.Appoints.Any(a => a.MedianPriceCommittees.Any(c => c.SuUserId == userId)) &&
                    x.ProcessType == ProcessType.MedianPrice &&
                    (!x.MedianPrices.Any() || x.MedianPrices.Any(mp =>
                        mp.Status == MedianPriceStatus.Draft ||
                        mp.Status == MedianPriceStatus.Rejected ||
                        mp.Status == MedianPriceStatus.RejectToAssignee));
    }

    private static Expression<Func<Procurement, bool>> HasPurchaseRequisitionAccess(BusinessUnitId? userPrimaryDepartmentId, bool isJorPor)
    {
        return x => x.ProcessType == ProcessType.PurchaseRequisition &&
                    (!x.PurchaseRequisitions.Any() || x.PurchaseRequisitions.Any(pr =>
                        pr.Status == PurchaseRequisitionStatus.Draft ||
                        pr.Status == PurchaseRequisitionStatus.Rejected)) &&
                    (isJorPor || userPrimaryDepartmentId == null || x.DepartmentId == userPrimaryDepartmentId);
    }

    private static Expression<Func<Procurement, bool>> HasPrincipleApprovalAccess(UserId userId)
    {
        return x => x.ProcessType == ProcessType.PrincipleApproval &&
                    (!x.PrincipleApprovals.Any() || x.PrincipleApprovals.Any(pa =>
                        pa.Status == PPrincipleApprovalStatus.Draft ||
                        pa.Status == PPrincipleApprovalStatus.Rejected)) &&
                    x.PrincipleApprovals.Any(pa => pa.PrincipleApprovalAcceptors.Any(ac => ac.UserId == userId));
    }

    private static Expression<Func<Procurement, bool>> HasPrincipleApprovalRentalAccess(UserId userId)
    {
        return x => x.ProcessType == ProcessType.PrincipleApprovalRental &&
                    (!x.PrincipleApprovalRentals.Any() || x.PrincipleApprovalRentals.Any(pa =>
                        pa.Status == PPrincipleApprovalRentalStatus.Draft ||
                        pa.Status == PPrincipleApprovalRentalStatus.Rejected)) &&
                    x.PrincipleApprovalRentals.Any(pa => pa.Acceptors.Any(ac => ac.UserId == userId));
    }

    private static Expression<Func<Procurement, bool>> HasAssigneeAccess(UserId userId)
    {
        return HasTorDraftAssigneeAccess(userId)
                   .Or(HasMedianPriceAssigneeAccess(userId))
                   .Or(HasPurchaseRequisitionAssigneeAccess(userId));
    }

    private static Expression<Func<Procurement, bool>> HasProcurementAcceptorRole(UserId userId)
    {
        return x => x.Jp005.Any(mp => mp.Acceptors.Any(ac => ac.IsCurrent && ac.UserId == userId)) ||
                    x.Invites.Any(inv => inv.Acceptors.Any(ac => ac.IsCurrent && ac.UserId == userId)) ||
                    x.PurchaseOrder.Any(po => po.Acceptors.Any(ac => ac.IsCurrent && ac.UserId == userId)) ||
                    x.PurchaseOrderApprovals.Any(poa => poa.Acceptors.Any(ac => ac.IsCurrent && ac.UserId == userId));
    }

    private static Expression<Func<Procurement, bool>> HasJp005Access(UserId userId)
    {
        return x => x.PurchaseRequisitions.Any(a => a.Assignees.Any(c => c.UserId == userId && c.Type == AssigneeType.Assignee)) &&
                    x.ProcessType == ProcessType.Jp005 &&
                    (!x.Jp005.Any() || x.Jp005.Any(mp =>
                        mp.Status == PJp005Status.Draft ||
                        mp.Status == PJp005Status.Rejected));
    }

    private static Expression<Func<Procurement, bool>> HasInviteAccess(UserId userId)
    {
        return x => x.Jp005.Any(a => a.Committees.Any(c => c.SuUserId == userId) ||
                                     x.PurchaseRequisitions.Any(a => a.Assignees.Any(c => c.UserId == userId && c.Type == AssigneeType.Assignee))) &&
                    x.ProcessType == ProcessType.Invite &&
                    (!x.Invites.Any() || x.Invites.Any(td =>
                        td.Status == PInviteStatus.Draft ||
                        td.Status == PInviteStatus.Rejected));
    }

    private static Expression<Func<Procurement, bool>> HasPurchaseOrderAccess(UserId userId)
    {
        return x => x.Jp005.Any(a => a.Committees.Any(c => c.SuUserId == userId) ||
                                     x.PurchaseRequisitions.Any(a => a.Assignees.Any(c => c.UserId == userId && c.Type == AssigneeType.Assignee))) &&
                    x.ProcessType == ProcessType.PurchaseOrder &&
                    (!x.PurchaseOrder.Any() || x.PurchaseOrder.Any(td =>
                        td.Status == PurchaseOrderStatus.Draft ||
                        td.Status == PurchaseOrderStatus.Rejected));
    }

    private static Expression<Func<Procurement, bool>> HasPurchaseOrderApprovalAccess(UserId userId, bool isJorPor)
    {
        return x => (isJorPor || x.Jp005.Any(a => a.Committees.Any(c => c.SuUserId == userId))) &&
                    x.ProcessType == ProcessType.PurchaseOrderApproval &&
                    (!x.PurchaseOrderApprovals.Any() || x.PurchaseOrderApprovals.Any(td =>
                        td.Status == PurchaseOrderApprovalStatus.Draft ||
                        td.Status == PurchaseOrderApprovalStatus.Rejected));
    }

    private static Expression<Func<Procurement, bool>> HasProcurementAssigneeAccess(UserId userId)
    {
        return x => x.PurchaseOrderApprovals.Any(td => td.Status == PurchaseOrderApprovalStatus.WaitingAssign &&
                                                       td.Assignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Director));
    }

    private static Expression<Func<Procurement, bool>> HasPrincipleApprovalProcurementAccess(UserId userId)
    {
        return x => x.PrincipleApprovals.Any(pa => pa.PrincipleApprovalAcceptors.Any(ac => ac.IsCurrent && ac.UserId == userId)) ||
                    x.PrincipleApprovals.Any(pa => pa.Status == PPrincipleApprovalStatus.WaitingAssign &&
                                                   pa.PrincipleApprovalAssignees.Any(ac => ac.Type == AssigneeType.Director)) ||
                    x.PrincipleApprovals.Any(pa => pa.Status == PPrincipleApprovalStatus.WaitingComment &&
                                                   pa.PrincipleApprovalAssignees.Any(ac => ac.Type == AssigneeType.Assignee));
    }

    private static Expression<Func<Procurement, bool>> HasPrincipleApprovalRentalProcurementAccess(UserId userId)
    {
        return x => x.PrincipleApprovalRentals.Any(par => par.Acceptors.Any(ac => ac.IsCurrent && ac.UserId == userId)) ||
                    x.PrincipleApprovalRentals.Any(pa => pa.Status == PPrincipleApprovalRentalStatus.WaitingAssign &&
                                                         pa.Assignees.Any(ac => ac.Type == AssigneeType.Director)) ||
                    x.PrincipleApprovalRentals.Any(pa => pa.Status == PPrincipleApprovalRentalStatus.WaitingContractAssign &&
                                                         pa.Assignees.Any(ac => ac.Type == AssigneeType.Director && ac.Group == AssigneeGroup.Contract));
    }

    private static Expression<Func<Procurement, bool>> HasTorDraftProcurementAccess(UserId userId)
    {
        return x => x.Appoints.Any(a => a.TorDraftCommittees.Any(c => c.SuUserId == userId)) &&
                    x.ProcessType == ProcessType.TorDraft &&
                    (!x.TorDrafts.Any() || x.TorDrafts.Any(td =>
                        td.Status == TorDraftStatus.Draft ||
                        td.Status == TorDraftStatus.Rejected));
    }

    private static Expression<Func<Procurement, bool>> HasContractInvitationAccess(UserId userId)
    {
        return x => x.ContractInvitations.Any(ci => ci.Acceptors.Any(ac => ac.IsCurrent && ac.UserId == userId)) ||
                    (x.PurchaseOrderApprovals.Any(a => a.Assignees.Any(c => c.UserId == userId && c.Type == AssigneeType.Assignee)) &&
                     x.ProcessType == ProcessType.ContractInvitation &&
                     (!x.ContractInvitations.Any() || x.ContractInvitations.Any(mp =>
                         mp.Status == ContractInvitationStatus.Draft ||
                         mp.Status == ContractInvitationStatus.Rejected)));
    }

    private static Expression<Func<Procurement, bool>> HasContractDraftAccess(UserId userId)
    {
        return x => (x.PurchaseOrderApprovals.Any(a => a.Assignees.Any(c => c.UserId == userId && c.Type == AssigneeType.Assignee)) ||
                     x.ContractDrafts.Any(cd => cd.Vendors.Any(v => v.Acceptors.Any(ac => ac.IsCurrent && ac.UserId == userId)))) &&
                    x.ProcessType == ProcessType.ContractDraft &&
                    (!x.ContractDrafts.Any() || x.ContractDrafts.Any(cd => cd.Vendors.Any(v =>
                        v.Status == ContractDraftVendorStatus.Pending ||
                        v.Status == ContractDraftVendorStatus.Rejected)));
    }

    private IQueryable<Procurement> ApplyRelatedFilter(IQueryable<Procurement> query, UserId userId)
    {
        var expression = IsRelatedPreProcurement(userId)
                             .Or(IsRelatedProcurement(userId))
                             .Or(IsRelatedContractAgreement(userId));

        return query.Where(expression);
    }

    private static Expression<Func<Procurement, bool>> IsRelatedPreProcurement(UserId userId)
    {
        Expression<Func<Procurement, bool>> stepCheck = x => x.Step == ProcurementStep.PreProcurement;

        return stepCheck
               .And(HasRelatedAcceptorHistory(userId))
               .Or(HasRelatedAssigneeHistory(userId));
    }

    private static Expression<Func<Procurement, bool>> IsRelatedProcurement(UserId userId)
    {
        Expression<Func<Procurement, bool>> stepCheck = x => x.Step == ProcurementStep.Procurement;

        return stepCheck
               .And(HasRelatedProcurementAcceptorHistory(userId))
               .Or(HasRelatedPrincipleApprovalHistory(userId));
    }

    private static Expression<Func<Procurement, bool>> IsRelatedContractAgreement(UserId userId)
    {
        Expression<Func<Procurement, bool>> stepCheck = x => x.Step == ProcurementStep.ContractAgreement;

        return HasRelatedContractHistory(userId);
    }

    private static Expression<Func<Procurement, bool>> HasRelatedAcceptorHistory(UserId userId)
    {
        return HasRelatedAppointHistory(userId)
                   .Or(HasRelatedTorDraftHistory(userId))
                   .Or(HasRelatedMedianPriceHistory(userId))
                   .Or(HasRelatedPurchaseRequisitionHistory(userId));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedAssigneeHistory(UserId userId)
    {
        return HasRelatedTorDraftAssigneeHistory(userId)
                   .Or(HasRelatedMedianPriceAssigneeHistory(userId))
                   .Or(HasRelatedPurchaseRequisitionAssigneeHistory(userId));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedProcurementAcceptorHistory(UserId userId)
    {
        return x => x.Jp005.Any(mp => mp.Acceptors.Any(ac => !ac.IsCurrent && ac.UserId == userId &&
                                                             (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected) &&
                                                             x.ProcessType == ProcessType.Jp005)) ||
                    x.Invites.Any(inv => inv.Acceptors.Any(ac => !ac.IsCurrent && ac.UserId == userId &&
                                                                 (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected) &&
                                                                 x.ProcessType == ProcessType.Invite)) ||
                    x.PurchaseOrder.Any(po => po.Acceptors.Any(ac => !ac.IsCurrent && ac.UserId == userId &&
                                                                     (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected) &&
                                                                     x.ProcessType == ProcessType.PurchaseOrder)) ||
                    x.PurchaseOrderApprovals.Any(poa => poa.Acceptors.Any(ac => !ac.IsCurrent && ac.UserId == userId &&
                                                                                (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected) &&
                                                                                x.ProcessType == ProcessType.PurchaseOrderApproval)) ||
                    x.ContractDrafts.Any(cd => cd.Vendors.Any(v => v.Acceptors.Any(ac => !ac.IsCurrent && ac.UserId == userId &&
                                                                                         (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected) &&
                                                                                         x.ProcessType == ProcessType.ContractDraft)));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedPrincipleApprovalHistory(UserId userId)
    {
        return x => x.PrincipleApprovals.Any(pa => pa.PrincipleApprovalAcceptors.Any(ac => !ac.IsCurrent && ac.UserId == userId &&
                                                                                           (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected) &&
                                                                                           x.ProcessType == ProcessType.PrincipleApproval)) ||
                    x.PrincipleApprovals.Any(pa => pa.Status == PPrincipleApprovalStatus.WaitingComment &&
                                                   pa.PrincipleApprovalAssignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Director &&
                                                                                          x.ProcessType == ProcessType.PrincipleApproval)) ||
                    x.PrincipleApprovals.Any(pa => pa.Status == PPrincipleApprovalStatus.WaitingAssign &&
                                                   pa.PrincipleApprovalAssignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Assignee &&
                                                                                          x.ProcessType == ProcessType.PrincipleApproval)) ||
                    x.PrincipleApprovalRentals.Any(pa => pa.Acceptors.Any(ac => !ac.IsCurrent && ac.UserId == userId &&
                                                                                (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected) &&
                                                                                x.ProcessType == ProcessType.PrincipleApprovalRental)) ||
                    x.PrincipleApprovalRentals.Any(pa => pa.Status == PPrincipleApprovalRentalStatus.WaitingComment &&
                                                         pa.Assignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Director &&
                                                                               x.ProcessType == ProcessType.PrincipleApprovalRental)) ||
                    x.PrincipleApprovalRentals.Any(pa => pa.Status == PPrincipleApprovalRentalStatus.WaitingAssign &&
                                                         pa.Assignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Assignee &&
                                                                               x.ProcessType == ProcessType.PrincipleApprovalRental));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedContractHistory(UserId userId)
    {
        return x => x.ContractInvitations.Any(ci => ci.Acceptors.Any(ac => ac.IsCurrent && ac.UserId == userId)) ||
                    (x.PurchaseOrderApprovals.Any(poa => poa.Assignees.Any(assignee => assignee.UserId == userId && assignee.Type == AssigneeType.Assignee)) &&
                     x.ProcessType == ProcessType.ContractInvitation &&
                     (!x.ContractInvitations.Any() || x.ContractInvitations.Any(mp =>
                         mp.Status != ContractInvitationStatus.Draft &&
                         mp.Status != ContractInvitationStatus.Rejected))) ||
                    ((x.PurchaseOrderApprovals.Any(poa => poa.Assignees.Any(assignee => assignee.UserId == userId && assignee.Type == AssigneeType.Assignee)) ||
                      x.ContractDrafts.Any(cd => cd.Vendors.Any(v => v.Acceptors.Any(ac => !ac.IsCurrent && ac.UserId == userId &&
                                                                                           (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected))))) &&
                     x.ProcessType == ProcessType.ContractDraft &&
                     (!x.ContractDrafts.Any() || x.ContractDrafts.Any(cd => cd.Vendors.Any(v =>
                         v.Status != ContractDraftVendorStatus.Draft &&
                         v.Status != ContractDraftVendorStatus.Rejected))));
    }

    private static IQueryable<Procurement> ApplyCompletedFilter(IQueryable<Procurement> query, UserId userId)
    {
        var expression = IsCompletedPreProcurement(userId)
                             .Or<Procurement>(IsCompletedProcurement(userId))
                             .Or<Procurement>(IsCompletedContractAgreement(userId));

        return query.Where(expression);
    }

    private static Expression<Func<Procurement, bool>> IsCompletedPreProcurement(UserId userId)
    {
        return x => x.Step == ProcurementStep.PreProcurement &&
                    (x.Appoints.Any(a => a.Status == AppointStatus.Approved && a.Acceptors.Any(ac => ac.UserId == userId)) ||
                     x.TorDrafts.Any(td => td.Status == TorDraftStatus.Approved && td.PpTorDraftAcceptors.Any(ac => ac.UserId == userId)) ||
                     x.MedianPrices.Any(mp => mp.Status == MedianPriceStatus.Approved && mp.Acceptors.Any(ac => ac.UserId == userId)) ||
                     x.PurchaseRequisitions.Any(rq => rq.Status == PurchaseRequisitionStatus.Approved && rq.Acceptors.Any(ac => ac.UserId == userId)) ||
                     x.PrincipleApprovals.Any(pa => pa.Status == PPrincipleApprovalStatus.Approved && pa.PrincipleApprovalAcceptors.Any(ac => ac.UserId == userId)) ||
                     x.PrincipleApprovalRentals.Any(pa => pa.Status == PPrincipleApprovalRentalStatus.Approved && pa.Acceptors.Any(ac => ac.UserId == userId)));
    }

    private static Expression<Func<Procurement, bool>> IsCompletedProcurement(UserId userId)
    {
        return x => x.Step == ProcurementStep.Procurement &&
                    (x.Jp005.Any(j => j.Status == PJp005Status.Approved && j.Acceptors.Any(ac => ac.UserId == userId)) ||
                     x.Invites.Any(inv => inv.Status == PInviteStatus.Approved && inv.Acceptors.Any(ac => ac.UserId == userId)) ||
                     x.PurchaseOrder.Any(po => po.Status == PurchaseOrderStatus.Approved && po.Acceptors.Any(ac => ac.UserId == userId)) ||
                     x.PurchaseOrderApprovals.Any(poa => poa.Status == PurchaseOrderApprovalStatus.Assigned && poa.Acceptors.Any(ac => ac.UserId == userId)));
    }

    private static Expression<Func<Procurement, bool>> IsCompletedContractAgreement(UserId userId)
    {
        return x => x.Step == ProcurementStep.ContractAgreement &&
                    (x.ContractInvitations.Any(ci => ci.Status == ContractInvitationStatus.Approved && ci.Acceptors.Any(ac => ac.UserId == userId)) ||
                     x.ContractDrafts.Any(cd => cd.Vendors.Any(v => v.Status == ContractDraftVendorStatus.Approved && v.Acceptors.Any(ac => ac.UserId == userId))));
    }

    private static IQueryable<Procurement> ApplyRemainingFilters(IQueryable<Procurement> query, ExportExcelProcurementRequest req)
    {
        return query
               .WhereIfTrue(
                   !string.IsNullOrWhiteSpace(req.Keyword),
                   x => EF.Functions.ILike(x.Name, $"%{req.Keyword}%") ||
                        (x.Plan != null && EF.Functions.ILike((string)x.Plan.PlanNumber, $"%{req.Keyword}%")) ||
                        (x.ProcurementNumber != null && EF.Functions.ILike((string)x.ProcurementNumber, $"%{req.Keyword}%")))
               .WhereIfTrue(
                   !string.IsNullOrWhiteSpace(req.DepartmentCode),
                   x => x.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
               .WhereIfTrue(req.BudgetYear.HasValue, x => x.BudgetYear == req.BudgetYear)
               .WhereIfTrue(
                   !string.IsNullOrEmpty(req.SupplyMethodCode),
                   x => x.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
               .WhereIfTrue(
                   !string.IsNullOrEmpty(req.SupplyMethodTypeCode),
                   x => x.SupplyMethodTypeCode == ParameterCode.From(req.SupplyMethodTypeCode!))
               .WhereIfTrue(
                   !string.IsNullOrEmpty(req.SupplyMethodSpecialTypeCode),
                   x => x.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
               .WhereIfTrue(req.Status.HasValue, x => x.Status == req.Status)
               .WhereIfTrue(
                   !string.IsNullOrWhiteSpace(req.RentTypeCode),
                   x => x.PrincipleApprovals.Any(pa => (string)pa.RentTypeCode == req.RentTypeCode));
    }
}
