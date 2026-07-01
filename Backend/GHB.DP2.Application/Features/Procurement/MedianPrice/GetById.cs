namespace GHB.DP2.Application.Features.Procurement.MedianPrice;

using System.ComponentModel;
using System.Text.Json.Serialization;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.MedianPrice.Abstract;
using GHB.DP2.Application.Features.Procurement.MedianPrice.Dto;
using GHB.DP2.Application.Features.Procurement.Procurement;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record MedianPriceDocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);

public record GetMedianPriceByIdRequest(
    Guid ProcurementId,
    Guid Id);

public record GetMedianPriceByIdResponse(
    [property: Description("รหัสราคากลาง")]
    MedianPriceId Id,
    [property: Description("รหัสการจัดซื้อจัดจ้าง")]
    ProcurementId ProcurementId,
    [property: Description("ข้อมูลการจัดซื้อจัดจ้าง")]
    ProcurementDto Procurement,
    [property: Description("เลขที่อ้างอิง")]
    string ReferenceNumber,
    DateTimeOffset? DocumentDate,
    [property: Description("วัตถุประสงค์")]
    string Object,
    [property: Description("เหตุผล")] string Reason,
    [property: Description("คำอธิบายเพิ่มเติม")]
    string SpecialDescription,
    [property: Description("รายละเอียดงาน")]
    string? JobDescription,
    [property: Description("ข้อมูลความเหมาะสมของราคา")]
    string PriceReasonablenessInfo,
    [property: Description("รหัสเทมเพลตเอกสารราคากลาง")]
    string MedianPriceDocumentTemplateCode,
    [property: Description("สถานะราคากลาง")]
    MedianPriceStatus Status,
    [property: Description("รหัสเอกสารราคากลาง")]
    Guid? MedianPriceDocumentId,
    [property: Description("รหัสเอกสารราคากลาง เปลี่ยนแปลง")]
    bool? IsMedianPriceDocumentIdReplaced,
    [property: Description("ข้อมูลการจัดสรรงบประมาณ")]
    BudgetAllocationInfo BudgetAllocations,
    [property: Description("ข้อมูลบุคลากร")]
    MedianPriceStaffInfo? Staff,
    [property: Description("ข้อมูลรายละเอียดค่าใช้จ่าย")]
    MedianPriceExpenseDescriptionInfo? ExpenseDescription,
    [property: Description("ผู้อนุมัติ")] MedianPriceAcceptorResponseInfo[] Acceptors,
    [property: Description("ผู้รับมอบหมาย")]
    AssigneeResponse[] Assignees,
    [property: Description("หมายเหตุขอยกเลิก")]
    string? CancelReason,
    [property: Description("หมายเหตุขอเปลี่ยนแปลง")]
    string? ChangeReason,
    [property: Description("มีการเปลี่ยนแปลง")]
    bool IsChange,
    [property: Description("ถูกยกเลิก")]
    bool IsCancel,
    string? Telephone,
    [property: Description("ใช้งานอยู่")] bool IsActive,
    string? TorTemplate,
    [property: Description("ประวัติเวอร์ชันเอกสาร")]
    MedianPriceDocumentVersionResponse[] DocumentVersions);

public record BudgetAllocationInfo(
    [property: Description("รหัสการจัดสรรงบประมาณ")]
    BudgetAllocationsId Id,
    [property: Description("วันที่อ้างอิง")]
    DateTimeOffset? ReferenceDate,
    [property: Description("งบประมาณ")] decimal Budget,
    [property: Description("ราคากลางอ้างอิง")]
    decimal ReferenceMedianPrice,
    [property: Description("รายละเอียดการจัดสรร")]
    BudgetAllocationDetailInfo[] Details);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(BudgetAllocationsWithDetail), nameof(BudgetAllocationsDetailType.With))]
[JsonDerivedType(typeof(BudgetAllocationsWithoutDetail), nameof(BudgetAllocationsDetailType.Without))]
public abstract record BudgetAllocationDetailInfo(
    [property: Description("รหัสรายละเอียดการจัดสรร")]
    BudgetAllocationsDetailId Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("แหล่งที่มา")] string Source);

public record BudgetAllocationsWithDetail(
    BudgetAllocationsDetailId Id,
    int Sequence,
    string Source,
    decimal ReferenceBudge) : BudgetAllocationDetailInfo(Id, Sequence, Source);

public record BudgetAllocationsWithoutDetail(
    BudgetAllocationsDetailId Id,
    int Sequence,
    string Source) : BudgetAllocationDetailInfo(Id, Sequence, Source);

public record MedianPriceStaffInfo(
    [property: Description("รหัสบุคลากรราคากลาง")]
    MedianPriceStaffId Id,
    [property: Description("ค่าตอบแทนบุคลากร")]
    decimal PersonnelCompensation,
    [property: Description("จำนวนบุคลากร")]
    int PersonnelCount,
    [property: Description("รายละเอียดบุคลากร")]
    MedianPriceStaffDetailInfo[] Details);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(StaffPersonnelDetail), nameof(MedianPriceStaffType.Personal))]
[JsonDerivedType(typeof(StaffConsultantTypeDetail), nameof(MedianPriceStaffType.ConsultantTypes))]
[JsonDerivedType(typeof(StaffConsultantQualificationDetail), nameof(MedianPriceStaffType.ConsultantQualifications))]
public record MedianPriceStaffDetailInfo(
    [property: Description("รหัสรายละเอียดบุคลากร")]
    MedianPriceStaffDetailId Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("คำอธิบาย")] string Description);

public record StaffPersonnelDetail(
    MedianPriceStaffDetailId Id,
    int Sequence,
    string Description,
    decimal PersonnelCount) : MedianPriceStaffDetailInfo(Id, Sequence, Description);

public record StaffConsultantTypeDetail(
    MedianPriceStaffDetailId Id,
    int Sequence,
    string Description) : MedianPriceStaffDetailInfo(Id, Sequence, Description);

public record StaffConsultantQualificationDetail(
    MedianPriceStaffDetailId Id,
    int Sequence,
    string Description) : MedianPriceStaffDetailInfo(Id, Sequence, Description);

public class GetMedianPriceByIdEndpoint : MedianPriceEndpointBase<GetMedianPriceByIdRequest, Results<Ok<GetMedianPriceByIdResponse>, NotFound<string>>>
{
    public GetMedianPriceByIdEndpoint(
        ILogger<GetMedianPriceByIdEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        Dp2DbContext dbContext)
        : base(logger, dbContext, operationService, commandTextService)
    {
    }

    public override void Configure()
    {
        this.Get("procurement/{ProcurementId:guid}/median-price/{Id:guid}");
        this.Description(b => b
                              .WithTags(nameof(MedianPrice))
                              .WithName("GetMedianPriceById")
                              .Produces<GetMedianPriceByIdResponse>()
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<GetMedianPriceByIdResponse>, NotFound<string>>> HandleRequestAsync(GetMedianPriceByIdRequest req, CancellationToken ct)
    {
        var medianPrice = await this.GetMedianPriceById(MedianPriceId.From(req.Id), ProcurementId.From(req.ProcurementId), ct);

        var response = this.MapToResponse(medianPrice);

        return TypedResults.Ok(response);
    }
}