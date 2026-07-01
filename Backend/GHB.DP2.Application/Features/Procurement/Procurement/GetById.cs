namespace GHB.DP2.Application.Features.Procurement.Procurement;

using GHB.DP2.Application.Features.Procurement.Procurement.Abstact;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ProcurementDto(
    Guid? PlanId,
    ProcurementNumber? ProcurementNumber,
    ProcurementType ProcurementType,
    ProcurementStep ProcurementStep,
    string DepartmentName,
    BusinessUnitId DepartmentCode,
    string? PlanNumber,
    string PlanName,
    decimal? Budget,
    string? BudgetText,
    decimal? BudgetYear,
    string? SupplyMethod,
    ParameterCode? SupplyMethodCode,
    string? SupplyMethodType,
    ParameterCode? SupplyMethodTypeCode,
    string? SupplyMethodSpecialType,
    ParameterCode? SupplyMethodSpecialTypeCode,
    ProcurementStatus Status,
    DateTimeOffset? ExpectingProcurementAt,
    bool IsStock,
    bool IsCommercialMaterial,
    PlanType? PlanType,
    ProcessType CurrentStep)
{
    public static ProcurementDto Map(Procurement procurement)
    {
        return new ProcurementDto(
            procurement.PlanId.HasValue ? (Guid)procurement.PlanId : null,
            procurement.ProcurementNumber,
            procurement.Type,
            procurement.Step,
            procurement.Department.Name,
            procurement.DepartmentId,
            procurement.Plan?.PlanNumber.ToString() ?? string.Empty,
            procurement.Name,
            procurement.Budget,
            procurement.Budget.ThaiBahtText(),
            procurement.BudgetYear,
            procurement.SupplyMethod.Label,
            procurement.SupplyMethodCode,
            procurement.SupplyMethodType?.Label ?? string.Empty,
            procurement.SupplyMethodTypeCode,
            procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
            procurement.SupplyMethodSpecialTypeCode,
            procurement.Status,
            procurement.ExpectingProcurementAt,
            procurement.IsStock,
            procurement.IsCommercialMaterial,
            procurement.Plan?.Type,
            procurement.ProcessType);
    }
}

public record GetProcurementByIdResponse(
    Guid Id,
    Guid? PlanId,
    string? ProcurementNumber,
    ProcurementType ProcurementType,
    ProcurementStep ProcurementStep,
    string DepartmentName,
    BusinessUnitId DepartmentCode,
    string DepartmentOrganizationLevel,
    string? PlanNumber,
    string PlanName,
    decimal? Budget,
    decimal? BudgetYear,
    string? SupplyMethod,
    ParameterCode? SupplyMethodCode,
    string? SupplyMethodType,
    ParameterCode? SupplyMethodTypeCode,
    string? SupplyMethodSpecialType,
    ParameterCode? SupplyMethodSpecialTypeCode,
    ProcurementStatus Status,
    ProcurementStatus ProcurementStatus,
    DateTimeOffset? ExpectingProcurementAt,
    bool IsStock,
    bool IsCommercialMaterial,
    PlanType? PlanType,
    ProcessType CurrentStep,
    ProcessType[] Steps,
    bool HasMd,
    ProcessDto? Appoint,
    ProcessDto? TorDraft,
    ProcessDto? PrincipleApproval,
    ProcessDto? PrincipleApprovalRental,
    ProcessDto? MedianPrice,
    ProcessDto? PurchaseRequisition,
    ProcessDto? Jp005,
    ProcessDto? Invite,
    ProcessDto? PurchaseOrder,
    ProcessDto? PurchaseOrderApproval,
    ProcessDto? ContractInvitation,
    ProcessDto? ContractDraft,
    Guid CreatedBy,
    IEnumerable<ProcurementAttachmentsResponseDto> Attachments,
    string? AppointNumber,
    string? ContractType,
    string? RemarkClosed);

public record ProcurementAttachmentsResponseDto(
    ProcurementAttachmentId Id,
    string DocumentTypeCode,
    int Sequence,
    string? Remark,
    IEnumerable<ProcurementFileAttachmentsResponse> FileAttachments);

public record ProcurementFileAttachmentsResponse(
    ProcurementAttachmentInfoId Id,
    Guid FileId,
    string FileName,
    int Sequence,
    bool IsPublic,
    Guid CreatedBy);

public record ProcessDto(
    Guid Id,
    string Status
);

public record GetProcurementByIdRequest(Guid Id);

public class GetProcurementByIdEndpoint : ProcurementEndpointBase<GetProcurementByIdRequest, Results<Ok<GetProcurementByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetProcurementByIdEndpoint(
        Dp2DbContext dbContext,
        ILogger<GetProcurementByIdEndpoint> logger)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement")
             .WithName("GetProcurementById")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Get("procurement/{id:guid}");
    }

    protected override async ValueTask<Results<Ok<GetProcurementByIdResponse>, NotFound<string>>> HandleRequestAsync(GetProcurementByIdRequest req, CancellationToken ct)
    {
        var procurement = await
            this.dbContext.Procurements
                .Include(x => x.Plan)
                .Include(x => x.Department)
                .Include(x => x.SupplyMethod)
                .Include(x => x.SupplyMethodType)
                .Include(x => x.SupplyMethodSpecialType)
                .Include(procurement => procurement.Attachments)
                .ThenInclude(attachments => attachments.ProcurementAttachmentInfos)
                .Include(auditableEntity => auditableEntity.AuditInfo)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == ProcurementId.From(req.Id), ct);

        if (procurement is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        var response = await this.MapToResponse(procurement, ct);

        return TypedResults.Ok(response);
    }
}