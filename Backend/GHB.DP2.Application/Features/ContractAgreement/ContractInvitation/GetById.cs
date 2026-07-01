namespace GHB.DP2.Application.Features.ContractAgreement.ContractInvitation;

using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.ContractAgreement.ContractInvitation.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Invite;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
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
    public record GetContractInvitationByIdRequest(
        [property: FromClaim(JwtRegisteredClaimNames.Sub)]
        Guid UserId,
        Guid ProcurementId,
        Guid? Id = null);

    public record GetContractInvitationByIdResponse(
        [property: Description("รหัสหนังสือเชิญสัญญา")]
        ContractInvitationId? Id,
        [property: Description("รหัสการจัดซื้อ")]
        ProcurementId ProcurementId,
        [property: Description("สถานะหนังสือเชิญสัญญา")]
        ContractInvitationStatus Status,
        [property: Description("รายชื่อผู้ขาย")]
        ContractInvitationVendorResponse[] Vendors,
        [property: Description("รายชื่อผู้อนุมัติ")]
        AcceptorResponse[] Acceptors,
        Guid? HasPermissionUserId,
        [property: Description("สิทธิ์ในการแก้ไข")]
        bool HasEditPermission = false,
        bool IsDocumentReplace = false);

    public record DocumentVersionResponse(
        [property: Description("รหัสไฟล์เอกสาร")]
        Guid FileId,
        [property: Description("เลข Version")]
        string Version,
        [property: Description("วันที่สร้าง")]
        DateTimeOffset CreatedAt,
        [property: Description("ชื่อผู้สร้าง")]
        string CreatedByName,
        [property: Description("เป็น Version ปัจจุบัน")]
        bool IsCurrent);

    public record ContractInvitationVendorResponse(
        [property: Description("รหัสผู้ขายในหนังสือเชิญสัญญา")]
        ContractInvitationVendorsId? Id,
        [property: Description("รหัสสัญญาอนุมัติใบสั่งซื้อ")]
        PurchaseOrderApprovalContractId PurchaseOrderApprovalContractId,
        [property: Description("รหัสเอกสาร")] Guid? DocumentId,
        [property: Description("รหัสเอกสาร เปลี่ยนแปลง")]
        bool? IsDocumentIdReplace,
        [property: Description("ประวัติ Version เอกสาร")]
        DocumentVersionResponse[] DocumentVersions,
        [property: Description("ชื่อผู้ขาย")] string VendorName,
        [property: Description("อีเมล")] string Email,
        [property: Description("ชื่อสัญญา")] string ContractName,
        [property: Description("เลขที่ใบสั่งซื้อ")]
        string PoNumber,
        [property: Description("เลขที่สัญญา")] string? ContractNumber,
        [property: Description("ราคาที่ตกลงได้")]
        decimal AgreedPrice,
        [property: Description("มีหลักประกันสัญญา")]
        bool HasContractGuarantee,
        [property: Description("เปอร์เซ็นต์หลักประกันสัญญา")]
        decimal? ContractGuaranteePercent,
        [property: Description("จำนวนเงินหลักประกัน")]
        decimal? GuaranteeAmount,
        [property: Description("รหัสรูปแบบสัญญา")]
        string? DocumentTemplateCode,
        [property: Description("ชื่อเจ้าหน้าที่สัญญา")]
        string ContractOfficerName,
        [property: Description("โทรศัพท์เจ้าหน้าที่สัญญา")]
        string ContractOfficerPhone,
        [property: Description("อีเมลเจ้าหน้าที่สัญญา")]
        string ContractOfficerEmail,
        [property: Description("ผลการตรวจสอบ EGP")]
        bool? EgpResult,
        [property: Description("หมายเหตุ EGP")]
        string? EgpRemark,
        [property: Description("วันที่ตรวจสอบ EGP")]
        DateTimeOffset? EgpDate,
        [property: Description("ผลการตรวจสอบ EGP")]
        bool? CoiResult,
        [property: Description("หมายเหตุ EGP")]
        string? CoiRemark,
        [property: Description("วันที่ตรวจสอบ EGP")]
        DateTimeOffset? CoiDate,
        [property: Description("ผลการตรวจสอบ EGP")]
        bool? WatchlistResult,
        [property: Description("หมายเหตุ EGP")]
        string? WatchlistRemark,
        [property: Description("วันที่ตรวจสอบ EGP")]
        DateTimeOffset? WatchlistDate,
        [property: Description("ข้อมูลผู้ประกอบการ")]
        VendorInfoResponse? Entrepreneur,
        [property: Description("ผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
        QualificationResultDto? CoiCheckerResult,
        [property: Description("ผลการตรวจสอบ Watchlist")]
        QualificationResultDto? WatchlistCheckerResult,
        [property: Description("ผู้ถือหุ้น")]
        ContractInviteShareholderDTO[]? Shareholder,
        [property: Description("เอกสารแนบ")]
        EntrepreneurResponseAttachment[] Attachments,
        string? EmailSend,
        string? EmailTemplate,
        EmailAttachment[] EmailAttachments,
        string? BudgetDetail,
        [property: Description("วันที่เอกสาร")]
        DateTimeOffset? DocumentDate = null);

    public record VendorInfoResponse(
        SuVendorId Id,
        SuVendorNationality Nationality,
        SuVendorType Type,
        ParameterCode EntrepreneurType,
        string EntrepreneurTypeName,
        string TaxpayerIdentificationNo,
        string EstablishmentName,
        string? Tel,
        string? Fax,
        string SapVendorNumber,
        string SapBranchNumber,
        string Email);

    public record ContractInviteShareholderDTO(
        [property: Description("รหัสผู้ถือหุ้น")]
        Guid Id,
        [property: Description("ลำดับ")] int Sequence,
        [property: Description("เลขที่ผู้เสียภาษี")]
        string? TaxId,
        [property: Description("ชื่อจริง")] string? FirstName,
        [property: Description("นามสกุล")] string? LastName,
        [property: Description("เป็นกรรมการหรือถือหุ้น 20%")]
        bool? IsDirector,
        [property: Description("เป็นผู้ถือหุ้น")]
        bool? IsShareholder,
        [property: Description("เป็นนิติบุคคล")]
        bool? IsJuristic,
        [property: Description("ประเภทการตรวจสอบ")]
        string? CheckType,
        [property: Description("ผลการตรวจสอบ Watchlist")]
        bool? WatchlistResult,
        [property: Description("หมายเหตุผลการตรวจสอบ Watchlist")]
        string? WatchlistResultRemark,
        [property: Description("วันที่ตรวจสอบ Watchlist")]
        DateTimeOffset? WatchlistResultAt,
        [property: Description("ผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
        bool? CoiResult,
        [property: Description("หมายเหตุผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
        string? CoiResultRemark,
        [property: Description("วันที่ตรวจสอบความขัดแย้งทางผลประโยชน์")]
        DateTimeOffset? CoiResultAt,
        [property: Description("ผลการตรวจสอบ eGP")]
        bool? EgpResult,
        [property: Description("หมายเหตุ eGP")]
        string? EgpRemark,
        [property: Description("วันที่ตรวจสอบ eGP")]
        DateTimeOffset? EgpResultAt,
        [property: Description("ผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
        QualificationResultDto? CoiCheckerResult,
        [property: Description("ผลการตรวจสอบ Watchlist")]
        QualificationResultDto? WatchlistCheckerResult);

    public class GetContractInvitationById
        : ContractInvitationEndpointBase<
            Features.ContractAgreement.ContractInvitation.GetById.GetContractInvitationByIdRequest,
            Results<Ok<Features.ContractAgreement.ContractInvitation.GetById.GetContractInvitationByIdResponse>, NotFound<string>>>
    {
        private readonly Dp2DbContext dbContext;

        public GetContractInvitationById(
            Dp2DbContext dbContext,
            IOperationService operationService,
            IFileServiceClient fileServiceClient,
            ILogger<UpsertAttachmentsEndpoint> logger)
            : base(dbContext, operationService, fileServiceClient, logger)
        {
            this.dbContext = dbContext;
        }

        public override void Configure()
        {
            this.Get("procurement/{ProcurementId:guid}/contractInvitation/{Id:guid?}");
            this.Description(b => b
                                  .WithTags("ContractAgreement/ContractInvitation")
                                  .Produces<Features.ContractAgreement.ContractInvitation.GetById.GetContractInvitationByIdResponse>()
                                  .Produces<string>(StatusCodes.Status404NotFound));
        }

        protected override async ValueTask<Results<Ok<GetContractInvitationByIdResponse>, NotFound<string>>>
            HandleRequestAsync(
                GetContractInvitationByIdRequest req,
                CancellationToken ct)
        {
            var editableAssignee =
                await this.TryGetAssigneeWithEditPermissionAsync(
                    ProcurementId.From(req.ProcurementId),
                    UserId.From(req.UserId),
                    ct);

            var hasEditPermission = editableAssignee is not null;

            if (req.Id is null)
            {
                return await HandleNoIdExistingAsync();
            }

            var contractInvitationExisting =
                await this.GetById(
                    ContractInvitationId.From(req.Id.Value),
                    ProcurementId.From(req.ProcurementId),
                    ct);

            var vendors =
                contractInvitationExisting.Vendors
                                          .OrderBy(v =>
                                              contractInvitationExisting.Procurement.Type is ProcurementType.Procurement
                                                  ? v.PurchaseOrderApprovalContract.Budget?.Sequence
                                                  : v.PurchaseOrderApprovalContract.PrincipleApprovalRentalBudget?.Sequence)
                                          .ThenBy(o => o.PurchaseOrderApprovalContract.Sequence)
                                          .Select(s => MapToInvitationVendorResponse(contractInvitationExisting.Procurement.Type, s))
                                          .ToArray();

            var acceptors =
                MapAcceptors([.. contractInvitationExisting.Acceptors]);

            var result =
                new GetContractInvitationByIdResponse(
                    contractInvitationExisting.Id,
                    contractInvitationExisting.ProcurementId,
                    contractInvitationExisting.Status,
                    vendors,
                    acceptors,
                    (Guid?)editableAssignee?.Id,
                    hasEditPermission,
                    false);

            return TypedResults.Ok(result);

            async ValueTask<Results<Ok<GetContractInvitationByIdResponse>, NotFound<string>>> HandleNoIdExistingAsync()
            {
                var purchaseOrderApprovalExisting =
                    await this.dbContext.PPurchaseOrderApprovals
                              .Include(p => p.Procurement)
                              .Include(p => p.Contracts)
                              .ThenInclude(c => c.Entrepreneur)
                              .ThenInclude(e => e!.SuVendor)
                              .Include(c => c.Contracts)
                              .ThenInclude(c => c.PrincipleApprovalRentalEntrepreneurs)
                              .ThenInclude(c => c!.Vendor)
                              .Include(a => a.Contracts)
                              .ThenInclude(a => a.PPurchaseOrderApprovalEntrepreneurs)
                              .ThenInclude(a => a!.Vendor)
                              .Include(p => p.Contracts)
                              .ThenInclude(c => c.Budget)
                              .Include(p => p.Contracts)
                              .ThenInclude(c => c.PrincipleApprovalRentalBudget)
                              .Include(p => p.Contracts)
                              .ThenInclude(c => c.PpPurchaseRequisitionBudget)
                              .Include(p => p.Contracts)
                              .ThenInclude(c => c.PPurchaseOrderApprovalBudget)
                              .AsSplitQuery()
                              .FirstOrDefaultAsync(
                                  p =>
                                      p.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                                      p.Status == PurchaseOrderApprovalStatus.Assigned,
                                  ct);

                if (purchaseOrderApprovalExisting is null)
                {
                    this.ThrowError(
                        r =>
                            req.ProcurementId,
                        $"ไม่พบการอนุมัติใบสั่งซื้อ/จ้าง/เช่า ในระบบ",
                        StatusCodes.Status404NotFound);
                }

                var tor = await this.dbContext.PpTorDrafts
                      .FirstOrDefaultAsync(
                          p =>
                              p.ProcurementId == ProcurementId.From(req.ProcurementId),
                          ct);

                var initContractInvitationVendors = this.MapContractByProcurementType(
                    purchaseOrderApprovalExisting,
                    editableAssignee,
                    purchaseOrderApprovalExisting.Procurement.Type,
                    tor);

                var initContractInvitation =
                    new GetContractInvitationByIdResponse(
                        null,
                        ProcurementId.From(req.ProcurementId),
                        ContractInvitationStatus.Draft,
                        initContractInvitationVendors,
                        [],
                        (Guid?)editableAssignee?.Id,
                        hasEditPermission,
                        false);

                return TypedResults.Ok(initContractInvitation);
            }
        }
    }
}