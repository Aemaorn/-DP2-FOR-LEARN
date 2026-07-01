namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod;

using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetById
{
    public record GetDeliveryAcceptancePeriodByIdRequest(
        [property: FromClaim(JwtRegisteredClaimNames.Sub)]
        Guid UserId,
        Guid DeliveryAcceptanceId,
        Guid? Id);

    public record DeliveryAcceptancePeriodDocumentVersionResponse(
        Guid FileId,
        string Version,
        DateTimeOffset CreatedAt,
        string CreatedByName,
        bool IsCurrent);

    public record InspectionCommitteeInfoResponse(
        [property: Description("รหัสคณะกรรมการ")]
        Guid? Id,
        [property: Description("รหัสผู้ใช้งาน")]
        Guid UserId,
        [property: Description("ชื่อ-สกุล")] string FullName,
        [property: Description("ชื่อตำแหน่งเต็ม")]
        string FullPositionName,
        [property: Description("รหัสตำแหน่งคณะกรรมการ")]
        string? CommitteePositionsCode,
        [property: Description("ชื่อตำแหน่งคณะกรรมการ")]
        string? CommitteePositionName,
        [property: Description("ลำดับ")] int Sequence);

    public record InspectionCommitteeSectionResponse(
        [property: Description("คณะกรรมการตรวจรับ")]
        IEnumerable<InspectionCommitteeInfoResponse> Committees,
        [property: Description("เป็นคณะกรรมการ")]
        bool IsCommittee);

    public record GetDeliveryAcceptancePeriodByIdResponse(
        [property: Description("รหัสงวดการตรวจรับการส่งมอบ")]
        CmDeliveryAcceptancePeriodId? Id,
        [property: Description("สถานะงวดการตรวจรับการส่งมอบ")]
        CmDeliveryAcceptancePeriodStatus Status,
        string? AcceptanceNumber,
        string? Description,
        string? PhoneNumber,
        decimal? ContractBudgetAmount,
        string? ObjectiveDescription,
        bool HasDeduction,
        string? DeductionDescription,
        decimal? DeductionAmount,
        bool HasInvoiceSlip,
        string? InvoiceSlipDescription,
        decimal? InvoiceSlipAmount,
        Cm001PaymentTermResponse[] PaymentTerms,
        [property: Description("มีการมอบหมาย จพ.")]
        bool HasJorPorAssign,
        [property: Description("มีสิทธิ์แก้ไข")]
        bool HasEditPermission,
        [property: Description("คณะกรรมการตรวจรับ")]
        IEnumerable<AcceptorNoIdResponse> AcceptanceCommittees,
        [property: Description("ผู้รับมอบหมาย")]
        IEnumerable<AssigneeNoIdResponse> Assignees,
        [property: Description("ผู้อนุมัติ")] IEnumerable<AcceptorNoIdResponse> Acceptors,
        Guid? DocumentId,
        bool? IsDocumentReplaced,
        [property: Description("ข้อมูลสัญญา")] DeliveryAcceptance.GetById.Cm001Info? Cm001Info,
        string? SupplyMethod,
        ParameterCode? SupplyMethodCode,
        string? SupplyMethodType,
        ParameterCode? SupplyMethodTypeCode,
        string? SupplyMethodSpecialType,
        ParameterCode? SupplyMethodSpecialTypeCode,
        BudgetDetail[] BudgetDetails,
        bool IsCommercialMaterial,
        string DepartmentCode,
        string? DepartmentOrganizationLevel,
        [property: Description("สถานะบัญชี")] CmDeliveryAcceptancePeriodAccountStatus AccountStatus,
        [property: Description("วันที่เบิกจ่าย")]
        DateTimeOffset? DisbursementDate,
        [property: Description("จำนวนเงินเบิกจ่าย")]
        decimal? DisbursementAmount,
        [property: Description("หมายเหตุการเบิกจ่าย")]
        string? DisbursementRemark,
        [property: Description("ผู้ตรวจสอบบัญชี")]
        IEnumerable<AcceptorNoIdResponse> AcceptanceOfAccounting,
        IEnumerable<AcceptorNoIdResponse> AcceptanceConfirmers,
        int[] PaymentTermOnUser,
        decimal TotalPaymentOnUser,
        DeliveryAcceptancePeriodDocumentVersionResponse[]? DocumentVersions = null,
        [property: Description("คณะกรรมการตรวจรับ")]
        InspectionCommitteeSectionResponse? InspectionCommittees = null,
        [property: Description("เอกสารแนบ")] AttachmentsDtoWithId[]? Attachments = null,
        [property: Description("วันที่เอกสาร")]
        DateTimeOffset? DocumentDate = null);

    public record Cm001PaymentTermResponse(
        Guid? Id,
        int Sequence,
        int PaymentTerm,
        string Description,
        decimal Amount);

    public class GetCmDeliveryAcceptancePeriodById
        : DeliveryAcceptancePeriodEndpointBase<
            GetDeliveryAcceptancePeriodByIdRequest,
            Results<Ok<GetDeliveryAcceptancePeriodByIdResponse>, NotFound<string>>>
    {
        private readonly Dp2DbContext dbContext;
        private readonly IOperationService operationService;

        public GetCmDeliveryAcceptancePeriodById(
            Dp2DbContext dbContext,
            ILogger<GetCmDeliveryAcceptancePeriodById> logger,
            IOperationService operationService,
            ICommandTextService commandTextService)
            : base(dbContext, logger, operationService, commandTextService)
        {
            this.dbContext = dbContext;
            this.operationService = operationService;
        }

        public override void Configure()
        {
            this.Get("delivery-acceptance/{DeliveryAcceptanceId:guid?}/period/{Id:guid?}");
            this.Description(b => b
                                  .WithTags("ContractManagement/DeliveryAcceptance/Period")
                                  .Produces<GetDeliveryAcceptancePeriodByIdResponse>()
                                  .Produces<string>(StatusCodes.Status404NotFound));
        }

        protected override async ValueTask<Results<Ok<GetDeliveryAcceptancePeriodByIdResponse>, NotFound<string>>>
            HandleRequestAsync(
                GetDeliveryAcceptancePeriodByIdRequest req,
                CancellationToken ct)
        {
            if (!req.Id.HasValue)
            {
                var defaultData = await this.BuildDefaultResponseAsync(req.UserId, req.DeliveryAcceptanceId, ct);

                return TypedResults.Ok(defaultData);
            }

            var periodExisting = await this.GetById(
                CmDeliveryAcceptanceId.From(req.DeliveryAcceptanceId),
                CmDeliveryAcceptancePeriodId.From(req.Id.Value),
                ct);

            if (periodExisting is null)
            {
                this.ThrowError(
                    r => req.Id,
                    $"ไม่พบ งวดการส่งมอบ/ตรวจรับ {req.Id} ในระบบ",
                    StatusCodes.Status404NotFound);
            }

            var cmDeliveryAcceptance = await this.dbContext.CmDeliveryAcceptances
                                                 .Where(c => c.Id == CmDeliveryAcceptanceId.From(req.DeliveryAcceptanceId))
                                                 .Include(x => x.Periods)
                                                 .Include(x => x.Department)
                                                 .Include(x => x.SupplyMethod)
                                                 .Include(x => x.SupplyMethodType)
                                                 .Include(x => x.SupplyMethodSpecialType)
                                                 .FirstOrDefaultAsync(ct);

            if (cmDeliveryAcceptance == null)
            {
                this.ThrowError("ไม่พบ ข้อมูลการส่งมอบ/ตรวจรับ ในระบบ", StatusCodes.Status404NotFound);
            }

            var acceptanceCommittees = this.GetAcceptanceCommittees(periodExisting, req);
            var acceptors = MapAcceptorsForFindCase(periodExisting, AcceptorType.Approver);
            var accountingConfirmers = MapAcceptorsForFindCase(periodExisting, AcceptorType.AccountingConfirmer);
            var assignees = await this.MapToAssigneeResponse(periodExisting, ct);
            var hasJorPorAssign = await this.HasJorPorAssign(periodExisting, req.UserId, ct);
            var lastedDocumentHistory = periodExisting.Document;
            var isReplacedDoc = periodExisting.DocumentHistories.Any(d => d.IsReplaced);

            var documentVersions = periodExisting.DocumentHistories
                                                 .OrderVersions()
                                                 .Select((d, index) => new DeliveryAcceptancePeriodDocumentVersionResponse(
                                                     d.FileId.Value,
                                                     d.Version,
                                                     d.CreatedAt,
                                                     d.CreatedByName ?? string.Empty,
                                                     index == 0))
                                                 .ToArray();

            DeliveryAcceptance.GetById.Cm001Info cm001Info = default!;
            string departmentCode = string.Empty;
            string? departmentOrganizationLevel = null;

            if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.Plan)
            {
                var planData = await this.dbContext.Plans
                                         .Include(plan => plan.Department)
                                         .Include(plan => plan.SupplyMethod)
                                         .Include(plan => plan.SupplyMethodType)
                                         .Include(plan => plan.SupplyMethodSpecialType)
                                         .FirstOrDefaultAsync(p => p.Id == PlanId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

                if (planData == null)
                {
                    this.ThrowError("ไม่พบข้อมูลแผน", StatusCodes.Status404NotFound);
                }

                departmentCode = planData.Department.Id.Value ?? string.Empty;
                departmentOrganizationLevel = planData.Department.OrganizationLevel;

                cm001Info = new DeliveryAcceptance.GetById.Cm001Info(
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
            else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.ContractDraftVendor)
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
                                                    .ThenInclude(procurement => procurement.SupplyMethodSpecialType)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.Vendor)
                                                    .ThenInclude(v => v.VendorInfo)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.ContractType)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.Template)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.PeriodConditionType)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.DraftTermsConditions)
                                                    .AsSplitQuery()
                                                    .FirstOrDefaultAsync(p => p.Id == ContractDraftVendorId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

                if (contractDraftVendor == null)
                {
                    this.ThrowError("ไม่พบข้อมูล", StatusCodes.Status404NotFound);
                }

                var vendorName = !string.IsNullOrWhiteSpace(contractDraftVendor.Vendor.EstablishmentName) switch
                {
                    true => contractDraftVendor.Vendor.EstablishmentName,
                    false => contractDraftVendor.Vendor?.VendorInfo?.EstablishmentName ?? string.Empty,
                };

                departmentCode = contractDraftVendor.ContractDraft.Procurement.Department.Id.Value ?? string.Empty;
                departmentOrganizationLevel = contractDraftVendor.ContractDraft.Procurement.Department.OrganizationLevel;

                cm001Info = new DeliveryAcceptance.GetById.Cm001Info(
                    contractDraftVendor.ContractDraft.Procurement.Plan?.PlanNumber.Value,
                    contractDraftVendor.ContractDraft.Procurement.Plan?.Id,
                    contractDraftVendor.ContractDraft.Procurement.ProcurementNumber?.Value.ToString(),
                    contractDraftVendor.ContractDraft.Procurement.Id,
                    contractDraftVendor.ContractDraft.Procurement.Department.Name,
                    contractDraftVendor.ContractDraft.Procurement.Plan?.Type,
                    vendorName,
                    contractDraftVendor.Vendor?.EstablishmentName,
                    contractDraftVendor.ContractInvitationVendors.Email,
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
                    null);
            }
            else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.Procurement)
            {
                var poaData = await this.dbContext.PPurchaseOrderApprovals

                                        .Include(poa => poa.Procurement).ThenInclude(p => p.Plan)
                                        .Include(poa => poa.Procurement).ThenInclude(p => p.Department)
                                        .Include(poa => poa.Procurement).ThenInclude(p => p.SupplyMethod)
                                        .Include(poa => poa.Procurement).ThenInclude(p => p.SupplyMethodType)
                                        .Include(poa => poa.Procurement).ThenInclude(p => p.SupplyMethodSpecialType)
                                        .AsSplitQuery()
                                        .FirstOrDefaultAsync(poa => poa.Id == PurchaseOrderApprovalId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

                if (poaData == null)
                {
                    this.ThrowError("ไม่พบข้อมูลใบสั่งซื้อ/จ้าง/เช่า", StatusCodes.Status404NotFound);
                }

                var procurementData = poaData.Procurement;

                departmentCode = procurementData.Department.Id.Value ?? string.Empty;
                departmentOrganizationLevel = procurementData.Department.OrganizationLevel;

                cm001Info = new DeliveryAcceptance.GetById.Cm001Info(
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
            else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.ContractDraftVendorEdit)
            {
                var vendorEditData = await this.dbContext.CaContractDraftVendorEdits
                    .Include(ve => ve.Vendor).ThenInclude(v => v.VendorInfo)
                    .Include(ve => ve.ContractType)
                    .Include(ve => ve.Template)
                    .Include(ve => ve.PeriodConditionType)
                    .Include(ve => ve.DraftTermsConditions)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(ve => ve.Id == ContractDraftVendorEditId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

                if (vendorEditData == null)
                {
                    this.ThrowError("ไม่พบข้อมูลบันทึกต่อท้ายสัญญา", StatusCodes.Status404NotFound);
                }

                var cdvForEdit = await this.dbContext.CaContractDraftVendors
                    .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.Plan)
                    .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.Department)
                    .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.SupplyMethod)
                    .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.SupplyMethodType)
                    .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.SupplyMethodSpecialType)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(v => v.Id == vendorEditData.ContractDraftVendorId, ct);

                if (cdvForEdit == null)
                {
                    this.ThrowError("ไม่พบข้อมูลสัญญาต้นฉบับ", StatusCodes.Status404NotFound);
                }

                var vendorInfoEdit = vendorEditData.Vendor.VendorInfo;
                var vendorNameEdit = string.IsNullOrWhiteSpace(vendorInfoEdit?.EstablishmentName)
                    ? $"{vendorInfoEdit?.SapVendorNumber}"
                    : $"{vendorInfoEdit?.SapVendorNumber} : {vendorInfoEdit?.EstablishmentName}";
                var procEdit = cdvForEdit.ContractDraft.Procurement;

                departmentCode = procEdit.Department.Id.Value ?? string.Empty;
                departmentOrganizationLevel = procEdit.Department.OrganizationLevel;

                cm001Info = new DeliveryAcceptance.GetById.Cm001Info(
                    procEdit.Plan?.PlanNumber.Value,
                    procEdit.Plan?.Id,
                    procEdit.ProcurementNumber?.Value.ToString(),
                    procEdit.Id,
                    procEdit.Department.Name,
                    procEdit.Plan?.Type,
                    vendorNameEdit,
                    vendorInfoEdit?.EstablishmentName,
                    vendorEditData.Email,
                    vendorEditData.ContractNumber,
                    vendorEditData.PoNumber,
                    vendorEditData.Budget,
                    vendorEditData.ContractName,
                    vendorEditData.ContractType?.Label,
                    vendorEditData.Template?.Label,
                    vendorEditData.ContractSignedDate,
                    vendorEditData.PeriodConditionType?.Label,
                    vendorEditData.EndDate,
                    procEdit.SupplyMethod.Label,
                    procEdit.SupplyMethodCode.Value,
                    procEdit.SupplyMethodType != null ? procEdit.SupplyMethodType.Label : string.Empty,
                    procEdit.SupplyMethodType != null ? procEdit.SupplyMethodType.Code.Value : string.Empty,
                    procEdit.SupplyMethodSpecialType != null ? procEdit.SupplyMethodSpecialType.Label : string.Empty,
                    procEdit.SupplyMethodSpecialType != null ? procEdit.SupplyMethodSpecialType.Code.Value : string.Empty,
                    procEdit.SupplyMethodSpecialType != null ? procEdit.Plan?.BudgetYear : null,
                    procEdit.Plan?.Budget,
                    SourceType.ContractDraftVendorEdit,
                    null);
            }
            else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.Manual)
            {
                var manualParent = periodExisting.CmDeliveryAcceptance;
                departmentCode = manualParent.DepartmentId?.Value ?? string.Empty;
                departmentOrganizationLevel = manualParent.Department?.OrganizationLevel;
                cm001Info = new DeliveryAcceptance.GetById.Cm001Info(
                    null,
                    null,
                    null,
                    null,
                    manualParent.Department?.Name ?? string.Empty,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    manualParent.Name,
                    null,
                    null,
                    null,
                    null,
                    null,
                    manualParent.SupplyMethod?.Label,
                    manualParent.SupplyMethodCode?.Value,
                    manualParent.SupplyMethodType?.Label ?? string.Empty,
                    manualParent.SupplyMethodType?.Code.Value ?? string.Empty,
                    manualParent.SupplyMethodSpecialType?.Label ?? string.Empty,
                    manualParent.SupplyMethodSpecialType?.Code.Value ?? string.Empty,
                    null,
                    manualParent.Budget,
                    SourceType.Manual,
                    null);
            }
            else
            {
                this.ThrowError("SourceType ไม่ถูกต้อง", StatusCodes.Status400BadRequest);
            }

            var inspectionCommittees = MapInspectionCommitteeSection(periodExisting);

            var result = new GetDeliveryAcceptancePeriodByIdResponse(
                periodExisting.Id,
                periodExisting.Status,
                periodExisting.AcceptanceNumber,
                periodExisting.Description,
                periodExisting.PhoneNumber,
                periodExisting.ContractBudgetAmount,
                periodExisting.ObjectiveDescription,
                periodExisting.HasDeduction,
                periodExisting.DeductionDescription,
                periodExisting.DeductionAmount,
                periodExisting.HasInvoiceSlip,
                periodExisting.InvoiceSlipDescription,
                periodExisting.InvoiceSlipAmount,
                periodExisting.PaymentTerms
                              .OrderBy(pt => pt.Sequence)
                              .Select(pt => new Cm001PaymentTermResponse(
                                  (Guid?)pt.Id,
                                  pt.Sequence,
                                  pt.PaymentTerm,
                                  pt.Description,
                                  pt.Amount))
                              .ToArray(),
                hasJorPorAssign,
                acceptanceCommittees.Length <= 0 || acceptanceCommittees.Any(x => x.UserId == req.UserId),
                acceptanceCommittees,
                assignees,
                acceptors,
                lastedDocumentHistory?.FileId.Value,
                isReplacedDoc,
                cm001Info,
                string.Empty,
                null,
                string.Empty,
                null,
                string.Empty,
                null,
                periodExisting.Budgets
                              .Select(b => new BudgetDetail(
                                  b.Id.Value,
                                  b.Sequence,
                                  b.Department,
                                  b.BudgetTypeCode.Value,
                                  b.ProjectCode,
                                  b.AccountNoCode.Value,
                                  b.Budget))
                              .ToArray(),
                false,
                departmentCode,
                departmentOrganizationLevel,
                periodExisting.AccountStatus,
                periodExisting.DisbursementDate,
                periodExisting.DisbursementAmount,
                periodExisting.DisbursementRemark,
                [.. MapAcceptorsForFindCase(periodExisting, AcceptorType.AccountingOperator), .. MapAcceptorsForFindCase(periodExisting, AcceptorType.Accounting)],
                accountingConfirmers,
                cmDeliveryAcceptance.Periods
                                    .Where(y => y.Id != periodExisting.Id && y.Status != CmDeliveryAcceptancePeriodStatus.Draft && y.Status != CmDeliveryAcceptancePeriodStatus.Edit &&
                                                y.Status != CmDeliveryAcceptancePeriodStatus.Rejected).SelectMany(x => x.PaymentTerms).Select(y => y.PaymentTerm).Distinct().OrderBy(t => t).ToArray(),
                cmDeliveryAcceptance.Periods
                                    .Where(y => y.Id != periodExisting.Id && y.Status != CmDeliveryAcceptancePeriodStatus.Draft && y.Status != CmDeliveryAcceptancePeriodStatus.Edit &&
                                                y.Status != CmDeliveryAcceptancePeriodStatus.Rejected).Sum(x => x.PaymentTerms.Sum(y => y.Amount)),
                documentVersions,
                inspectionCommittees,
                periodExisting.Attachments
                              .GroupBy(a => a.DocumentTypeCode.Value)
                              .Select(g => new AttachmentsDtoWithId(
                                  g.Key,
                                  g.OrderBy(a => a.Sequence)
                                   .Select(a => new FileAttachmentsWithId(
                                       a.Id.Value,
                                       a.FileId.Value,
                                       a.FileName,
                                       a.Sequence,
                                       a.IsPublic,
                                       a.AuditInfo.CreatedBy))
                                   .ToArray()))
                              .ToArray(),
                periodExisting.DocumentDate);

            return TypedResults.Ok(result);
        }

        private async Task<GetDeliveryAcceptancePeriodByIdResponse> BuildDefaultResponseAsync(Guid userId, Guid deliveryAcceptanceId, CancellationToken ct)
        {
            var cmDeliveryAcceptanceData = await this.dbContext.CmDeliveryAcceptances
                                                     .Where(c => c.Id == CmDeliveryAcceptanceId.From(deliveryAcceptanceId))
                                                     .Include(x => x.Periods)
                                                     .Include(x => x.Department)
                                                     .Include(x => x.SupplyMethod)
                                                     .Include(x => x.SupplyMethodType)
                                                     .Include(x => x.SupplyMethodSpecialType)
                                                     .FirstOrDefaultAsync(ct);

            if (cmDeliveryAcceptanceData == null)
            {
                this.ThrowError("ไม่พบ ข้อมูลการส่งมอบ/ตรวจรับ ในระบบ", StatusCodes.Status404NotFound);
            }

            Domain.Procurement.Procurement? procurementData = null;

            AcceptorNoIdResponse[] acceptanceCommitteesData = [];

            DeliveryAcceptance.GetById.Cm001Info? cm001Info = null;

            InspectionCommitteeSectionResponse inspectionCommitteesData = new InspectionCommitteeSectionResponse([], false);

            if (cmDeliveryAcceptanceData.SourceType == SourceType.Plan)
            {
                var planData = await this.dbContext.Plans
                                         .Include(plan => plan.Department)
                                         .Include(plan => plan.SupplyMethod)
                                         .Include(plan => plan.SupplyMethodType)
                                         .Include(plan => plan.SupplyMethodSpecialType)
                                         .FirstOrDefaultAsync(p => p.Id == PlanId.From((Guid)cmDeliveryAcceptanceData.RefId), ct);

                if (planData == null)
                {
                    this.ThrowError("ไม่พบข้อมูลแผน", StatusCodes.Status404NotFound);
                }

                cm001Info = new DeliveryAcceptance.GetById.Cm001Info(
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
            else if (cmDeliveryAcceptanceData.SourceType == SourceType.ContractDraftVendor)
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
                                                    .ThenInclude(procurement => procurement.SupplyMethodSpecialType)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.Vendor)
                                                    .ThenInclude(vendor => vendor.VendorInfo)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.ContractType)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.Template)
                                                    .Include(caContractDraftVendor => caContractDraftVendor.PeriodConditionType)
                                                    .AsSplitQuery()
                                                    .FirstOrDefaultAsync(p => p.Id == ContractDraftVendorId.From((Guid)cmDeliveryAcceptanceData.RefId), ct);

                if (contractDraftVendor == null)
                {
                    this.ThrowError("ไม่พบข้อมูล", StatusCodes.Status404NotFound);
                }

                acceptanceCommitteesData = contractDraftVendor.ContractDraft.Procurement.Type == ProcurementType.Rent
                    ? await GetAcceptanceCommitteeFromRental()
                    : await GetAcceptanceCommitteeFromJp005();

                if (!acceptanceCommitteesData.Any())
                {
                    acceptanceCommitteesData = await GetAcceptanceCommitteeFromContractDraftVendor();
                }

                inspectionCommitteesData = this.MapInspectionCommitteeSectionFromCommitteesData(acceptanceCommitteesData);

                async Task<AcceptorNoIdResponse[]> GetAcceptanceCommitteeFromJp005()
                {
                    var committees = await this.dbContext.PJp005S
                                               .Include(f => f.Committees)
                                               .ThenInclude(c => c.User)
                                               .ThenInclude(e => e.Employee)
                                               .ThenInclude(e => e.View)
                                               .Where(w => w.ProcurementId == contractDraftVendor.ContractDraft.ProcurementId)
                                               .SelectMany(s => s.Committees)
                                               .Where(w => w.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
                                               .OrderBy(o => o.Sequence)
                                               .ToListAsync(ct);

                    if (!committees.Any())
                    {
                        return [];
                    }

                    return committees
                           .Select(a => CreateAcceptorResponse(
                               null,
                               AcceptorType.AcceptanceCommittee,
                               a.User.Id.Value,
                               a.Sequence,
                               a.FullName,
                               positionName: a.User.Employee.View?.FullPositionName.Trim() ?? string.Empty,
                               string.Empty,
                               AcceptorStatus.Draft,
                               committeePositionsCode: a.CommitteePositionsCode.Value,
                               committeePositionName: a.CommitteePositionsName))
                           .ToArray();
                }

                async Task<AcceptorNoIdResponse[]> GetAcceptanceCommitteeFromContractDraftVendor()
                {
                    var approvalData = await this.dbContext.PPurchaseOrderApprovals
                                                 .Include(a => a.Assignees)
                                                 .ThenInclude(u => u.User)
                                                 .ThenInclude(e => e.Employee)
                                                 .Include(c => c.Acceptors)
                                                 .ThenInclude(a => a.User)
                                                 .ThenInclude(a => a.Employee)
                                                 .ThenInclude(a => a.View)
                                                 .Include(pPurchaseOrderApproval => pPurchaseOrderApproval.Contracts)
                                                 .ThenInclude(pPurchaseOrderApprovalContract => pPurchaseOrderApprovalContract.Entrepreneur)
                                                 .ThenInclude(entrepreneur => entrepreneur!.SuVendor)
                                                 .Include(pPurchaseOrderApproval => pPurchaseOrderApproval.Contracts)
                                                 .ThenInclude(pPurchaseOrderApprovalContract => pPurchaseOrderApprovalContract.PPurchaseOrderApprovalEntrepreneurs)
                                                 .ThenInclude(pPurchaseOrderApprovalEntrepreneurs => pPurchaseOrderApprovalEntrepreneurs!.Vendor)
                                                 .AsSplitQuery()
                                                 .FirstOrDefaultAsync(
                                                     a => a.ProcurementId == contractDraftVendor.ContractDraft.ProcurementId, ct);

                    if (approvalData is not null)
                    {
                        return this.GetAcceptanceCommitteesData(approvalData);
                    }

                    return [];
                }

                async Task<AcceptorNoIdResponse[]> GetAcceptanceCommitteeFromRental()
                {
                    var committees = await this.dbContext.PPrincipleApprovals
                                               .Include(c => c.PrincipleApprovalCommittees)
                                               .ThenInclude(c => c.User)
                                               .ThenInclude(e => e.Employee)
                                               .ThenInclude(e => e.View)
                                               .Where(w => w.ProcurementId == contractDraftVendor.ContractDraft.ProcurementId)
                                               .SelectMany(s => s.PrincipleApprovalCommittees)
                                               .Where(w => w.GroupType == CommitteeGroupType.AcceptanceCommittee)
                                               .OrderBy(o => o.Sequence)
                                               .ToArrayAsync(ct);

                    if (!committees.Any())
                    {
                        return [];
                    }

                    return committees
                           .Select(a => CreateAcceptorResponse(
                               null,
                               AcceptorType.AcceptanceCommittee,
                               a.User.Id.Value,
                               a.Sequence,
                               a.FullName,
                               a.User.Employee.View?.FullPositionName.Trim() ?? string.Empty,
                               string.Empty,
                               AcceptorStatus.Draft,
                               committeePositionsCode: a.CommitteePositionsCode.Value,
                               committeePositionName: a.CommitteePositionsName))
                           .ToArray();
                }

                procurementData = contractDraftVendor.ContractDraft.Procurement;

                cm001Info = new DeliveryAcceptance.GetById.Cm001Info(
                    contractDraftVendor.ContractDraft.Procurement.Plan?.PlanNumber.Value,
                    contractDraftVendor.ContractDraft.Procurement.Plan?.Id,
                    contractDraftVendor.ContractDraft.Procurement.ProcurementNumber?.Value.ToString(),
                    contractDraftVendor.ContractDraft.Procurement.Id,
                    contractDraftVendor.ContractDraft.Procurement.Department.Name,
                    contractDraftVendor.ContractDraft.Procurement.Plan?.Type,
                    $"{contractDraftVendor.Vendor.VendorInfo.SapVendorNumber} : {contractDraftVendor.Vendor.VendorInfo.EstablishmentName}",
                    contractDraftVendor.Vendor.VendorInfo.EstablishmentName,
                    contractDraftVendor.ContractInvitationVendors.Email,
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
                    null);
            }
            else if (cmDeliveryAcceptanceData.SourceType == SourceType.Procurement)
            {
                var poaData = await this.dbContext.PPurchaseOrderApprovals
                                        .Include(poa => poa.Procurement).ThenInclude(p => p.Plan)
                                        .Include(poa => poa.Procurement).ThenInclude(p => p.Department)
                                        .Include(poa => poa.Procurement).ThenInclude(p => p.SupplyMethod)
                                        .Include(poa => poa.Procurement).ThenInclude(p => p.SupplyMethodType)
                                        .Include(poa => poa.Procurement).ThenInclude(p => p.SupplyMethodSpecialType)
                                        .AsSplitQuery()
                                        .FirstOrDefaultAsync(poa => poa.Id == PurchaseOrderApprovalId.From((Guid)cmDeliveryAcceptanceData.RefId), ct);

                if (poaData == null)
                {
                    this.ThrowError("ไม่พบข้อมูลใบสั่งซื้อ/จ้าง/เช่า", StatusCodes.Status404NotFound);
                }

                procurementData = poaData.Procurement;

                acceptanceCommitteesData = await GetAcceptanceCommitteeFromProcurementJp005();

                if (!acceptanceCommitteesData.Any())
                {
                    acceptanceCommitteesData = await GetAcceptanceCommitteeFromProcurementPOA();
                }

                inspectionCommitteesData = this.MapInspectionCommitteeSectionFromCommitteesData(acceptanceCommitteesData);

                async Task<AcceptorNoIdResponse[]> GetAcceptanceCommitteeFromProcurementJp005()
                {
                    var committees = await this.dbContext.PJp005S
                                               .Include(f => f.Committees)
                                               .ThenInclude(c => c.User)
                                               .ThenInclude(e => e.Employee)
                                               .ThenInclude(e => e.View)
                                               .Where(w => w.ProcurementId == procurementData.Id)
                                               .SelectMany(s => s.Committees)
                                               .Where(w => w.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
                                               .OrderBy(o => o.Sequence)
                                               .ToListAsync(ct);

                    if (!committees.Any())
                    {
                        return [];
                    }

                    return committees
                           .Select(a => CreateAcceptorResponse(
                               null,
                               AcceptorType.AcceptanceCommittee,
                               a.User.Id.Value,
                               a.Sequence,
                               a.FullName,
                               positionName: a.User.Employee.View?.FullPositionName.Trim() ?? string.Empty,
                               string.Empty,
                               AcceptorStatus.Draft,
                               committeePositionsCode: a.CommitteePositionsCode.Value,
                               committeePositionName: a.CommitteePositionsName))
                           .ToArray();
                }

                async Task<AcceptorNoIdResponse[]> GetAcceptanceCommitteeFromProcurementPOA()
                {
                    var approvalData = await this.dbContext.PPurchaseOrderApprovals
                                                 .Include(a => a.Committees)
                                                 .ThenInclude(c => c.User)
                                                 .ThenInclude(e => e.Employee)
                                                 .ThenInclude(e => e.View)
                                                 .AsSplitQuery()
                                                 .FirstOrDefaultAsync(
                                                     a => a.ProcurementId == procurementData.Id
                                                          && a.ContractType == "CType002"
                                                          && a.Status == PurchaseOrderApprovalStatus.Assigned,
                                                     ct);

                    if (approvalData is not null)
                    {
                        return this.GetAcceptanceCommitteesData(approvalData);
                    }

                    return [];
                }

                cm001Info = new DeliveryAcceptance.GetById.Cm001Info(
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
                    procurementData.Plan.Budget,
                    SourceType.Procurement,
                    null);
            }
            else if (cmDeliveryAcceptanceData.SourceType == SourceType.ContractDraftVendorEdit)
            {
                var vendorEditDefault = await this.dbContext.CaContractDraftVendorEdits
                    .Include(ve => ve.Vendor).ThenInclude(v => v.VendorInfo)
                    .Include(ve => ve.ContractType)
                    .Include(ve => ve.Template)
                    .Include(ve => ve.PeriodConditionType)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(ve => ve.Id == ContractDraftVendorEditId.From((Guid)cmDeliveryAcceptanceData.RefId), ct);

                if (vendorEditDefault == null)
                {
                    this.ThrowError("ไม่พบข้อมูลบันทึกต่อท้ายสัญญา", StatusCodes.Status404NotFound);
                }

                var cdvDefault = await this.dbContext.CaContractDraftVendors
                    .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.Plan)
                    .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.Department)
                    .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.SupplyMethod)
                    .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.SupplyMethodType)
                    .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.SupplyMethodSpecialType)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(v => v.Id == vendorEditDefault.ContractDraftVendorId, ct);

                if (cdvDefault == null)
                {
                    this.ThrowError("ไม่พบข้อมูลสัญญาต้นฉบับ", StatusCodes.Status404NotFound);
                }

                procurementData = cdvDefault.ContractDraft.Procurement;

                acceptanceCommitteesData = procurementData.Type == ProcurementType.Rent
                    ? await GetAcceptanceCommitteeFromRentalByProcId(procurementData.Id)
                    : await GetAcceptanceCommitteeFromJp005ByProcId(procurementData.Id);

                if (!acceptanceCommitteesData.Any())
                {
                    acceptanceCommitteesData = await GetAcceptanceCommitteeFromPOAByProcId(procurementData.Id);
                }

                inspectionCommitteesData = this.MapInspectionCommitteeSectionFromCommitteesData(acceptanceCommitteesData);

                async Task<AcceptorNoIdResponse[]> GetAcceptanceCommitteeFromJp005ByProcId(ProcurementId procId)
                {
                    var committees = await this.dbContext.PJp005S
                        .Include(f => f.Committees).ThenInclude(c => c.User).ThenInclude(e => e.Employee).ThenInclude(e => e.View)
                        .Where(w => w.ProcurementId == procId)
                        .SelectMany(s => s.Committees)
                        .Where(w => w.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
                        .OrderBy(o => o.Sequence)
                        .ToListAsync(ct);

                    return committees.Select(a => CreateAcceptorResponse(
                        null,
                        AcceptorType.AcceptanceCommittee,
                        a.User.Id.Value,
                        a.Sequence,
                        a.FullName,
                        a.User.Employee.View?.FullPositionName.Trim() ?? string.Empty,
                        string.Empty,
                        AcceptorStatus.Draft,
                        committeePositionsCode: a.CommitteePositionsCode.Value,
                        committeePositionName: a.CommitteePositionsName)).ToArray();
                }

                async Task<AcceptorNoIdResponse[]> GetAcceptanceCommitteeFromPOAByProcId(ProcurementId procId)
                {
                    var approvalData = await this.dbContext.PPurchaseOrderApprovals
                        .Include(a => a.Assignees).ThenInclude(u => u.User).ThenInclude(e => e.Employee)
                        .Include(c => c.Acceptors).ThenInclude(a => a.User).ThenInclude(a => a.Employee).ThenInclude(a => a.View)
                        .Include(p => p.Contracts).ThenInclude(c => c.Entrepreneur).ThenInclude(e => e!.SuVendor)
                        .Include(p => p.Contracts).ThenInclude(c => c.PPurchaseOrderApprovalEntrepreneurs).ThenInclude(e => e!.Vendor)
                        .AsSplitQuery()
                        .FirstOrDefaultAsync(a => a.ProcurementId == procId, ct);

                    return approvalData is not null ? this.GetAcceptanceCommitteesData(approvalData) : [];
                }

                async Task<AcceptorNoIdResponse[]> GetAcceptanceCommitteeFromRentalByProcId(ProcurementId procId)
                {
                    var committees = await this.dbContext.PPrincipleApprovals
                        .Include(c => c.PrincipleApprovalCommittees).ThenInclude(c => c.User).ThenInclude(e => e.Employee).ThenInclude(e => e.View)
                        .Where(w => w.ProcurementId == procId)
                        .SelectMany(s => s.PrincipleApprovalCommittees)
                        .Where(w => w.GroupType == CommitteeGroupType.AcceptanceCommittee)
                        .OrderBy(o => o.Sequence)
                        .ToArrayAsync(ct);

                    return committees.Select(a => CreateAcceptorResponse(
                        null,
                        AcceptorType.AcceptanceCommittee,
                        a.User.Id.Value,
                        a.Sequence,
                        a.FullName,
                        a.User.Employee.View?.FullPositionName.Trim() ?? string.Empty,
                        string.Empty,
                        AcceptorStatus.Draft,
                        committeePositionsCode: a.CommitteePositionsCode.Value,
                        committeePositionName: a.CommitteePositionsName)).ToArray();
                }

                var vendorInfoDefault = vendorEditDefault.Vendor.VendorInfo;
                var vendorNameDefault = string.IsNullOrWhiteSpace(vendorInfoDefault?.EstablishmentName)
                    ? $"{vendorInfoDefault?.SapVendorNumber}"
                    : $"{vendorInfoDefault?.SapVendorNumber} : {vendorInfoDefault?.EstablishmentName}";

                cm001Info = new DeliveryAcceptance.GetById.Cm001Info(
                    procurementData.Plan?.PlanNumber.Value,
                    procurementData.Plan?.Id,
                    procurementData.ProcurementNumber?.Value.ToString(),
                    procurementData.Id,
                    procurementData.Department.Name,
                    procurementData.Plan?.Type,
                    vendorNameDefault,
                    vendorInfoDefault?.EstablishmentName,
                    vendorEditDefault.Email,
                    vendorEditDefault.ContractNumber,
                    vendorEditDefault.PoNumber,
                    vendorEditDefault.Budget,
                    vendorEditDefault.ContractName,
                    vendorEditDefault.ContractType?.Label,
                    vendorEditDefault.Template?.Label,
                    vendorEditDefault.ContractSignedDate,
                    vendorEditDefault.PeriodConditionType?.Label,
                    vendorEditDefault.EndDate,
                    procurementData.SupplyMethod.Label,
                    procurementData.SupplyMethodCode.Value,
                    procurementData.SupplyMethodType != null ? procurementData.SupplyMethodType.Label : string.Empty,
                    procurementData.SupplyMethodType != null ? procurementData.SupplyMethodType.Code.Value : string.Empty,
                    procurementData.SupplyMethodSpecialType != null ? procurementData.SupplyMethodSpecialType.Label : string.Empty,
                    procurementData.SupplyMethodSpecialType != null ? procurementData.SupplyMethodSpecialType.Code.Value : string.Empty,
                    procurementData.SupplyMethodSpecialType != null ? procurementData.Plan?.BudgetYear : null,
                    procurementData.Plan?.Budget,
                    SourceType.ContractDraftVendorEdit,
                    null);
            }
            else if (cmDeliveryAcceptanceData.SourceType == SourceType.Manual)
            {
                cm001Info = new DeliveryAcceptance.GetById.Cm001Info(
                    null,
                    null,
                    null,
                    null,
                    cmDeliveryAcceptanceData.Department?.Name ?? string.Empty,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    cmDeliveryAcceptanceData.Name,
                    null,
                    null,
                    null,
                    null,
                    null,
                    cmDeliveryAcceptanceData.SupplyMethod?.Label,
                    cmDeliveryAcceptanceData.SupplyMethodCode?.Value,
                    cmDeliveryAcceptanceData.SupplyMethodType?.Label ?? string.Empty,
                    cmDeliveryAcceptanceData.SupplyMethodType?.Code.Value ?? string.Empty,
                    cmDeliveryAcceptanceData.SupplyMethodSpecialType?.Label ?? string.Empty,
                    cmDeliveryAcceptanceData.SupplyMethodSpecialType?.Code.Value ?? string.Empty,
                    null,
                    cmDeliveryAcceptanceData.Budget,
                    SourceType.Manual,
                    null);
            }

            var hasEditPermissionData = acceptanceCommitteesData.Length <= 0 || acceptanceCommitteesData.Any(x => x.UserId == userId);

            var assigneesData = await this.MapToAssigneeResponseData(ct);

            var defaultData = new GetDeliveryAcceptancePeriodByIdResponse(
                null,
                CmDeliveryAcceptancePeriodStatus.Draft,
                null,
                null,
                null,
                null,
                null,
                false,
                null,
                null,
                false,
                null,
                null,
                [],
                false,
                hasEditPermissionData,
                acceptanceCommitteesData,
                assigneesData,
                [],
                null,
                false,
                cm001Info,
                procurementData?.SupplyMethod.Label ?? string.Empty,
                procurementData?.SupplyMethodCode,
                procurementData?.SupplyMethodType?.Label ?? string.Empty,
                procurementData?.SupplyMethodTypeCode,
                procurementData?.SupplyMethodSpecialType?.Label ?? string.Empty,
                procurementData?.SupplyMethodSpecialTypeCode,
                [new BudgetDetail(null, 1, string.Empty, string.Empty, string.Empty, string.Empty, 0m)],
                procurementData?.IsCommercialMaterial ?? false,
                procurementData?.Department.Id.Value ?? string.Empty,
                cmDeliveryAcceptanceData.Department?.OrganizationLevel,
                CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval,
                null,
                null,
                null,
                [],
                [],
                cmDeliveryAcceptanceData.Periods
                                        .Where(x => x.Status != CmDeliveryAcceptancePeriodStatus.Draft && x.Status != CmDeliveryAcceptancePeriodStatus.Edit && x.Status != CmDeliveryAcceptancePeriodStatus.Rejected)
                                        .SelectMany(x => x.PaymentTerms).Select(y => y.PaymentTerm).Distinct().OrderBy(t => t).ToArray(),
                cmDeliveryAcceptanceData.Periods
                                        .Where(x => x.Status != CmDeliveryAcceptancePeriodStatus.Draft && x.Status != CmDeliveryAcceptancePeriodStatus.Edit && x.Status != CmDeliveryAcceptancePeriodStatus.Rejected)
                                        .Sum(x => x.PaymentTerms.Sum(y => y.Amount)),
                null,
                inspectionCommitteesData,
                null);

            return defaultData;
        }

        private AcceptorNoIdResponse[] GetDefaultAcceptanceCommittee(
            CmDeliveryAcceptancePeriod periodExisting,
            GetDeliveryAcceptancePeriodByIdRequest req)
        {
            _ = periodExisting;

            _ = req;

            return [];
        }

        private async Task<AssigneeNoIdResponse[]> MapToAssigneeResponse(
            CmDeliveryAcceptancePeriod periodExisting,
            CancellationToken ct)
        {
            if (!periodExisting.Assignees.Any())
            {
                var defaultAssignee = await this.operationService.GetDefaultJorPorDirectorAsync(ct);

                if (defaultAssignee is null)
                {
                    return [];
                }

                var jorPorUserDate = await this.dbContext.SuUsers
                                               .Include(u => u.Employee)
                                               .ThenInclude(e => e.View)
                                               .FirstOrDefaultAsync(u => u.Id == defaultAssignee.UserId, ct);

                var assignees = new AssigneeNoIdResponse(
                    null,
                    AssigneeGroup.Contract,
                    AssigneeType.Director,
                    defaultAssignee.UserId.Value,
                    1,
                    defaultAssignee.FullName,
                    jorPorUserDate?.Employee.View?.FullPositionName.Trim() ?? string.Empty,
                    jorPorUserDate?.Employee.View?.BusinessUnitName.Trim() ?? string.Empty,
                    AssigneeStatus.Draft);

                return [assignees];
            }

            return
            [
                .. periodExisting.Assignees
                                 .Where(a => !a.IsDeleted)
                                 .OrderBy(a => a.Sequence)
                                 .Select(DelegatorExtensions.DelegatorToAssignee)
                                 .Select(a => new AssigneeNoIdResponse(
                                     a.Id.Value,
                                     a.Group,
                                     a.Type,
                                     a.UserId.Value,
                                     a.Sequence,
                                     a.FullName,
                                     a.PositionName,
                                     a.BusinessUnitName,
                                     a.Status,
                                     a.Remark,
                                     a.ActionAt,
                                     DelegateeUserId: a.Delegatee?.SuUserId.Value))
            ];
        }

        private AcceptorNoIdResponse[] GetAcceptanceCommittees(
            CmDeliveryAcceptancePeriod periodExisting,
            GetDeliveryAcceptancePeriodByIdRequest req)
        {
            var acceptanceCommittees = MapAcceptorsForFindCase(periodExisting, AcceptorType.AcceptanceCommittee);

            if (!acceptanceCommittees.Any())
            {
                acceptanceCommittees = this.GetDefaultAcceptanceCommittee(periodExisting, req);
            }

            return acceptanceCommittees;
        }

        private static CmDeliveryAcceptancePeriodDocumentHistory? GetLastedDocumentHistory(CmDeliveryAcceptancePeriod periodExisting)
        {
            return periodExisting.GetDocumentForStatus(periodExisting.Status);
        }

        private static InspectionCommitteeSectionResponse MapInspectionCommitteeSection(
            CmDeliveryAcceptancePeriod periodExisting)
        {
            var committees = periodExisting.Acceptors
                                           .Where(a => a.Type == AcceptorType.AcceptanceCommittee &&
                                                       a is { IsDeleted: false, IsActive: true })
                                           .OrderBy(a => a.Sequence)
                                           .Select(a => new InspectionCommitteeInfoResponse(
                                               a.Id.Value,
                                               a.UserId.Value,
                                               a.FullName,
                                               a.PositionName,
                                               a.CommitteePositionsCode.HasValue ? (string)a.CommitteePositionsCode : null,
                                               a.CommitteePosition?.Label,
                                               a.Sequence))
                                           .ToArray();

            var isCommittee = periodExisting.Acceptors
                                            .Where(a => a.Type == AcceptorType.AcceptanceCommittee &&
                                                        a is { IsDeleted: false, IsActive: true })
                                            .All(a => a.IsCommittee());

            return new InspectionCommitteeSectionResponse(committees, isCommittee);
        }

        private InspectionCommitteeSectionResponse MapInspectionCommitteeSectionFromApproval(
            PPurchaseOrderApproval pPurchaseOrderApproval)
        {
            var committees = pPurchaseOrderApproval.Committees
                                                   .Where(c => c.GroupType == GroupType.InspectionCommittee)
                                                   .OrderBy(a => a.Sequence)
                                                   .Select(a => new InspectionCommitteeInfoResponse(
                                                       null,
                                                       a.User.Id.Value,
                                                       a.User.FullName,
                                                       a.FullPositionName,
                                                       a.CommitteePositionsCode.Value,
                                                       a.CommitteePositionsName,
                                                       a.Sequence))
                                                   .ToArray();

            var isCommittee = pPurchaseOrderApproval.Committees
                                                    .Where(c => c.GroupType == GroupType.InspectionCommittee)
                                                    .All(a => a.IsCommittee());

            return new InspectionCommitteeSectionResponse(committees, isCommittee);
        }

        private InspectionCommitteeSectionResponse MapInspectionCommitteeSectionFromCommitteesData(
            AcceptorNoIdResponse[] acceptanceCommitteesData)
        {
            var committees = acceptanceCommitteesData
                             .OrderBy(a => a.Sequence)
                             .Select((a, index) => new InspectionCommitteeInfoResponse(
                                 a.Id,
                                 a.UserId,
                                 a.FullName,
                                 a.PositionName,
                                 a.CommitteePositionsCode,
                                 a.CommitteePositionName,
                                 index + 1))
                             .ToArray();

            var isCommittee = acceptanceCommitteesData.Any();

            return new InspectionCommitteeSectionResponse(committees, isCommittee);
        }

        private AcceptorNoIdResponse[] GetAcceptanceCommitteesData(
            PPurchaseOrderApproval pPurchaseOrderApproval)
        {
            var committees = this.MapAcceptorsData(pPurchaseOrderApproval, AcceptorType.AcceptanceCommittee);

            if (committees.Length > 0)
            {
                return committees;
            }

            return this.GetDefaultAcceptanceCommitteeData(pPurchaseOrderApproval);
        }

        private static AcceptorNoIdResponse CreateAcceptorResponse(
            Guid? id,
            AcceptorType type,
            Guid userId,
            int sequence,
            string fullName,
            string positionName,
            string businessUnitName,
            AcceptorStatus status,
            DateTimeOffset? actionAt = null,
            string? remark = null,
            string? committeePositionsCode = null,
            string? committeePositionName = null,
            Guid? delegateId = null,
            bool isUnableToPerformDuties = false,
            bool isCurrent = true,
            Guid? delegateeUserId = null,
            string? departmentCode = null)
        {
            var resolvedPositionName = string.IsNullOrWhiteSpace(positionName) ? committeePositionName : positionName;

            return new AcceptorNoIdResponse(
                id,
                type,
                userId,
                sequence,
                fullName,
                resolvedPositionName?.Trim() ?? string.Empty,
                businessUnitName?.Trim() ?? string.Empty,
                status,
                remark,
                actionAt,
                committeePositionsCode ?? string.Empty,
                committeePositionName,
                DelegateId: delegateId,
                IsUnableToPerformDuties: isUnableToPerformDuties,
                IsCurrent: isCurrent,
                DelegateeUserId: delegateeUserId,
                DepartmentCode: departmentCode);
        }

        private AcceptorNoIdResponse[] MapAcceptorsData(
            PPurchaseOrderApproval pPurchaseOrderApproval,
            AcceptorType acceptorType)
        {
            var acceptorsApprover = pPurchaseOrderApproval.Acceptors
                                                          .Where(x => x.Type != AcceptorType.AcceptanceCommittee)
                                                          .Select(DelegatorExtensions.DelegatorToAcceptor)
                                                          .ToList();

            var committee = pPurchaseOrderApproval.Acceptors
                                                  .Where(x => x.Type == AcceptorType.AcceptanceCommittee)
                                                  .ToList();

            var acceptors = acceptorsApprover.Union(committee).ToArray();

            if (!acceptors.Any())
            {
                return new AcceptorNoIdResponse[0];
            }

            return acceptors
                   .Where(a => a.Type == acceptorType && a is { IsDeleted: false, IsActive: true })
                   .OrderBy(a => a.Sequence)
                   .Select(a => CreateAcceptorResponse(
                       a.Id.Value,
                       a.Type,
                       a.UserId.Value,
                       a.Sequence,
                       a.FullName,
                       a.User.Employee.View?.FullPositionName.Trim() ?? string.Empty,
                       a.BusinessUnitName,
                       a.Status,
                       a.ActionAt,
                       a.Remark,
                       committeePositionsCode: null,
                       committeePositionName: a.PositionName,
                       delegateId: a.DelegateeId?.Value,
                       isUnableToPerformDuties: false,
                       isCurrent: true,
                       delegateeUserId: a.Delegatee?.SuUserId.Value,
                       departmentCode: a.User.Employee.View?.BusinessUnitId.Value))
                   .ToArray();
        }

        private AcceptorNoIdResponse[] GetDefaultAcceptanceCommitteeData(
            PPurchaseOrderApproval pPurchaseOrderApproval)
        {
            return pPurchaseOrderApproval.Committees
                                         .Where(c => c.GroupType == GroupType.InspectionCommittee)
                                         .OrderBy(a => a.Sequence)
                                         .Select(a => CreateAcceptorResponse(
                                             null,
                                             AcceptorType.AcceptanceCommittee,
                                             a.User.Id.Value,
                                             a.Sequence,
                                             a.User.FullName,
                                             a.User.Employee.View?.FullPositionName ?? string.Empty,
                                             string.Empty,
                                             AcceptorStatus.Draft,
                                             committeePositionsCode: a.CommitteePositionsCode.Value,
                                             committeePositionName: a.CommitteePositionsName,
                                             isUnableToPerformDuties: false,
                                             departmentCode: a.User.Employee.View?.BusinessUnitId.Value))
                                         .ToArray();
        }

        private bool HasJorPorAssignData(
            IEnumerable<AcceptorNoIdResponse> acceptors)
        {
            var acceptorUserIds = acceptors.Select(a => a.UserId).ToHashSet();
            var acceptorEmployeeCodes = this.dbContext.SuUsers
                                            .Where(u => acceptorUserIds.Contains((Guid)u.Id))
                                            .Select(u => u.EmployeeCode)
                                            .ToHashSet();

            // Inline the IsAssistantManagingDirector() logic - EF Core cannot translate extension methods
            var assistantManagingDirectorCodes = new[]
            {
                InRefCodeConstant.Bp006,
                InRefCodeConstant.Bp024,
                InRefCodeConstant.Bp025,
            };

            return this.dbContext.RawEmployees
                       .Any(e => acceptorEmployeeCodes.Contains(e.Id) &&
                                 e.Positions.Any(p => assistantManagingDirectorCodes.Contains(p.BusinessUnit.OrganizationLevel)));
        }

        private async Task<AssigneeNoIdResponse[]> MapToAssigneeResponseData(
            CancellationToken ct)
        {
            var defaultAssignee = await this.operationService.GetDefaultJorPorDirectorAsync(ct);

            if (defaultAssignee is null)
            {
                return [];
            }

            var jorPorUserDate = await this.dbContext.SuUsers
                                           .Include(u => u.Employee)
                                           .ThenInclude(e => e.View)
                                           .FirstOrDefaultAsync(u => u.Id == defaultAssignee.UserId, ct);

            var assignees = new AssigneeNoIdResponse(
                null,
                AssigneeGroup.Contract,
                AssigneeType.Director,
                defaultAssignee.UserId.Value,
                1,
                defaultAssignee.FullName,
                jorPorUserDate?.Employee.View?.FullPositionName.Trim() ?? string.Empty,
                jorPorUserDate?.Employee.View?.BusinessUnitName.Trim() ?? string.Empty,
                AssigneeStatus.Draft);

            return [assignees];
        }
    }
}