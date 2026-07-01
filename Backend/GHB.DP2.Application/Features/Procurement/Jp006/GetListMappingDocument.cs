namespace GHB.DP2.Application.Features.Procurement.Jp006;

using System.ComponentModel;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record Jp006ReplaceDto(
    [property: Description("หมายเลขโทรศัพท์")]
    string? Telephone,
    [property: Description("ข้อมูลการจัดซื้อจัดจ้าง")]
    Jp06ProcurementReplaceDto? Procurement,
    [property: Description("วันที่ดำเนินการทำขออนุมัติสั่งซื้อ/สั่งจ้าง (จพ.006)")]
    string? AcceptorDate,
    [property: Description("ผู้จัดซื้อจัดจ้าง หรือคณะกรรมการจัดซื้อจัดจ้าง")]
    string? DocumentFrom,
    [property: Description("หัวเรื่องเอกสาร")]
    string? DocumentSubject,
    [property: Description("ตำแหน่งผู้มีอำนาจอนุมัติตามคำสั่ง")]
    IEnumerable<SectionApprove>? SectionApproveName,
    [property: Description("ตำแหน่งผู้อนุมัติ จพ.005")]
    string? Jp005LastSectionApproverPosition,
    [property: Description("หน่วยงาน")]
    string? SolName,
    [property: Description("ผู้จัดซื้อจัดจ้าง หรือคณะกรรมการจัดซื้อจัดจ้าง")]
    string? ProcurementCommitteeSection,
    [property: Description("ราคากลาง")]
    string? MedianPriceAmount,
    [property: Description("ราคากลาง (ตัวอักษร)")]
    string? MedianPriceAmountText,
    [property: Description("ราคารวมที่ตกลง")]
    string? AgreedPriceTotal,
    [property: Description("ราคารวมที่ตกลง (ตัวอักษร)")]
    string? AgreedPriceTotalText,
    [property: Description("ราคารวมที่เสนอ")]
    string? OfferPriceTotal,
    [property: Description("ราคารวมที่เสนอ (ตัวอักษร)")]
    string? OfferPriceTotalText,
    [property: Description("ราคารวมผู้ค้าที่ตกลง")]
    string? EntrepreneurPriceTotal,
    [property: Description("ราคารวมผู้ค้าที่ตกลง (ตัวอักษร)")]
    string? EntrepreneurPriceTotalText,
    [property: Description("รวมภาษีมูลค่าเพิ่ม หรือไม่รวมภาษีมูลค่าเพิ่ม")]
    string? VatDescription,
    [property: Description("เหตุผลที่เลือกเป็นผู้ชนะ")]
    string? SelectionReason,
    [property: Description("รายละเอียดเหตุผลที่เลือกเป็นผู้ชนะ")]
    string? Remark,
    [property: Description("ข้อความคำสั่งอำนาจอนุมัติ")]
    string? CommandText,
    [property: Description("รหัสบัญชี")]
    string? GlAccountText,
    [property: Description("ผู้ให้เช่า, ผู้รับจ้าง, ผู้ขาย")]
    string? SupplyMethodTypeValue2,
    [property: Description("ข้อมูลผู้จัดทำเอกสาร")]
    CreatorDto? Creator,
    [property: Description("ผู้ประกอบการ")]
    IEnumerable<Jp006EntrepreneurReplaceDto> Entrepreneurs,
    IEnumerable<Jp006EntrepreneurReplaceDto> AllEntrepreneurs,
    [property: Description("ผู้ประกอบการ")]
    Jp006EntrepreneurReplaceDto? EntrepreneurWinner,
    [property: Description("ผู้อนุมัติ")]
    IEnumerable<Jp006AcceptorReplaceDtoInfo>? Acceptors,
    [property: Description("ผู้อนุมัติ")]
    IEnumerable<Jp006AcceptorReplaceDtoInfo>? Committees,
    [property: Description("ผู้รับมอบหมาย")]
    IEnumerable<AssigneeResponse>? Assignees,
    [property: Description("ผู้ประกาศผู้ชนะ")]
    PublisherDto? Publisher,
    string? PublisherDate,
    string? InviteText,
    string? Jp06Number,
    string? LastedAssigneeJp04Department,
    JorPorCommentReplace? JorPorComment);

public record Jp06ProcurementReplaceDto(
    [property: Description("รหัสแผนงาน")]
    Guid? PlanId,
    [property: Description("เลขที่การจัดซื้อจัดจ้าง")]
    string? ProcurementNumber,
    [property: Description("ประเภทการจัดซื้อจัดจ้าง")]
    ProcurementType ProcurementType,
    [property: Description("ขั้นตอนการจัดซื้อจัดจ้าง")]
    ProcurementStep ProcurementStep,
    [property: Description("ชื่อหน่วยงาน")]
    string DepartmentName,
    [property: Description("รหัสหน่วยงาน")]
    string DepartmentCode,
    [property: Description("เลขที่แผนงาน")]
    string? PlanNumber,
    [property: Description("ชื่อแผนงาน")]
    string PlanName,
    [property: Description("งบประมาณ")]
    string? Budget,
    [property: Description("งบประมาณ ภาษาไทย")]
    string? BudgetText,
    [property: Description("ปีงบประมาณ")]
    decimal? BudgetYear,
    [property: Description("วิธีการจัดหา")]
    string? SupplyMethod,
    [property: Description("รหัสวิธีการจัดหา")]
    string? SupplyMethodCode,
    [property: Description("ประเภทวิธีการจัดหา")]
    string? SupplyMethodType,
    [property: Description("รหัสประเภทวิธีการจัดหา")]
    string? SupplyMethodTypeCode,
    [property: Description("ประเภทวิธีการจัดหาพิเศษ")]
    string? SupplyMethodSpecialType,
    [property: Description("รหัสประเภทวิธีการจัดหาพิเศษ")]
    string? SupplyMethodSpecialTypeCode,
    [property: Description("สถานะการจัดซื้อจัดจ้าง")]
    ProcurementStatus Status,
    [property: Description("วันที่คาดว่าจะดำเนินการจัดซื้อจัดจ้าง")]
    DateTimeOffset? ExpectingProcurementAt,
    [property: Description("เป็นสต็อก")]
    bool IsStock,
    [property: Description("เป็นวัสดุเชิงพาณิชย์")]
    bool IsCommercialMaterial,
    [property: Description("ประเภทแผนงาน")]
    PlanType? PlanType,
    [property: Description("ขั้นตอนปัจจุบัน")]
    ProcessType CurrentStep);

public record PublisherDto(
    [property: Description("ลายเซ็นต์ผู้ประกาศผู้ชนะ")]
    string? Signature,
    [property: Description("ชื่อผู้ประกาศผู้ชนะ")]
    string? FullName,
    [property: Description("ตำแหน่งผู้ประกาศผู้ชนะ")]
    string? PositionName,
    [property: Description("ตำแหน่งในคณะกรรมการ หรือผู้จัดทำ")]
    string? ActingPosition,
    [property: Description("ตำแหน่งในคณะกรรมการ หรือผู้จัดทำ")]
    string? Delegate,
    [property: Description("กรรมการผู้จัดการ")]
    string? ManagingDirector);

public record PpAppointMedianPriceCommitteeReplace(
    [property: Description("สถานะการอนุมัติ")]
    string? Action,
    [property: Description("ชื่อ-สกุล")]
    string? FullName,
    [property: Description("ตำแหน่ง")]
    string? FullPositionName,
    [property: Description("ไม่สามารถปฏิบัติหน้าที่ได้")]
    PpAppointMedianPriceCommitteeDelegateReplace? Delegate);

public record PpAppointMedianPriceCommitteeDelegateReplace(
    [property: Description("สถานะการอนุมัติ")]
    string? Action,
    [property: Description("ชื่อ-สกุล")]
    string? FullName,
    [property: Description("ตำแหน่ง")]
    string? FullPositionName);

public record CreatorDto(
    [property: Description("ลายเซ็นต์")]
    string Signature,
    [property: Description("ชื่อ-สกุล")]
    string FullName,
    [property: Description("ตำแหน่ง")]
    string PositionName,
    [property: Description("สถานะผู้จัดทำ")]
    string Action,
    [property: Description("ตำแหน่งในคณะกรรมการ หรือผู้จัดทำ")]
    string? PositionOnBoard);

public record PpPurchaseRequisitionBudgetDetailReplace(
    [property: Description("ชื่อหน่วยงานเจ้าของงบ")]
    string Department,
    [property: Description("ผู้จัดซื้อจัดจ้าง หรือคณะกรรมการจัดซื้อจัดจ้าง")]
    string AccountNo);

public record PpMedianPriceBudgetAllocationsReplace(
    [property: Description("ราคากลาง")]
    string? ReferenceMedianPrice,
    [property: Description("ราคากลาง (ตัวอักษร)")]
    string? ReferenceMedianPriceText);

public record PPurchaseOrderPriceDetailsReplace(
    [property: Description("ราคาที่ตกลง")] string? AgreedPrice,
    [property: Description("ราคาที่ตกลง (ตัวอักษร)")]
    string? AgreedPriceText,
    [property: Description("ราคาที่เสนอทั้งหมด")]
    string? OfferedPriceTotal);

public record Jp006AcceptorReplaceDtoInfo(
    [property: Description("สถานะการเห็นชอบ")]
    string? Action,
    [property: Description("รหัสผู้อนุมัติ")]
    Guid? Id,
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
    [property: Description("ตำแหน่งผู้ปฏิบัติหน้าที่แทนผู้เห็นชอบ หรือนุมัติ")]
    string? Delegate);

public record Jp006EntrepreneurReplaceDto(
    [property: Description("รหัสผู้ประกอบการ")]
    Guid? EntrepreneurId,
    [property: Description("รหัสผู้ขาย")] Guid VendorId,
    [property: Description("ส่งอีเมลแล้ว")]
    bool EmailSended,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("การตรวจสอบความขัดแย้งทางผลประโยชน์")]
    EntrepreneurCheckConditions Coi,
    [property: Description("การตรวจสอบ Watchlist")]
    EntrepreneurCheckConditions Watchlist,
    [property: Description("การตรวจสอบ eGP")]
    EntrepreneurCheckConditions Egp,
    [property: Description("เลขที่ผู้เสียภาษีผู้ประกอบการ")]
    string EntrepreneurTaxId,
    [property: Description("ประเภทผู้ประกอบการ")]
    string EntrepreneurType,
    [property: Description("ชื่อผู้ประกอบการ")]
    string EntrepreneurName,
    [property: Description("อีเมลผู้ประกอบการ")]
    string EntrepreneurEmail,
    [property: Description("สัญชาติผู้ประกอบการ")]
    SuVendorNationality EntrepreneurNationality,
    [property: Description("ประเภทผู้ขาย")]
    SuVendorType Type,
    [property: Description("ชื่อสถานที่ผู้ประกอบการ")]
    string EntrepreneurPlaceName,
    [property: Description("เบอร์โทรศัพท์ผู้ประกอบการ")]
    string? EntrepreneurPhoneNumber,
    [property: Description("เป็นผู้ชนะ")] bool IsWinner,
    [property: Description("รหัสเหตุผลการเลือก")]
    string? SelectionReasonCode,
    [property: Description("หมายเหตุ")] string? Remark,
    [property: Description("รายละเอียดราคา")]
    IEnumerable<Jp006PriceDetailsReplaceDto> PriceDetails,
    Jp006PriceDetailReplaceDto PriceDetail,
    [property: Description("เข้าร่วมการเสนอราคา")]
    bool IsBidding,
    [property: Description("ผู้ถือหุ้น")] PurchaseOrderEntrepreneurShareholderReplaceDto[]? Shareholder,
    string? Detail1,
    string? Detail2);

public record PurchaseOrderEntrepreneurShareholderReplaceDto(
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
    DateTimeOffset? EgpResultAt
);

public record Jp006PriceDetailsReplaceDto(
    [property: Description("รหัสรายละเอียดราคา")]
    Guid PriceDetailsId,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("ชื่อพัสดุ")] string ParcelName,
    [property: Description("จำนวนพัสดุ")] string ParcelQuantity,
    [property: Description("รหัสหน่วยนับพัสดุ")]
    string ParcelUnitCode,
    [property: Description("รหัสประเภทภาษีมูลค่าเพิ่ม")]
    string? VatTypeCode,
    [property: Description("ราคาที่เสนอ")] decimal OfferedPrice,
    [property: Description("ราคาที่ตกลง")] decimal AgreedPrice,
    [property: Description("รายละเอียด")] string Description);

public record Jp006PriceDetailReplaceDto(
    [property: Description("ราคาที่เสนอ")] string OfferedPrice,
    [property: Description("ราคาที่ตกลง")] string AgreedPrice);

public class GetListMappingJp006DocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingJp006DocumentEndpoint(ILogger<GetListMappingJp006DocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags(nameof(Jp006)));
        this.Get("procurement/jp006/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(Jp006ReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}