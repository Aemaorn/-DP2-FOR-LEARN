namespace GHB.DP2.Application.Features.AttachmentFileAll;

using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetAttachmentFileAllRequest
{
    public Guid ProcurementId { get; init; }
}

public record GetAttachmentFileAllResponse(
    Guid? PlanId,
    string PlanNumber,
    Guid ProcurementId,
    string ProcurementNumber,
    string ProjectName,
    string DepartmentName,
    int? BudgetYear,
    decimal Budget,
    string SupplyMethod,
    string SupplyMethodType,
    string SupplyMethodSpecialType,
    AttachmentStageDto[] Stages);

public record AttachmentStageDto(string StageName, AttachmentSubSectionDto[] SubSections);

public record AttachmentSubSectionDto(string Label, AttachmentRefGroupDto[] Groups);

public record AttachmentRefGroupDto(Guid? Id, string RefNumber, AttachmentFileItemDto[] Files);

public record AttachmentFileItemDto(Guid FileId, string FileName, bool IsPublic, Guid CreatedBy, int Sequence);

public class GetAttachmentFileAll : EndpointBase<GetAttachmentFileAllRequest, Ok<GetAttachmentFileAllResponse>>
{
    private readonly Dp2DbContext dbContext;

    public GetAttachmentFileAll(ILogger<GetAttachmentFileAll> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b => b
            .WithTags("AttachmentFileAll")
            .WithName("GetAttachmentFileAll"));
        this.Get("attachment-files");
    }

    protected override async ValueTask<Ok<GetAttachmentFileAllResponse>> HandleRequestAsync(
        GetAttachmentFileAllRequest req, CancellationToken ct)
    {
        var procurement = await this.dbContext.Procurements
            .AsNoTracking()
            .Include(p => p.SupplyMethod)
            .Include(p => p.SupplyMethodType)
            .Include(p => p.SupplyMethodSpecialType)
            .Include(p => p.Department)
            .Include(p => p.Attachments)
                .ThenInclude(a => a.ProcurementAttachmentInfos)
            .Where(p => !p.IsDeleted && p.Id == ProcurementId.From(req.ProcurementId))
            .FirstOrDefaultAsync(ct);

        if (procurement is null)
        {
            return TypedResults.Ok(new GetAttachmentFileAllResponse(
                null,
                string.Empty,
                req.ProcurementId,
                string.Empty,
                string.Empty,
                string.Empty,
                null,
                0,
                string.Empty,
                string.Empty,
                string.Empty,
                []));
        }

        Plan? plan = null;
        if (procurement.PlanId is not null)
        {
            plan = await this.dbContext.Plans
                .AsNoTracking()
                .Include(p => p.Attachments)
                .FirstOrDefaultAsync(p => p.Id == procurement.PlanId, ct);
        }

        var contractDrafts = await this.dbContext.CaContractDrafts
            .AsNoTracking()
            .Include(c => c.Vendors)
                .ThenInclude(v => v.Attachments)
                    .ThenInclude(a => a.Files)
            .Where(c => c.ProcurementId == procurement.Id)
            .ToListAsync(ct);

        var vendors = contractDrafts.SelectMany(c => c.Vendors).ToList();
        var vendorIds = vendors.Select(v => v.Id).ToList();
        var vendorGuids = vendorIds.Select(v => v.Value).ToList();

        var amendments = await this.dbContext.CamContractAmendments
            .AsNoTracking()
            .Include(a => a.Attachments)
            .Where(a => vendorIds.Contains(a.ContractDraftVendorId))
            .ToListAsync(ct);

        var vendorEdits = await this.dbContext.CaContractDraftVendorEdits
            .AsNoTracking()
            .Include(e => e.CheckerAttachment)
            .Where(e => e.ProcurementId == procurement.Id.Value)
            .ToListAsync(ct);

        var vendorEditGuids = vendorEdits.Select(e => e.Id.Value).ToList();

        var deliveryAcceptancePeriods = await this.dbContext.CmDeliveryAcceptancePeriods
            .AsNoTracking()
            .Include(p => p.CmDeliveryAcceptance)
            .Include(p => p.Attachments)
            .Where(p =>
                (p.CmDeliveryAcceptance.SourceType == SourceType.ContractDraftVendor &&
                 vendorGuids.Contains((Guid)p.CmDeliveryAcceptance.RefId)) ||
                (p.CmDeliveryAcceptance.SourceType == SourceType.ContractDraftVendorEdit &&
                 vendorEditGuids.Contains((Guid)p.CmDeliveryAcceptance.RefId)) ||
                (p.CmDeliveryAcceptance.SourceType == SourceType.Procurement &&
                 p.CmDeliveryAcceptance.RefId == procurement.Id.Value))
            .ToListAsync(ct);

        var contractTerminations = await this.dbContext.CmContractTerminations
            .AsNoTracking()
            .Include(t => t.Attachments)
            .Where(t => vendorIds.Contains(t.ContractDraftVendorId))
            .ToListAsync(ct);

        var guaranteeReturns = await this.dbContext.CmContractGuaranteeReturns
            .AsNoTracking()
            .Include(g => g.Attachments)
            .Where(g => vendorIds.Contains(g.ContractDraftVendorId))
            .ToListAsync(ct);

        var stages = new[]
        {
            BuildPlanStage(plan),
            BuildProcurementStage(procurement),
            BuildContractManagementStage(vendorEdits, deliveryAcceptancePeriods, contractTerminations, guaranteeReturns),
        };

        var response = new GetAttachmentFileAllResponse(
            plan?.Id.Value,
            plan?.PlanNumber.Value ?? string.Empty,
            procurement.Id.Value,
            procurement.ProcurementNumber?.Value ?? string.Empty,
            plan?.Name ?? string.Empty,
            procurement.Department?.Name ?? string.Empty,
            procurement.BudgetYear ?? plan?.BudgetYear,
            procurement.Budget ?? 0,
            procurement.SupplyMethod?.Label ?? procurement.SupplyMethodCode.Value,
            procurement.SupplyMethodType?.Label ?? procurement.SupplyMethodTypeCode?.Value ?? string.Empty,
            procurement.SupplyMethodSpecialType?.Label ?? procurement.SupplyMethodSpecialTypeCode?.Value ?? string.Empty,
            stages);

        return TypedResults.Ok(response);
    }

    private static AttachmentStageDto BuildPlanStage(Plan? plan)
    {
        var files = plan?.Attachments
            .OrderBy(a => a.Sequence)
            .Select(a => new AttachmentFileItemDto(a.FileId.Value, a.FileName, a.IsPublic, a.AuditInfo.CreatedBy, a.Sequence))
            .ToArray() ?? [];

        AttachmentRefGroupDto[] planGroups = files.Length > 0
            ? [new AttachmentRefGroupDto(plan!.Id.Value, string.Format("เลขที่รายการจัดซื้อจัดจ้าง: {0}", plan!.PlanNumber.Value), files)]
            : [];

        return new AttachmentStageDto(
            "Plan",
            [new AttachmentSubSectionDto("รายการจัดซื้อจัดจ้าง", planGroups)]);
    }

    private static AttachmentStageDto BuildProcurementStage(Procurement procurement)
    {
        var files = procurement.Attachments
            .SelectMany(a => a.ProcurementAttachmentInfos)
            .OrderBy(f => f.Sequence)
            .Select(f => new AttachmentFileItemDto(f.FileId.Value, f.FileName, f.IsPublic, f.AuditInfo.CreatedBy, f.Sequence))
            .ToArray();

        AttachmentRefGroupDto[] procGroups = files.Length > 0
            ? [new AttachmentRefGroupDto(procurement.Id.Value, string.Format("เลขที่การจัดซื้อจัดจ้าง: {0}", procurement.ProcurementNumber?.Value ?? string.Empty), files)]
            : [];

        return new AttachmentStageDto(
            "Procurement",
            [new AttachmentSubSectionDto("รายการจัดซื้อจัดจ้าง", procGroups)]);
    }

    private static AttachmentStageDto BuildContractManagementStage(
        IList<CaContractDraftVendorEdit> vendorEdits,
        IList<CmDeliveryAcceptancePeriod> deliveryAcceptancePeriods,
        IList<CmContractTermination> contractTerminations,
        IList<CmContractGuaranteeReturn> guaranteeReturns)
    {
        var editGroups = vendorEdits
            .Where(e => e.CheckerAttachment.Any(c => c.Type == EntrepreneurAttachmentType.General))
            .Select(e => new AttachmentRefGroupDto(
                e.Id.Value,
                string.Format("เลขที่สัญญา: {0}", e.ContractNumber ?? string.Empty),
                e.CheckerAttachment
                    .Where(c => c.Type == EntrepreneurAttachmentType.General)
                    .OrderBy(c => c.Sequence)
                    .Select(c => new AttachmentFileItemDto(c.FileId.Value, c.FileName, c.IsPublic, c.AuditInfo.CreatedBy, c.Sequence))
                    .ToArray()))
            .ToArray();

        var deliveryGroups = deliveryAcceptancePeriods
            .Where(p => p.Attachments.Any())
            .Select(p => new AttachmentRefGroupDto(
                p.Id.Value,
                string.Format("เลขที่ตรวจรับ: {0}", p.AcceptanceNumber ?? string.Empty),
                p.Attachments
                    .OrderBy(a => a.Sequence)
                    .Select(a => new AttachmentFileItemDto(a.FileId.Value, a.FileName, a.IsPublic, a.AuditInfo.CreatedBy, a.Sequence))
                    .ToArray()))
            .ToArray();

        var terminationGroups = contractTerminations
            .Where(t => t.Attachments.Any())
            .Select(t => new AttachmentRefGroupDto(
                t.Id.Value,
                string.Format("เลขที่สัญญา: {0}", t.CaContractDraftVendor.ContractNumber ?? string.Empty),
                t.Attachments
                    .OrderBy(a => a.Sequence)
                    .Select(a => new AttachmentFileItemDto(a.FileId.Value, a.FileName, a.IsPublic, a.AuditInfo.CreatedBy, a.Sequence))
                    .ToArray()))
            .ToArray();

        var guaranteeGroups = guaranteeReturns
            .Where(g => g.Attachments.Any())
            .Select(g => new AttachmentRefGroupDto(
                g.Id.Value,
                string.Format("เลขที่คืนหลักประกันสัญญา: {0}", g.GuaranteeNumber.Value),
                g.Attachments
                    .OrderBy(a => a.Sequence)
                    .Select(a => new AttachmentFileItemDto(a.FileId.Value, a.FileName, a.IsPublic, a.AuditInfo.CreatedBy, a.Sequence))
                    .ToArray()))
            .ToArray();

        return new AttachmentStageDto(
            "Contract Management",
            [
                new AttachmentSubSectionDto("บันทึกรายงานผลการตรวจรับ (จพ.008)", deliveryGroups),
                new AttachmentSubSectionDto("คืนหลักประกันสัญญา", guaranteeGroups),
                new AttachmentSubSectionDto("บันทึกต่อท้ายสัญญา", editGroups),
                new AttachmentSubSectionDto("บอกเลิกสัญญา", terminationGroups),
            ]);
    }
}
