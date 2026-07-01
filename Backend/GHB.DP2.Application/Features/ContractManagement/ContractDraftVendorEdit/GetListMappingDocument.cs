namespace GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit;

using System.ComponentModel;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

// ── Main Replace DTO ──
public record ContractDraftVendorEditReplaceDto(
    string ContractName,
    string EditContractNumber,
    string ContractDraftNumber,
    string ContractDraftSignedDateFormat,
    string ContractDraftBudget,
    string ContractDraftBudgetText,
    string? Title,
    string? Description,
    string? AcceptorDate,
    AcceptorSignDto? AcceptorSign,
    [property: Description("ข้อมูลผู้จัดทำเอกสาร")]
    ContractDraftVendorEditCreatorReplace? Creator,
    [property: Description("รายชื่อผู้เห็นชอบ/อนุมัติ")]
    IEnumerable<ContractDraftVendorEditAcceptorReplace>? Acceptors,
    [property: Description("รายชื่อคณะกรรมการ")]
    IEnumerable<ContractDraftVendorEditCommitteeReplace>? Committee,
    [property: Description("รายชื่อผู้รับมอบหมาย")]
    IEnumerable<ContractDraftVendorEditAssigneeReplace>? Assignees,
    JorPorCommentDto? JorPorComment,
    ContractDraftInfoReplaceDto ContractDraftInfoDetail,
    NewContractDraftEditReplaceDto New);

public record JorPorCommentDto(
    string? Action,
    string? FullName,
    string? PositionName,
    string? Remark);

public record NewContractDraftEditReplaceDto(
    ContractDraftInfoReplaceDto ContractDraftInfoDetail);

// ── Sub DTOs ──
public record ContractDraftVendorEditBuyerReplace(
    [property: Description("ชื่อผู้ซื้อ")] string? Name,
    [property: Description("ที่อยู่ผู้ซื้อ")]
    string? Address);

public record ContractDraftVendorEditVendorReplace(
    [property: Description("ชื่อผู้ขาย")] string? Name,
    [property: Description("เลขที่ผู้เสียภาษี")]
    string? TaxId,
    [property: Description("ที่อยู่ผู้ขาย")]
    string? Address);

public record ContractDraftVendorEditCreatorReplace(
    [property: Description("ลายเซ็นต์")] string? Action,
    [property: Description("ลายเซ็นต์")] string? Signature,
    [property: Description("ชื่อ-สกุล")] string? FullName,
    [property: Description("ตำแหน่ง")] string? FullPositionName);

public record ContractDraftVendorEditAcceptorReplace(
    [property: Description("เห็นชอบ/อนุมัติ")]
    string Action,
    [property: Description("ชื่อผู้เห็นชอบ/อนุมัติ")]
    string FullName,
    [property: Description("ตำแหน่ง")] string PositionName);

public record ContractDraftVendorEditCommitteeReplace(
    [property: Description("เห็นชอบ/อนุมัติ")]
    string Action,
    [property: Description("ชื่อคณะกรรมการ")]
    string FullName,
    [property: Description("ตำแหน่ง")] string PositionName);

public record ContractDraftVendorEditAssigneeReplace(
    [property: Description("ชื่อผู้รับมอบหมาย")]
    string FullName,
    [property: Description("ตำแหน่ง")] string PositionName,
    [property: Description("ความเห็น")] string? Remark);

// ── GetListMappingDocument Endpoint ──
public class GetListMappingContractDraftVendorEditDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingContractDraftVendorEditDocumentEndpoint(
        ILogger<GetListMappingContractDraftVendorEditDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("ContractManagement/ContractDraftVendorEdit"));
        this.Get("contract/contract-draft-vendor-edit/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(ContractDraftVendorEditReplaceDto);
        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}