namespace GHB.DP2.Application.Features.Procurement.Pw119;

using System.ComponentModel;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Pw119;
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
    string? DelegatePositionName,
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

public record GLAccountReplace(
    [property: Description("รหัสบัญชี GL")]
    Guid? Id,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("รหัส SOL")]
    string SolId,
    [property: Description("SOL Code")]
    string SolCode,
    [property: Description("SOL Label")]
    string SolLabel,
    [property: Description("รหัสประเภทงบประมาณ")]
    string BudgetTypeCode,
    [property: Description("รหัสประเภทงบประมาณ")]
    string BudgetTypeName,
    [property: Description("รหัสบัญชี GL")]
    string GLAccountCode,
    [property: Description("รหัสบัญชี GL")]
    string GLAccountName,
    [property: Description("เลขโครงการ")]
    string? ProjectNumber,
    [property: Description("จำนวนเงิน")]
    string Amount,
    [property: Description("จำนวนเงิน (ตัวอักษร)")]
    string AmountText);

public record VendorParcelReplace(
    [property: Description("รหัสรายการสินค้า")]
    Guid? Id,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("รายการสินค้า")]
    string Item,
    [property: Description("รายละเอียดรายการ")]
    string? ItemDetail,
    [property: Description("จำนวน")]
    int Quantity,
    [property: Description("รหัสหน่วย")]
    string UnitCode,
    [property: Description("หน่วย")]
    string UnitName,
    [property: Description("ราคาต่อหน่วย")]
    string UnitPrice,
    [property: Description("ราคาต่อหน่วย (ตัวอักษร)")]
    string UnitPriceText,
    [property: Description("ราคารวม")]
    string TotalPrice,
    [property: Description("ราคารวม (ตัวอักษร)")]
    string TotalPriceText,
    [property: Description("ราคารวมรวมภาษี")]
    string TotalPriceVat,
    [property: Description("ราคารวมรวมภาษี (ตัวอักษร)")]
    string TotalPriceVatText);

public record VendorReplace(
    [property: Description("รหัสผู้ขาย")]
    Guid? Id,
    [property: Description("ประเภทผู้ขาย")]
    string VendorType,
    [property: Description("รหัสผู้ขายในระบบ")]
    Guid? SuVendorId,
    [property: Description("ชื่อผู้ขาย")]
    string VendorName,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("เลขประจำตัวผู้เสียภาษี")]
    string? TaxNumber,
    [property: Description("เลขสาขาผู้ขาย")]
    string? VendorBranchNumber,
    [property: Description("รหัสประเภทภาษีมูลค่าเพิ่ม")]
    string? VatIncludeTypeCode,
    [property: Description("รหัสประเภทบิล")]
    string BillTypeCode,
    [property: Description("ประเภทเอกสาร อื่นๆ")]
    string BillType,
    [property: Description("ประเภทเอกสาร อื่นๆ (ตัวอักษร)")]
    string? BillTypeOther,
    [property: Description("เลขที่สมุดบิล")]
    string BillBookNo,
    [property: Description("วันที่บิล")]
    string BillDate,
    [property: Description("รายละเอียดบิล")]
    string? BillDetail,
    [property: Description("รายการสินค้าของผู้ขาย")]
    IEnumerable<VendorParcelReplace> VendorParcels);

public record Pw119ReplaceDto(
    [property: Description("รหัส PW119")] Guid Id,
    [property: Description("เลขที่ PW119")]
    string Pw119Number,
    [property: Description("วันที่บันทึก")]
    string? AcceptorDate,
    [property: Description("ตำแหน่งผู้มีอำนาจอนุมัติ")]
    IEnumerable<SectionApprove>? SectionApprovePositionName,
    [property: Description("หมายเลขโทรศัพท์")]
    string Telephone,
    [property: Description("สถานะ")] Pw119Status Status,
    [property: Description("วันที่ PW119")]
    DateTimeOffset Pw119Date,
    [property: Description("รหัสหน่วยงาน")]
    string DepartmentCode,
    [property: Description("รหัสหน่วยงาน")]
    string DepartmentName,
    [property: Description("ปีงบประมาณ")] int BudgetYear,
    [property: Description("รหัสวิธีการจัดหา")]
    string? SupplyMethodCode,
    [property: Description("วิธีการจัดหา")]
    string? SupplyMethodName,
    [property: Description("รหัสประเภทวิธีการจัดหาพิเศษ")]
    string? SupplyMethodSpecialTypeCode,
    [property: Description("ประเภทวิธีการจัดหาพิเศษ")]
    string? SupplyMethodSpecialType,
    [property: Description("หัวข้อ")] string Subject,
    [property: Description("แหล่งที่มา")] string Source,
    [property: Description("งบประมาณ")] string Budget,
    [property: Description("งบประมาณ (ตัวอักษร)")] string BudgetText,
    [property: Description("ราคากลาง")] string? MedianPrice,
    [property: Description("ราคากลาง (ตัวอักษร)")] string? MedianPriceText,
    [property: Description("รหัสหมวดหมู่ W119")]
    string W119CategoriesCode,
    [property: Description("หมวดหมู่ W119")]
    string W119CategoriesName,
    [property: Description("เหตุผล")] string? Reason,
    [property: Description("ข้อมูลการเบิกล่วงหน้า")]
    Pw119AdvanceResponseDto Advance,
    [property: Description("ผู้ขาย")] IEnumerable<VendorReplace> Vendors,
    [property: Description("บัญชี GL")] IEnumerable<GLAccountReplace> GLAccounts,
    [property: Description("ผู้อนุมัติ")] AcceptorReplace[] Acceptors,
    [property: Description("เอกสารแนบ")] AttachmentsDto[] Attachments,
    [property: Description("ข้อมูลผู้จัดทำเอกสาร")]
    CreatorReplace? Create,
    [property: Description("ข้อมูลผู้ประกาศผู้ชนะ")]
    PublisherDto? Publisher,
    [property: Description("จำนวนรายการพัสดุ")]
    string ParcelItemCount,
    [property: Description("ข้อมูลภาษี")]
    string VatDescription,
    [property: Description("ชื่อคู่ค้า")]
    string VendorsName,
    [property: Description("SolId รวม")]
    string GlAccountCodes,
    [property: Description("ลำดับค่าใช้จ่าย")]
    string? W119Categorie);

public class GetListMappingPw119DocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingPw119DocumentEndpoint(ILogger<GetListMappingPw119DocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Pw119"));
        this.Get("pw119/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(Pw119ReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}