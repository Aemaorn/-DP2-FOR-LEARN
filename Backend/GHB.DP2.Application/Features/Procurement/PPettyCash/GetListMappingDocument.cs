namespace GHB.DP2.Application.Features.Procurement.PPettyCash;

using System.ComponentModel;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPettyCash;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record GetPPettyCashReplaceDto(
    [property: Description("รหัสเงินสดย่อย")]
    Guid Id,
    [property: Description("วันที่ขออนุมัติ")]
    string? AcceptorDate,
    [property: Description("ขอบเขตของงานหรือรายละเอียด")]
    string? ParcelDescription,
    [property: Description("ตำแหน่งผู้มีอำนาจอนุมัติ")]
    IEnumerable<SectionApprove>? SectionApproveName,
    [property: Description("เลขที่เงินสดย่อย")]
    string PPettyCashNumber,
    [property: Description("สถานะ")]
    PettyCashStatus Status,
    [property: Description("วันที่เงินสดย่อย")]
    string PPettyCashDate,
    [property: Description("รหัสหน่วยงาน")]
    string DepartmentCode,
    [property: Description("รหัสหน่วยงาน")]
    string DepartmentName,
    [property: Description("ปีงบประมาณ")]
    int BudgetYear,
    [property: Description("รหัสวิธีจัดซื้อจัดจ้าง")]
    string SupplyMethodCode,
    [property: Description("วิธีจัดซื้อจัดจ้าง")]
    string SupplyMethodName,
    [property: Description("รหัสประเภทวิธีจัดซื้อจัดจ้าง")]
    string SupplyMethodTypeCode,
    [property: Description("ประเภทวิธีจัดซื้อจัดจ้าง")]
    string SupplyMethodTypeName,
    [property: Description("รหัสประเภทพิเศษวิธีจัดซื้อจัดจ้าง")]
    string? SupplyMethodSpecialTypeCode,
    [property: Description("ประเภทพิเศษวิธีจัดซื้อจัดจ้าง")]
    string? SupplyMethodSpecialTypeName,
    [property: Description("เรื่อง")]
    string Subject,
    [property: Description("ขอบเขตของงานหรือรายละเอียดคุณลักษณะเฉพาะของพัสดุที่จะซื้อหรือจ้าง")]
    string? Source,
    [property: Description("เหตุผลความจำเป็นที่ต้องซื่อหรือจ้าง")]
    string Reasons,
    [property: Description("วันที่กำหนดเวลาที่ต้องการใช้พัสดุนั้น หรือ ให้งานนั้นแล้วเสร็จ")]
    string? DeliveryDate,
    [property: Description("งบประมาณ")]
    string Budget,
    string BudgetText,
    [property: Description("กำหนดเวลาที่ต้องการใช้พัสดุนั้น หรือ ให้งานนั้นแล้วเสร็จ")]
    int? DeliveryPeriod,
    [property: Description("วัน/เดือน/ปี")]
    string? DeliveryPeriodTypeCode,
    [property: Description("เงื่อนไขการนับเวลาที่ต้องการใช้พัสดุนั้น")]
    string? DeliveryConditionCode,
    [property: Description("วันที่เบิกจ่าย")]
    string? DisbursementDate,
    [property: Description("เป็นการจ่ายล่วงหน้า")]
    bool IsAdvance,
    [property: Description("ข้อมูลการจ่ายล่วงหน้า")]
    PPettyCashAdvanceReplace Advance,
    [property: Description("หมวดค่าใช้จ่าย")]
    IEnumerable<CategoriesDto>? Categories,
    [property: Description("ผู้ขาย")]
    IEnumerable<VendorReplace> Vendors,
    [property: Description("บัญชี GL")]
    IEnumerable<GlAccountReplace> GlAccounts,
    [property: Description("ผู้ขอซื้อขอจ้าง")]
    CommitteeReplace[]? ProcurementCommittees,
    [property: Description("ผู้ตรวจรับพัสดุ")]
    CommitteeReplace[]? InspectionCommittees,
    CommitteeReplace[]? AcceptorInspectionCommittees,
    [property: Description("ผู้ให้ความเห็นชอบ")]
    AcceptorReplace[]? Acceptors,
    [property: Description("ผู้รับมอบหมาย")]
    AssigneeResponse[] Assignees,
    [property: Description("ไฟล์แนบ")] AttachmentsDtoWithId[] Attachments,
    [property: Description("สามารถเรียกคืนแก้ไขได้")]
    bool HasPermission,
    [property: Description("ประเภทเงินสด")]
    string CashType,
    [property: Description("ข้อมูลผู้จัดทำเอกสาร")]
    CreatorReplace? Creator);

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
    [property: Description("ตำแหน่งผู้ผู้เห็นชอบ หรืออนุมัติ")]
    string? CommitteePositionsName,
    [property: Description("ตำแหน่งผู้ปฏิบัติหน้าที่แทน เห็นชอบ หรืออนุมัติ")]
    string? Delegate,
    [property: Description("หมายเหตุ")]
    string? Remark,
    [property: Description("สถานะ")]
    AcceptorStatus Status);

public record CommitteeReplace(
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("การดำเนินการ")]
    string Action,
    [property: Description("คณะกรรมการหรือผู้จัดทำ เห็นชอบ")]
    string Signature,
    [property: Description("ชื่อคณะกรรมการหรือผู้จัดทำ")]
    string FullName,
    [property: Description("ตำแหน่งจริง คณะกรรมการ หรือผู้จัดทำ")]
    string? FullPositionName,
    [property: Description("ตำแหน่งในผู้ตรวจรับพัสดุ")]
    string? CommitteePositionsName,
    [property: Description("ตำแหน่งในคณะกรรมการ หรือผู้จัดทำ")]
    string? PositionOnBoard,
    [property: Description("ตำแหน่งปฏิบัติหน้าที่แทน คณะกรรมการ หรือผู้จัดทำ")]
    string? Delegate);

public record VendorParcelReplace(
    [property: Description("รหัสรายการพัสดุ")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รายการ")] string Item,
    [property: Description("รายละเอียดรายการ")]
    string? ItemDetail,
    [property: Description("จำนวน")] int Quantity,
    [property: Description("รหัสหน่วยนับ")]
    string UnitCode,
    [property: Description("ราคาต่อหน่วย")]
    string UnitPrice,
    [property: Description("ราคารวม")] string TotalPrice,
    [property: Description("ราคารวมรวมภาษี")]
    string TotalPriceVat);

public record VendorReplace(
    [property: Description("รหัสผู้ขาย")] Guid? Id,
    [property: Description("ประเภทผู้ขาย")]
    string VendorType,
    [property: Description("รหัสผู้ขายในระบบ")]
    Guid? SuVendorId,
    [property: Description("ชื่อผู้ขาย")] string VendorName,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("เลขที่ผู้เสียภาษี")]
    string? TaxNumber,
    [property: Description("เลขที่สาขาผู้ขาย")]
    string? VendorBranchNumber,
    [property: Description("รหัสประเภทภาษีมูลค่าเพิ่ม")]
    string? VatIncludeTypeCode,
    [property: Description("รหัสประเภทใบเสร็จ")]
    string BillTypeCode,
    [property: Description("ประเภทเอกสาร อื่นๆ")]
    string? BillTypeOther,
    [property: Description("เลขที่เล่มใบเสร็จ")]
    string BillBookNo,
    [property: Description("วันที่ใบเสร็จ")]
    string? BillDate,
    [property: Description("รายละเอียดใบเสร็จ")]
    string? BillDetail,
    [property: Description("รายการพัสดุ")] IEnumerable<VendorParcelReplace> VendorParcels);

public record PPettyCashAdvanceReplace(
    [property: Description("ชื่อผู้รับเงินล่วงหน้า")]
    string? AdvanceName,
    [property: Description("รหัสวิธีการจ่ายล่วงหน้า")]
    string? AdvancePaymentMethodCode,
    [property: Description("วันที่จ่ายล่วงหน้า")]
    string? AdvancePaymentDate,
    [property: Description("รหัสธนาคารจ่ายล่วงหน้า")]
    string? AdvanceBankCode,
    [property: Description("เลขที่บัญชีจ่ายล่วงหน้า")]
    string? AdvanceBankAccount,
    [property: Description("สาขาธนาคารจ่ายล่วงหน้า")]
    string? AdvanceBankBranch,
    [property: Description("ชื่อบัญชีจ่ายล่วงหน้า")]
    string? AdvanceBankAccountName,
    [property: Description("รายละเอียดการจ่ายล่วงหน้า")]
    string? AdvanceDetail);

public record GlAccountReplace(
    [property: Description("รหัสบัญชี GL")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รหัส SOL")] string SolId,
    [property: Description("รหัส SOL")] string SolLabel,
    [property: Description("รหัสประเภทงบประมาณ")]
    string BudgetTypeCode,
    [property: Description("รหัสประเภทงบประมาณ")]
    string BudgetTypeName,
    [property: Description("รหัสบัญชี GL")]
    string GlAccountCode,
    [property: Description("รหัสบัญชี GL")]
    string GlAccountName,
    [property: Description("เลขที่โครงการ")]
    string? ProjectNumber,
    [property: Description("จำนวนเงิน")]
    string Amount);

public class GetListMappingPPettyCashDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingPPettyCashDocumentEndpoint(ILogger<GetListMappingPPettyCashDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("PPettyCash"));
        this.Get("pPettyCash/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(GetPPettyCashReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}