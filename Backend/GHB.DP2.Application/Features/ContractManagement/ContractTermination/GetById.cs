namespace GHB.DP2.Application.Features.ContractManagement.ContractTermination;

using System.ComponentModel;
using System.Linq;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.ContractTermination.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record GetContractTerminationRequest(
    Guid ContractVendorId,
    Guid Id);

public class GetContractTerminationByIdEndpoint
    : ContractTerminationEndpoint<GetContractTerminationRequest, Results<Ok<ContractVendorTerminalResponse>, NotFound<string>>>
{
    public GetContractTerminationByIdEndpoint(
        ILogger<GetContractTerminationByIdEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Get("contract/{contractVendorId:guid}/contract-termination/{id:guid}");
        this.Description(b => b
                              .WithTags("ContractManagement/ContractTermination")
                              .WithName("GetContractTerminationById")
                              .Produces<ContractVendorTerminalResponse>(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<ContractVendorTerminalResponse>, NotFound<string>>> HandleRequestAsync(
        GetContractTerminationRequest req, CancellationToken ct)
    {
        var entity = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractVendorId), ct);

        var termination = entity.CmContractTerminations.FirstOrDefault(s => s.Id == CmContractTerminationId.From(req.Id));

        var delivery = entity.Delivery;

        var suVendor = this.MapSuVendorByType(entity.ContractInvitationVendors, entity.ContractDraft.Procurement.Type);

        if (suVendor is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลผู้ประกอบการ");
        }

        if (termination is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลการบอกเลิกสัญญารหัส {req.Id}");
        }

        var documentId = termination.DocumentHistories
                                    .Where(d => d.DocumentType == CmContractTerminationDocumentType.ContractTermination)
                                    .OrderVersions()
                                    .Select(r => r.FileId.Value)
                                    .FirstOrDefault();

        var documentVersions = termination.DocumentHistories
                                          .Where(d => d.DocumentType == CmContractTerminationDocumentType.ContractTermination)
                                          .OrderVersions()
                                          .Select((d, idx) => new ContractTerminationDocumentVersionResponse(
                                              d.FileId.Value,
                                              d.Version,
                                              d.CreatedAt,
                                              d.CreatedByName,
                                              idx == 0))
                                          .ToArray();

        var acceptorsApprover = termination.Acceptors
                                           .Where(x => x.Type != AcceptorType.AcceptanceCommittee)
                                           .Select(DelegatorExtensions.DelegatorToAcceptor)
                                           .ToList();

        var committees = termination.Acceptors
                                    .Where(x => x.Type == AcceptorType.AcceptanceCommittee)
                                    .ToList();
        var acceptors =
            acceptorsApprover
                .Union(committees)
                .ToArray();

        var terminationDto = new ContractTerminationDto(
            termination.Id.Value,
            termination.TerminateDate,
            termination.TerminateType,
            termination.TerminateReasonOther,
            termination.TerminateReason,
            termination.TerminateReasonDetail,
            termination.IsProposedApprover,
            termination.Status,
            documentId,
            false,
            [
                .. termination.Assignees
                              .OrderBy(a => a.Sequence)
                              .Select(DelegatorExtensions.DelegatorToAssignee)
                              .Select(a => new AssigneeResponse(
                                  a.Id.Value,
                                  a.Group,
                                  a.Type,
                                  a.UserId.Value,
                                  a.Sequence,
                                  a.FullName,
                                  a.PositionName,
                                  a.BusinessUnitName,
                                  a.Status,
                                  a.Remark,
                                  a.ActionAt,
                                  a.Delegatee?.SuUserId.Value))
            ],
            [
                .. acceptors.OrderBy(a => a.Sequence)
                            .Select(a => new AcceptorResponse(
                                a.Id.Value,
                                a.Type,
                                a.UserId.Value,
                                a.Sequence,
                                a.FullName,
                                a.PositionName,
                                a.BusinessUnitName,
                                a.Status,
                                a.Remark,
                                a.ActionAt,
                                (string?)a.CommitteePositionsCode ?? string.Empty,
                                a.CommitteePosition?.Label ?? string.Empty,
                                a.IsUnableToPerformDuties,
                                IsCurrent: a.IsCurrentApprover(),
                                DelegateeUserId: a.Delegatee?.SuUserId.Value))
            ],
            documentVersions,
            termination.Attachments
                       .GroupBy(a => a.DocumentTypeCode.Value)
                       .Select(g => new AttachmentsDtoWithId(
                           g.Key,
                           g.OrderBy(a => a.Sequence)
                            .Select(a => new FileAttachmentsWithId(
                                a.Id.Value,
                                a.FileId.Value,
                                a.FileName,
                                a.Sequence,
                                a.IsPublic,
                                a.AuditInfo.CreatedBy))
                            .ToArray()))
                       .ToArray());

        var response = new ContractVendorTerminalResponse(
            entity.Id,
            suVendor.TaxpayerIdentificationNo,
            suVendor.EstablishmentName,
            suVendor.Email,
            entity.ContractNumber,
            entity.PoNumber,
            entity.Budget,
            entity.ContractName,
            entity.ContractType?.Label,
            entity.Template?.Label,
            entity.ContractSignedDate,
            delivery?.LeadTime,
            delivery?.LeadTimeTypeCode,
            delivery?.LeadTimeType?.Label,
            delivery?.Date,
            entity.ContractDraft?.Procurement?.SupplyMethodCode,
            terminationDto);

        return TypedResults.Ok(response);
    }
}

public record ContractVendorTerminalResponse(
    [property: Description("รหัสคู่ค้าร่างสัญญา")]
    ContractDraftVendorId Id,
    [property: Description("เลขที่ผู้เสียภาษี")]
    string TaxId,
    [property: Description("ชื่อผู้ประกอบการ")]
    string EntrepreneurName,
    [property: Description("อีเมลผู้ประกอบการ")]
    string EntrepreneurEmail,
    [property: Description("เลขที่สัญญา")] string ContractNumber,
    [property: Description("เลขที่ใบสั่งซื้อ")]
    string PoNumber,
    [property: Description("งบประมาณ")] decimal Budget,
    [property: Description("ชื่อสัญญา")] string ContractName,
    [property: Description("ประเภทสัญญา")] string? ContractType,
    [property: Description("แม่แบบสัญญา")] string? ContractTemplate,
    [property: Description("วันที่ลงนามสัญญา")]
    DateTimeOffset? ContractSignedDate,
    [property: Description("ระยะเวลาการส่งมอบ (วัน)")]
    int? DeliveryLeadTime,
    [property: Description("รหัสประเภทระยะเวลาการส่งมอบ")]
    ParameterCode? DeliveryLeadTimeTypeCode,
    [property: Description("ชื่อประเภทระยะเวลาการส่งมอบ")]
    string? DeliveryLeadTimeTypeLabel,
    [property: Description("วันที่กำหนดส่งมอบ")]
    DateTimeOffset? DeliveryDate,
    [property: Description("รหัสวิธีจัดซื้อจัดจ้าง")]
    ParameterCode? SupplyMethodCode,
    [property: Description("ข้อมูลการบอกเลิกสัญญา")]
    ContractTerminationDto ContractTermination);

public record ContractTerminationDocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);

public record ContractTerminationDto(
    [property: Description("รหัสการบอกเลิกสัญญา")]
    Guid Id,
    [property: Description("วันที่บอกเลิกสัญญา")]
    DateTimeOffset? TerminationDate,
    [property: Description("สาเหตุการยกเลิกสัญญา")]
    ParameterCode? TerminateType,
    [property: Description("สาเหตุการยกเลิกอื่น ๆ")]
    string? TerminateReasonOther,
    [property: Description("หมายเหตุการยกเลิกสัญญา")]
    string? TerminateReason,
    [property: Description("รายละเอียดหมายเหตุการยกเลิกสัญญา")]
    string? TerminateReasonDetail,
    [property: Description("เสนอผู้มีอำนาจเห็นชอบ/อนุมัติ")]
    bool IsProposedApprover,
    [property: Description("สถานะการบอกเลิกสัญญา")]
    CmContractTerminationStatus Status,
    [property: Description("รหัสเอกสารบอกเลิกสัญญา")]
    Guid? ContractTerminationDocumentId,
    [property: Description("รหัสเอกสารบอกเลิกสัญญา")]
    bool? IsContractTerminationDocumentIdReplace,
    [property: Description("รายชื่อผู้รับมอบหมาย")]
    List<AssigneeResponse> Assignees,
    [property: Description("รายชื่อผู้อนุมัติ")]
    List<AcceptorResponse> Acceptors,
    ContractTerminationDocumentVersionResponse[]? DocumentVersions = null,
    [property: Description("เอกสารแนบ")] AttachmentsDtoWithId[]? Attachments = null);