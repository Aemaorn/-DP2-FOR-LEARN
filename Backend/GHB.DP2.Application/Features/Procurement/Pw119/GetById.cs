namespace GHB.DP2.Application.Features.Procurement.Pw119;

using System.ComponentModel;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Pw119.Abstract;
using GHB.DP2.Domain.Procurement.Pw119;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record GetPw119DetailRequest(
    Guid Id);

public record DocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);

public record GetPw119Response(
    [property: Description("รหัส PW119")] Guid Id,
    [property: Description("เลขที่ PW119")]
    string Pw119Number,
    [property: Description("สถานะ")] Pw119Status Status,
    [property: Description("รหัสเอกสารขออนุมัติ")]
    Guid? ApprovalRequestDocumentId,
    [property: Description("เอกสารขออนุมัติ เปลี่ยนแปลง")]
    bool? IsApprovalRequestDocumentReplace,
    [property: Description("ประวัติเอกสารขออนุมัติ")]
    DocumentVersionResponse[] ApprovalRequestDocumentVersions,
    [property: Description("รหัสเอกสารประกาศผู้ชนะ")]
    Guid? WinnerAnnounceDocumentId,
    [property: Description("เอกสารประกาศผู้ชนะ เปลี่ยนแปลง")]
    bool? IsWinnerAnnounceDocumentReplace,
    [property: Description("ประวัติเอกสารประกาศผู้ชนะ")]
    DocumentVersionResponse[] WinnerAnnounceDocumentVersions,
    [property: Description("วันที่ PW119")]
    DateTimeOffset Pw119Date,
    [property: Description("รหัสหน่วยงาน")]
    string DepartmentCode,
    string? DepartmentOrganizationLevel,
    [property: Description("ปีงบประมาณ")] int BudgetYear,
    [property: Description("รหัสวิธีการจัดหา")]
    string SupplyMethodCode,
    [property: Description("รหัสประเภทวิธีการจัดหาพิเศษ")]
    string? SupplyMethodSpecialTypeCode,
    string? AssignSegmentCode,
    [property: Description("หัวข้อ")] string Subject,
    [property: Description("แหล่งที่มา")] string Source,
    [property: Description("งบประมาณ")] decimal Budget,
    [property: Description("ราคากลาง")] decimal? MedianPrice,
    [property: Description("รหัสหมวดหมู่ W119")]
    string W119CategoriesCode,
    [property: Description("เหตุผล")] string? Reason,
    [property: Description("เบอร์โทร")] string? Telephone,
    [property: Description("ข้อมูลการเบิกล่วงหน้า")]
    Pw119AdvanceResponseDto Advance,
    [property: Description("ผู้ขาย")] IEnumerable<VendorResponseDto> Vendors,
    [property: Description("บัญชี GL")] IEnumerable<GLAccountResponseDto> GLAccounts,
    [property: Description("ผู้อนุมัติ")] AcceptorResponse[] Acceptors,
    [property: Description("เอกสารแนบ")] AttachmentsDto[] Attachments,
    AcceptorResponse[] AcceptanceConfirmers,
    DateTimeOffset? DisbursementDate,
    decimal? DisbursementAmount,
    string? DisbursementDescription);

public class GetPw119Detail : Pw119EndpointBase<GetPw119DetailRequest, Results<Ok<GetPw119Response>, NotFound<string>>>
{
    private readonly IOperationService operationService;

    public GetPw119Detail(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IOperationService operationService,
        ILogger<GetPw119Detail> logger)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Pw119")
             .WithName("GetPw119Detail")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Get("pw119/{Id:guid}");
    }

    protected override async ValueTask<Results<Ok<GetPw119Response>, NotFound<string>>> HandleRequestAsync(GetPw119DetailRequest req, CancellationToken ct)
    {
        var data = await this.GetPw119ById(Pw119Id.From(req.Id), ct);

        var response = this.MapToResponse(data);

        return TypedResults.Ok(response);
    }
}