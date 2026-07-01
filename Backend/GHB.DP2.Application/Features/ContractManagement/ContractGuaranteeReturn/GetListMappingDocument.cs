namespace GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn;

using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

public record ContractVendorGuaranteeReturnReplaceDto(
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
    [property: Description("งบประมาณ")] string Budget,
    [property: Description("งบประมาณข้อความ")]
    string BudgetText,
    [property: Description("ชื่อสัญญา")] string ContractName,
    [property: Description("ประเภทสัญญา")] string? ContractType,
    [property: Description("แม่แบบสัญญา")] string? ContractTemplate,
    [property: Description("วันที่ลงนามสัญญา")]
    string? ContractSignedDate,
    [property: Description("ระยะเวลาการส่งมอบ (วัน)")]
    int? DeliveryLeadTime,
    [property: Description("รหัสประเภทระยะเวลาการส่งมอบ")]
    ParameterCode? DeliveryLeadTimeTypeCode,
    [property: Description("ชื่อประเภทระยะเวลาการส่งมอบ")]
    string? DeliveryLeadTimeTypeLabel,
    [property: Description("วันที่กำหนดส่งมอบ")]
    string? DeliveryDate,
    [property: Description("ข้อมูลการคืนหลักประกัน")]
    ContractGuaranteeReturnReplaceDto GuaranteeReturn,
    [property: Description("ข้อมูลสัญญา")] ContractDraftInfoReplaceDto? ContractDraftInfoDetail,
    string? ContractDescription,
    string? ProofOfPaymentDescription,
    string? GuranteeDescription,
    string? Warranty,
    string? WarrantyDueDate,
    string? ReceiveDate,
    string? PaidDate,
    string? AcceptorDate,
    [property: Description("ข้อมูลผู้จัดทำเอกสาร")]
    ContractGuaranteeReturnCreatorDto? Creator,
    IEnumerable<ContractGuaranteeReturnAcceptorReplace>? Acceptors,
    IEnumerable<SectionApprove>? SectionApproveName,
    string? BankOrderDescription);

public record ContractGuaranteeReturnReplaceDto(
    [property: Description("รหัสการคืนหลักประกัน")]
    Guid? Id,
    [property: Description("วันที่คืนหลักประกัน")]
    string? GuaranteeReturnDate,
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
    [property: Description("สถานะการคืนหลักประกัน")]
    CmContractGuaranteeReturnStatus? Status,
    [property: Description("เอกสารขออนุมัติคืนหหลักประกัน")]
    Guid? ApprovalCmContractGuaranteeReturnDocumentId,
    [property: Description("เอกสารผลการพิจารณาคืนหลักประกันสัญญา")]
    Guid? ContractGuaranteeReturnResultDocumentId,
    [property: Description("รายชื่อผู้รับมอบหมาย")]
    IEnumerable<ContractGuaranteeReturnAssigneeResponse>? Assignees,
    [property: Description("รายชื่อผู้อนุมัติ")]
    IEnumerable<ContractGuaranteeReturnAcceptorReplace>? Acceptors,
    IEnumerable<ContractGuaranteeReturnCommitteeReplace>? Committee,
    [property: Description("ข้อมูลผู้จัดทำเอกสาร")]
    ContractGuaranteeReturnCreatorDto? Creator,
    [property: Description("ข้อมูลผู้เผยแพร่เอกสาร")]
    ContractGuaranteeReturnAcceptorReplace? Publisher,
    [property: Description("เงื่อนไขการคืนหลักประกัน")]
    IEnumerable<ConditionReplaceDto>? Conditions,
    [property: Description("เอกสารที่ต้องใช้ประกอบ")]
    IEnumerable<RequiredDocumentReplaceDto>? RequiredDocuments,
    [property: Description("เอกสารแนบ")] AttachmentsDtoWithId[] Attachments);

public record ContractGuaranteeReturnAssigneeResponse(
    [property: Description("เห็นชอบ/อนุมัติ")]
    string Action,
    [property: Description("ชื่อผู้เห็นชอบ/อนุมัติ")]
    string FullName,
    [property: Description("ตำแหน่งผู้เห็นชอบ/อนุมัติ")]
    string PositionName,
    [property: Description("ปฏิบัติหน้าที่แทน")]
    string Delegate);

public record ContractGuaranteeReturnAcceptorReplace(
    [property: Description("เห็นชอบ/อนุมัติ")]
    string Action,
    [property: Description("ชื่อผู้เห็นชอบ/อนุมัติ")]
    string FullName,
    [property: Description("ตำแหน่งผู้เห็นชอบ/อนุมัติ")]
    string PositionName,
    [property: Description("ปฏิบัติหน้าที่แทน")]
    string Delegate);

public record ContractGuaranteeReturnCommitteeReplace(
    [property: Description("เห็นชอบ/อนุมัติ")]
    string Action,
    [property: Description("ชื่อผู้เห็นชอบ/อนุมัติ")]
    string FullName,
    [property: Description("ตำแหน่งผู้เห็นชอบ/อนุมัติ")]
    string PositionName,
    [property: Description("ปฏิบัติหน้าที่แทน")]
    string Delegate);

public record ContractGuaranteeReturnCreatorDto(
    [property: Description("ลายเซ็นต์")] string? Action,
    [property: Description("ลายเซ็นต์")] string? Signature,
    [property: Description("ชื่อ-สกุล")] string? FullName,
    [property: Description("ตำแหน่ง")] string? FullPositionName);

public record ConditionReplaceDto(
    [property: Description("รหัสเงื่อนไข")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รายละเอียดเงื่อนไข")]
    string Description,
    [property: Description("สำเร็จแล้ว")] bool IsSatisfied);

public record RequiredDocumentReplaceDto(
    [property: Description("รหัสเอกสาร")] Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("ชื่อเอกสาร")] string DocumentName,
    [property: Description("ส่งมอบแล้ว")] bool IsSubmitted);

public class GetListMappingContractGuaranteeReturnDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingContractGuaranteeReturnDocumentEndpoint(ILogger<GetListMappingContractGuaranteeReturnDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("ContractManagement/ContractGuaranteeReturn"));
        this.Get("contract/contract-guarantee-return/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(ContractVendorGuaranteeReturnReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}