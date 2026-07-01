namespace GHB.DP2.Application.Features.Procurement.Procurement.Abstact;

using System.ComponentModel;
using System.Linq.Expressions;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ProcurementReplaceDto(
    [property: Description("รหัสแผนงาน")] Guid? PlanId,
    [property: Description("เลขที่การจัดซื้อจัดจ้าง")]
    string? ProcurementNumber,
    [property: Description("ประเภทการจัดซื้อจัดจ้าง")]
    ProcurementType ProcurementType,
    [property: Description("ขั้นตอนการจัดซื้อจัดจ้าง")]
    ProcurementStep ProcurementStep,
    [property: Description("ชื่อหน่วยงาน")]
    string DepartmentName,
    [property: Description("รหัสหน่วยงาน")]
    BusinessUnitId DepartmentCode,
    [property: Description("เลขที่แผนงาน")]
    string? PlanNumber,
    [property: Description("ชื่อแผนงาน")] string PlanName,
    [property: Description("งบประมาณ")] string? Budget,
    [property: Description("งบประมาณ ภาษาไทย")]
    string? BudgetText,
    [property: Description("ปีงบประมาณ")] decimal? BudgetYear,
    [property: Description("วิธีการจัดหา")]
    string? SupplyMethod,
    [property: Description("รหัสวิธีการจัดหา")]
    ParameterCode? SupplyMethodCode,
    [property: Description("ประเภทวิธีการจัดหา")]
    string? SupplyMethodType,
    [property: Description("รหัสประเภทวิธีการจัดหา")]
    ParameterCode? SupplyMethodTypeCode,
    [property: Description("ประเภทวิธีการจัดหาพิเศษ")]
    string? SupplyMethodSpecialType,
    [property: Description("รหัสประเภทวิธีการจัดหาพิเศษ")]
    ParameterCode? SupplyMethodSpecialTypeCode,
    [property: Description("สถานะการจัดซื้อจัดจ้าง")]
    ProcurementStatus Status,
    [property: Description("วันที่คาดว่าจะดำเนินการจัดซื้อจัดจ้าง")]
    DateTimeOffset? ExpectingProcurementAt,
    [property: Description("เป็นสต็อก")] bool IsStock,
    [property: Description("เป็นวัสดุเชิงพาณิชย์")]
    bool IsCommercialMaterial,
    [property: Description("ประเภทแผนงาน")]
    PlanType? PlanType,
    [property: Description("ขั้นตอนปัจจุบัน")]
    ProcessType CurrentStep);

public abstract class ProcurementEndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;

    protected ProcurementEndpointBase(
        ILogger logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    protected async Task<GetProcurementByIdResponse> MapToResponse(Domain.Procurement.Procurement procurement, CancellationToken ct)
    {
        var steps = new List<ProcessType>();

        var appointDto = await this.GetProcessDtoAsync(
            this.dbContext.PpAppoints.Where(w => w.ProcurementId == procurement.Id && w.IsActive),
            s => new ProcessDto(s.Id.Value, s.Status.ToString()),
            ProcessType.Appoint,
            steps,
            ct);

        var torDraftDto = await this.GetProcessDtoAsync(
            this.dbContext.PpTorDrafts.Where(w => w.ProcurementId == procurement.Id && w.IsActive),
            s => new ProcessDto(s.Id.Value, s.Status.ToString()),
            ProcessType.TorDraft,
            steps,
            ct);

        var principleApprovalDto = await this.GetProcessDtoAsync(
            this.dbContext.PPrincipleApprovals.Where(w => w.ProcurementId == procurement.Id),
            s => new ProcessDto(s.Id.Value, s.Status.ToString()),
            ProcessType.PrincipleApproval,
            steps,
            ct);

        var principleApprovalRentalDto = await this.GetProcessDtoAsync(
            this.dbContext.PPrincipleApprovalRentals.Where(w => w.ProcurementId == procurement.Id),
            s => new ProcessDto(s.Id.Value, s.Status.ToString()),
            ProcessType.PrincipleApprovalRental,
            steps,
            ct);

        var medianPriceDto = await this.GetProcessDtoAsync(
            this.dbContext.PpMedianPrices.Where(w => w.ProcurementId == procurement.Id && w.IsActive),
            s => new ProcessDto(s.Id.Value, s.Status.ToString()),
            ProcessType.MedianPrice,
            steps,
            ct);

        var purchaseRequisitionDto = await this.GetProcessDtoAsync(
            this.dbContext.PpPurchaseRequisitions
                .Where(w => w.ProcurementId == procurement.Id),
            s => new ProcessDto(s.Id.Value, s.Status.ToString()),
            ProcessType.PurchaseRequisition,
            steps,
            ct);

        var jp005Dto = await this.GetProcessDtoAsync(
            this.dbContext.PJp005S.Where(w => w.ProcurementId == procurement.Id && w.IsActive),
            s => new ProcessDto(s.Id.Value, s.Status.ToString()),
            ProcessType.Jp005,
            steps,
            ct);

        var inviteDto = await this.GetProcessDtoAsync(
            this.dbContext.PInvites.Where(w => w.ProcurementId == procurement.Id),
            s => new ProcessDto(s.Id.Value, s.Status.ToString()),
            ProcessType.Invite,
            steps,
            ct);

        var purchaseOrderDto = await this.GetProcessDtoAsync(
            this.dbContext.PJp006S.Where(w => w.ProcurementId == procurement.Id),
            s => new ProcessDto(s.Id.Value, s.Status.ToString()),
            ProcessType.PurchaseOrder,
            steps,
            ct);

        var purchaseOrderApprovalDto = await this.GetProcessDtoAsync(
            this.dbContext.PPurchaseOrderApprovals.Where(w => w.ProcurementId == procurement.Id),
            s => new ProcessDto(s.Id.Value, s.Status.ToString()),
            ProcessType.PurchaseOrderApproval,
            steps,
            ct);

        var purchaseOrderApproval = await this.dbContext.PPurchaseOrderApprovals.FirstOrDefaultAsync(w => w.ProcurementId == procurement.Id, ct);

        var contractInvitationDto = await this.GetProcessDtoAsync(
            this.dbContext.CaContractInvitations.Where(w => w.ProcurementId == procurement.Id),
            s => new ProcessDto(s.Id.Value, s.Status.ToString()),
            ProcessType.ContractInvitation,
            steps,
            ct);

        var contractDraftDto = await this.GetProcessDtoAsync(
            this.dbContext.CaContractDrafts.Where(w => w.ProcurementId == procurement.Id),
            s => new ProcessDto(s.Id.Value, s.Status.ToString()),
            ProcessType.ContractDraft,
            steps,
            ct);

        var processTypes = MapProcessByType(procurement, steps);

        return new GetProcurementByIdResponse(
            procurement.Id.Value,
            procurement.PlanId.HasValue ? (Guid)procurement.PlanId : null,
            procurement.ProcurementNumber?.Value,
            procurement.Type,
            procurement.Step,
            procurement.Department.Name,
            procurement.DepartmentId,
            procurement.Department.OrganizationLevel,
            (string?)procurement.Plan?.PlanNumber ?? string.Empty,
            procurement.Name,
            procurement.Budget,
            procurement.BudgetYear,
            procurement.SupplyMethod.Label,
            procurement.SupplyMethodCode,
            procurement.SupplyMethodType?.Label ?? string.Empty,
            procurement.SupplyMethodTypeCode,
            procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
            procurement.SupplyMethodSpecialTypeCode,
            procurement.Status,
            procurement.Status,
            procurement.ExpectingProcurementAt,
            procurement.IsStock,
            procurement.IsCommercialMaterial,
            procurement.Plan?.Type,
            procurement.ProcessType,
            MapProcessByType(procurement, steps),
            procurement.HasMd,
            appointDto,
            torDraftDto,
            principleApprovalDto,
            principleApprovalRentalDto,
            medianPriceDto,
            purchaseRequisitionDto,
            jp005Dto,
            inviteDto,
            purchaseOrderDto,
            purchaseOrderApprovalDto,
            contractInvitationDto,
            contractDraftDto,
            procurement.AuditInfo.CreatedBy,
            procurement.Attachments
                       .OrderBy(a => a.Sequence)
                       .Select(a => new ProcurementAttachmentsResponseDto(
                           a.Id,
                           a.TypeCode.ToString(),
                           a.Sequence,
                           a.Remark,
                           a.ProcurementAttachmentInfos
                            .OrderBy(p => p.Sequence)
                            .Select(info => new ProcurementFileAttachmentsResponse(
                                info.Id,
                                info.FileId.Value,
                                info.FileName,
                                info.Sequence,
                                info.IsPublic,
                                info.AuditInfo.CreatedBy)))),
            procurement.ProcurementNumber?.Value,
            purchaseOrderApproval?.ContractType,
            procurement.RemarkClosed);
    }

    private static ProcessType[] MapProcessByType(Procurement procurement, List<ProcessType> steps)
    {
        if (!steps.Any())
        {
            return procurement.Type == ProcurementType.Procurement ? [ProcessType.Appoint, ProcessType.PurchaseRequisition, ProcessType.PurchaseOrderApproval] : [ProcessType.PrincipleApproval];
        }

        ProcessType[] result = [.. steps, procurement.ProcessType];

        return [.. result.Distinct()];
    }

    private async Task<ProcessDto?> GetProcessDtoAsync<TEntity>(
        IQueryable<TEntity> query,
        Expression<Func<TEntity, ProcessDto>> selector,
        ProcessType processType,
        List<ProcessType> steps,
        CancellationToken ct)
        where TEntity : class
    {
        var data = await query
                         .Select(selector)
                         .AsNoTracking()
                         .FirstOrDefaultAsync(ct);

        return ProcurementEndpointBase<TRequest, TResponse>.MapToProcessDto(data, processType, steps);
    }

    private static ProcessDto? MapToProcessDto(
        ProcessDto? data,
        ProcessType processType,
        List<ProcessType> steps)
    {
        if (data is not null)
        {
            steps.Add(processType);
        }

        return data;
    }
}