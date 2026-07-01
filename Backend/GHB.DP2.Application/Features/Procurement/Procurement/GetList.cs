namespace GHB.DP2.Application.Features.Procurement.Procurement;

using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Procurement.PPrincipleApproval; // added if principle approval statuses needed
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental; // added for rental statuses
using GHB.DP2.Domain.Procurement.PpTorDraft; // added for TorDraftStatus
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.Features.WorkList.Programs;
using GHB.DP2.Application.Features.WorkList;

public record GetProcurementRequest(
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
    WorkProcess? WorkProcess,
    string? StatusCode,
    Guid? PlanId) : IProcurementBaseQuery
{
    public bool? IsMd { get; private set; }

    public bool? IsPendingDepartment => false;

    public GetProcurementRequest WhitMd()
    {
        this.IsMd = true;

        return this;
    }

    public GetProcurementRequest WhitNonMd()
    {
        this.IsMd = false;

        return this;
    }
}

public record GetProcurementRequestResponse(
    Guid Id,
    Guid? PlanId,
    string? PlanNumber,
    string? ProcurementNumber,
    string Name,
    decimal? Budget,
    int? BudgetYear,
    string? Type,
    string DepartmentName,
    BusinessUnitId DepartmentCode,
    string SupplyMethod,
    string SupplyMethodType,
    string SupplyMethodSpecialType,
    string? RentTypeName,
    ProcurementStep Step,
    ProcessType ProcessType,
    string Status,
    string ProcurementStatus,
    bool IsCancel,
    bool IsChange,
    bool CanDelete,
    string? Period = "");

public record GetProcurementResult(
    int All,
    int PreProcurement,
    int Procurement,
    int ContractAgreement,
    int ContractManagement,
    PaginatedQueryResult<GetProcurementRequestResponse> Data);

public class GetProcurementListEndpoint : EndpointBase<GetProcurementRequest, Ok<GetProcurementResult>>
{
    private readonly Dp2DbContext dbContext;
    private readonly ProcurementProgram procurementProgram;

    public GetProcurementListEndpoint(Dp2DbContext dbContext, ILogger<GetProcurementListEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.procurementProgram = new ProcurementProgram();
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement")
             .WithName("GetProcurementList")
             .Produces<Ok>());
        this.Get("procurement");
    }

    protected override async ValueTask<Ok<GetProcurementResult>> HandleRequestAsync(GetProcurementRequest req, CancellationToken ct)
    {
        var baseQuery = await this.BuildFilterQuery(req, ct);

        var paginatedQuery = baseQuery.WhereIfTrue(req.Step.HasValue, x => x.Step == req.Step);

        var projectedQuery = paginatedQuery.Select(p => new
        {
            Id = p.Id.Value,
            PlanId = (Guid?)p.PlanId,
            PlanNumber = p.PlanId != null ? (string)p.Plan.PlanNumber : string.Empty,
            ProcurementNumber = p.ProcurementNumber != null ? p.ProcurementNumber.Value.ToString() : (string?)null,
            p.Name,
            p.Budget,
            p.BudgetYear,
            Type = p.PlanId != null ? p.Plan.Type.ToString() : string.Empty,
            DepartmentName = p.Department.Name,
            DepartmentCode = p.DepartmentId,
            SupplyMethod = p.SupplyMethod.Label,
            SupplyMethodType = (string?)(p.SupplyMethodType != null ? p.SupplyMethodType.Label : null),
            SupplyMethodSpecialType = (string?)(p.SupplyMethodSpecialType != null ? p.SupplyMethodSpecialType.Label : null),
            PrincipleApprovalInfo = p.PrincipleApprovals.Select(pa => new
            {
                RentTypeName = (string?)pa.RentTypeCodeInfo.Label,
                Status = pa.Status.ToString(),
            }).FirstOrDefault(),
            p.Step,
            p.ProcessType,
            ProcurementStatus = p.Status.ToString(),
            IsCancel = p.IsCancelled,
            AppointStatus = p.Appoints.Where(a => a.IsActive).Select(x => x.Status.ToString()).FirstOrDefault(),
            TorDraftStatus = p.TorDrafts.Where(t => t.IsActive).Select(x => x.Status.ToString()).FirstOrDefault(),
            MedianPriceStatus = p.MedianPrices.Where(m => m.IsActive).Select(x => x.Status.ToString()).FirstOrDefault(),
            AppointIsCancel = p.Appoints.Where(a => a.IsActive).Select(x => (bool?)x.IsCancel).FirstOrDefault(),
            TorDraftIsCancel = p.TorDrafts.Where(t => t.IsActive).Select(x => (bool?)x.IsCancel).FirstOrDefault(),
            MedianPriceIsCancel = p.MedianPrices.Where(m => m.IsActive).Select(x => (bool?)x.IsCancel).FirstOrDefault(),
            AppointIsChange = p.Appoints.Where(a => a.IsActive).Select(x => (bool?)x.IsChange).FirstOrDefault(),
            TorDraftIsChange = p.TorDrafts.Where(t => t.IsActive).Select(x => (bool?)x.IsChange).FirstOrDefault(),
            MedianPriceIsChange = p.MedianPrices.Where(m => m.IsActive).Select(x => (bool?)x.IsChange).FirstOrDefault(),
            PrincipleApprovalRentalStatus = p.PrincipleApprovalRentals.Select(x => x.Status.ToString()).FirstOrDefault(),
            PurchaseRequisitionStatus = p.PurchaseRequisitions.Where(pr => !pr.IsDeleted).Select(x => x.Status.ToString()).FirstOrDefault(),
            Jp005Status = p.Jp005.Select(x => x.Status.ToString()).FirstOrDefault(),
            InviteStatus = p.Invites.Select(x => x.Status.ToString()).FirstOrDefault(),
            PurchaseOrderStatus = p.PurchaseOrder.Select(x => x.Status.ToString()).FirstOrDefault(),
            PurchaseOrderApprovalStatus = p.PurchaseOrderApprovals.Where(poa => !poa.IsDeleted).Select(x => x.Status.ToString()).FirstOrDefault(),
            ContractInvitationStatus = p.ContractInvitations.Select(x => x.Status.ToString()).FirstOrDefault(),
            ContractDraftStatus = p.ContractDrafts.Select(x => x.Status.ToString()).FirstOrDefault(),
        });

        if (!string.IsNullOrWhiteSpace(req.StatusCode))
        {
            projectedQuery = projectedQuery.Where(p =>
                p.ProcurementStatus == req.StatusCode ||
                p.AppointStatus == req.StatusCode ||
                p.TorDraftStatus == req.StatusCode ||
                p.MedianPriceStatus == req.StatusCode ||
                (p.PrincipleApprovalInfo != null && p.PrincipleApprovalInfo.Status == req.StatusCode) ||
                p.PrincipleApprovalRentalStatus == req.StatusCode ||
                p.PurchaseRequisitionStatus == req.StatusCode ||
                p.Jp005Status == req.StatusCode ||
                p.InviteStatus == req.StatusCode ||
                p.PurchaseOrderStatus == req.StatusCode ||
                p.PurchaseOrderApprovalStatus == req.StatusCode ||
                p.ContractInvitationStatus == req.StatusCode ||
                p.ContractDraftStatus == req.StatusCode);
        }

        var fullList = await GetFullListAsync(baseQuery, ct);
        var stepCounts = CalculateStepCounts(fullList);

        var projectedList = await projectedQuery
            .Skip((req.PageNumber - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync(ct);

        var totalCount = await projectedQuery.CountAsync(ct);

        var deletableStatuses = new[] { nameof(AppointStatus.Draft), nameof(AppointStatus.Edit), nameof(AppointStatus.Rejected) };

        var items = projectedList.Select(p =>
        {
            bool canDelete;
            if (p.ProcurementStatus == nameof(ProcurementStatus.Cancelled))
            {
                canDelete = true;
            }
            else if (p.AppointStatus != null)
            {
                canDelete = deletableStatuses.Contains(p.AppointStatus);
            }
            else if (p.PurchaseRequisitionStatus != null)
            {
                canDelete = deletableStatuses.Contains(p.PurchaseRequisitionStatus);
            }
            else if (p.PurchaseOrderApprovalStatus != null)
            {
                canDelete = deletableStatuses.Contains(p.PurchaseOrderApprovalStatus);
            }
            else if (p.PrincipleApprovalInfo?.Status != null)
            {
                canDelete = deletableStatuses.Contains(p.PrincipleApprovalInfo.Status);
            }
            else if (p.PrincipleApprovalRentalStatus != null)
            {
                canDelete = deletableStatuses.Contains(p.PrincipleApprovalRentalStatus);
            }
            else
            {
                canDelete = p.ProcurementStatus == nameof(ProcurementStatus.Draft) ||
                            (p.ProcurementStatus == nameof(ProcurementStatus.InProgress)
                             && p.PrincipleApprovalInfo == null
                             && p.PrincipleApprovalRentalStatus == null);
            }

            var childStatus = p.ProcurementStatus == nameof(ProcurementStatus.Cancelled)
                ? p.ProcurementStatus
                : p.ProcessType switch
                {
                    ProcessType.Appoint => p.AppointStatus ?? nameof(AppointStatus.Draft),
                    ProcessType.TorDraft => p.TorDraftStatus ?? nameof(TorDraftStatus.Draft),
                    ProcessType.MedianPrice => p.MedianPriceStatus ?? nameof(MedianPriceStatus.Draft),
                    ProcessType.PrincipleApproval => p.PrincipleApprovalInfo?.Status ?? nameof(PPrincipleApprovalStatus.Draft),
                    ProcessType.PrincipleApprovalRental => p.PrincipleApprovalRentalStatus ?? nameof(PPrincipleApprovalRentalStatus.Draft),
                    ProcessType.PurchaseRequisition => p.PurchaseRequisitionStatus ?? nameof(PurchaseRequisitionStatus.Draft),
                    ProcessType.Jp005 => p.Jp005Status ?? nameof(PJp005Status.Draft),
                    ProcessType.Invite => p.InviteStatus ?? nameof(PInviteStatus.Draft),
                    ProcessType.PurchaseOrder => p.PurchaseOrderStatus ?? nameof(PurchaseOrderStatus.Draft),
                    ProcessType.PurchaseOrderApproval => p.PurchaseOrderApprovalStatus ?? nameof(PurchaseOrderApprovalStatus.Draft),
                    ProcessType.ContractInvitation => p.ContractInvitationStatus ?? nameof(ContractInvitationStatus.Draft),
                    ProcessType.ContractDraft => p.ContractDraftStatus ?? nameof(ContractDraftStatus.Draft),
                    _ => p.ProcurementStatus,
                };

            return new GetProcurementRequestResponse(
                p.Id,
                p.PlanId,
                p.PlanNumber,
                p.ProcurementNumber,
                p.Name,
                p.Budget,
                p.BudgetYear,
                p.Type,
                p.DepartmentName,
                p.DepartmentCode,
                p.SupplyMethod,
                p.SupplyMethodType ?? string.Empty,
                p.SupplyMethodSpecialType ?? string.Empty,
                p.PrincipleApprovalInfo?.RentTypeName ?? string.Empty,
                p.Step,
                p.ProcessType,
                childStatus,
                p.ProcurementStatus,
                p.MedianPriceIsCancel ?? p.TorDraftIsCancel ?? p.AppointIsCancel ?? p.IsCancel,
                p.MedianPriceIsChange ?? p.TorDraftIsChange ?? p.AppointIsChange ?? false,
                canDelete);
        }).ToList();

        var data = new PaginatedQueryResult<GetProcurementRequestResponse>(items, totalCount);

        return TypedResults.Ok(new GetProcurementResult(
            stepCounts.All,
            stepCounts.PreProcurement,
            stepCounts.Procurement,
            stepCounts.ContractAgreement,
            stepCounts.ContractManagement,
            data));
    }

    private async Task<IQueryable<Procurement>> BuildFilterQuery(
        GetProcurementRequest req,
        CancellationToken ct)
    {
        var userId = UserId.From(req.UserId);

        var query = await this.ApplyWorkProcessFilter(req, ct);

        return query.OrderByDescending(o =>
            o.AuditInfo.LastModifiedAt != null
                ? o.AuditInfo.LastModifiedAt
                : o.AuditInfo.CreatedAt);
    }

    private static async Task<List<Procurement>> GetFullListAsync(IQueryable<Procurement> baseQuery, CancellationToken ct)
    {
        return await baseQuery
            .ToListAsync(ct);
    }

    private static (int All, int PreProcurement, int Procurement, int ContractAgreement, int ContractManagement) CalculateStepCounts(List<Procurement> fullList)
    {
        return (
            All: fullList.Count,
            PreProcurement: fullList.Count(x => x.Step == ProcurementStep.PreProcurement),
            Procurement: fullList.Count(x => x.Step == ProcurementStep.Procurement),
            ContractAgreement: fullList.Count(x => x.Step == ProcurementStep.ContractAgreement),
            ContractManagement: fullList.Count(x => x.Step == ProcurementStep.ContractManagement));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedAppointHistory(UserId userId)
    {
        return x => x.Appoints
                     .Where(t => t.IsActive)
                     .Any(a => a.Acceptors.Any(ac =>
                         !ac.IsCurrent &&
                         ac.UserId == userId &&
                         (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected)));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedTorDraftHistory(UserId userId)
    {
        return x => x.TorDrafts
                     .Where(t => t.IsActive)
                     .Any(td => td.PpTorDraftAcceptors.Any(ac =>
                         !ac.IsCurrent &&
                         ac.UserId == userId &&
                         (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected)));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedMedianPriceHistory(UserId userId)
    {
        return x => x.MedianPrices
                     .Where(m => m.IsActive)
                     .Any(mp => mp.Acceptors.Any(ac =>
                         !ac.IsCurrent &&
                         ac.UserId == userId &&
                         (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected)));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedPurchaseRequisitionHistory(UserId userId)
    {
        return x => x.PurchaseRequisitions.Any(rq => rq.Acceptors.Any(ac =>
            !ac.IsCurrent &&
            ac.UserId == userId &&
            (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected)));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedTorDraftAssigneeHistory(UserId userId)
    {
        return x => x.TorDrafts
                     .Where(t => t.IsActive)
                     .Any(td =>
                         ((td.Status == TorDraftStatus.WaitingComment &&
                           td.Assignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Director)) ||
                          (td.Status == TorDraftStatus.WaitingApproval &&
                           td.Assignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Assignee))));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedMedianPriceAssigneeHistory(UserId userId)
    {
        return x => x.MedianPrices
                     .Where(t => t.IsActive)
                     .Any(mp =>
                         (((mp.Status == MedianPriceStatus.WaitingAssign || mp.Status == MedianPriceStatus.RejectToAssignee) &&
                           mp.Assignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Director)) ||
                          ((mp.Status == MedianPriceStatus.WaitingComment || mp.Status == MedianPriceStatus.RejectToAssignee) &&
                           mp.Assignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Assignee))));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedPurchaseRequisitionAssigneeHistory(UserId userId)
    {
        return x => x.PurchaseRequisitions.Any(pr =>
            pr.Status == PurchaseRequisitionStatus.Approved &&
            pr.Assignees.Any(a => a.UserId == userId));
    }

    private IQueryable<Procurement> ProcurementBaseQuery(GetProcurementRequest req)
        => this.dbContext.Procurements
               .AsNoTracking()
               .Include(p => p.Plan)
               .Include(p => p.Department)
               .Include(p => p.SupplyMethod)
               .Include(p => p.PrincipleApprovals)
               .ThenInclude(p => p.RentTypeCodeInfo)
               .Where(x => x.Type == req.ProcurementType);

    private async Task<IQueryable<Procurement>> ApplyWorkProcessFilter(
        GetProcurementRequest req,
        CancellationToken ct)
    {
        return req.WorkProcess switch
        {
            WorkProcess.InProcess => await this.ApplyInProcessFilter(req, ct),
            WorkProcess.Related => this.ApplyRelatedFilter(req),
            WorkProcess.Completed => this.ApplyCompletedFilter(req),
            _ => this.ApplyAllFilter(req),
        };
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

    private async Task<IQueryable<Procurement>> ApplyInProcessFilter(GetProcurementRequest req, CancellationToken ct)
    {
        var userId = UserId.From(req.UserId);
        var userContext = await this.GetUserContextAsync(userId, ct);

        if (userContext is null)
        {
            return null!;
        }

        var query =
            this.procurementProgram.BuildProcurementBaseQuery(this.dbContext, req, userContext)
                .Where(p =>
                    p.Step == ProcurementStep.PreProcurement ||
                    p.Step == ProcurementStep.Procurement ||
                    p.Step == ProcurementStep.ContractAgreement)
                .Where(p => p.Status != ProcurementStatus.Cancelled)
                .Include(x => x.Plan)
                .Include(x => x.Department)
                .Include(x => x.SupplyMethod)
                .Include(p => p.PrincipleApprovals)
                .ThenInclude(p => p.RentTypeCodeInfo)
                .Where(x => x.Type == req.ProcurementType)
                .WhereIfTrue(req.Status.HasValue, x => x.Status == req.Status)
                .WhereIfTrue(
                    !string.IsNullOrWhiteSpace(req.RentTypeCode),
                    p =>
                        p.PrincipleApprovals.Any(pa =>
                            (string)pa.RentTypeCode == req.RentTypeCode))
                .WhereIfTrue(req.PlanId.HasValue, x => (Guid?)x.PlanId == req.PlanId)
                .AsNoTracking();

        return query;
    }

    private IQueryable<Procurement> ApplyRelatedFilter(GetProcurementRequest req)
    {
        var userId = UserId.From(req.UserId);

        var expression = HasRelatedAcceptorHistory(userId)
                         .Or(HasRelatedAssigneeHistory(userId))
                         .Or(HasRelatedProcurementAcceptorHistory(userId))
                         .Or(HasRelatedPrincipleApprovalHistory(userId))
                         .Or(HasRelatedContractHistory(userId));

        var query =
            this.ProcurementBaseQuery(req)
                .Where(x => x.Status != ProcurementStatus.Completed && x.Status != ProcurementStatus.Cancelled)
                .Where(expression);

        query = ApplyRemainingFilters(query, req);

        return query;
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
                                                             (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected))) ||
                    x.Invites.Any(inv => inv.Acceptors.Any(ac => !ac.IsCurrent && ac.UserId == userId &&
                                                                 (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected))) ||
                    x.PurchaseOrder.Any(po => po.Acceptors.Any(ac => !ac.IsCurrent && ac.UserId == userId &&
                                                                     (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected))) ||
                    x.PurchaseOrderApprovals.Any(poa => poa.Acceptors.Any(ac => !ac.IsCurrent && ac.UserId == userId &&
                                                                                (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected))) ||
                    x.ContractDrafts.Any(cd => cd.Vendors.Any(v => v.Acceptors.Any(ac => !ac.IsCurrent && ac.UserId == userId &&
                                                                                         (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected))));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedPrincipleApprovalHistory(UserId userId)
    {
        return x => x.PrincipleApprovals.Any(pa => pa.PrincipleApprovalAcceptors.Any(ac => !ac.IsCurrent && ac.UserId == userId &&
                                                                                           (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected))) ||
                    x.PrincipleApprovals.Any(pa => pa.Status == PPrincipleApprovalStatus.WaitingComment &&
                                                   pa.PrincipleApprovalAssignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Director)) ||
                    x.PrincipleApprovals.Any(pa => pa.Status == PPrincipleApprovalStatus.WaitingAssign &&
                                                   pa.PrincipleApprovalAssignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Assignee)) ||
                    x.PrincipleApprovalRentals.Any(pa => pa.Acceptors.Any(ac => !ac.IsCurrent && ac.UserId == userId &&
                                                                                (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected))) ||
                    x.PrincipleApprovalRentals.Any(pa => pa.Status == PPrincipleApprovalRentalStatus.WaitingComment &&
                                                         pa.Assignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Director)) ||
                    x.PrincipleApprovalRentals.Any(pa => pa.Status == PPrincipleApprovalRentalStatus.WaitingAssign &&
                                                         pa.Assignees.Any(a => a.UserId == userId && a.Type == AssigneeType.Assignee));
    }

    private static Expression<Func<Procurement, bool>> HasRelatedContractHistory(UserId userId)
    {
        return x => x.ContractInvitations.Any(ci => ci.Acceptors.Any(ac => ac.IsCurrent && ac.UserId == userId)) ||
                    (x.PurchaseOrderApprovals.Any(poa => poa.Assignees.Any(assignee => assignee.UserId == userId && assignee.Type == AssigneeType.Assignee)) &&
                     (!x.ContractInvitations.Any() || x.ContractInvitations.Any(mp =>
                         mp.Status != ContractInvitationStatus.Draft &&
                         mp.Status != ContractInvitationStatus.Rejected))) ||
                    ((x.PurchaseOrderApprovals.Any(poa => poa.Assignees.Any(assignee => assignee.UserId == userId && assignee.Type == AssigneeType.Assignee)) ||
                      x.ContractDrafts.Any(cd => cd.Vendors.Any(v => v.Acceptors.Any(ac => !ac.IsCurrent && ac.UserId == userId &&
                                                                                           (ac.Status == AcceptorStatus.Approved || ac.Status == AcceptorStatus.Rejected))))) &&
                     (!x.ContractDrafts.Any() || x.ContractDrafts.Any(cd => cd.Vendors.Any(v =>
                         v.Status != ContractDraftVendorStatus.Draft &&
                         v.Status != ContractDraftVendorStatus.Rejected))));
    }

    private IQueryable<Procurement> ApplyCompletedFilter(GetProcurementRequest req)
    {
        var userId = UserId.From(req.UserId);

        var query =
            this.ProcurementBaseQuery(req)
                .Where(HasCompletedAcceptorAccess(userId))
                .WhereIfTrue(
                    req.ProcurementType == ProcurementType.Procurement,
                    x => x.Status == ProcurementStatus.Completed || x.Status == ProcurementStatus.Cancelled);

        query = ApplyRemainingFilters(query, req);

        return query;
    }

    private IQueryable<Procurement> ApplyAllFilter(GetProcurementRequest req)
    {
        var query = this.ProcurementBaseQuery(req);

        query = ApplyRemainingFilters(query, req);

        return query;
    }

    private static Expression<Func<Procurement, bool>> HasCompletedAcceptorAccess(UserId userId)
    {
        return x =>
            (x.Status == ProcurementStatus.Completed || x.Status == ProcurementStatus.Cancelled) &&
            (x.Appoints.Where(t => t.IsActive).Any(a => a.Status == AppointStatus.Approved && a.Acceptors.Any(ac => ac.UserId == userId)) ||
            x.TorDrafts.Where(t => t.IsActive).Any(td => td.Status == TorDraftStatus.Approved && td.PpTorDraftAcceptors.Any(ac => ac.UserId == userId)) ||
            x.MedianPrices.Where(t => t.IsActive).Any(mp => mp.Status == MedianPriceStatus.Approved && mp.Acceptors.Any(ac => ac.UserId == userId)) ||
            x.PurchaseRequisitions.Any(rq => rq.Status == PurchaseRequisitionStatus.Approved && rq.Acceptors.Any(ac => ac.UserId == userId)) ||
            x.PrincipleApprovals.Any(pa => pa.Status == PPrincipleApprovalStatus.Approved && pa.PrincipleApprovalAcceptors.Any(ac => ac.UserId == userId)) ||
            x.PrincipleApprovalRentals.Any(pa => pa.Status == PPrincipleApprovalRentalStatus.Approved && pa.Acceptors.Any(ac => ac.UserId == userId)) ||
            x.Jp005.Any(j => j.Status == PJp005Status.Approved && j.Acceptors.Any(ac => ac.UserId == userId)) ||
            x.Invites.Any(inv => inv.Status == PInviteStatus.Approved && inv.Acceptors.Any(ac => ac.UserId == userId)) ||
            x.PurchaseOrder.Any(po => po.Status == PurchaseOrderStatus.Approved && po.Acceptors.Any(ac => ac.UserId == userId)) ||
            x.PurchaseOrderApprovals.Any(poa => poa.Status == PurchaseOrderApprovalStatus.Assigned && poa.Acceptors.Any(ac => ac.UserId == userId)) ||
            x.ContractInvitations.Any(ci => ci.Status == ContractInvitationStatus.Approved && ci.Acceptors.Any(ac => ac.UserId == userId)) ||
            x.ContractDrafts.Any(cd => cd.Vendors.Any(v => v.Status == ContractDraftVendorStatus.Approved && v.Acceptors.Any(ac => ac.UserId == userId))));
    }

    private static IQueryable<Procurement> ApplyRemainingFilters(IQueryable<Procurement> query, GetProcurementRequest req)
    {
        return query
               .Where(x => !x.IsDeleted)
               .WhereIfTrue(
                   !string.IsNullOrWhiteSpace(req.Keyword),
                   x => EF.Functions.ILike(x.Name, $"%{req.Keyword}%") ||
                        (x.Plan != null && EF.Functions.ILike((string)x.Plan.PlanNumber, $"%{req.Keyword}%")) ||
                        (x.ProcurementNumber != null && EF.Functions.ILike((string)x.ProcurementNumber.Value, $"%{req.Keyword}%")))
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
                   x => x.PrincipleApprovals.Any(pa => (string)pa.RentTypeCode == req.RentTypeCode))
               .WhereIfTrue(req.PlanId.HasValue, x => (Guid?)x.PlanId == req.PlanId);
    }
}