namespace GHB.DP2.Application.Features.Procurement.MedianPrice;

using System.ComponentModel;
using System.Text.Json.Serialization;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.Procurement.Abstact;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record MedianPriceReplaceDto(
    [property: Description("ข้อมูลการแต่งตั้งคณะกรรมการ")]
    MedianPriceAppointReplaceDto Appoint,
    [property: Description("วันที่ทำเอกสารราคากลาง")]
    string? AcceptorDate,
    [property: Description("หมายเลขโทรศัพท์")]
    string? Telephone,
    [property: Description("ตำแหน่งผู้มีอำนาจอนุมัติ")]
    IEnumerable<SectionApprove>? SectionApproveName,
    [property: Description("ประเภทคณะกรรมการ หรือผู้จัดทำ ราคากลาง")]
    string? MedianPriceCommitteeName,
    [property: Description("วันที่อนุมัติขอแต่งตั้งคณะกรรมการ หรือผู้จัดทำ")]
    string? MemorandumDate,
    [property: Description("ประเภทคณะกรรมการ หรือผู้จัดทำ TORและราคากลาง")]
    string? AppointTypeDescription,
    [property: Description("ข้อมูลประกอบการพิจารณาด้านความเหมาะสมของราคา")]
    string? ConsiderInformation,
    [property: Description("จำนวนคู่ค้าเสนอราคากลาง")]
    string? MedianPriceSourceQty,
    [property: Description("คำสั่งอนุมัติ")]
    string? CommandText,
    [property: Description("รหัสราคากลาง")]
    MedianPriceId Id,
    [property: Description("รหัสการจัดซื้อจัดจ้าง")]
    ProcurementId ProcurementId,
    [property: Description("ข้อมูลการจัดซื้อจัดจ้าง")]
    ProcurementReplaceDto Procurement,
    [property: Description("เลขที่อ้างอิง")]
    string ReferenceNumber,
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
    [property: Description("ข้อมูลการจัดสรรงบประมาณ")]
    BudgetAllocationInfoReplaceDto BudgetAllocations,
    [property: Description("ข้อมูลบุคลากร")]
    MedianPriceStaffInfoReplaceDto? Staff,
    [property: Description("ข้อมูลที่ปรึกษา")]
    MedianPriceConsultantInfoReplaceDto? Consultant,
    [property: Description("ข้อมูลรายละเอียดค่าใช้จ่าย")]
    MedianPriceExpenseDescriptionInfoReplaceDto? ExpenseDescription,
    [property: Description("ผู้อนุมัติ")] MedianPriceAcceptorReplaceInfo[] Acceptors,
    [property: Description("คณะกรรมการกำหนดราคากลาง ")]
    MedianPriceAcceptorReplaceInfo[] Committees,
    [property: Description("ผู้รับมอบหมาย")]
    AssigneeResponse[] Assignees,
    [property: Description("ข้อมูลผู้จัดทำเอกสาร")]
    CreatorReplaceDto? Creator,
    [property: Description("ผู้รับมอบหมาย")]
    JorPorCommentReplace? JorPorComment);

public record MedianPriceStaffDetailReplaceDto(
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("รายละอียด")]
    string Description,
    [property: Description("จำนวนคน")]
    int PersonalCount);

// ข้อมูลบุคลากร บก. 02 -03
public record MedianPriceStaffInfoReplaceDto(
    [property: Description("ค่าตอบแทนบุคลากร")]
    string Compensation,
    [property: Description("ค่าตอบแทนบุคลากร (ตัวอักษร)")]
    string CompensationText,
    [property: Description("ข้อมูลบุคลากร")]
    MedianPriceStaffDetailReplaceDto[] Staffs);

public record MedianPriceConsultantDetailReplaceDto(
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รายละอียด")] string Description);

// ข้อมูลที่ปรึกษา บก. 04
public record MedianPriceConsultantInfoReplaceDto(
    [property: Description("ค่าตอบแทนบุคลากร")]
    string Compensation,
    [property: Description("ค่าตอบแทนบุคลากร (ตัวอักษร)")]
    string CompensationText,
    [property: Description("จำนวนที่ปรึกษา")]
    int PersonalCount,
    [property: Description("รายการประเภทที่ปรึกษา")]
    MedianPriceConsultantDetailReplaceDto[]? Types,
    [property: Description("รายการคุณสมบัติที่ปรึกษา")]
    MedianPriceConsultantDetailReplaceDto[]? Qualifications);

public record MedianPriceAppointReplaceDto(
    [property: Description("หมายเลขคำสั่งแต่งตั้ง")]
    string AppointNumber);

public record CreatorReplaceDto(
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("ผู้จัดทำ")] string Signature,
    [property: Description("ชื่อผู้จัดทำ")]
    string FullName,
    [property: Description("ตำแหน่งผู้จัดทำ")]
    string PositionName,
    [property: Description("ตำแหน่งผู้จัดทำในคณะกรรมการ")]
    string? PositionOnBoard);

public record MedianPriceAcceptorReplaceInfo(
    [property: Description("รหัสผู้อนุมัติ")]
    Guid Id,
    [property: Description("การดำเนินการ")]
    string? Action,
    [property: Description("ประเภทผู้อนุมัติ")]
    AcceptorType AcceptorType,
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("ชื่อ-สกุล")] string FullName,
    [property: Description("ตำแหน่ง")] string PositionName,
    [property: Description("ชื่อหน่วยงาน")]
    string DepartmentName,
    [property: Description("สถานะการอนุมัติ")]
    AcceptorStatus Status,
    [property: Description("หมายเหตุ")] string? Remark,
    [property: Description("วันที่ดำเนินการ")]
    DateTimeOffset? ActionAt,
    [property: Description("รหัสตำแหน่งคณะกรรมการ")]
    string? CommitteePositionsCode,
    [property: Description("ชื่อตำแหน่งคณะกรรมการ")]
    string? CommitteePositionName,
    [property: Description("ไม่สามารถปฏิบัติหน้าที่ได้")]
    bool? IsUnableToPerformDuties,
    [property: Description("เป็นผู้อนุมัติปัจจุบัน")]
    bool IsCurrent,
    [property: Description("รหัสหน่วยงาน")]
    string? DepartmentCode,
    [property: Description("ตำแหน่งในคณะกรรมการ หรือผู้จัดทำ")]
    string? PositionOnBoard,
    [property: Description("ตำแหน่งผู้ปฏิบัติหน้าที่แทน ผู้เห็นชอบ หรืออนุมัติ")]
    string? DelegatePositionName);

public record BudgetAllocationInfoReplaceDto(
    [property: Description("รหัสการจัดสรรงบประมาณ")]
    BudgetAllocationsId Id,
    [property: Description("วันที่อ้างอิง")]
    string ReferenceDate,
    [property: Description("งบประมาณ")] string Budget,
    [property: Description("งบประมาณ (ตัวอักษร)")]
    string BudgetText,
    [property: Description("ราคากลางอ้างอิง")]
    string ReferenceMedianPrice,
    [property: Description("ราคากลางอ้างอิง (ข้อความ)")]
    string ReferenceMedianPriceText,
    [property: Description("รายละเอียดการจัดสรร")]
    BudgetAllocationDetailInfoReplaceDto[] Details);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(BudgetAllocationsWithDetailReplaceDto), nameof(BudgetAllocationsDetailType.With))]
[JsonDerivedType(typeof(BudgetAllocationsWithoutDetailReplaceDto), nameof(BudgetAllocationsDetailType.Without))]
public abstract record BudgetAllocationDetailInfoReplaceDto(
    [property: Description("รหัสรายละเอียดการจัดสรร")]
    BudgetAllocationsDetailId Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("แหล่งที่มา")] string Source,
    [property: Description("ราคา")] string ReferenceBudget,
    [property: Description("ราคา (ตัวอักษร)")]
    string ReferenceBudgetText);

public record BudgetAllocationsWithDetailReplaceDto(
    BudgetAllocationsDetailId Id,
    int Sequence,
    string Source,
    string ReferenceBudget,
    string ReferenceBudgetText)
    : BudgetAllocationDetailInfoReplaceDto(
        Id,
        Sequence,
        Source,
        ReferenceBudget,
        ReferenceBudgetText);

public record BudgetAllocationsWithoutDetailReplaceDto(
    BudgetAllocationsDetailId Id,
    int Sequence,
    string Source)
    : BudgetAllocationDetailInfoReplaceDto(
        Id,
        Sequence,
        Source,
        string.Empty,
        string.Empty);

public record MedianPriceExpenseDescriptionInfoReplaceDto(
    [property: Description("ค่าวัสดุ")]
    string MaterialCost,
    [property: Description("ค่าวัสดุ (ตัวอักษร)")]
    string MaterialCostText,
    [property: Description("ค่าเดินทางต่างประเทศ")]
    string OverseasTravelCost,
    [property: Description("ค่าเดินทางต่างประเทศ (ตัวอักษร)")]
    string OverseasTravelCostText,
    [property: Description("ค่าใช้จ่ายอื่นๆ")]
    string OtherExpenses,
    [property: Description("ค่าใช้จ่ายอื่นๆ (ตัวอักษร)")]
    string OtherExpensesText,
    [property: Description("ค่าฮาร์ดแวร์")]
    string HardwareCost,
    [property: Description("ค่าฮาร์ดแวร์ (ตัวอักษร)")]
    string HardwareCostText,
    [property: Description("ค่าซอฟต์แวร์")]
    string SoftwareCost,
    [property: Description("ค่าซอฟต์แวร์ (ตัวอักษร)")]
    string SoftwareCostText,
    [property: Description("ค่าพัฒนาระบบ")]
    string SystemDevelopmentCost,
    [property: Description("ค่าพัฒนาระบบ (ตัวอักษร)")]
    string SystemDevelopmentCostText);

public class GetListMappingMedianPriceDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingMedianPriceDocumentEndpoint(ILogger<GetListMappingMedianPriceDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags(nameof(MedianPrice)));
        this.Get("procurement/median-price/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(MedianPriceReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}