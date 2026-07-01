namespace GHB.DP2.Application.Features.Report.AuditAndRevenue;

using System.ComponentModel;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Report.RpAuditAndRevenue;
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
    string Signature,
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

public record AuthorityReplace(
    [property: Description("เห็นชอบหรืออนุมัติ")]
    string? Signature,
    [property: Description("ชื่อคณะกรรมการหรือผู้จัดทำ")]
    string? FullName,
    [property: Description("ตำแหน่งผู้ผู้เห็นชอบ หรืออนุมัติ")]
    string? FullPositionName);

public record AuditAndRevenueDetailReplaceDto(
    Guid Id,
    Guid CaContractDraftVendorId,
    string ContractTypeCode,
    string ContractTypeName,
    string ContractNumber,
    string ContractName,
    string? ContractSignedDate,
    string EntrepreneurName,
    string Budget,
    string? BudgetText,
    string? Description,
    bool Overdue,
    int Sequence,
    string? Remark);

public record GetAuditAndRevenueReplaceDto(
    Guid Id,
    string? AcceptorDate,
    string DocumentNumber,
    string DocumentDate,
    string SignStartDate,
    string SignEndDate,
    RpAuditAndRevenueStatus Status,
    AuthorityReplace? Publisher,
    CreatorReplace? Creator,
    int ContractDraftCount,
    IEnumerable<AuditAndRevenueDetailReplaceDto> Details,
    IEnumerable<AcceptorReplace>? Acceptors,
    bool HasPermission,
    [property: Description("ตำแหน่งผู้มีอำนาจอนุมัติ")]
    IEnumerable<SectionApprove>? SectionApproveName);

public class GetListMappingDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingDocumentEndpoint(ILogger<GetListMappingDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Report/AuditAndRevenue"));
        this.Get("contract-amendment/audit-revenue/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(GetAuditAndRevenueReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}