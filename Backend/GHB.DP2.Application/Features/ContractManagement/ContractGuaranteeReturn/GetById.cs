namespace GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn;

using System.ComponentModel;
using System.Linq;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetContractGuaranteeReturnRequest(
    Guid ContractVendorId,
    Guid? Id);

public class GetContractGuaranteeReturnByIdEndpoint
    : ContractGuaranteeReturnEndpoint<GetContractGuaranteeReturnRequest, Results<Ok<ContractVendorGuaranteeReturnResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetContractGuaranteeReturnByIdEndpoint(
        ILogger<GetContractGuaranteeReturnByIdEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("contract/{contractVendorId:guid}/contract-guarantee-return/{id:guid?}");
        this.Description(b => b
                              .WithTags("ContractManagement/ContractGuaranteeReturn")
                              .WithName("GetContractGuaranteeReturnById")
                              .AllowAnonymous()
                              .Produces<ContractVendorGuaranteeReturnResponse>(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<ContractVendorGuaranteeReturnResponse>, NotFound<string>>> HandleRequestAsync(
        GetContractGuaranteeReturnRequest req, CancellationToken ct)
    {
        var entity = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractVendorId), ct);

        var invitationVendor = entity.ContractInvitationVendors;
        var suVendor = entity.ContractDraft.Procurement.Type is ProcurementType.Procurement
            ? invitationVendor?.PurchaseOrderApprovalContract?.Entrepreneur?.SuVendor
            : invitationVendor?.PurchaseOrderApprovalContract?.PrincipleApprovalRentalEntrepreneurs?.Vendor;

        if (suVendor is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้ประกอบการ");
        }

        if (req.Id is null)
        {
            var contractManager = await this.dbContext.RawEmployeePositions
                                            .Include(p => p.Employee)
                                            .ThenInclude(e => e.View)
                                            .Where(p =>
                                                p.BusinessUnitId == BusinessUnitId.From(JorPor.DefaultSectionHead.BusinessUnitId) &&
                                                p.Position.Name == JorPor.DefaultSectionHead.PositionName)
                                            .SelectMany(p => p.Employee.Users)
                                            .FirstOrDefaultAsync(ct);

            if (contractManager?.Employee?.View is null)
            {
                return TypedResults.NotFound("ไม่พบข้อมูลหัวหน้าส่วนบริหารสัญญา");
            }

            List<AssigneeResponse> assigneeResponses =
            [
                new(
                    null,
                    AssigneeGroup.Contract,
                    AssigneeType.Director,
                    contractManager.Id.Value,
                    1,
                    contractManager.FullName,
                    contractManager.Employee.View.FullPositionName,
                    contractManager.Employee.View.BusinessUnitName,
                    AssigneeStatus.Draft)
            ];

            List<AcceptorNoIdResponse> acceptors;

            if (entity.ContractDraft.Procurement.Type is ProcurementType.Procurement)
            {
                var committees = await this.dbContext.PJp005S
                                           .Where(w => w.ProcurementId == entity.ContractDraft.ProcurementId)
                                           .SelectMany(s => s.Committees)
                                           .Include(pJp005Committee => pJp005Committee.User)
                                           .ThenInclude(suUser => suUser.Employee)
                                           .ThenInclude(rawEmployee => rawEmployee.View)
                                           .Where(w => w.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
                                           .OrderBy(o => o.Sequence)
                                           .ToListAsync(ct);

                acceptors =
                [
                    .. committees.Map(s => new AcceptorNoIdResponse(
                        null,
                        AcceptorType.AcceptanceCommittee,
                        s.SuUserId.Value,
                        s.Sequence,
                        s.FullName,
                        s.FullPositionName.Trim(),
                        s.User.Employee.View?.BusinessUnitName ?? string.Empty,
                        AcceptorStatus.Draft,
                        CommitteePositionsCode: s.CommitteePositionsCode.Value,
                        CommitteePositionName: s.CommitteePositionsName,
                        IsUnableToPerformDuties: false))
                ];

                if (!committees.Any())
                {
                    var purchaseOrderApproval = await this.dbContext.PPurchaseOrderApprovals
                                                          .Where(w => w.ProcurementId == entity.ContractDraft.ProcurementId)
                                                          .SelectMany(s => s.Committees)
                                                          .Include(pJp005Committee => pJp005Committee.User)
                                                          .ThenInclude(suUser => suUser.Employee)
                                                          .ThenInclude(rawEmployee => rawEmployee.View)
                                                          .Where(w => w.GroupType == GroupType.InspectionCommittee)
                                                          .OrderBy(o => o.Sequence)
                                                          .ToListAsync(ct);

                    acceptors =
                    [
                        .. purchaseOrderApproval.Map(s => new AcceptorNoIdResponse(
                            null,
                            AcceptorType.AcceptanceCommittee,
                            s.SuUserId.Value,
                            s.Sequence,
                            s.FullName,
                            s.FullPositionName.Trim(),
                            s.User.Employee.View?.BusinessUnitName ?? string.Empty,
                            AcceptorStatus.Draft,
                            CommitteePositionsCode: s.CommitteePositionsCode.Value,
                            CommitteePositionName: s.CommitteePositionsName,
                            IsUnableToPerformDuties: false))
                    ];
                }
            }
            else
            {
                var committees = await this.dbContext.PPrincipleApprovals
                                           .Where(w => w.ProcurementId == entity.ContractDraft.ProcurementId)
                                           .SelectMany(s => s.PrincipleApprovalCommittees)
                                           .Include(i => i.User)
                                           .ThenInclude(suUser => suUser.Employee)
                                           .ThenInclude(rawEmployee => rawEmployee.View)
                                           .Where(w => w.GroupType == CommitteeGroupType.AcceptanceCommittee)
                                           .OrderBy(o => o.Sequence)
                                           .ToListAsync(ct);

                acceptors =
                [
                    .. committees.Map(s => new AcceptorNoIdResponse(
                        null,
                        AcceptorType.AcceptanceCommittee,
                        s.SuUserId.Value,
                        s.Sequence,
                        s.FullName,
                        s.FullPositionName.Trim(),
                        s.User.Employee.View?.BusinessUnitName ?? string.Empty,
                        AcceptorStatus.Draft,
                        CommitteePositionsCode: s.CommitteePositionsCode.Value,
                        CommitteePositionName: s.CommitteePositionsName,
                        IsUnableToPerformDuties: false))
                ];
            }

            var conditionList = new List<ConditionDto>()
            {
                new(null, 1, "บริษัทฯ ได้มีการปฏิบัติตามข้อกำหนดเงื่อนไขในสัญญาครบถ้วนหรือไม่", false),
                new(null, 2, "มีความเสียหายเกิดขึ้นจากการปฏิบัติงานตามสัญญาหรือไม่", false),
                new(null, 3, "มีความชำรุดบกพร่องของสิ่งของตามสัญญานี้ ซึ่งจะต้องเรียกร้องให้บริษัทฯ แก้ไขหรือชดใช้หรือไม่", false),
                new(null, 4, "พร้อมนำส่งสำเนาเอกสารการตรวจรับและเบิกจ่ายเงินงวดสุดท้ายที่มีการอนุมัติเรียบร้อยแล้ว", false),
            };

            var requiredDocumentList = new List<RequiredDocumentDto>()
            {
                new(null, 1, "ใบเสร็จรับเงินตัวจริงที่ธนาคารอาคารสงเคราะห์ออกให้", false),
                new(null, 2, "สำเนาบัญชีของธนาคารพาณิชย์", false),
                new(null, 3, "สำเนาบัตรประชาชนของผู้มีอำนาจลงนามผูกพันบริษัทฯ/ห้างหุ้นส่วนจำกัด", false),
                new(null, 4, "หนังสือรับรองสำนักงานทะเบียนหุ้นส่วนบริษัท กระทรวงพาณิชย์ที่ออกให้ไม่เกิน 6 เดือน", false),
            };

            if (entity.DraftTermsConditions.Guarantee.TypeCode != ParameterCode.From("PBondType001"))
            {
                requiredDocumentList = new List<RequiredDocumentDto>()
                {
                    new(null, 1, "ใบเสร็จรับเงินตัวจริงที่ธนาคารอาคารสงเคราะห์ออกให้", false),
                    new(null, 2, "สำเนาบัญชีของธนาคารพาณิชย์", false),
                };
            }

            var dto = new ContractGuaranteeReturnDto(
                null,
                entity.DraftTermsConditions.Guarantee.TypeCode,
                entity.DraftTermsConditions.Guarantee.TypeCode?.Value != "PBondType001" ? $"ประเภทหลักประกัน {entity.DraftTermsConditions.Guarantee.Type?.Label} {entity.DraftTermsConditions.Guarantee.Bank?.Label} สาขา{entity.DraftTermsConditions.Guarantee.BankBranch} เลขที่ {entity.DraftTermsConditions.Guarantee.ReferenceNumber} ลงวันที่ {entity.DraftTermsConditions.Guarantee.GuaranteeDate?.ToThaiDateString()} จำนวนเงิน {entity.DraftTermsConditions.Guarantee.Amount.Value.ToCurrencyStringWithComma()} บาท" : $"ประเภทหลักประกัน เงินสด (เงินโอน) เลขที่ {entity.DraftTermsConditions.Guarantee.ReferenceNumber} ลงวันที่ {entity.DraftTermsConditions.Guarantee.GuaranteeDate?.ToThaiDateString()} จำนวนเงิน {entity.DraftTermsConditions.Guarantee.Amount.Value.ToCurrencyStringWithComma()} บาท",
                $"สัญญา{entity.ContractName} สัญญาเลขที่ {entity.ContractNumber} ลงวันที่ {entity.ContractSignedDate.ToThaiDateString()} {entity.Vendor.EstablishmentName}",
                null,
                entity.DraftTermsConditions.Guarantee.TypeCode?.Value != "PBondType001" ? $"{entity.DraftTermsConditions.Guarantee.Type?.Label} {entity.DraftTermsConditions.Guarantee.Bank?.Label} สาขา{entity.DraftTermsConditions.Guarantee.BankBranch} เลขที่ {entity.DraftTermsConditions.Guarantee.BankAccountNumber} ลงวันที่ {entity.DraftTermsConditions.Guarantee.GuaranteeDate?.ToThaiDateString()}" : $"{entity.DraftTermsConditions.Guarantee.Type?.Label} เลขที่ {entity.DraftTermsConditions.Guarantee.ReferenceNumber}",
                null,
                entity.DraftTermsConditions.Guarantee.Amount,
                false,
                null,
                null,
                null,
                null,
                null,
                null,
                CmContractGuaranteeReturnStatus.Draft,
                null,
                null,
                null,
                null,
                assigneeResponses ?? [],
                acceptors,
                conditionList,
                requiredDocumentList,
                []);

            var responseNoId = new ContractVendorGuaranteeReturnResponse(
                entity.Id,
                suVendor.TaxpayerIdentificationNo,
                suVendor.EstablishmentName,
                suVendor.Email,
                entity.ContractNumber,
                entity.PoNumber,
                entity.Budget,
                entity.ContractName,
                entity.ContractType?.Label,
                entity.Template?.Label,
                entity.ContractSignedDate,
                entity.Delivery?.LeadTime,
                entity.Delivery?.LeadTimeTypeCode,
                entity.Delivery?.LeadTimeType?.Label,
                entity.Delivery?.Date,
                entity.DraftTermsConditions.Guarantee.Bank?.Label,
                entity.DraftTermsConditions.Guarantee.BankBranch,
                entity.ContractEndDate,
                dto);

            return TypedResults.Ok(responseNoId);
        }

        var guarantee = entity.CmContractGuaranteeReturns.SingleOrDefault(t => t.Id.Value == req.Id);

        if (guarantee is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลคืนหลักประกันสัญญา {req.Id}");
        }

        var lastedApprovalCmContractGuaranteeReturn = guarantee.GetApprovalDocumentForStatus(guarantee.Status);

        var isReplacedApproval = guarantee.DocumentHistories
                                          .Any(d => d.DocumentType == CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn && d.IsReplaced);

        var lastedContractGuaranteeReturnResult = guarantee.GetResultDocumentForStatus(guarantee.Status);

        var isReplacedContractGuarantee = guarantee.DocumentHistories
                                                   .Any(d => d.DocumentType == CmContractGuaranteeReturnDocumentType.CmContractGuaranteeReturnResule && d.IsReplaced);

        var approvalDocumentVersions = guarantee.DocumentHistories
                                                .Where(d => d.DocumentType == CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn)
                                                .OrderVersions()
                                                .Select((d, index) => new ContractGuaranteeReturnDocumentVersionResponse(
                                                    d.FileId.Value,
                                                    d.Version,
                                                    d.CreatedAt,
                                                    d.CreatedByName ?? string.Empty,
                                                    index == 0))
                                                .ToArray();

        var resultDocumentVersions = guarantee.DocumentHistories
                                              .Where(d => d.DocumentType == CmContractGuaranteeReturnDocumentType.CmContractGuaranteeReturnResule)
                                              .OrderVersions()
                                              .Select((d, index) => new ContractGuaranteeReturnDocumentVersionResponse(
                                                  d.FileId.Value,
                                                  d.Version,
                                                  d.CreatedAt,
                                                  d.CreatedByName ?? string.Empty,
                                                  index == 0))
                                              .ToArray();

        var currentAcceptors = guarantee.Acceptors
                                        .Where(x => x.Type != AcceptorType.AcceptanceCommittee)
                                        .Select(DelegatorExtensions.DelegatorToAcceptor)
                                        .ToList();

        var currentCommittees = guarantee.Acceptors
                                         .Where(x => x.Type == AcceptorType.AcceptanceCommittee)
                                         .ToList();
        var acceptorsApprover =
            currentAcceptors
                .Union(currentCommittees);

        var dtoGuarantee = new ContractGuaranteeReturnDto(
            guarantee.Id.Value,
            entity.DraftTermsConditions.Guarantee.TypeCode,
            entity.DraftTermsConditions.Guarantee.TypeCode?.Value != "PBondType001" ? $"ประเภทหลักประกัน {entity.DraftTermsConditions.Guarantee.Type?.Label} {entity.DraftTermsConditions.Guarantee.Bank?.Label} สาขา{entity.DraftTermsConditions.Guarantee.BankBranch} เลขที่ {entity.DraftTermsConditions.Guarantee.ReferenceNumber} ลงวันที่ {entity.DraftTermsConditions.Guarantee.GuaranteeDate?.ToThaiDateString()} จำนวนเงิน {entity.DraftTermsConditions.Guarantee.Amount.Value.ToCurrencyStringWithComma()} บาท" : $"ประเภทหลักประกัน เงินสด (เงินโอน) เลขที่ {entity.DraftTermsConditions.Guarantee.ReferenceNumber} ลงวันที่ {entity.DraftTermsConditions.Guarantee.GuaranteeDate?.ToThaiDateString()} จำนวนเงิน {entity.DraftTermsConditions.Guarantee.Amount.Value.ToCurrencyStringWithComma()} บาท",
            guarantee.ContractDescription,
            guarantee.ProofOfPaymentDescription,
            guarantee.GuranteeDescription,
            guarantee.GuaranteeReturnDate,
            guarantee.ReturnAmount,
            guarantee.IsDeducted,
            guarantee.DeductedAmount,
            guarantee.NetReturnAmount,
            guarantee.AdditionalComment,
            guarantee.DisbursementDate,
            guarantee.DisbursementAmount,
            guarantee.DisbursementRemark,
            guarantee.Status,
            lastedApprovalCmContractGuaranteeReturn?.FileId.Value,
            isReplacedApproval,
            lastedContractGuaranteeReturnResult?.FileId.Value,
            isReplacedContractGuarantee,
            [
                .. guarantee.Assignees
                            .OrderBy(a => a.Sequence)
                            .Select(DelegatorExtensions.DelegatorToAssignee)
                            .Select(a => new AssigneeResponse(
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
                                a.Delegatee?.SuUserId.Value))
            ],
            [
                .. acceptorsApprover.OrderBy(a => a.Sequence)
                                    .Select(a => new AcceptorNoIdResponse(
                                        a.Id.Value,
                                        a.Type,
                                        a.UserId.Value,
                                        a.Sequence,
                                        a.FullName,
                                        a.PositionName,
                                        a.BusinessUnitName,
                                        a.Status,
                                        a.Remark,
                                        a.ActionAt,
                                        (string?)a.CommitteePositionsCode,
                                        a.CommitteePosition?.Label ?? string.Empty,
                                        a.IsUnableToPerformDuties,
                                        IsCurrent: a.IsCurrentApprover(),
                                        DelegateeUserId: a.Delegatee?.SuUserId.Value))
            ],
            [
                .. guarantee.Conditions
                            .OrderBy(o => o.Sequence)
                            .Select(c => new ConditionDto(c.Id.Value, c.Sequence, c.Description, c.IsSatisfied))
            ],
            [
                .. guarantee.RequiredDocuments
                            .OrderBy(o => o.Sequence)
                            .Select(d => new RequiredDocumentDto(d.Id.Value, d.Sequence, d.DocumentName, d.IsSubmitted))
            ],
            [
                .. guarantee.Attachments
                            .OrderBy(o => o.Sequence)
                            .GroupBy(
                                a => a.DocumentTypeCode,
                                (key, g) => new AttachmentsDtoWithId(
                                    key.Value,
                                    [.. g.Select(s => new FileAttachmentsWithId(s.Id.Value, s.FileId.Value, s.FileName, s.Sequence, s.IsPublic, s.AuditInfo.CreatedBy))]))
            ],
            approvalDocumentVersions,
            resultDocumentVersions,
            guarantee.IsSendMail,
            guarantee.EmailSend,
            guarantee.EmailTemplate,
            [
                .. guarantee.EmailAttachments
                            .OrderBy(o => o.Sequence)
                            .Select(a => new GuaranteeReturnEmailAttachmentDto(
                                a.Id.Value,
                                a.FileName,
                                a.FileId.Value,
                                a.Sequence))
            ],
            guarantee.DocumentDate);

        var response = new ContractVendorGuaranteeReturnResponse(
            entity.Id,
            suVendor.TaxpayerIdentificationNo,
            suVendor.EstablishmentName,
            suVendor.Email,
            entity.ContractNumber,
            entity.PoNumber,
            entity.Budget,
            entity.ContractName,
            entity.ContractType?.Label,
            entity.Template?.Label,
            entity.ContractSignedDate,
            entity.Delivery?.LeadTime,
            entity.Delivery?.LeadTimeTypeCode,
            entity.Delivery?.LeadTimeType?.Label,
            entity.Delivery?.Date,
            entity.DraftTermsConditions.Guarantee.Bank?.Label,
            entity.DraftTermsConditions.Guarantee.BankBranch,
            entity.ContractEndDate,
            dtoGuarantee);

        return TypedResults.Ok(response);
    }
}

public record ContractVendorGuaranteeReturnResponse(
    [property: Description("รหัสคู่ค้าร่างสัญญา")]
    ContractDraftVendorId Id,
    [property: Description("เลขที่ผู้เสียภาษี")]
    string TaxId,
    [property: Description("ชื่อผู้ประกอบการ")]
    string EntrepreneurName,
    [property: Description("อีเมลผู้ประกอบการ")]
    string EntrepreneurEmail,
    [property: Description("เลขที่สัญญา")] string ContractNumber,
    [property: Description("เลขที่ใบสั่งซื้อ")]
    string PoNumber,
    [property: Description("งบประมาณ")] decimal Budget,
    [property: Description("ชื่อสัญญา")] string ContractName,
    [property: Description("ประเภทสัญญา")] string? ContractType,
    [property: Description("แม่แบบสัญญา")] string? ContractTemplate,
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
    [property: Description("ชื่อธนาคารค้ำประกัน")]
    string? GuaranteeBankName,
    [property: Description("สาขาธนาคารค้ำประกัน")]
    string? GuaranteeBankBranch,
    [property: Description("วันที่สิ้นสุดสัญญา")]
    DateTimeOffset? ContractEndDate,
    [property: Description("ข้อมูลการคืนหลักประกัน")]
    ContractGuaranteeReturnDto GuaranteeReturn);

public record ContractGuaranteeReturnDocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);

public record ContractGuaranteeReturnDto(
    [property: Description("รหัสการคืนหลักประกัน")]
    Guid? Id,
    ParameterCode? GuaranteeTypeCode,
    string? GuaranteeReturnDescription,
    string? ContractDescription,
    string? ProofOfPaymentDescription,
    string? GuranteeDescription,
    [property: Description("วันที่คืนหลักประกัน")]
    DateTimeOffset? GuaranteeReturnDate,
    [property: Description("จำนวนเงินที่คืน")]
    decimal? ReturnAmount,
    [property: Description("มีการหักระเงิน")]
    bool IsDeducted,
    [property: Description("จำนวนเงินที่หัก")]
    decimal? DeductedAmount,
    [property: Description("จำนวนเงินสุทธิที่คืน")]
    decimal? NetReturnAmount,
    [property: Description("คำอธิบายเพิ่มเติม")]
    string? AdditionalComment,
    [property: Description("วันที่เบิกจ่าย")]
    DateTimeOffset? DisbursementDate,
    [property: Description("จำนวนเงินเบิกจ่าย")]
    decimal? DisbursementAmount,
    [property: Description("หมายเหตุเบิกจ่าย")]
    string? DisbursementRemark,
    [property: Description("สถานะการคืนหลักประกัน")]
    CmContractGuaranteeReturnStatus? Status,
    [property: Description("เอกสารขออนุมัติคืนหหลักประกัน")]
    Guid? ApprovalCmContractGuaranteeReturnDocumentId,
    bool? IsApprovalCmContractGuaranteeReturnDocumentIdReplaced,
    [property: Description("เอกสารผลการพิจารณาคืนหลักประกันสัญญา")]
    Guid? ContractGuaranteeReturnResultDocumentId,
    bool? IsContractGuaranteeReturnResultDocumentIdReplaced,
    [property: Description("รายชื่อผู้รับมอบหมาย")]
    List<AssigneeResponse>? Assignees,
    [property: Description("รายชื่อผู้อนุมัติ")]
    List<AcceptorNoIdResponse> Acceptors,
    [property: Description("เงื่อนไขการคืนหลักประกัน")]
    List<ConditionDto>? Conditions,
    [property: Description("เอกสารที่ต้องใช้ประกอบ")]
    List<RequiredDocumentDto>? RequiredDocuments,
    [property: Description("เอกสารแนบ")] AttachmentsDtoWithId[] Attachments,
    ContractGuaranteeReturnDocumentVersionResponse[]? ApprovalDocumentVersions = null,
    ContractGuaranteeReturnDocumentVersionResponse[]? ResultDocumentVersions = null,
    [property: Description("สถานะการส่งอีเมล")]
    bool IsSendMail = false,
    [property: Description("อีเมลที่ส่ง")]
    string? EmailSend = null,
    [property: Description("เทมเพลตอีเมล")]
    string? EmailTemplate = null,
    [property: Description("ไฟล์แนบอีเมล")]
    GuaranteeReturnEmailAttachmentDto[]? EmailAttachments = null,
    [property: Description("วันที่เอกสาร")]
    DateTimeOffset? DocumentDate = null);

public record GuaranteeReturnEmailAttachmentDto(
    Guid? Id,
    string FileName,
    Guid FileId,
    int Sequence);

public record ConditionDto(
    [property: Description("รหัสเงื่อนไข")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รายละเอียดเงื่อนไข")]
    string Description,
    [property: Description("สำเร็จแล้ว")] bool IsSatisfied);

public record RequiredDocumentDto(
    [property: Description("รหัสเอกสาร")] Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("ชื่อเอกสาร")] string DocumentName,
    [property: Description("ส่งมอบแล้ว")] bool IsSubmitted);