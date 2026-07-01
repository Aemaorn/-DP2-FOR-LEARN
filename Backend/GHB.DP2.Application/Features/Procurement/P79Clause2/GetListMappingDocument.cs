namespace GHB.DP2.Application.Features.Procurement.P79Clause2;

using System.ComponentModel;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.P79Clause2;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

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
    string? Signature,
    [property: Description("ชื่อคณะกรรมการหรือผู้จัดทำ")]
    string FullName,
    [property: Description("ตำแหน่งผู้ผู้เห็นชอบ หรืออนุมัติ")]
    string? FullPositionName,
    [property: Description("ตำแหน่งผู้ปฏิบัติหน้าที่แทน เห็นชอบ หรืออนุมัติ")]
    string? Delegate,
    [property: Description("หมายเหตุ")]
    string? Remark,
    [property: Description("สถานะ")]
    AcceptorStatus Status);

public record PublisherDto(
    [property: Description("ลายเซ็นต์ผู้ประกาศผู้ชนะ")]
    string? Signature,
    [property: Description("ชื่อผู้ประกาศผู้ชนะ")]
    string? FullName,
    [property: Description("ตำแหน่งผู้ประกาศผู้ชนะ")]
    string? PositionName,
    [property: Description("ตำแหน่งในคณะกรรมการ หรือผู้จัดทำ")]
    string? ActingPosition,
    [property: Description("ตำแหน่งผู้ปฏิบัติหน้าที่แทน")]
    string? DelegatePosition,
    [property: Description("วันที่ประกาศผู้ชนะ")]
    string PublishedDate);

public record P79Clause2AdvanceReplaceDto(
    [property: Description("ชื่อผู้รับเงินล่วงหน้า")]
    string? AdvanceName,
    [property: Description("รหัสวิธีการจ่ายล่วงหน้า")]
    string? AdvancePaymentMethodCode,
    [property: Description("รหัสวิธีการจ่ายล่วงหน้า")]
    string? AdvancePaymentMethodName,
    [property: Description("วันที่จ่ายล่วงหน้า")]
    string? AdvancePaymentDate,
    [property: Description("รหัสธนาคารจ่ายล่วงหน้า")]
    string? AdvanceBankCode,
    [property: Description("รหัสธนาคารจ่ายล่วงหน้า")]
    string? AdvanceBankName,
    [property: Description("เลขที่บัญชีจ่ายล่วงหน้า")]
    string? AdvanceBankAccount,
    [property: Description("สาขาธนาคารจ่ายล่วงหน้า")]
    string? AdvanceBankBranch,
    [property: Description("ชื่อบัญชีจ่ายล่วงหน้า")]
    string? AdvanceBankAccountName,
    [property: Description("รายละเอียดการจ่ายล่วงหน้า")]
    string? AdvanceDetail);

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
    [property: Description("หน่วยนับ")]
    string Unit,
    [property: Description("ราคาต่อหน่วย")]
    string UnitPrice,
    [property: Description("ราคาต่อหน่วย (ตัวอักษร)")]
    string UnitPriceText,
    [property: Description("ราคารวม")] string TotalPrice,
    [property: Description("ราคารวม (ตัวอักษร)")] string TotalPriceText,
    [property: Description("ราคารวมรวมภาษี")]
    string TotalPriceVat,
    [property: Description("ราคารวมรวมภาษี (ตัวอักษร)")]
    string TotalPriceVatText);

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
    [property: Description("ประเภทภาษีมูลค่าเพิ่ม")]
    string? VatIncludeTypeName,
    [property: Description("รหัสประเภทใบเสร็จ")]
    string BillTypeCode,
    [property: Description("ประเภทใบเสร็จ")]
    string BillType,
    [property: Description("ประเภทเอกสาร อื่นๆ")]
    string? BillTypeOther,
    [property: Description("เลขที่เล่มใบเสร็จ")]
    string BillBookNo,
    [property: Description("วันที่ใบเสร็จ")]
    string BillDate,
    [property: Description("รายละเอียดใบเสร็จ")]
    string? BillDetail,
    [property: Description("รายการพัสดุ")] IEnumerable<VendorParcelReplace> VendorParcels);

public record GLAccountReplace(
    [property: Description("รหัสบัญชี GL")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รหัส SOL")] string SolId,
    [property: Description("รหัสประเภทงบประมาณ")]
    string BudgetTypeCode,
    [property: Description("ประเภทงบประมาณ")]
    string BudgetType,
    [property: Description("รหัสบัญชี GL")]
    string GLAccountCode,
    [property: Description("บัญชี GL")]
    string GLAccountName,
    [property: Description("เลขที่โครงการ")]
    string? ProjectNumber,
    [property: Description("จำนวนเงิน")] string Amount,
    [property: Description("จำนวนเงิน (ตัวอักษร)")] string AmountText);

public record GetP79Clause2ReplaceDto(
    [property: Description("รหัส P79 ข้อ 2")]
    Guid Id,
    [property: Description("เลขที่ P79 ข้อ 2")]
    string P79Clause2Number,
    [property: Description("ตำแหน่งผู้มีอำนาจอนุมัติ")]
    IEnumerable<SectionApprove>? SectionApproveName,
    [property: Description("สถานะ")] P79Clause2Status Status,
    [property: Description("วันที่ P79 ข้อ 2")]
    string P79Clause2Date,
    [property: Description("รหัสหน่วยงาน")]
    string DepartmentCode,
    [property: Description("หน่วยงาน")]
    string DepartmentName,
    [property: Description("ปีงบประมาณ")] int BudgetYear,
    [property: Description("รหัสวิธีจัดซื้อจัดจ้าง")]
    string SupplyMethodCode,
    [property: Description("รหัสวิธีจัดซื้อจัดจ้าง (ตัวอักษร)")]
    string SupplyMethodName,
    [property: Description("รหัสประเภทวิธีจัดซื้อจัดจ้าง")]
    string SupplyMethodTypeCode,
    [property: Description("รหัสประเภทวิธีจัดซื้อจัดจ้าง (ตัวอักษร)")]
    string SupplyMethodTypeName,
    [property: Description("รหัสประเภทพิเศษวิธีจัดซื้อจัดจ้าง")]
    string? SupplyMethodSpecialTypeCode,
    [property: Description("รหัสประเภทพิเศษวิธีจัดซื้อจัดจ้าง (ตัวอักษร)")]
    string? SupplyMethodSpecialTypeName,
    [property: Description("เรื่อง")] string Subject,
    [property: Description("เบอร์โทร")] string? Telephone,
    [property: Description("แหล่งที่มา")] string Source,
    [property: Description("งบประมาณ")] string Budget,
    [property: Description("งบประมาณ (ตัวอักษร)")] string BudgetText,
    [property: Description("ราคากลาง")] string? MedianPrice,
    [property: Description("ราคากลาง (ตัวอักษร)")] string? MedianPriceText,
    [property: Description("เหตุผลข้อ 1")] string? ReasonItem1,
    [property: Description("เหตุผลข้อ 2")] string? ReasonItem2,
    [property: Description("เหตุผลข้อ3")] string? ReasonItem3,
    [property: Description("เป็นการจ่ายล่วงหน้า")]
    bool IsAdvance,
    [property: Description("ข้อมูลการจ่ายล่วงหน้า")]
    P79Clause2AdvanceReplaceDto Advance,
    [property: Description("ผู้ขาย")] IEnumerable<VendorReplace>? Vendors,
    [property: Description("บัญชี GL")] IEnumerable<GLAccountReplace>? GLAccounts,
    [property: Description("ผู้อนุมัติ")] AcceptorReplace[] Acceptors,
    [property: Description("ไฟล์แนบ")] AttachmentsDto[] Attachments,
    [property: Description("ข้อมูลผู้จัดทำเอกสาร")]
    CreatorReplace? Creator,
    [property: Description("ข้อมูลผู้ประกาศผู้ชนะ")]
    PublisherDto? Publisher,
    [property: Description("ข้อมูลภาษี")]
    string VatDescription,
    [property: Description("ชื่อคู่ค้า")]
    string VendorsName,
    [property: Description("คำสั่งแต่งตั้งคณะกรรมการ")]
    string CommandText,
    [property: Description("SolId รวม")]
    string GlAccountCodes,
    [property: Description("วันที่")]
    string? AcceptorDate,
    string? TotalAmount,
    string? TotalAmountText,
    string? GlAccountText,
    string? DeliveryDate,
    string? ProcurementReasonItem1,
    string? ProcurementReasonItem2,
    IEnumerable<VendorParcelReplace> VendorParcels);

public class GetListMappingP79Clause2DocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingP79Clause2DocumentEndpoint(ILogger<GetListMappingP79Clause2DocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("P79Clause2"));
        this.Get("p79Clause2/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(GetP79Clause2Response);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}