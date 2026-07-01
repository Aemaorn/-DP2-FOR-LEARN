namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptance;

using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptance.Abstract;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetById
{
    public record GetDeliveryAcceptanceByIdRequest(
        [property: FromClaim(JwtRegisteredClaimNames.Sub)]
        Guid UserId,
        Guid Id);

    public record GetDeliveryAcceptanceByIdResponse(
        [property: Description("รหัสการตรวจรับการส่งมอบ")]
        CmDeliveryAcceptanceId Id,
        [property: Description("สถานะการตรวจรับ")]
        CmDeliveryAcceptanceStatus Status,
        string? ContractType,
        [property: Description("รายการงวดการตรวจรับ")]
        DeliveryAcceptancePeriodInfoDto[] Periods,
        Cm001Info Cm001Info,
        SourceType SourceType,
        Guid? RefId,
        string? RefCode,
        [property: Description("สามารถอนุมัติการส่งมอบตรวจรับได้หรือไม่")]
        bool CanApproveDeliveryAcceptance,
        string? Number,
        string? DepartmentId,
        string? DepartmentOrganizationLevel,
        string? SupplyMethodCode,
        string? SupplyMethodTypeCode,
        string? SupplyMethodSpecialTypeCode,
        string? Name,
        decimal? Budget,
        bool? IsCommercialMaterial);

    public record Cm001Info(
        [property: Description("เลขที่รายการจัดซื้อจัดจ้าง")]
        string? PlanCode,
        PlanId? PlanId,
        string? ProcurementNumber,
        ProcurementId? ProcurementId,
        [property: Description("ฝ่าย/ภาคเขต")] string DepartmentName,
        PlanType? PlanType,
        string? VendorName,
        string? EstablishmentName,
        string? VendorEmail,
        string? ContractNumber,
        string? PoNumber,
        decimal? ContractBudget,
        string? Name,
        string? ContractTypeName,
        string? TemplateName,
        DateTimeOffset? ContractDate,
        string? Period,
        DateTimeOffset? DeliveryDate,
        string? SupplyMethod,
        string? SupplyMethodCode,
        string? SupplyMethodType,
        string? SupplyMethodTypeCode,
        string? SupplyMethodSpecialType,
        string? SupplyMethodSpecialTypeCode,
        int? BudgetYear,
        decimal? Budget,
        SourceType SourceType,
        WarrantyInfoDto? Warranty);

    public record WarrantyInfoDto(
        bool HasWarranty,
        string? WarrantyConditionCode,
        RentalDurationInfo? WarrantyPeriod,
        RentalDurationInfo? FixingDeadlinePeriod,
        int? WarrantyMonthlyAllowedDowntimeHours,
        decimal? WarrantyDowntimePercentPerMonth,
        decimal? WarrantyPenaltyPerHour,
        int? DowntimeResolutionHours,
        int? DowntimeResolutionDay,
        int? RepairCompletionHours,
        int? RepairCompletionDay,
        decimal? RepairDelayPenaltyPercentPerHour,
        int? MaxMonthlyMalfunction,
        ParameterCode? MaxMonthlyMalfunctionTypeCode,
        decimal? MaxMonthlyMalfunctionRate,
        decimal? MaxMonthlyMalfunctionPenaltyPercentageRate,
        decimal? MaxMonthlyMalfunctionPenaltyPerHour,
        int? MaxMonthlyMalfunctionPenaltyDueDays,
        DateTimeOffset? WarrantyStartDate,
        DateTimeOffset? WarrantyEndDate,
        int? WarrantyMaintenanceCount,
        ParameterCode? WarrantyMaintenanceTypeCode,
        DateTimeOffset? LastAcceptanceDate)
    {
        public static WarrantyInfoDto? MapFromEntity(Warranty? entity, CmDeliveryAcceptance deliveryAcceptance)
        {
            if (entity?.HasWarranty == null)
            {
                return null;
            }

            var warrantyStartDate = entity.WarrantyStartDate;
            var warrantyEndDate = entity.WarrantyEndDate;
            DateTimeOffset? lastAcceptanceDate = null;

            var lastPeriod = deliveryAcceptance
               .Periods
               .OrderByDescending(p => p.PaymentTerms.Max(pt => pt.PaymentTerm))
               .FirstOrDefault();

            var approverActionAt = lastPeriod?
                .Acceptors
                .Where(x => x.Type == AcceptorType.Approver && x.ActionAt.HasValue)
                .Max(x => (DateTimeOffset?)x.ActionAt!.Value);

            lastAcceptanceDate = approverActionAt;

            if ((warrantyStartDate == null || warrantyEndDate == null) && entity.WarrantyPeriod != null)
            {
                if (approverActionAt.HasValue)
                {
                    warrantyStartDate = lastAcceptanceDate.Value.AddDays(1);

                    var warrantyPeriod = entity.WarrantyPeriod;
                    warrantyEndDate = warrantyStartDate
                        .Value
                        .AddYears(warrantyPeriod.Year ?? 0)
                        .AddMonths(warrantyPeriod.Month ?? 0)
                        .AddDays((warrantyPeriod.Day ?? 0) - 1);
                }
            }

            return new WarrantyInfoDto(
                entity.HasWarranty ?? false,
                entity.WarrantyConditionCode?.Value,
                entity.WarrantyPeriod,
                entity.FixingDeadlinePeriod,
                entity.WarrantyMonthlyAllowedDowntimeHours,
                entity.WarrantyDowntimePercentPerMonth,
                entity.WarrantyPenaltyPerHour,
                entity.DowntimeResolutionHours,
                entity.DowntimeResolutionDay,
                entity.RepairCompletionHours,
                entity.RepairCompletionDay,
                entity.RepairDelayPenaltyPercentPerHour,
                entity.MaxMonthlyMalfunction,
                entity.MaxMonthlyMalfunctionTypeCode,
                entity.MaxMonthlyMalfunctionRate,
                entity.MaxMonthlyMalfunctionPenaltyPercentageRate,
                entity.MaxMonthlyMalfunctionPenaltyPerHour,
                entity.MaxMonthlyMalfunctionPenaltyDueDays,
                warrantyStartDate,
                warrantyEndDate,
                entity.WarrantyMaintenanceCount,
                entity.WarrantyMaintenanceTypeCode,
                lastAcceptanceDate);
        }
    }

    public record DeliveryAcceptancePeriodInfoDto(
        [property: Description("รหัสงวดการตรวจรับ")]
        CmDeliveryAcceptancePeriodId Id,
        [property: Description("สถานะงวดการตรวจรับ")]
        CmDeliveryAcceptancePeriodStatus Status,
        string AcceptanceNumber,
        [property: Description("ลำดับงวด")] string PaymentTermNo,
        [property: Description("ข้อมูลเงื่อนไขการจ่ายเงิน")]
        string Description,
        CmDeliveryAcceptancePeriodAccountStatus AccountStatus);

    public record ContractDraftPaymentTermInfoDto(
        [property: Description("ลำดับเงื่อนไขการจ่ายเงิน")]
        int? PaymentTermNo,
        [property: Description("ระยะเวลาการส่งมอบ")]
        int? LeadTime,
        [property: Description("วันที่ส่งมอบ")]
        DateTimeOffset? DeliveryDate,
        [property: Description("เปอร์เซ็นต์การจ่ายเงิน")]
        decimal? InstallmentPercentage,
        [property: Description("จำนวนเงิน")] decimal? Amount,
        [property: Description("จำนวนเงินหักค่าล่วงหน้า")]
        decimal? AdvanceDeductionAmount,
        [property: Description("จำนวนเงินหักประกันผลงาน")]
        decimal? PerformanceDeductionAmount,
        [property: Description("รายละเอียด")] string? Description);

    public record PeriodAcceptanceInfoDto(
        [property: Description("วันที่ตรวจรับ")]
        DateTimeOffset? AcceptanceDate,
        [property: Description("เลขที่การตรวจรับ")]
        string? AcceptanceNumber,
        [property: Description("จำนวนเงินที่รับ")]
        decimal? AcceptedAmount,
        [property: Description("รายละเอียด")] string? Description,
        [property: Description("จำนวนเงินหักรวม")]
        decimal? TotalDeductions);

    public class GetCmDeliveryAcceptanceById
        : DeliveryAcceptanceEndpointBase<
            Features.ContractManagement.DeliveryAcceptance.GetById.GetDeliveryAcceptanceByIdRequest,
            Results<Ok<Features.ContractManagement.DeliveryAcceptance.GetById.GetDeliveryAcceptanceByIdResponse>, NotFound<string>>>
    {
        public GetCmDeliveryAcceptanceById(
            Dp2DbContext dbContext,
            ILogger<GetCmDeliveryAcceptanceById> logger)
            : base(dbContext, logger)
        {
        }

        public override void Configure()
        {
            this.Get("delivery-acceptance/{Id:guid?}");
            this.Description(
                b => b
                     .WithTags("ContractManagement/DeliveryAcceptance")
                     .Produces<Features.ContractManagement.DeliveryAcceptance.GetById.GetDeliveryAcceptanceByIdResponse>()
                     .Produces<string>(StatusCodes.Status404NotFound));
        }

        protected override async ValueTask<Results<Ok<GetDeliveryAcceptanceByIdResponse>, NotFound<string>>>
            HandleRequestAsync(
                GetDeliveryAcceptanceByIdRequest req,
                CancellationToken ct)
        {
            var deliveryAcceptanceExisting =
                await this.GetById(
                    CmDeliveryAcceptanceId.From(req.Id),
                    ct);

            var periods = CreateDeliveryAcceptancePeriods();

            Cm001Info cm001Info = default!;
            string? refCode = null;
            string? departmentOrganizationLevel = null;

            if (deliveryAcceptanceExisting.SourceType == SourceType.Plan)
            {
                var planData = await this.dbContext.Plans
                                         .Include(plan => plan.Department)
                                         .Include(plan => plan.SupplyMethod)
                                         .Include(plan => plan.SupplyMethodType)
                                         .Include(plan => plan.SupplyMethodSpecialType)
                                         .FirstOrDefaultAsync(p => p.Id == PlanId.From(deliveryAcceptanceExisting.RefId!.Value), ct);

                if (planData == null)
                {
                    this.ThrowError("ไม่พบข้อมูลแผน", StatusCodes.Status404NotFound);
                }

                refCode = planData.PlanNumber.Value;
                departmentOrganizationLevel = planData.Department.OrganizationLevel;
                cm001Info = new Cm001Info(
                    planData.PlanNumber.Value,
                    planData.Id,
                    null,
                    null,
                    planData.Department.Name,
                    planData.Type,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    planData.Name,
                    null,
                    null,
                    null,
                    null,
                    null,
                    planData.SupplyMethod.Label,
                    planData.SupplyMethod.Code.Value,
                    planData.SupplyMethodType != null ? planData.SupplyMethodType.Label : string.Empty,
                    planData.SupplyMethodType != null ? planData.SupplyMethodType.Code.Value : string.Empty,
                    planData.SupplyMethodSpecialType != null ? planData.SupplyMethodSpecialType.Label : string.Empty,
                    planData.SupplyMethodSpecialType != null ? planData.SupplyMethodSpecialType.Code.Value : string.Empty,
                    planData.BudgetYear,
                    planData.Budget,
                    SourceType.Plan,
                    null);
            }
            else if (deliveryAcceptanceExisting.SourceType == SourceType.ContractDraftVendor)
            {
                var contractDraftVendor = await this.dbContext.CaContractDraftVendors
                                                    .Include(caContractDraftVendor => caContractDraftVendor.ContractInvitationVendors)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.ContractDraft)
                                                    .ThenInclude(caContractDraft => caContractDraft.Procurement)
                                                    .ThenInclude(procurement => procurement.Plan)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.ContractDraft)
                                                    .ThenInclude(caContractDraft => caContractDraft.Procurement)
                                                    .ThenInclude(procurement => procurement.Department)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.ContractDraft)
                                                    .ThenInclude(caContractDraft => caContractDraft.Procurement)
                                                    .ThenInclude(procurement => procurement.SupplyMethod)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.ContractDraft)
                                                    .ThenInclude(caContractDraft => caContractDraft.Procurement)
                                                    .ThenInclude(procurement => procurement.SupplyMethodType)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.ContractDraft)
                                                    .ThenInclude(caContractDraft => caContractDraft.Procurement)
                                                    .ThenInclude(procurement => procurement.SupplyMethodSpecialType).Include(caContractDraftVendor => caContractDraftVendor.Vendor)
                                                    .ThenInclude(vendor => vendor.VendorInfo)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.ContractType).Include(caContractDraftVendor => caContractDraftVendor.Template)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.PeriodConditionType)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.DraftTermsConditions)
                                                    .AsSplitQuery()
                                                    .FirstOrDefaultAsync(p => p.Id == ContractDraftVendorId.From(deliveryAcceptanceExisting.RefId!.Value), ct);

                if (contractDraftVendor == null)
                {
                    this.ThrowError("ไม่พบข้อมูล", StatusCodes.Status404NotFound);
                }

                refCode = contractDraftVendor.ContractNumber ?? string.Empty;
                departmentOrganizationLevel = contractDraftVendor.ContractDraft.Procurement.Department.OrganizationLevel;
                cm001Info = new Cm001Info(
                    contractDraftVendor.ContractDraft.Procurement.Plan?.PlanNumber.Value,
                    contractDraftVendor.ContractDraft.Procurement.Plan?.Id,
                    contractDraftVendor.ContractDraft.Procurement.ProcurementNumber?.Value.ToString(),
                    contractDraftVendor.ContractDraft.Procurement.Id,
                    contractDraftVendor.ContractDraft.Procurement.Department.Name,
                    contractDraftVendor.ContractDraft.Procurement.Plan?.Type,
                    $"{contractDraftVendor.Vendor.VendorInfo.SapVendorNumber} : {contractDraftVendor.Vendor.VendorInfo.EstablishmentName}",
                    contractDraftVendor.Vendor.VendorInfo.EstablishmentName,
                    contractDraftVendor.Vendor.VendorInfo.Email ?? contractDraftVendor.ContractInvitationVendors.Email,
                    contractDraftVendor.ContractNumber,
                    contractDraftVendor.PoNumber,
                    contractDraftVendor.ContractInvitationVendors.AgreedPrice,
                    contractDraftVendor.ContractName,
                    contractDraftVendor.ContractType?.Label,
                    contractDraftVendor.Template?.Label,
                    contractDraftVendor.ContractSignedDate,
                    contractDraftVendor.PeriodConditionType?.Label,
                    contractDraftVendor.EndDate,
                    contractDraftVendor.ContractDraft.Procurement.SupplyMethod.Label,
                    contractDraftVendor.ContractDraft.Procurement.SupplyMethodCode.Value,
                    contractDraftVendor.ContractDraft.Procurement.SupplyMethodType != null ? contractDraftVendor.ContractDraft.Procurement.SupplyMethodType.Label : string.Empty,
                    contractDraftVendor.ContractDraft.Procurement.SupplyMethodType != null ? contractDraftVendor.ContractDraft.Procurement.SupplyMethodType.Code.Value : string.Empty,
                    contractDraftVendor.ContractDraft.Procurement.SupplyMethodSpecialType != null ? contractDraftVendor.ContractDraft.Procurement.SupplyMethodSpecialType.Label : string.Empty,
                    contractDraftVendor.ContractDraft.Procurement.SupplyMethodSpecialType != null ? contractDraftVendor.ContractDraft.Procurement.SupplyMethodSpecialType.Code.Value : string.Empty,
                    contractDraftVendor.ContractDraft.Procurement.SupplyMethodSpecialType != null ? contractDraftVendor.ContractDraft.Procurement.Plan?.BudgetYear : null,
                    contractDraftVendor.ContractDraft.Procurement.Plan?.Budget,
                    SourceType.ContractDraftVendor,
                    WarrantyInfoDto.MapFromEntity(contractDraftVendor.DraftTermsConditions.Warranty, deliveryAcceptanceExisting));
            }
            else if (deliveryAcceptanceExisting.SourceType == SourceType.ContractDraftVendorEdit)
            {
                var vendorEdit = await this.dbContext.CaContractDraftVendorEdits
                                                     .Include(e => e.ContractType)
                                                     .Include(e => e.Template)
                                                     .Include(e => e.PeriodConditionType)
                                                     .Include(e => e.DraftTermsConditions)
                                                     .FirstOrDefaultAsync(e => e.Id == ContractDraftVendorEditId.From(deliveryAcceptanceExisting.RefId!.Value), ct);

                if (vendorEdit == null)
                {
                    this.ThrowError("ไม่พบข้อมูลการแก้ไขร่างสัญญา", StatusCodes.Status404NotFound);
                }

                var vendorForEdit = await this.dbContext.CaContractDraftVendors
                                                        .Include(v => v.ContractInvitationVendors)
                                                        .Include(v => v.Vendor).ThenInclude(vd => vd.VendorInfo)
                                                        .Include(v => v.ContractDraft).ThenInclude(d => d.Procurement).ThenInclude(p => p.Plan)
                                                        .Include(v => v.ContractDraft).ThenInclude(d => d.Procurement).ThenInclude(p => p.Department)
                                                        .Include(v => v.ContractDraft).ThenInclude(d => d.Procurement).ThenInclude(p => p.SupplyMethod)
                                                        .Include(v => v.ContractDraft).ThenInclude(d => d.Procurement).ThenInclude(p => p.SupplyMethodType)
                                                        .Include(v => v.ContractDraft).ThenInclude(d => d.Procurement).ThenInclude(p => p.SupplyMethodSpecialType)
                                                        .AsSplitQuery()
                                                        .FirstOrDefaultAsync(v => v.Id == vendorEdit.ContractDraftVendorId, ct);

                if (vendorForEdit == null)
                {
                    this.ThrowError("ไม่พบข้อมูลสัญญาต้นฉบับ", StatusCodes.Status404NotFound);
                }

                var procurement = vendorForEdit.ContractDraft.Procurement;
                refCode = vendorEdit.ContractNumber ?? string.Empty;
                departmentOrganizationLevel = procurement.Department.OrganizationLevel;
                cm001Info = new Cm001Info(
                    procurement.Plan?.PlanNumber.Value,
                    procurement.Plan?.Id,
                    procurement.ProcurementNumber?.Value.ToString(),
                    procurement.Id,
                    procurement.Department.Name,
                    procurement.Plan?.Type,
                    $"{vendorForEdit.Vendor.VendorInfo.SapVendorNumber} : {vendorForEdit.Vendor.VendorInfo.EstablishmentName}",
                    vendorForEdit.Vendor.VendorInfo.EstablishmentName,
                    vendorForEdit.Vendor.VendorInfo.Email ?? vendorForEdit.ContractInvitationVendors.Email,
                    vendorEdit.ContractNumber,
                    vendorEdit.PoNumber,
                    vendorEdit.Budget,
                    vendorEdit.ContractName,
                    vendorEdit.ContractType?.Label,
                    vendorEdit.Template?.Label,
                    vendorEdit.ContractSignedDate,
                    vendorEdit.PeriodConditionType?.Label,
                    vendorEdit.EndDate,
                    procurement.SupplyMethod.Label,
                    procurement.SupplyMethodCode.Value,
                    procurement.SupplyMethodType != null ? procurement.SupplyMethodType.Label : string.Empty,
                    procurement.SupplyMethodType != null ? procurement.SupplyMethodType.Code.Value : string.Empty,
                    procurement.SupplyMethodSpecialType != null ? procurement.SupplyMethodSpecialType.Label : string.Empty,
                    procurement.SupplyMethodSpecialType != null ? procurement.SupplyMethodSpecialType.Code.Value : string.Empty,
                    procurement.SupplyMethodSpecialType != null ? procurement.Plan?.BudgetYear : null,
                    procurement.Plan?.Budget,
                    SourceType.ContractDraftVendorEdit,
                    WarrantyInfoDto.MapFromEntity(vendorEdit.DraftTermsConditions.Warranty, deliveryAcceptanceExisting));
            }
            else if (deliveryAcceptanceExisting.SourceType == SourceType.Procurement)
            {
                var poaData = await this.dbContext.PPurchaseOrderApprovals
                                                .Include(poa => poa.Procurement).ThenInclude(p => p.Plan)
                                                .Include(poa => poa.Procurement).ThenInclude(p => p.Department)
                                                .Include(poa => poa.Procurement).ThenInclude(p => p.SupplyMethod)
                                                .Include(poa => poa.Procurement).ThenInclude(p => p.SupplyMethodType)
                                                .Include(poa => poa.Procurement).ThenInclude(p => p.SupplyMethodSpecialType)
                                                .AsSplitQuery()
                                                .FirstOrDefaultAsync(poa => poa.Id == PurchaseOrderApprovalId.From(deliveryAcceptanceExisting.RefId!.Value), ct);

                if (poaData == null)
                {
                    this.ThrowError("ไม่พบข้อมูลใบสั่งซื้อ/จ้าง/เช่า", StatusCodes.Status404NotFound);
                }

                var procurementData = poaData.Procurement;

                refCode = (string)procurementData.ProcurementNumber.Value;
                departmentOrganizationLevel = procurementData.Department.OrganizationLevel;
                cm001Info = new Cm001Info(
                    procurementData.Plan.PlanNumber.Value,
                    procurementData.Plan.Id,
                    (string?)procurementData.ProcurementNumber.Value,
                    procurementData.Id,
                    procurementData.Department.Name,
                    procurementData.Plan.Type,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    procurementData.Name,
                    null,
                    null,
                    null,
                    null,
                    null,
                    procurementData.SupplyMethod.Label,
                    procurementData.SupplyMethodCode.Value,
                    procurementData.SupplyMethodType != null ? procurementData.SupplyMethodType.Label : string.Empty,
                    procurementData.SupplyMethodType != null ? procurementData.SupplyMethodType.Code.Value : string.Empty,
                    procurementData.SupplyMethodSpecialType != null ? procurementData.SupplyMethodSpecialType.Label : string.Empty,
                    procurementData.SupplyMethodSpecialType != null ? procurementData.SupplyMethodSpecialType.Code.Value : string.Empty,
                    procurementData.BudgetYear,
                    procurementData.Budget,
                    SourceType.Procurement,
                    null);
            }
            else if (deliveryAcceptanceExisting.SourceType == SourceType.Manual)
            {
                refCode = deliveryAcceptanceExisting.Number;
                departmentOrganizationLevel = deliveryAcceptanceExisting.Department?.OrganizationLevel;
                cm001Info = new Cm001Info(
                    null,
                    null,
                    null,
                    null,
                    deliveryAcceptanceExisting.Department?.Name ?? string.Empty,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    deliveryAcceptanceExisting.Name,
                    null,
                    null,
                    null,
                    null,
                    null,
                    deliveryAcceptanceExisting.SupplyMethod?.Label,
                    deliveryAcceptanceExisting.SupplyMethodCode?.Value,
                    deliveryAcceptanceExisting.SupplyMethodType?.Label ?? string.Empty,
                    deliveryAcceptanceExisting.SupplyMethodType?.Code.Value ?? string.Empty,
                    deliveryAcceptanceExisting.SupplyMethodSpecialType?.Label ?? string.Empty,
                    deliveryAcceptanceExisting.SupplyMethodSpecialType?.Code.Value ?? string.Empty,
                    null,
                    deliveryAcceptanceExisting.Budget,
                    SourceType.Manual,
                    null);
            }
            else
            {
                this.ThrowError("SourceType ไม่ถูกต้อง", StatusCodes.Status400BadRequest);
            }

            var canApproveDeliveryAcceptance =
                deliveryAcceptanceExisting.Periods
                    .SelectMany(p => p.Acceptors)
                    .Any(a =>
                        a.Type == AcceptorType.AcceptanceCommittee &&
                        a.UserId == UserId.From(req.UserId) &&
                        a.IsActive);

            var result =
                new GetDeliveryAcceptanceByIdResponse(
                    deliveryAcceptanceExisting.Id,
                    deliveryAcceptanceExisting.Status,
                    deliveryAcceptanceExisting.ContractType,
                    periods,
                    cm001Info,
                    deliveryAcceptanceExisting.SourceType,
                    deliveryAcceptanceExisting.RefId,
                    refCode,
                    canApproveDeliveryAcceptance,
                    deliveryAcceptanceExisting.Number,
                    deliveryAcceptanceExisting.DepartmentId?.Value,
                    departmentOrganizationLevel,
                    deliveryAcceptanceExisting.SupplyMethodCode?.Value,
                    deliveryAcceptanceExisting.SupplyMethodTypeCode?.Value,
                    deliveryAcceptanceExisting.SupplyMethodSpecialTypeCode?.Value,
                    deliveryAcceptanceExisting.Name,
                    deliveryAcceptanceExisting.Budget,
                    deliveryAcceptanceExisting.IsCommercialMaterial);

            return TypedResults.Ok(result);

            DeliveryAcceptancePeriodInfoDto[] CreateDeliveryAcceptancePeriods()
            {
                return
                [
                    .. deliveryAcceptanceExisting
                       .Periods
                       .OrderBy(x => x.PaymentTerms.Min(pt => pt.PaymentTerm))
                       .ThenBy(x => x.AuditInfo.CreatedAt)
                       .Select((p, index) =>
                       {
                           var paymentTermMerge = string.Join(",", p.PaymentTerms.OrderBy(z => z.Sequence).Select(x => x.PaymentTerm));

                           var descriptionMerge = string.Join(Environment.NewLine, p.PaymentTerms.OrderBy(z => z.Sequence).Select(x => $"งวดที่ {x.PaymentTerm} {x.Description} จำนวนเงินรวม/งวด {x.Amount.ToCurrencyStringWithComma()} บาท"));

                           return new DeliveryAcceptancePeriodInfoDto(
                               p.Id,
                               p.Status,
                               p.AcceptanceNumber ?? string.Empty,
                               paymentTermMerge,
                               descriptionMerge,
                               p.AccountStatus);
                       })
                ];
            }
        }
    }
}