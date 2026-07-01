namespace GHB.DP2.Application.Features.Procurement.P79Clause2;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.P79Clause2.Abstract;
using GHB.DP2.Domain.Procurement.P79Clause2;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

public record GetP79Clause2DetailRequest(
    Guid Id);

public record DocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);

public record GetP79Clause2Response(
    [property: Description("รหัส P79 ข้อ 2")]
    Guid Id,
    [property: Description("เลขที่ P79 ข้อ 2")]
    string P79Clause2Number,
    [property: Description("สถานะ")] P79Clause2Status Status,
    [property: Description("รหัสเอกสารขออนุมัติ")]
    Guid? ApprovalRequestDocumentId,
    [property: Description("รหัสเอกสารขออนุมัติ เปลี่ยนแปลง")]
    bool? IsApprovalRequestDocumentReplace,
    [property: Description("ประวัติเอกสารขออนุมัติ")]
    DocumentVersionResponse[] ApprovalRequestDocumentVersions,
    [property: Description("รหัสเอกสารประกาศผู้ชนะ")]
    Guid? WinnerAnnounceDocumentId,
    [property: Description("รหัสเอกสารประกาศผู้ชนะ เปลี่ยนแปลง")]
    bool? IsWinnerAnnounceDocumentReplace,
    [property: Description("ประวัติเอกสารประกาศผู้ชนะ")]
    DocumentVersionResponse[] WinnerAnnounceDocumentVersions,
    [property: Description("วันที่ P79 ข้อ 2")]
    DateTimeOffset P79Clause2Date,
    [property: Description("รหัสหน่วยงาน")]
    string DepartmentCode,
    string? DepartmentOrganizationLevel,
    [property: Description("ปีงบประมาณ")] int BudgetYear,
    [property: Description("รหัสวิธีจัดซื้อจัดจ้าง")]
    string SupplyMethodCode,
    [property: Description("รหัสประเภทวิธีจัดซื้อจัดจ้าง")]
    string SupplyMethodTypeCode,
    [property: Description("รหัสประเภทพิเศษวิธีจัดซื้อจัดจ้าง")]
    string? SupplyMethodSpecialTypeCode,
    string? AssignSegmentCode,
    [property: Description("เรื่อง")] string Subject,
    [property: Description("เบอร์โทร")] string? Telephone,
    [property: Description("แหล่งที่มา")] string Source,
    [property: Description("งบประมาณ")] decimal Budget,
    [property: Description("ราคากลาง")] decimal? MedianPrice,
    [property: Description("เหตุผลข้อ 1")] string? ReasonItem1,
    [property: Description("เหตุผลข้อ 2")] string? ReasonItem2,
    [property: Description("เหตุผลข้อ3")] string? ReasonItem3,
    [property: Description("เป็นการจ่ายล่วงหน้า")]
    bool IsAdvance,
    [property: Description("ข้อมูลการจ่ายล่วงหน้า")]
    P79Clause2AdvanceResponseDto Advance,
    [property: Description("ผู้ขาย")] IEnumerable<VendorResponseDto> Vendors,
    [property: Description("บัญชี GL")] IEnumerable<GLAccountResponseDto> GLAccounts,
    [property: Description("ผู้อนุมัติ")] AcceptorResponse[] Acceptors,
    [property: Description("ไฟล์แนบ")] AttachmentsDto[] Attachments,
    [property: Description("รหัสผู้สร้างข้อมูล")] Guid CreatedBy,
    DateTimeOffset? DeliveryDate,
    string? ProcurementReasonItem1,
    string? ProcurementReasonItem2,
    DateTimeOffset? DisbursementDate,
    decimal? DisbursementAmount,
    string? DisbursementDescription,
    AcceptorResponse[] AcceptanceConfirmers);

public class GetP79Clause2Detail : P79Clause2EndpointBase<GetP79Clause2DetailRequest, Results<Ok<GetP79Clause2Response>, NotFound<string>>>
{
    public GetP79Clause2Detail(Dp2DbContext dbContext, ILogger<GetP79Clause2Detail> logger, IOperationService operationService, IFileServiceClient fileServiceClient)
        : base(logger, dbContext, operationService, fileServiceClient)
    {
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("P79Clause2")
             .WithName("GetP79Clause2Detail")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Get("p79Clause2/{Id:guid}");
    }

    protected override async ValueTask<Results<Ok<GetP79Clause2Response>, NotFound<string>>> HandleRequestAsync(GetP79Clause2DetailRequest req, CancellationToken ct)
    {
        var data = await this.GetP79Clause2ById(P79Clause2Id.From(req.Id), ct);

        var response = this.MapToResponse(data);

        return TypedResults.Ok(response);
    }
}