namespace GHB.DP2.Application.Features.Procurement.Procurement;

using GHB.DP2.Application.Common;
using GHB.DP2.Application.Constants;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetListProcurementDialogRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    PlanType? Type,
    string? DepartmentCode,
    int? BudgetYear,
    string? SupplyMethodCode,
    string? SupplyMethodTypeCode,
    string? SupplyMethodSpecialTypeCode,
    string? SupplyMethodFilter);

public record GetListProcurementDialogResponse(
    ProcurementId ProcurementId,
    PlanId? Id,
    string? Status,
    string? PlanNumber,
    string? PlanName,
    decimal? Budget,
    PlanType? Type,
    ProcurementType? ProcurementType,
    string? DepartmentName,
    BusinessUnitId DepartmentCode,
    string? SupplyMethod,
    ParameterCode? SupplyMethodCode,
    string? SupplyMethodType,
    ParameterCode? SupplyMethodTypeCode,
    string? SupplyMethodSpecialType,
    ParameterCode? SupplyMethodSpecialTypeCode,
    bool? IsStock,
    bool? IsCommercialMaterial,
    int? BudgetYear,
    ProcessType? ProcessType,
    ProcurementStep? ProcurementStep,
    string? ProcurementNumber);

public record GetProcurementSupplyMethodCount(ParameterCode Code, string Name, int Count);

public record GetListProcurementDialogResult(
    GetProcurementSupplyMethodCount[] SupplyMethodCount,
    PaginatedQueryResult<GetListProcurementDialogResponse> Data
);

public class GetListProcurementDialog : EndpointBase<GetListProcurementDialogRequest, Ok<GetListProcurementDialogResult>>
{
    private readonly Dp2DbContext dbContext;

    public GetListProcurementDialog(Dp2DbContext dbContext, ILogger<GetListProcurementDialog> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement")
             .WithName("GetListProcurementDialog")
             .Produces<Ok>());
        this.Get("procurement/dialog");
    }

    protected override async ValueTask<Ok<GetListProcurementDialogResult>> HandleRequestAsync(GetListProcurementDialogRequest req, CancellationToken ct)
    {
        var baseQuery = this.dbContext.Procurements
            .AsNoTracking()
            .Include(x => x.Plan)
            .Include(p => p.Department)
            .Include(p => p.SupplyMethod)
            .Where(x => !x.IsDeleted)
            .WhereIfTrue(
                !string.IsNullOrWhiteSpace(req.Keyword),
                x => EF.Functions.ILike(x.Name, $"%{req.Keyword}%") ||
                     (x.PlanId != null && EF.Functions.ILike((string)x.Plan.PlanNumber, $"%{req.Keyword}%")) ||
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
                x => x.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!));

        var supplyMethodCount =
            await this.dbContext.SuParameters
                      .Where(p =>
                          p.GroupCode == GroupCode.From(ParameterGroupConstant.SupplyMethod) &&
                          p.ParentId == null)
                      .GroupJoin(
                          baseQuery,
                          p => p.Code,
                          pl => pl.SupplyMethodCode,
                          (p, pl) => new GetProcurementSupplyMethodCount(
                              p.Code,
                              p.Label,
                              pl.Count()))
                      .ToListAsync(ct);

        supplyMethodCount.Insert(0, new GetProcurementSupplyMethodCount(
            ParameterCode.From("ALL"),
            "ทั้งหมด",
            supplyMethodCount.Sum(x => x.Count)));

        var projectedQuery = baseQuery
            .OrderByDescending(p => p.ProcurementNumber)
            .Select(p => new
            {
                Id = p.Id,
                PlanId = p.Plan != null ? p.Plan.Id : (PlanId?)null,
                PlanNumber = p.Plan != null ? p.Plan.PlanNumber.Value : (string?)null,
                PlanType = p.Plan != null ? (PlanType?)p.Plan.Type : null,
                p.Name,
                p.Budget,
                p.BudgetYear,
                ProcurementType = p.Type,
                DepartmentName = p.Department.Name,
                DepartmentCode = p.DepartmentId,
                SupplyMethod = p.SupplyMethod.Label,
                p.SupplyMethodCode,
                SupplyMethodType = (string?)(p.SupplyMethodType != null ? p.SupplyMethodType.Label : null),
                p.SupplyMethodTypeCode,
                SupplyMethodSpecialType = (string?)(p.SupplyMethodSpecialType != null ? p.SupplyMethodSpecialType.Label : null),
                p.SupplyMethodSpecialTypeCode,
                p.IsStock,
                IsCommercialMaterial = p.Plan != null ? (bool?)p.Plan.IsCommercialMaterial : null,
                p.ProcessType,
                Step = p.Step,
                ProcurementNumber = p.ProcurementNumber != null ? p.ProcurementNumber.Value.ToString() : (string?)null,
                ProcurementStatus = p.Status.ToString(),
                AppointStatus = p.Appoints.Where(a => a.IsActive).Select(x => x.Status.ToString()).FirstOrDefault(),
                TorDraftStatus = p.TorDrafts.Where(t => t.IsActive).Select(x => x.Status.ToString()).FirstOrDefault(),
                MedianPriceStatus = p.MedianPrices.Where(m => m.IsActive).Select(x => x.Status.ToString()).FirstOrDefault(),
                PrincipleApprovalStatus = p.PrincipleApprovals.Select(x => x.Status.ToString()).FirstOrDefault(),
                PrincipleApprovalRentalStatus = p.PrincipleApprovalRentals.Select(x => x.Status.ToString()).FirstOrDefault(),
                PurchaseRequisitionStatus = p.PurchaseRequisitions.Select(x => x.Status.ToString()).FirstOrDefault(),
                Jp005Status = p.Jp005.Select(x => x.Status.ToString()).FirstOrDefault(),
                InviteStatus = p.Invites.Select(x => x.Status.ToString()).FirstOrDefault(),
                PurchaseOrderStatus = p.PurchaseOrder.Select(x => x.Status.ToString()).FirstOrDefault(),
                PurchaseOrderApprovalStatus = p.PurchaseOrderApprovals.Select(x => x.Status.ToString()).FirstOrDefault(),
                ContractInvitationStatus = p.ContractInvitations.Select(x => x.Status.ToString()).FirstOrDefault(),
                ContractDraftStatus = p.ContractDrafts.Select(x => x.Status.ToString()).FirstOrDefault(),
            });

        var totalCount = await projectedQuery.CountAsync(ct);
        var pageItems = await projectedQuery
            .Skip((req.PageNumber - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync(ct);

        var items = pageItems.Select(p =>
        {
            var childStatus = p.ProcurementStatus == nameof(ProcurementStatus.Cancelled)
                ? p.ProcurementStatus
                : p.ProcessType switch
                {
                    ProcessType.Appoint => p.AppointStatus ?? nameof(AppointStatus.Draft),
                    ProcessType.TorDraft => p.TorDraftStatus ?? nameof(TorDraftStatus.Draft),
                    ProcessType.MedianPrice => p.MedianPriceStatus ?? nameof(MedianPriceStatus.Draft),
                    ProcessType.PrincipleApproval => p.PrincipleApprovalStatus ?? nameof(PPrincipleApprovalStatus.Draft),
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

            return new GetListProcurementDialogResponse(
                p.Id,
                p.PlanId,
                childStatus,
                p.PlanNumber,
                p.Name,
                p.Budget ?? 0m,
                p.PlanType,
                p.ProcurementType,
                p.DepartmentName,
                p.DepartmentCode,
                p.SupplyMethod,
                p.SupplyMethodCode,
                p.SupplyMethodType,
                p.SupplyMethodTypeCode,
                p.SupplyMethodSpecialType,
                p.SupplyMethodSpecialTypeCode,
                p.IsStock,
                p.IsCommercialMaterial,
                p.BudgetYear,
                p.ProcessType,
                p.Step,
                p.ProcurementNumber);
        }).ToList();

        var data = new PaginatedQueryResult<GetListProcurementDialogResponse>(items, totalCount);

        return TypedResults.Ok(new GetListProcurementDialogResult([.. supplyMethodCount], data));
    }
}