namespace GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Dto;

using System.ComponentModel;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Dto;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.SystemUtility;

// ── Component DTO ──
public record ContractDraftVendorEditComponentDto(
    [property: Description("รหัส Component")]
    Guid? Id,
    [property: Description("รหัส Section")]
    string ComponentCode,
    [property: Description("ชื่อ Section")]
    string ComponentName,
    [property: Description("ได้แก้ไขแล้ว")]
    bool IsEdited);

// ── Attachment DTO ──
public record ContractDraftVendorEditAttachmentDto(
    Guid? Id,
    string? TypeCode,
    string? Description,
    int? PageNumber,
    int Sequence,
    string? FormatOtherName,
    ContractDraftVendorEditAttachmentFileDto[] Files);

public record ContractDraftVendorEditAttachmentFileDto(
    Guid? Id,
    Guid FileId,
    string FileName,
    string? FileType,
    int Sequence);

// ── Shareholder DTO ──
public record ContractDraftVendorEditShareholderDto(
    Guid? Id,
    int Sequence,
    string? TaxId,
    string? FirstName,
    string? LastName,
    bool IsDirector,
    bool? IsShareholder);

// ── Checker Attachment DTO ──
public record ContractDraftVendorEditCheckerAttachmentDto(
    Guid? Id,
    string? DocumentTypeCode,
    Guid FileId,
    string FileName,
    bool IsPublic,
    int Sequence,
    string? Type);

// ── Main Detail Response (grouped by concern, mirrors ContractDraftDetailBase) ──
public record ContractDraftVendorEditDetailResponse(

    // ─── Identity ───
    [property: Description("รหัสแก้ไขร่างสัญญา")]
    Guid Id,
    [property: Description("รหัส ContractDraftVendor ต้นฉบับ")]
    Guid ContractDraftVendorId,
    [property: Description("รหัส Procurement")]
    Guid ProcurementId,
    [property: Description("สถานะ")] ContractDraftVendorEditStatus Status,

    // ─── Contract Info (read-only header) ───
    string Email,
    string? Title,
    string? Description,
    string ContractName,
    string PoNumber,
    string ContractDraftNumber,
    string ContractNumber,
    decimal Budget,
    DateTimeOffset? ContractSignedDate,
    DateTimeOffset? ContractEndDate,
    ParameterCode? SupplyMethodCode,
    ParameterCode? SupplyMethodSpecialTypeCode,
    string? ContractTypeCode,
    string? ContractTypeLabel,
    string? TemplateCode,
    string? TemplateLabel,
    string? TemplateText,
    string? SubTemplateCode,
    string? SubTemplateText,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    bool IsWorkingDayOnly,
    DateTimeOffset? VendorAppointmentMemoDate,
    string? PeriodConditionTypeCode,

    // ─── Section Data (editable, mirrors ContractDraftDetailBase) ───
    BuyerInfo? Buyer,
    VendorInfo? Vendor,
    AgreementBase? Agreement,
    PaymentBase? Payment,
    DeliveryInfo? Delivery,
    TerminationInfo? Termination,
    string? DefectWarrantyTypeCode,
    WarrantyInfo? Warranty,
    PenaltyInfo? Penalty,
    GuaranteeInfo? Guarantee,
    AdvancePayment? AdvancePayment,
    RetentionPayment? RetentionPayment,
    RedeliveryBase? Redelivery,
    CopierLeaseInfo? CopierLease,
    ComputerLeaseInfo? ComputerLease,
    CarLeaseInfo? CarLease,

    // ─── Qualification Check ───
    bool? EgpResult,
    string? EgpRemark,
    DateTimeOffset? EgpDate,
    bool? CoiResult,
    string? CoiRemark,
    DateTimeOffset? CoiDate,
    bool? WatchlistResult,
    string? WatchlistRemark,
    DateTimeOffset? WatchlistDate,
    ContractStatus ContractStatus,

    // ─── Participants ───
    List<AssigneeResponse> Assignees,
    List<AcceptorNoIdResponse> Acceptors,

    // ─── Collections ───
    ContractDraftVendorEditComponentDto[] Components,
    ContractDraftVendorEditAttachmentDto[] Attachments,
    ContractDraftVendorEditShareholderDto[] Shareholders,

    // ─── Old Data (section-level only, for comparison) ───
    ContractDraftVendorEditOldDataDto? OldData,

    // ─── Documents ───
    [property: Description("fileId ของเอกสารแก้ไขสัญญา (Amendment)")]
    Guid? AmendmentDocumentId,
    [property: Description("เอกสารแก้ไขสัญญาถูก replace หรือไม่")]
    bool? IsAmendmentDocumentIdReplaced,
    [property: Description("fileId ของเอกสารบันทึกขออนุมัติแก้ไข")]
    Guid? AmendmentApprovalRequestDocumentId,
    [property: Description("เอกสารบันทึกขออนุมัติแก้ไขถูก replace หรือไม่")]
    bool? IsAmendmentApprovalRequestDocumentIdReplaced,
    [property: Description("version history ของเอกสารแก้ไขสัญญา")]
    ContractDraftVendorEditDocumentVersionResponse[]? AmendmentDocumentVersions,
    [property: Description("version history ของเอกสารบันทึกขออนุมัติแก้ไข")]
    ContractDraftVendorEditDocumentVersionResponse[]? ApprovalRequestDocumentVersions,

    // ─── Flags ───
    [property: Description("user ปัจจุบันเป็น Assignee ใน PurchaseOrderApproval หรือไม่")]
    bool IsPurchaseOrderApprovalAssignee = false,

    // ─── General File Attachments ───
    AttachmentsDto[]? FileAttachments = null,

    [property: Description("วันที่เอกสาร")]
    DateTimeOffset? DocumentDate = null);

// ── Old Data DTO (only section-level data from original ContractDraftVendor, mirrors ContractDraftDetailBase) ──
public record ContractDraftVendorEditOldDataDto(
    string? PoNumber,
    decimal? Budget,
    DateTimeOffset? ContractSignedDate,
    DateTimeOffset? ContractEndDate,
    BuyerInfo? Buyer,
    VendorInfo? Vendor,
    AgreementBase? Agreement,
    PaymentBase? Payment,
    DeliveryInfo? Delivery,
    TerminationInfo? Termination,
    string? DefectWarrantyTypeCode,
    WarrantyInfo? Warranty,
    PenaltyInfo? Penalty,
    GuaranteeInfo? Guarantee,
    AdvancePayment? AdvancePayment,
    RetentionPayment? RetentionPayment,
    RedeliveryBase? Redelivery,
    CopierLeaseInfo? CopierLease,
    ComputerLeaseInfo? ComputerLease,
    CarLeaseInfo? CarLease);

// ── Dialog Response ──
public record ContractDraftVendorDialogItemResponse(
    [property: Description("รหัส ContractDraftVendor")]
    ContractDraftVendorId Id,
    [property: Description("เลขร่างสัญญา")]
    string ContractDraftNumber,
    [property: Description("เลขสัญญา")] string ContractNumber,
    [property: Description("เลข PO")] string PoNumber,
    [property: Description("ชื่อสัญญา")] string ContractName,
    [property: Description("ชื่อผู้ค้า")] string EntrepreneurName,
    [property: Description("เลขผู้เสียภาษี")]
    string TaxId,
    [property: Description("วงเงิน")] decimal Budget,
    [property: Description("วันลงนามสัญญา")]
    DateTimeOffset? ContractSignedDate,
    [property: Description("วันสิ้นสุดสัญญา")]
    DateTimeOffset? ContractEndDate,
    [property: Description("รหัส Template")]
    string? TemplateCode,
    [property: Description("วิธีจัดซื้อจัดจ้าง")]
    string? SupplyMethodName,
    [property: Description("หน่วยงาน")] string? DepartmentName);

// ── List Response ──
public record ContractDraftVendorEditListItemResponse(
    [property: Description("รหัสแก้ไขร่างสัญญา")]
    ContractDraftVendorEditId Id,
    [property: Description("รหัส ContractDraftVendor ต้นฉบับ")]
    ContractDraftVendorId ContractDraftVendorId,
    [property: Description("เลขสัญญา")] string ContractNumber,
    [property: Description("เลข PO")] string PoNumber,
    [property: Description("วันลงนามสัญญา")]
    DateTimeOffset? ContractSignedDate,
    [property: Description("ชื่อสัญญา")] string ContractName,
    [property: Description("วงเงิน")] decimal Budget,
    [property: Description("ประเภทสัญญา")] string? ContractTypeLabel,
    [property: Description("สถานะ")] ContractDraftVendorEditStatus Status,
    [property: Description("หน่วยงาน")] string DepartmentName,
    [property: Description("วิธีการจัดหา")] string? SupplyMethodName);

// ── Document Version Response ──
public record ContractDraftVendorEditDocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);