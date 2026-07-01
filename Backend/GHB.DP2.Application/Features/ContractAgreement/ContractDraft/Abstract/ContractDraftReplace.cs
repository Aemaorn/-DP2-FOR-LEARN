namespace GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Dto;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public abstract partial class ContractDraftEndpointBase<TRequest, TResponse>
{
    protected record DocumentProcessingOptions(
        ParameterCode SupplyMethodCode,
        ParameterCode? SupplyMethodSpecialTypeCode,
        bool IsReplace,
        bool HasCreator,
        bool HasAcceptor);

    protected async ValueTask UpdateDocumentAsync(
        CaContractDraftVendor vendor,
        DocumentProcessingOptions options,
        UserId userId,
        CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var lastedDraftApprovalDocument = vendor.ApprovedDocument;
        var lastedDraftContractDraftDocument = vendor.ContractDraftDocument;
        var lastedDraftConfidentialDocument = vendor.ConfidentialDocument;

        if (lastedDraftApprovalDocument is null ||
            lastedDraftContractDraftDocument is null)
        {
            this.ThrowError(
                $"ไม่พบเอกสารร่างที่ต้องการอัปโหลด",
                StatusCodes.Status404NotFound);
        }

        // Pre-fetch shared data once (previously queried 3 times inside ReplaceDocument)
        object? cachedReplaceDto = null;

        if (options.IsReplace)
        {
            var user = await this.dbContext.SuUsers
                                 .Where(u => u.Id == userId)
                                 .FirstOrDefaultAsync(ct);

            if (options.HasCreator && user is null)
            {
                this.ThrowError("User not found", StatusCodes.Status404NotFound);
            }

            var purchaseOrder = await this.dbContext
                    .PPurchaseOrder
                    .FirstOrDefaultAsync(
                        v => v.ProcurementId == vendor.ContractDraft.ProcurementId,
                        ct);

            var creator = new CreatorResponse(
                "ผู้จัดทำ",
                user?.Employee.View?.FullName,
                user?.Employee.View?.FullPositionName);

            var managers =
                await this.operationService.GetDefaultAcceptorPositionAsync(
                    SectionProcessType.ContractDraft,
                    userId.Value,
                    vendor.Budget,
                    options.SupplyMethodCode.Value,
                    options.SupplyMethodCode.Value is SupplyMethodConstant.Eighty ? default : (string?)options.SupplyMethodSpecialTypeCode,
                    ct);

            var commandNumber = managers.FirstOrDefault()?.CommandNumber;
            var supplyMethodSpecialType = await this.dbContext.SuParameters
                .FirstOrDefaultAsync(s => s.Code == options.SupplyMethodSpecialTypeCode, CancellationToken.None);

            var commandText = this.commandTextService.GetCommandText(CommandTextProgram.MedianPrice, managers, options.SupplyMethodCode, vendor.Budget, supplyMethodSpecialType: options.SupplyMethodSpecialTypeCode, supplyMethodSpecialName: supplyMethodSpecialType?.Label, commandNumber: commandNumber);

            await this.LoadNavigationPropertiesForReplaceAsync(vendor, ct);

            cachedReplaceDto = GetVendorReplaceDto.FromEntity(vendor, commandText, purchaseOrder, creator, options.HasCreator, options.HasAcceptor);
        }

        var approvalFileId = await ReplaceDocument(lastedDraftApprovalDocument.FileId, CaContractDraftVendorDocumentType.ApprovalContractDraft);
        var contractDraftFileId = await ReplaceDocument(lastedDraftContractDraftDocument.FileId, CaContractDraftVendorDocumentType.ContractDraft);

        vendor.AddDocumentHistory(
            CaContractDraftVendorDocumentType.ApprovalContractDraft,
            approvalFileId,
            options.HasAcceptor);

        vendor.AddDocumentHistory(
            CaContractDraftVendorDocumentType.ContractDraft,
            contractDraftFileId,
            options.HasAcceptor);

        if (lastedDraftConfidentialDocument != null)
        {
            var confidentialFileId = await ReplaceDocument(lastedDraftConfidentialDocument.FileId, CaContractDraftVendorDocumentType.ConfidentialContractDraft);

            vendor.AddDocumentHistory(
                CaContractDraftVendorDocumentType.ConfidentialContractDraft,
                confidentialFileId,
                options.HasAcceptor);
        }

        return;

        async Task<FileId> ReplaceDocument(FileId fileId, CaContractDraftVendorDocumentType documentType)
        {
            if (!options.IsReplace)
            {
                return fileId;
            }

            var parentDirectory =
                $"{DocumentTemplateGroups.PlanAnnouncement}/{vendor.Id}_{documentType.ToString()}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

            var copyFileId =
                await documentService.CopyDocumentTemplateAsync(
                    fileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, cachedReplaceDto!),
                    parentDirectory: parentDirectory,
                    cancellationToken: ct);

            if (copyFileId is null)
            {
                this.ThrowError(
                    DocumentErrorMessages.CopyDocumentFailed,
                    StatusCodes.Status500InternalServerError);
            }

            return copyFileId.Value;
        }
    }
}