namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment;

using System.ComponentModel;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoAddendum;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetContractAmendmentByIdRequest(Guid Id);

public record GetContractAmendmentByIdResponse(
    Guid Id,
    Guid ContractDraftVendorId,
    ContractInfo ContractInfo,
    CmContractAmendmentType Type,
    string Remark,
    CmContractAmendmentPoStep? CurrentStep,
    CmContractAmendmentPoStep[] Steps,
    CamContractAmendmentStatus Status,
    ProcessDto? PoAddendum,
    ProcessDto? PoSap,
    ProcessDto? WaiveOrReducePenalty,
    ProcessDto? AdjustContractDuration,
    AttachmentsDtoWithId[] Attachments);

public record ContractInfo(
    Guid ContractDraftVendorId,
    string ContractNumber,
    string PoNumber,
    string ContractName,
    string EntrepreneurCode,
    string EntrepreneurName,
    string? EntrepreneurEmail,
    DateTimeOffset? ContractSignedDate,
    decimal Budget,
    string? ContractTypeCode,
    string? ContractTypeLabel,
    ContractDraftVendorStatus Status,
    string? ContractTemplate,
    int? DeliveryLeadTime,
    string? DeliveryLeadTimeTypeCode,
    string? DeliveryLeadTimeTypeLabel,
    DateTimeOffset? DeliveryDate);

public record ProcessDto(
    [property: Description("รหัสขั้นตอน")] Guid Id,
    [property: Description("สถานะ")] string Status
);

public class GetContractAmendmentByIdEndpoint : EndpointBase<GetContractAmendmentByIdRequest, Results<Ok<GetContractAmendmentByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetContractAmendmentByIdEndpoint(
        Dp2DbContext dbContext,
        ILogger<GetContractAmendmentByIdEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("contract-amendment/{Id:guid}");
        this.Description(b =>
            b.WithTags("ContractAmendment")
             .WithName("GetContractAmendmentById")
             .Produces<Ok<GetContractAmendmentByIdResponse>>()
             .Produces<NotFound<string>>());
    }

    protected override async ValueTask<Results<Ok<GetContractAmendmentByIdResponse>, NotFound<string>>> HandleRequestAsync(
        GetContractAmendmentByIdRequest req,
        CancellationToken ct)
    {
        var data = await this.dbContext.CamContractAmendments
                             .Include(c => c.ContractDraftVendor)
                             .ThenInclude(p => p.ContractDraft)
                             .ThenInclude(pro => pro.Procurement)
                             .Include(po => po.PoAddendum)
                             .Include(sap => sap.PoSap)
                             .Include(cd => cd.ContractDraftVendor)
                             .ThenInclude(cdv => cdv.ContractInvitationVendors)
                             .Include(camContractAmendment => camContractAmendment.ContractDraftVendor)
                             .ThenInclude(caContractDraftVendor => caContractDraftVendor.ContractType)
                             .Include(camContractAmendment => camContractAmendment.ContractDraftVendor)
                             .ThenInclude(caContractDraftVendor => caContractDraftVendor.Template)
                             .Include(camContractAmendment => camContractAmendment.ContractDraftVendor)
                             .ThenInclude(caContractDraftVendor => caContractDraftVendor.Delivery)
                             .ThenInclude(delivery => delivery.LeadTimeType)
                             .Include(camContractAmendment => camContractAmendment.WaiveOrReducePenalty)
                             .Include(camContractAmendment => camContractAmendment.ExtendChange)
                             .AsSplitQuery()
                             .SingleOrDefaultAsync(w => w.Id == CamContractAmendmentId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลบันทึกต่อท้ายสัญญา");
        }

        var steps = new List<CmContractAmendmentPoStep>();

        if (data.Type is CmContractAmendmentType.AppendNewPurchaseOrder)
        {
            steps.Add(CmContractAmendmentPoStep.PoAddendum);

            if (data.PoAddendum?.Status is CamContractAmendmentPoAddendumStatus.Approved)
            {
                steps.Add(CmContractAmendmentPoStep.PoSap);
            }
        }

        var vendor = MapSuVendorByType(data.ContractDraftVendor.ContractInvitationVendors, data.ContractDraftVendor.ContractDraft.Procurement.Type);

        var result = new GetContractAmendmentByIdResponse(
            data.Id.Value,
            data.ContractDraftVendorId.Value,
            new ContractInfo(
                data.ContractDraftVendorId.Value,
                data.ContractDraftVendor.ContractNumber,
                data.ContractDraftVendor.PoNumber,
                data.ContractDraftVendor.ContractName,
                vendor != null ? vendor.SapVendorNumber : string.Empty,
                vendor != null ? vendor.EstablishmentName : string.Empty,
                vendor != null ? vendor.Email : string.Empty,
                data.ContractDraftVendor.ContractSignedDate,
                data.ContractDraftVendor.Budget,
                data.ContractDraftVendor.ContractTypeCode?.Value,
                data.ContractDraftVendor.ContractType?.Label,
                data.ContractDraftVendor.Status,
                data.ContractDraftVendor.Template?.Label,
                data.ContractDraftVendor.Delivery.LeadTime,
                (string?)data.ContractDraftVendor.Delivery.LeadTimeTypeCode,
                data.ContractDraftVendor.Delivery.LeadTimeType?.Label,
                data.ContractDraftVendor.Delivery?.Date),
            data.Type,
            data.Remark ?? string.Empty,
            data.Step,
            [.. steps],
            data.Status,
            data.PoAddendum != null ? new ProcessDto(data.PoAddendum.Id.Value, data.PoAddendum.Status.ToString()) : null,
            data.PoSap != null ? new ProcessDto(data.PoSap.Id.Value, data.PoSap.Status.ToString()) : null,
            data.WaiveOrReducePenalty != null ? new ProcessDto(data.WaiveOrReducePenalty.Id.Value, data.WaiveOrReducePenalty.Status.ToString()) : null,
            data.ExtendChange != null ? new ProcessDto(data.ExtendChange.Id.Value, data.ExtendChange.Status.ToString()) : null,
            [.. data.Attachments
                .OrderBy(o => o.Sequence)
                .GroupBy(
                    a => a.DocumentTypeCode,
                    (key, g) => new AttachmentsDtoWithId(
                        key.Value,
                        [.. g.Select(s => new FileAttachmentsWithId(s.Id.Value, s.FileId.Value, s.FileName, s.Sequence, s.IsPublic, s.AuditInfo.CreatedBy))]))]);

        return TypedResults.Ok(result);
    }

    private static SuVendor? MapSuVendorByType(CaContractInvitationVendors entity, ProcurementType type)
    {
        return type == ProcurementType.Procurement ? entity.PurchaseOrderApprovalContract.Entrepreneur?.SuVendor : entity.PurchaseOrderApprovalContract.PrincipleApprovalRentalEntrepreneurs?.Vendor;
    }
}