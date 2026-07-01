namespace GHB.DP2.Application.Features.ContractAgreement.ContractInvitation;

using System.ComponentModel;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record ContractInvitationVendorReplaceDto(
    [property: Description("รหัสผู้ขายในหนังสือเชิญสัญญา")]
    ContractInvitationVendorsId? Id,
    [property: Description("รหัสสัญญาอนุมัติใบสั่งซื้อ")]
    PurchaseOrderApprovalContractId PurchaseOrderApprovalContractId,
    [property: Description("รหัสเอกสาร")] Guid? DocumentId,
    [property: Description("ชื่อผู้ขาย")] string VendorName,
    [property: Description("อีเมล")] string Email,
    [property: Description("ชื่อสัญญา")] string ContractName,
    [property: Description("เลขที่ใบสั่งซื้อ")]
    string PoNumber,
    [property: Description("เลขที่สัญญา")] string ContractNumber,
    [property: Description("ราคาที่ตกลงได้")]
    string AgreedPrice,
    [property: Description("ราคาที่ตกลงได้ (ตัวอักษร)")]
    string AgreedPriceText,
    [property: Description("มีหลักประกันสัญญา")]
    bool HasContractGuarantee,
    [property: Description("หลักประกันสัญญา")]
    string? ContractGuarantee,
    [property: Description("เปอร์เซ็นต์หลักประกันสัญญา")]
    string? ContractGuaranteePercent,
    [property: Description("จำนวนเงินหลักประกัน")]
    string? GuaranteeAmount,
    [property: Description("จำนวนเงินหลักประกัน (ตัวอักษร)")]
    string? GuaranteeAmountText,
    [property: Description("ชื่อเจ้าหน้าที่สัญญา")]
    string ContractOfficerName,
    [property: Description("โทรศัพท์เจ้าหน้าที่สัญญา")]
    string ContractOfficerPhone,
    [property: Description("อีเมลเจ้าหน้าที่สัญญา")]
    string ContractOfficerEmail,
    [property: Description("ผลการตรวจสอบ EGP")]
    bool EgpResult,
    [property: Description("หมายเหตุ EGP")]
    string? EgpRemark,
    [property: Description("วันที่ตรวจสอบ EGP")]
    DateTimeOffset? EgpDate,
    [property: Description("ข้อมูลผู้ประกอบการ")]
    VendorInfoReplaceDto? Entrepreneur,
    [property: Description("เดือนปีปัจจุบัน")]
    string MonthYearNow,
    [property: Description("วันที่ปัจจุบัน")]
    string DateNow,
    [property: Description("วิธีการจัดหาประเภทพิเศษ")]
    string? SupplyMethodSpecialType,
    [property: Description("ผู้อนุมัติ")]
    AcceptorReplaceDto? Acceptor,
    [property: Description("ชื่อผุ้ทำเอกสาร")]
    string? AssigneeName,
    [property: Description("จำนวนเงินอากรแสตมป์")]
    string RevenueStampAmount,
    [property: Description("จำนวนเงินอากรแสตมป์ (ตัวอักษร)")]
    string RevenueStampAmountText);

public record AcceptorReplaceDto(
    [property: Description("ลายเซ็นต์ผู้อนุมัติ")]
    string? Signature,
    [property: Description("ชื่อผู้อนุมัติ")]
    string? FullName,
    [property: Description("ตำแหน่งผู้อนุมัติ")]
    string? FullPositionName);

public record VendorInfoReplaceDto(
    [property: Description("รหัสผู้ขาย")] SuVendorId Id,
    [property: Description("สัญชาติผู้ขาย")]
    SuVendorNationality Nationality,
    [property: Description("ประเภทผู้ขาย")]
    SuVendorType Type,
    [property: Description("รหัสประเภทผู้ประกอบการ")]
    ParameterCode EntrepreneurType,
    [property: Description("ชื่อประเภทผู้ประกอบการ")]
    string EntrepreneurTypeName,
    [property: Description("เลขที่ผู้เสียภาษี")]
    string TaxpayerIdentificationNo,
    [property: Description("ชื่อสถานประกอบการ")]
    string EstablishmentName,
    [property: Description("หมายเลขโทรศัพท์ผู้ประกอบการ")] string? Tel,
    [property: Description("หมายเลขโทรสารผู้ประกอบการ")] string? Fax,
    [property: Description("หมายเลขผู้ขายในระบบ SAP")]
    string SapVendorNumber,
    [property: Description("หมายเลขสาขาในระบบ SAP")]
    string SapBranchNumber,
    [property: Description("อีเมล")] string Email);

public class GetListMappingContractInvitationDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingContractInvitationDocumentEndpoint(ILogger<GetListMappingContractInvitationDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("ContractAgreement/ContractInvitation"));
        this.Get("procurement/contractInvitation/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(ContractInvitationVendorReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}