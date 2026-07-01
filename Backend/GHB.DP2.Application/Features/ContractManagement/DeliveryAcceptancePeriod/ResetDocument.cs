namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Application.Services.Document;
using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ResetDeliveryAcceptancePeriodDocumentRequest(
    Guid DeliveryAcceptanceId,
    Guid Id);

public class ResetDeliveryAcceptancePeriodDocumentEndpoint
    : DeliveryAcceptancePeriodEndpointBase<ResetDeliveryAcceptancePeriodDocumentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ResetDeliveryAcceptancePeriodDocumentEndpoint(
        Dp2DbContext dbContext,
        ILogger<ResetDeliveryAcceptancePeriodDocumentEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("delivery-acceptance/{DeliveryAcceptanceId:guid}/period/{Id:guid}/reset-document");
        this.Description(b => b
            .WithTags("ContractManagement/DeliveryAcceptance/Period")
            .WithName("ResetDeliveryAcceptancePeriodDocument"));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ResetDeliveryAcceptancePeriodDocumentRequest req,
        CancellationToken ct)
    {
        var entity = await this.dbContext.CmDeliveryAcceptancePeriods
            .Include(p => p.DocumentHistories)
            .Include(p => p.CmDeliveryAcceptance)
            .FirstOrDefaultAsync(
                p => p.Id == CmDeliveryAcceptancePeriodId.From(req.Id) &&
                     p.CmDeliveryAcceptanceId == CmDeliveryAcceptanceId.From(req.DeliveryAcceptanceId),
                ct);

        if (entity == null)
        {
            return TypedResults.NotFound(DocumentErrorMessages.DocumentNotFound);
        }

        var documentHistories = entity.DocumentHistories
            .Where(d => d.DocumentType == CmDeliveryAcceptanceDocumentType.DeliveryAcceptance)
            .ToList();

        if (documentHistories.Count == 0)
        {
            return TypedResults.BadRequest(DocumentErrorMessages.DocumentNotFound);
        }

        var newFileId = await this.GetDocumentTemplateForResetAsync(entity, ct);

        entity.AddDocumentHistory(
            CmDeliveryAcceptanceDocumentType.DeliveryAcceptance,
            newFileId,
            false);

        this.dbContext.CmDeliveryAcceptancePeriods.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<FileId> GetDocumentTemplateForResetAsync(
        CmDeliveryAcceptancePeriod entity,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        // Get SupplyMethodCode from source (Plan, ContractDraftVendor, or Procurement)
        ParameterCode supplyMethodCode;
        if (entity.CmDeliveryAcceptance.SourceType == SourceType.Plan)
        {
            var plan = await this.dbContext.Plans
                .FirstOrDefaultAsync(p => p.Id == PlanId.From((Guid)entity.CmDeliveryAcceptance.RefId), ct);

            if (plan == null)
            {
                this.ThrowError(
                    "ไม่พบข้อมูลแผนจัดซื้อจัดจ้าง",
                    StatusCodes.Status404NotFound);
            }

            supplyMethodCode = plan.SupplyMethodCode;
        }
        else if (entity.CmDeliveryAcceptance.SourceType == SourceType.ContractDraftVendor)
        {
            var contractDraftVendor = await this.dbContext.CaContractDraftVendors
                .Include(c => c.ContractDraft)
                .ThenInclude(cd => cd.Procurement)
                .FirstOrDefaultAsync(c => c.Id == ContractDraftVendorId.From((Guid)entity.CmDeliveryAcceptance.RefId), ct);

            if (contractDraftVendor == null)
            {
                this.ThrowError(
                    "ไม่พบข้อมูลสัญญา",
                    StatusCodes.Status404NotFound);
            }

            supplyMethodCode = contractDraftVendor.ContractDraft.Procurement.SupplyMethodCode;
        }
        else if (entity.CmDeliveryAcceptance.SourceType == SourceType.Procurement)
        {
            var poaData = await this.dbContext.PPurchaseOrderApprovals
                .Include(poa => poa.Procurement)
                .FirstOrDefaultAsync(poa => poa.Id == PurchaseOrderApprovalId.From((Guid)entity.CmDeliveryAcceptance.RefId), ct);

            if (poaData == null)
            {
                this.ThrowError(
                    "ไม่พบข้อมูลใบสั่งซื้อ/จ้าง/เช่า",
                    StatusCodes.Status404NotFound);
            }

            supplyMethodCode = poaData.Procurement.SupplyMethodCode;
        }
        else if (entity.CmDeliveryAcceptance.SourceType == SourceType.ContractDraftVendorEdit)
        {
            var vendorEditReset = await this.dbContext.CaContractDraftVendorEdits
                .FirstOrDefaultAsync(ve => ve.Id == ContractDraftVendorEditId.From((Guid)entity.CmDeliveryAcceptance.RefId), ct);

            if (vendorEditReset == null)
            {
                this.ThrowError("ไม่พบข้อมูลบันทึกต่อท้ายสัญญา", StatusCodes.Status404NotFound);
            }

            var cdvReset = await this.dbContext.CaContractDraftVendors
                .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement)
                .FirstOrDefaultAsync(v => v.Id == vendorEditReset.ContractDraftVendorId, ct);

            if (cdvReset == null)
            {
                this.ThrowError("ไม่พบข้อมูลสัญญาต้นฉบับ", StatusCodes.Status404NotFound);
            }

            supplyMethodCode = cdvReset.ContractDraft.Procurement.SupplyMethodCode;
        }
        else if (entity.CmDeliveryAcceptance.SourceType == SourceType.Manual)
        {
            if (entity.CmDeliveryAcceptance.SupplyMethodCode == null)
            {
                this.ThrowError("ไม่พบข้อมูลวิธีจัดหา", StatusCodes.Status404NotFound);
            }

            supplyMethodCode = entity.CmDeliveryAcceptance.SupplyMethodCode!.Value;
        }
        else
        {
            throw new InvalidOperationException("SourceType ไม่ถูกต้อง");
        }

        // Get template document from SuDocumentTemplates
        var templateFileId = await documentService.GetDocumentTemplateAsync(
            c => c.Group == DocumentTemplateGroups.CMR &&
                 c.SupplyMethodCode == supplyMethodCode,
            ct);

        if (templateFileId == null)
        {
            this.ThrowError(
                "ไม่พบเอกสาร Template",
                StatusCodes.Status404NotFound);
        }

        // Create replacement DTO - user who reset becomes the creator
        var currentUserId = Guid.TryParse(this.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var parsedUserId) ? parsedUserId : Guid.Empty;
        var replaceDto = await this.MapToReplaceDtoAsync(
            entity,
            ct,
            currentUserId,
            creatorUserId: (Domain.SystemUtility.UserId?)currentUserId);

        var parentDirectory =
            $"{DocumentTemplateGroups.CMR}/{entity.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        // Copy document WITH placeholder replacement
        var newFileId = await documentService.CopyDocumentTemplateAsync(
            templateFileId.Value,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: parentDirectory,
            cancellationToken: ct);

        if (newFileId == null)
        {
            this.ThrowError(
                DocumentErrorMessages.CopyDocumentFailed,
                StatusCodes.Status500InternalServerError);
        }

        return newFileId.Value;
    }
}
