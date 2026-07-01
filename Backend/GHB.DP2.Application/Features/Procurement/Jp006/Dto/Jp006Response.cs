namespace GHB.DP2.Application.Features.Procurement.Jp006.Dto;

using System.ComponentModel;
using GHB.DP2.Application.Contracts;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Application.Features.Procurement.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;
using global::GHB.DP2.Application.Dtos;

public record ProcurementSuppliesDivisionDto(
    Guid Id,
    Guid UserId,
    string FullName,
    string FullPositionName,
    int Sequence);

public record GetJp006ByIdResponse(
    [property: Description("รหัส จพ.006")] Guid? Jp006Id,
    [property: Description("รหัสการจัดซื้อจัดจ้าง")]
    Guid ProcurementId,
    [property: Description("ข้อมูลการจัดซื้อจัดจ้าง")]
    ProcurementDto? Procurement,
    [property: Description("สถานะใบสั่งซื้อ")]
    PurchaseOrderStatus Status,
    [property: Description("รหัสเอกสาร จพ.006")]
    Guid? Jp006DocumentId,
    [property: Description("รหัสเอกสาร จพ.006 เปบี่ยนแปลง")]
    bool? IsJp006DocumentIdReplaced,
    [property: Description("รหัสเอกสารผู้ชนะ")]
    Guid? WinnerDocumentId,
    [property: Description("รหัสเอกสารผู้ชนะ เปบี่ยนแปลง")]
    bool? IsWinnerDocumentIdReplaced,
    [property: Description("ผู้ประกอบการ")]
    IEnumerable<Jp006EntrepreneurResponse> Entrepreneurs,
    [property: Description("ผู้อนุมัติ")] IEnumerable<Jp006AcceptorResponseInfo> Acceptors,
    [property: Description("ผู้รับมอบหมาย")]
    IEnumerable<AssigneeResponse> Assignees,
    [property: Description("ราคากลาง")] decimal? MedianPrice,
    [property: Description("เป็นการจัดซื้อขนาดใหญ่")]
    bool IsBp,
    [property: Description("รหัสผู้มีสิทธิ์จัดการข้อมูล")]
    IEnumerable<Operators>? Operators,
    List<ProcurementSuppliesDivisionDto> ProcurementSuppliesDivision,
    [property: Description("ประวัติเวอร์ชันเอกสาร จพ.006")]
    Jp006DocumentVersionResponse[] Jp006DocumentVersions,
    [property: Description("ประวัติเวอร์ชันเอกสารผู้ชนะ")]
    Jp006DocumentVersionResponse[] WinnerDocumentVersions,
    string? PurchaseOrderNumber,
    IEnumerable<Jp006PriceDetailsResponse>? PriceDetails,
    [property: Description("วันที่เอกสาร")]
    DateTimeOffset? DocumentDate = null,
    [property: Description("วันที่แก้ไขล่าสุด")]
    DateTimeOffset? LastModifiedAt = null);

public record Operators(
    Guid UserId,
    int Sequence);

public record Jp006EntrepreneurResponse(
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
    IEnumerable<Jp006PriceDetailsResponse> PriceDetails,
    [property: Description("เข้าร่วมการเสนอราคา")]
    bool IsBidding,
    [property: Description("ผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
    QualificationResultDto? CoiCheckerResult,
    [property: Description("ผลการตรวจสอบ Watchlist")]
    QualificationResultDto? WatchlistCheckerResult,
    [property: Description("ผู้ถือหุ้น")] PurchaseOrderEntrepreneurShareholderDto[]? Shareholder,
    [property: Description("เอกสารแนบ")] EntrepreneurResponseAttachment[] Attachments,
    [property: Description("รหัสสาขา SAP")] string? SapBranchNumber = null);

public record EntrepreneurFileWithId(
    [property: Description("รหัสข้อมูล")] Guid? Id,
    [property: Description("รหัสไฟล์")] Guid FileId,
    [property: Description("ชื่อไฟล์")] string FileName,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("เปิดเผยสาธารณะ")]
    bool IsPublic,
    [property: Description("ผู้อัปโหลด")] Guid CreatedBy,
    EntrepreneurAttachmentType Type) : IHasSequenceFileAttachment;

public record EntrepreneurResponseAttachment(
    string DocumentTypeCode,
    EntrepreneurFileWithId[] FileAttachments
);

public record PurchaseOrderEntrepreneurShareholderDto(
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
    [property: Description("เป็นนิติบุคคล")]
    bool? IsJuristic,
    [property: Description("ประเภทการตรวจสอบ")]
    string? CheckType,
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
    DateTimeOffset? EgpResultAt,
    [property: Description("ผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
    QualificationResultDto? CoiCheckerResult,
    [property: Description("ผลการตรวจสอบ Watchlist")]
    QualificationResultDto? WatchlistCheckerResult
);

public record Jp006PriceDetailsResponse(
    [property: Description("รหัสรายละเอียดราคา")]
    Guid PriceDetailsId,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("ชื่อพัสดุ")] string ParcelName,
    [property: Description("จำนวนพัสดุ")] int ParcelQuantity,
    [property: Description("รหัสหน่วยนับพัสดุ")]
    string ParcelUnitCode,
    [property: Description("รหัสประเภทภาษีมูลค่าเพิ่ม")]
    string? VatTypeCode,
    [property: Description("ราคาที่เสนอ")] decimal OfferedPrice,
    [property: Description("ราคาที่ตกลง")] decimal AgreedPrice,
    [property: Description("รายละเอียด")] string Description);

public record Jp006DocumentVersionResponse(
    [property: Description("รหัสไฟล์")] Guid FileId,
    [property: Description("เวอร์ชัน")] string Version,
    [property: Description("วันที่สร้าง")] DateTimeOffset CreatedAt,
    [property: Description("ชื่อผู้สร้าง")] string CreatedByName,
    [property: Description("เป็นเวอร์ชันปัจจุบัน")] bool IsCurrent);