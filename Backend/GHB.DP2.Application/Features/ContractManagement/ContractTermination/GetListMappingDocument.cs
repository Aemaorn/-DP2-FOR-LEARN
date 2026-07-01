namespace GHB.DP2.Application.Features.ContractManagement.ContractTermination;

using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

public record ContractVendorTerminationReplaceDto(
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
    [property: Description("ข้อมูลการบอกเลิกสัญญา")]
    ContractTerminationReplaceDto ContractTermination,
    [property: Description("ข้อมูลสัญญา")] ContractDraftInfoReplaceDto? ContractDraftInfoDetail,
    [property: Description("วันที่อนุมัติ")] string? AcceptorDate,
    [property: Description("ความเห็นจากเจ้าหน้าที่พัสดุ")]
    JorPorCommentDto? JorPorComment,
    string? ContractDraftNumber,
    string? ContractDraftSignedDateFormat);

public record ContractTerminationReplaceDto(
    [property: Description("รหัสการบอกเลิกสัญญา")]
    Guid Id,
    [property: Description("วันที่บอกเลิกสัญญา")]
    DateTimeOffset? TerminationDate,
    [property: Description("เหตุผลการบอกเลิกสัญญา")]
    string? TerminateTypeName,
    [property: Description("เหตุผลการบอกเลิกอื่น ๆ")]
    string? TerminateReason,
    [property: Description("รายละเอียดเหตุผลการบอกเลิกสัญญา")]
    string? TerminateReasonDetail,
    [property: Description("สถานะการบอกเลิกสัญญา")]
    CmContractTerminationStatus Status,
    [property: Description("รหัสเอกสารบอกเลิกสัญญา")]
    Guid? ContractTerminationDocumentId,
    [property: Description("รายชื่อผู้รับมอบหมาย")]
    IEnumerable<ContractTerminationAssigneeResponse>? Assignees,
    [property: Description("รายชื่อผู้อนุมัติ")]
    IEnumerable<ContractTerminationAcceptorReplace>? Acceptors,
    [property: Description("ข้อมูลผู้จัดทำเอกสาร")]
    ContractTerminationCreatorDto? Creator);

public record ContractTerminationAssigneeResponse(
    [property: Description("เห็นชอบ/อนุมัติ")]
    string Action,
    [property: Description("ชื่อผู้เห็นชอบ/อนุมัติ")]
    string FullName,
    [property: Description("ตำแหน่งผู้เห็นชอบ/อนุมัติ")]
    string PositionName,
    [property: Description("ปฏิบัติหน้าที่แทน")]
    string Delegate);

public record ContractTerminationAcceptorReplace(
    [property: Description("เห็นชอบ/อนุมัติ")]
    string Action,
    [property: Description("ชื่อผู้เห็นชอบ/อนุมัติ")]
    string FullName,
    [property: Description("ตำแหน่งผู้เห็นชอบ/อนุมัติ")]
    string PositionName,
    [property: Description("ปฏิบัติหน้าที่แทน")]
    string Delegate);

public record ContractTerminationCreatorDto(
    [property: Description("ลายเซ็นต์")] string? Action,
    [property: Description("ลายเซ็นต์")] string? Signature,
    [property: Description("ชื่อ-สกุล")] string? FullName,
    [property: Description("ตำแหน่ง")] string? PositionName);

public class GetListMappingContractTerminationDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingContractTerminationDocumentEndpoint(ILogger<GetListMappingContractTerminationDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("ContractManagement/ContractTermination"));
        this.Get("contract/contract-termination/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(ContractVendorTerminationReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}