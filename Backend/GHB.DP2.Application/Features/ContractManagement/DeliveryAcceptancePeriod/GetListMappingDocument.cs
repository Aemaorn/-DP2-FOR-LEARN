namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod;

using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

public record CreatorReplace(
    [property: Description("รหัสผู้ใช้งาน")] Guid UserId,
    [property: Description("ลายเซ็นต์")] string Signature,
    [property: Description("ชื่อ-สกุล")] string FullName,
    [property: Description("ตำแหน่ง")] string PositionName,
    [property: Description("ตำแหน่งในคณะกรรมการ หรือผู้จัดทำ")]
    string? PositionOnBoard);

public record AcceptorReplace(
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("การดำเนินการ")]
    string Action,
    [property: Description("เห็นชอบหรืออนุมัติ")]
    string Signature,
    [property: Description("ชื่อคณะกรรมการหรือผู้จัดทำ")]
    string FullName,
    [property: Description("ตำแหน่งผู้ผู้เห็นชอบ หรืออนุมัติ")]
    string? FullPositionName,
    [property: Description("ตำแหน่งผู้ปฏิบัติหน้าที่แทน เห็นชอบ หรืออนุมัติ")]
    string? Delegate,
    [property: Description("หมายเหตุ")]
    string? Remark,
    [property: Description("สถานะ")]
    AcceptorStatus Status,
    string? PositionName,
    string? PositionOnBoard);

public record DeliveryReplaceDto(
    [property: Description("รหัสการส่งมอบ")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("วันที่ส่งมอบ")]
    string? DeliveryDate,
    [property: Description("ผลการพิจารณา")]
    string ConsiderationResult,
    [property: Description("รายการส่งมอบ")]
    IEnumerable<DeliveryItemReplaceDto> DeliveryItems);

public record DeliveryItemReplaceDto(
    [property: Description("รหัสรายการส่งมอบ")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รายละเอียด")] string Description,
    [property: Description("จำนวน")] int Quantity,
    [property: Description("ราคาต่อหน่วย")]
    string? Price,
    [property: Description("ราคาต่อหน่วย (ตัวอักษร)")]
    string? PriceText,
    [property: Description("รวมราคา")] string? Total,
    [property: Description("รวมราคา (ตัวอักษร)")] string? TotalText);

public record DeliveryAcceptancePeriodReplaceDto(
    [property: Description("รหัสงวดการตรวจรับการส่งมอบ")]
    CmDeliveryAcceptancePeriodId Id,
    [property: Description("สถานะงวดการตรวจรับการส่งมอบ")]
    CmDeliveryAcceptancePeriodStatus Status,
    [property: Description("วันที่ส่งมอบ")]
    string? DeliveryDate,
    [property: Description("วันที่ส่งเห็นชอบ")]
    string? AcceptorDate,
    [property: Description("ตำแหน่งผู้มีอำนาจอนุมัติ")]
    IEnumerable<SectionApprove>? SectionApproveName,
    [property: Description("เลขที่สัญญา")]
    string? ContractNumber,
    [property: Description("ชื่อสัญญา")]
    string? ContractName,
    [property: Description("ลำดับงวด")]
    int? Sequence,
    [property: Description("งวดที่")]
    int? PaymentTermNo,
    [property: Description("ระยะเวลานำส่ง (วัน)")]
    int? LeadTime,
    [property: Description("รายละเอียด")]
    string? Description,
    [property: Description("เปอร์เซ็นต์การจ่ายเงิน")]
    decimal? InstallmentPercentage,
    [property: Description("จำนวนเงิน")]
    string? Amount,
    [property: Description("จำนวนเงิน (ตัวอักษร)")]
    string? AmountText,
    [property: Description("มีการมอบหมาย จพ.")]
    bool HasJorPorAssign,
    [property: Description("มีสิทธิ์แก้ไข")]
    bool HasEditPermission,
    [property: Description("ข้อมูลการตรวจรับงวด")]
    PeriodAcceptanceInfoDto? PeriodAcceptanceInfo,
    [property: Description("คณะกรรมการตรวจรับ")]
    IEnumerable<AcceptorNoIdResponse> AcceptanceCommittees,
    [property: Description("ผู้รับมอบหมาย")]
    IEnumerable<AssigneeNoIdResponse> Assignees,
    [property: Description("ผู้อนุมัติ")]
    AcceptorReplace[] Acceptors,
    [property: Description("ผู้อนุมัติ")]
    AcceptorReplace[] Committees,
    Guid? DocumentId,
    bool? IsDocumentReplaced,
    [property: Description("ข้อมูลผู้จัดทำเอกสาร")]
    CreatorReplace? Creator,
    string? CommandText,
    string? Telephone,
    string? ObjectiveDescription,
    IEnumerable<PaymentTermReplaceDto> PaymentTerm,
    string? CommitteesGroupType,
    string? SpeaclalType,
    string? ContractDescription,
    [property: Description("ผู้รับมอบหมาย(จพ.ให้ความเห็น)")]
    JorPorCommentReplace? JorPorComment);

public record PaymentTermReplaceDto(
    [property: Description("งวดที่")] int PaymentTermNo,
    [property: Description("จำนวนเงิน")]
    string? Amount,
    [property: Description("จำนวนเงิน (ตัวอักษร)")]
    string? AmountText,
    [property: Description("รายละเอียด")] string? Description);

public class GetListMappingDeliveryAcceptancePeriodDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingDeliveryAcceptancePeriodDocumentEndpoint(ILogger<GetListMappingDeliveryAcceptancePeriodDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("ContractManagement/DeliveryAcceptance/Period"));
        this.Get("delivery-acceptance/period/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(DeliveryAcceptancePeriodReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}