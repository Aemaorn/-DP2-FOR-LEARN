namespace GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition;

using System.ComponentModel;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetById
{
    public record GetCertificateRequisitionByIdRequest(
        Guid? ContractDraftVendorId,
        Guid? Id);

    public record CertificateRequisitionDocumentVersionResponse(
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

    public record GetCertificateRequisitionByIdResponse(
        [property: Description("รหัสการขอใบรับรอง")]
        CamCertificateRequisitionId? Id,
        [property: Description("สถานะการขอใบรับรอง")]
        CamCertificateRequisitionStatus Status,
        [property: Description("เลขที่ใบรับรอง")]
        string? CertificateNo,
        [property: Description("วันที่รับเอกสาร")]
        DateTimeOffset? ReceiveDate,
        [property: Description("เลขที่เอกสาร SBS")]
        string? SbsDocumentNo,
        [property: Description("วันที่เอกสาร")]
        DateTimeOffset? DocumentDate,
        [property: Description("วันที่ออกใบรับรอง")]
        DateTimeOffset? IssuedDate,
        [property: Description("เหตุผลในการขอ")]
        string? RequestReason,
        [property: Description("รายชื่อผู้อนุมัติ")]
        AcceptorNoIdResponse[]? Acceptors,
        [property: Description("ข้อมูลคู่ค้าสัญญา")]
        ContractVendorInfoDto? ContractVendorInfo,
        [property: Description("ข้อมูลงวดการส่งมอบตรวจรับ")]
        DeliveryAcceptancePeriodInfoDto[]? DeliveryAcceptancePeriodInfo,
        Guid? DocumentId,
        CertificateRequisitionDocumentVersionResponse[]? DocumentVersions = null,
        bool? IsManual = null,
        [property: Description("คณะกรรมการตรวจรับ")]
        InspectionCommitteeSectionResponse? InspectionCommittees = null,
        [property: Description("เอกสารแนบ")]
        AttachmentsDtoWithId[]? Attachments = null);

    public record ContractVendorInfoDto(
        [property: Description("รหัสคู่ค้าร่างสัญญา")]
        ContractDraftVendorId? Id,
        [property: Description("ข้อมูลผู้ประกอบการ")]
        EntrepreneurInfoDto? Entrepreneur,
        [property: Description("เลขที่สัญญา")] string ContractNumber,
        [property: Description("เลขที่ใบสั่งซื้อ")]
        string PoNumber,
        [property: Description("งบประมาณ")] decimal Budget,
        [property: Description("ชื่อสัญญา")] string ContractName,
        [property: Description("รหัสประเภทสัญญา")]
        ParameterCode? ContractTypeCode,
        [property: Description("ชื่อประเภทสัญญา")]
        string? ContractTypeLabel,
        [property: Description("รหัสแม่แบบ")] ParameterCode? TemplateCode,
        [property: Description("ชื่อแม่แบบ")] string? TemplateLabel,
        [property: Description("วันที่ลงนามสัญญา")]
        DateTimeOffset? ContractSignedDate,
        [property: Description("ระยะเวลาการส่งมอบ (วัน)")]
        int? DeliveryLeadTime,
        [property: Description("รหัสประเภทระยะเวลาการส่งมอบ")]
        ParameterCode? DeliveryLeadTimeTypeCode,
        [property: Description("ชื่อประเภทระยะเวลาการส่งมอบ")]
        string? DeliveryLeadTimeTypeLabel,
        [property: Description("วันที่กำหนดส่งมอบ")]
        DateTimeOffset? DeliveryDate,
        bool? IsManual = null,
        string? EntrepreneurName = null,
        Guid? EntrepreneurId = null,
        string? EntrepreneurEmail = null,
        DateTimeOffset? ContractEndDate = null,
        string? SupplyMethodCode = null,
        string? SupplyMethodLabel = null,
        string? SupplyMethodTypeCode = null,
        string? SupplyMethodTypeLabel = null,
        string? SupplyMethodSpecialTypeCode = null,
        string? SupplyMethodSpecialTypeLabel = null);

    public record EntrepreneurInfoDto(
        [property: Description("รหัสผู้ประกอบการ")]
        string Code,
        [property: Description("ชื่อผู้ประกอบการ")]
        string Name,
        [property: Description("อีเมล")] string Email);

    public record DeliveryAcceptancePeriodInfoDto(
        [property: Description("รหัสการตรวจรับการส่งมอบ")]
        CmDeliveryAcceptanceId Id,
        [property: Description("รหัสงวดการตรวจรับ")]
        CmDeliveryAcceptancePeriodId PeriodId,
        [property: Description("สถานะงวดการตรวจรับ")]
        CmDeliveryAcceptancePeriodStatus Status,
        [property: Description("ลำดับงวด")] int Sequence,
        [property: Description("วันที่ส่งมอบ")]
        DateTimeOffset? DeliveryDate,
        [property: Description("ระยะเวลาการส่งมอบ")]
        int? LeadTime,
        [property: Description("เปอร์เซ็นต์การจ่ายเงิน")]
        decimal? InstallmentPercentage,
        [property: Description("จำนวนเงิน")] decimal? Amount,
        [property: Description("วันที่ตรวจรับ")]
        DateTimeOffset? DeliveryAcceptanceDate);

    public record DeliveryInfoDto(
        [property: Description("ลำดับการส่งมอบ")]
        int Sequence,
        [property: Description("วันที่ส่งมอบ")]
        DateTimeOffset? DeliveryDate,
        [property: Description("รายการสินค้าที่ส่งมอบ")]
        DeliveryItemInfoDto[] DeliveryItems);

    public record DeliveryItemInfoDto(
        [property: Description("ลำดับสินค้า")] int Sequence,
        [property: Description("รายละเอียดสินค้า")]
        string Description,
        [property: Description("จำนวน")] int Quantity,
        [property: Description("ราคาต่อหน่วย")]
        decimal Price,
        [property: Description("จำนวนเงินรวม")]
        decimal Total);

    public class GetCamCertificateRequisitionById
        : CertificateRequisitionEndpointBase<
            GetCertificateRequisitionByIdRequest,
            Results<Ok<GetCertificateRequisitionByIdResponse>, NotFound<string>>>
    {
        private readonly Dp2DbContext dbContext;

        public GetCamCertificateRequisitionById(
            Dp2DbContext dbContext,
            ILogger<GetCamCertificateRequisitionById> logger)
            : base(dbContext, logger)
        {
            this.dbContext = dbContext;
        }

        public override void Configure()
        {
            this.Get("certificate-requisition/{Id:guid}", "certificate-requisition/init");
            this.Description(b => b
                                  .WithTags("ContractAmendment/CertificateRequisition")
                                  .Produces<Features.ContractAmendment.CertificateRequisition.GetById.GetCertificateRequisitionByIdResponse>()
                                  .Produces<string>(StatusCodes.Status404NotFound));
        }

        protected override async ValueTask<Results<
                Ok<GetCertificateRequisitionByIdResponse>,
                NotFound<string>>>
            HandleRequestAsync(GetCertificateRequisitionByIdRequest req, CancellationToken ct)
        {
            CaContractDraftVendor? contractDraftVendorExisting = null;

            if (req.Id is null)
            {
                // Init new form: requires ContractDraftVendorId from query param
                if (!req.ContractDraftVendorId.HasValue)
                {
                    this.ThrowError(
                        r => r.ContractDraftVendorId,
                        "ต้องระบุ ContractDraftVendorId สำหรับการสร้างเอกสารแบบอ้างอิง",
                        StatusCodes.Status400BadRequest);
                }

                contractDraftVendorExisting = await LoadContractDraftVendorAsync(req.ContractDraftVendorId!.Value, ct);

                if (contractDraftVendorExisting is null)
                {
                    return TypedResults.NotFound("ไม่พบข้อมูลคู่ค้าสัญญา");
                }

                if (contractDraftVendorExisting.Status != ContractDraftVendorStatus.Approved)
                {
                    this.ThrowError(
                        r => req.ContractDraftVendorId,
                        $"สถานะของคู่ค้าสัญญา {req.ContractDraftVendorId} ยังไม่ได้รับการอนุมัติ",
                        StatusCodes.Status404NotFound);
                }

                var res = await HandleNoIdExisting();
                return TypedResults.Ok(res);
            }

            var certReqExisting =
                await this.dbContext.CamCertificateRequisitions
                          .Include(cRequest => cRequest.Acceptors)
                          .ThenInclude(a => a.User)
                          .Include(camCertificateRequisition => camCertificateRequisition.Acceptors)
                          .ThenInclude(camCertificateRequisitionAcceptor => camCertificateRequisitionAcceptor.CommitteePosition)
                          .Include(cr => cr.DocumentHistories)
                          .Include(cr => cr.Attachments)
                          .ThenInclude(a => a.DocumentType)
                          .Include(cr => cr.SupplyMethod)
                          .Include(cr => cr.SupplyMethodType)
                          .Include(cr => cr.SupplyMethodSpecialType)
                          .AsSplitQuery()
                          .FirstOrDefaultAsync(
                              da => da.Id == CamCertificateRequisitionId.From(req.Id.Value), ct);

            if (certReqExisting is null)
            {
                this.ThrowError(
                    r => req.Id,
                    $"ไม่พบข้อมูลการขอใบรับรอง {req.Id}.",
                    StatusCodes.Status404NotFound);
            }

            // For reference mode: load contract draft vendor from the cert req's own FK
            if (certReqExisting.ContractDraftVendorId is { } certReqContractDraftVendorId)
            {
                contractDraftVendorExisting = await LoadContractDraftVendorAsync(certReqContractDraftVendorId.Value, ct);
            }

            var certReqExistingAcceptors = MapAcceptors();

            var documentVersions = certReqExisting.DocumentHistories
                                                  .OrderVersions()
                                                  .Select(d => new CertificateRequisitionDocumentVersionResponse(
                                                      d.FileId.Value,
                                                      d.Version,
                                                      d.CreatedAt,
                                                      d.CreatedByName ?? string.Empty,
                                                      d.FileId == certReqExisting.LastedDocumentHistory?.FileId))
                                                  .ToArray();

            var vendorInfo = contractDraftVendorExisting is not null
                ? MapContractVendorInfo()
                : MapManualContractVendorInfo();

            var deliveryPeriods = contractDraftVendorExisting is not null
                ? await this.GetDeliveryAcceptancePeriods(ct)
                : [];

            var inspectionCommittees = MapInspectionCommitteeSection(certReqExisting, certReqExistingAcceptors);

            var attachments = certReqExisting.Attachments
                .OrderBy(a => a.Sequence)
                .GroupBy(
                    a => a.DocumentTypeCode,
                    (key, g) => new AttachmentsDtoWithId(
                        key.Value,
                        g.Select(s => new FileAttachmentsWithId(s.Id.Value, s.FileId.Value, s.FileName, s.Sequence, s.IsPublic, s.AuditInfo.CreatedBy)).ToArray()))
                .ToArray();

            var result =
                new GetCertificateRequisitionByIdResponse(
                    certReqExisting.Id,
                    certReqExisting.Status,
                    (string?)certReqExisting.CertificateNo,
                    certReqExisting.ReceiveDate,
                    certReqExisting.SbsDocumentNo,
                    certReqExisting.DocumentDate,
                    certReqExisting.IssuedDate,
                    certReqExisting.RequestReason,
                    certReqExistingAcceptors,
                    vendorInfo,
                    deliveryPeriods,
                    certReqExisting.LastedDocumentHistory?.FileId.Value,
                    documentVersions,
                    IsManual: certReqExisting.IsManual,
                    InspectionCommittees: inspectionCommittees,
                    Attachments: attachments);

            return TypedResults.Ok(result);

            async Task<GetCertificateRequisitionByIdResponse> HandleNoIdExisting()
            {
                var initAcceptanceCommitteeFromJp005 =
                    await GetAcceptanceCommitteeFromJp005(contractDraftVendorExisting!.ContractDraft.Procurement.Type, contractDraftVendorExisting.ContractDraft.ProcurementId);

                if (!initAcceptanceCommitteeFromJp005.Any())
                {
                    this.ThrowError(
                        r => r.ContractDraftVendorId,
                        $"ไม่พบข้อมูลคณะกรรมการตรวจรับจากแบบฟอร์ม JP005",
                        StatusCodes.Status404NotFound);
                }

                var vendorInfoForNew = MapContractVendorInfo();
                var periodsForNew = await this.GetDeliveryAcceptancePeriods(ct);
                var inspectionCommitteesForNew = new InspectionCommitteeSectionResponse(
                    initAcceptanceCommitteeFromJp005
                        .OrderBy(a => a.Sequence)
                        .Select((a, index) => new InspectionCommitteeInfoResponse(
                            a.Id,
                            a.UserId,
                            a.FullName,
                            a.PositionName,
                            a.CommitteePositionsCode,
                            a.CommitteePositionName,
                            index + 1)),
                    IsCommittee: true);

                return
                    new GetCertificateRequisitionByIdResponse(
                        null,
                        CamCertificateRequisitionStatus.Draft,
                        null,
                        null,
                        null,
                        null,
                        null,
                        string.Empty,
                        initAcceptanceCommitteeFromJp005,
                        vendorInfoForNew,
                        periodsForNew,
                        null,
                        InspectionCommittees: inspectionCommitteesForNew);
            }

            async Task<AcceptorNoIdResponse[]> GetAcceptanceCommitteeFromJp005(ProcurementType type, ProcurementId procurementId)
            {
                var query =
                    type is ProcurementType.Procurement
                        ? this.dbContext.PJp005S
                              .Include(jp => jp.Committees)
                              .ThenInclude(c => c.User)
                              .Where(w => w.ProcurementId == procurementId)
                              .SelectMany(s => s.Committees)
                              .Where(w => w.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
                              .OrderBy(a => a.Sequence)
                              .Select(a => new AcceptorNoIdResponse(
                                  null,
                                  AcceptorType.AcceptanceCommittee,
                                  a.User.Id.Value,
                                  a.Sequence,
                                  a.FullName,
                                  a.FullPositionName.Trim(),
                                  string.Empty,
                                  AcceptorStatus.Draft,
                                  default,
                                  default,
                                  a.CommitteePositionsCode.Value,
                                  a.CommitteePositionsName,
                                  false,
                                  null,
                                  null,
                                  false,
                                  null))
                              .ToListAsync(ct)
                        : this.dbContext.PPrincipleApprovals
                              .Include(c => c.PrincipleApprovalCommittees)
                              .ThenInclude(u => u.User)
                              .Where(w => w.ProcurementId == procurementId)
                              .SelectMany(s => s.PrincipleApprovalCommittees)
                              .Where(w => w.GroupType == CommitteeGroupType.AcceptanceCommittee)
                              .OrderBy(a => a.Sequence)
                              .Select(a => new AcceptorNoIdResponse(
                                  null,
                                  AcceptorType.AcceptanceCommittee,
                                  a.User.Id.Value,
                                  a.Sequence,
                                  a.FullName,
                                  a.FullPositionName.Trim(),
                                  string.Empty,
                                  AcceptorStatus.Draft,
                                  default,
                                  default,
                                  a.CommitteePositionsCode.Value,
                                  a.CommitteePositionsName,
                                  false,
                                  null,
                                  null,
                                  false,
                                  null))
                              .ToListAsync(ct);

                var committee = await query;

                return [.. committee];
            }

            async Task<CaContractDraftVendor?> LoadContractDraftVendorAsync(Guid id, CancellationToken token)
            {
                return await this.dbContext.CaContractDraftVendors
                                  .Include(cv => cv.ContractDraft)
                                  .ThenInclude(p => p.Procurement)
                                  .ThenInclude(proc => proc.SupplyMethod)
                                  .Include(cv => cv.ContractDraft)
                                  .ThenInclude(p => p.Procurement)
                                  .ThenInclude(proc => proc.SupplyMethodType)
                                  .Include(cv => cv.ContractDraft)
                                  .ThenInclude(p => p.Procurement)
                                  .ThenInclude(proc => proc.SupplyMethodSpecialType)
                                  .Include(cv => cv.ContractType)
                                  .Include(cv => cv.Template)
                                  .Include(cv => cv.Delivery)
                                  .ThenInclude(d => d.LeadTimeType)
                                  .Include(cv => cv.Vendor)
                                  .ThenInclude(v => v.VendorInfo)
                                  .AsSplitQuery()
                                  .FirstOrDefaultAsync(cv => cv.Id == ContractDraftVendorId.From(id), token);
            }

            AcceptorNoIdResponse[] MapAcceptors()
            {
                var activeAcceptors = certReqExisting
                       .Acceptors
                       .Where(a =>
                           a is
                           {
                               IsDeleted: false,
                               IsActive: true,
                           })
                       .ToList();

                var delegatable = activeAcceptors
                       .Where(a => DelegatorExtensions.IsDelegatableType(a.Type))
                       .Select(DelegatorExtensions.DelegatorToAcceptor)
                       .ToList();

                var nonDelegatable = activeAcceptors
                       .Where(a => !DelegatorExtensions.IsDelegatableType(a.Type))
                       .ToList();

                return
                [
                    .. delegatable.Union(nonDelegatable)
                       .OrderBy(a => a.Sequence)
                       .Select(a =>
                           new AcceptorNoIdResponse(
                               a.Id.Value,
                               a.Type,
                               a.UserId.Value,
                               a.Sequence,
                               a.User.FullName,
                               a.PositionName,
                               a.BusinessUnitName,
                               a.Status,
                               a.Remark,
                               a.ActionAt,
                               a.CommitteePositionsCode.HasValue ? (string)a.CommitteePositionsCode : string.Empty,
                               a.CommitteePosition?.Label ?? string.Empty,
                               DelegateId: a.DelegateeId?.Value,
                               IsUnableToPerformDuties: a.IsUnableToPerformDuties,
                               IsCurrent: a.IsCurrentApprover()))
                ];
            }

            ContractVendorInfoDto MapContractVendorInfo()
            {
                var suVendor = contractDraftVendorExisting!.Vendor.VendorInfo;

                var entrepreneurInfo =
                    new EntrepreneurInfoDto(
                        suVendor.SapVendorNumber,
                        suVendor.EstablishmentName,
                        suVendor.Email);

                var procurement = contractDraftVendorExisting.ContractDraft.Procurement;

                return
                    new ContractVendorInfoDto(
                        contractDraftVendorExisting.Id,
                        entrepreneurInfo,
                        contractDraftVendorExisting.ContractNumber,
                        contractDraftVendorExisting.PoNumber,
                        contractDraftVendorExisting.Budget,
                        contractDraftVendorExisting.ContractName,
                        contractDraftVendorExisting.ContractTypeCode,
                        contractDraftVendorExisting.ContractType?.Label,
                        contractDraftVendorExisting.TemplateCode,
                        contractDraftVendorExisting.Template?.Label,
                        contractDraftVendorExisting.ContractSignedDate,
                        contractDraftVendorExisting.Delivery?.LeadTime,
                        contractDraftVendorExisting.Delivery?.LeadTimeTypeCode,
                        contractDraftVendorExisting.Delivery?.LeadTimeType?.Label,
                        contractDraftVendorExisting.Delivery?.Date,
                        IsManual: false,
                        SupplyMethodCode: procurement.SupplyMethodCode.Value,
                        SupplyMethodLabel: procurement.SupplyMethod?.Label,
                        SupplyMethodTypeCode: procurement.SupplyMethodTypeCode?.Value,
                        SupplyMethodTypeLabel: procurement.SupplyMethodType?.Label,
                        SupplyMethodSpecialTypeCode: procurement.SupplyMethodSpecialTypeCode?.Value,
                        SupplyMethodSpecialTypeLabel: procurement.SupplyMethodSpecialType?.Label);
            }

            ContractVendorInfoDto MapManualContractVendorInfo()
            {
                var entrepreneurInfo = certReqExisting.EntrepreneurName is not null
                    ? new EntrepreneurInfoDto(
                        certReqExisting.EntrepreneurId?.Value.ToString() ?? string.Empty,
                        certReqExisting.EntrepreneurName,
                        certReqExisting.EntrepreneurEmail ?? string.Empty)
                    : null;

                return new ContractVendorInfoDto(
                    null,
                    entrepreneurInfo,
                    certReqExisting.ContractNumber ?? string.Empty,
                    certReqExisting.PoNumber ?? string.Empty,
                    certReqExisting.Budget ?? 0,
                    certReqExisting.ContractName ?? string.Empty,
                    null,
                    null,
                    null,
                    null,
                    certReqExisting.ContractSignedDate,
                    null,
                    null,
                    null,
                    certReqExisting.DeliveryDate,
                    IsManual: true,
                    EntrepreneurName: certReqExisting.EntrepreneurName,
                    EntrepreneurId: certReqExisting.EntrepreneurId?.Value,
                    EntrepreneurEmail: certReqExisting.EntrepreneurEmail,
                    ContractEndDate: certReqExisting.ContractEndDate,
                    SupplyMethodCode: certReqExisting.SupplyMethodCode?.Value,
                    SupplyMethodLabel: certReqExisting.SupplyMethod?.Label,
                    SupplyMethodTypeCode: certReqExisting.SupplyMethodTypeCode?.Value,
                    SupplyMethodTypeLabel: certReqExisting.SupplyMethodType?.Label,
                    SupplyMethodSpecialTypeCode: certReqExisting.SupplyMethodSpecialTypeCode?.Value,
                    SupplyMethodSpecialTypeLabel: certReqExisting.SupplyMethodSpecialType?.Label);
            }

            static InspectionCommitteeSectionResponse MapInspectionCommitteeSection(
                CamCertificateRequisition certReq,
                AcceptorNoIdResponse[] acceptors)
            {
                var committees = acceptors
                                 .Where(a => a.AcceptorType == AcceptorType.AcceptanceCommittee)
                                 .OrderBy(a => a.Sequence)
                                 .Select(a => new InspectionCommitteeInfoResponse(
                                     a.Id,
                                     a.UserId,
                                     a.FullName,
                                     a.PositionName,
                                     a.CommitteePositionsCode,
                                     a.CommitteePositionName,
                                     a.Sequence))
                                 .ToArray();

                var isCommittee = certReq.Acceptors
                                         .Where(a => a.Type == AcceptorType.AcceptanceCommittee &&
                                                     a is { IsDeleted: false, IsActive: true })
                                         .All(a => a.IsCommittee());

                return new InspectionCommitteeSectionResponse(committees, isCommittee);
            }
        }
    }
}