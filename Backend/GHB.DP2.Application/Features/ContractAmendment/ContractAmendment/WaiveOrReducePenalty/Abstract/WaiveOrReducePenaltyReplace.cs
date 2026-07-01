namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public abstract partial class WaiveOrReducePenaltyEndpointBase<TRequest, TResponse>
{
    protected async ValueTask UpdateDocumentAsync(
        CamContractAmendmentWaiveOrReducePenalty entity,
        UserId userId,
        bool isReplace,
        bool hasCreator = false,
        bool hasAcceptor = false,
        CancellationToken ct = default)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var lastedWaiveOrReducePenaltyDocument =
            hasAcceptor
                ? entity.LastedWaitingCommitteeApprovalWaiveOrReducePenaltyDocument
                : entity.LastedDraftWaiveOrReducePenaltyDocument;

        var lastedApprovedRequestDocument =
            hasAcceptor
                ? entity.LastedWaitingCommitteeApprovalApprovedRequestDocument
                : entity.LastedDraftApprovedRequestDocument;

        if (lastedWaiveOrReducePenaltyDocument is null || lastedApprovedRequestDocument is null)
        {
            this.ThrowError(
                "ไม่พบเอกสารร่าง ที่ต้องการอัปโหลด",
                StatusCodes.Status404NotFound);
        }

        var user = await this.dbContext.SuUsers
                             .Include(suUser => suUser.Employee)
                             .ThenInclude(rawEmployee => rawEmployee.View)
                             .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (hasCreator && user is null)
        {
            this.ThrowError("User not found", StatusCodes.Status404NotFound);
        }

        var waiveOrReducePenaltyFileId = await ReplaceDocument(lastedWaiveOrReducePenaltyDocument.FileId, WaiveOrReducePenaltyDocumentType.WaiveOrReducePenalty, user);
        var approvedFileId = await ReplaceDocument(lastedApprovedRequestDocument.FileId, WaiveOrReducePenaltyDocumentType.Approved, user);

        entity.AddDocumentHistory(
            WaiveOrReducePenaltyDocumentType.WaiveOrReducePenalty,
            waiveOrReducePenaltyFileId,
            hasAcceptor);

        entity.AddDocumentHistory(
            WaiveOrReducePenaltyDocumentType.Approved,
            approvedFileId,
            hasAcceptor);

        return;

        async Task<FileId> ReplaceDocument(FileId fileId, WaiveOrReducePenaltyDocumentType documentType, SuUser? creator)
        {
            if (!isReplace)
            {
                return fileId;
            }

            var replaceDto = WaiveOrReducePenaltyReplaceDto.MapToResponse(entity, creator, hasCreator, hasAcceptor);

            if (replaceDto is null)
            {
                this.ThrowError(
                    "ไม่สามารถดึงข้อมูล ขออนุมัติงด/ลดค่าปรับ",
                    StatusCodes.Status500InternalServerError);
            }

            var parentDirectory =
                $"{DocumentTemplateGroups.AddendumList}/{entity.Id}_{documentType}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

            var copyFileId =
                await documentService.CopyDocumentTemplateAsync(
                    fileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
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

    protected async ValueTask SetDefaultDocumentTemplate(CamContractAmendmentWaiveOrReducePenalty entity, CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var waiveOrReducePenaltyTemplateDocId = await documentService.GetDocumentTemplateAsync(
            c =>
                c.Group == DocumentTemplateGroups.AddendumList &&
                EF.Functions.JsonExists(c.AdditionalInfo!.RootElement, nameof(SuDocumentTemplate.IsApproval)) == false &&
                c.AdditionalInfo!.RootElement
                 .GetProperty(nameof(SuDocumentTemplate.ContractAmendmentDocumentType))
                 .GetString() == ContractAmendmentDocumentType.WaiveOrReducePenalty,
            ct);

        var approvedTemplateDocId = await documentService.GetDocumentTemplateAsync(
            c =>
                c.Group == DocumentTemplateGroups.AddendumList &&
                c.AdditionalInfo!.RootElement
                 .GetProperty(nameof(SuDocumentTemplate.IsApproval))
                 .GetBoolean() == true &&
                c.AdditionalInfo!.RootElement
                 .GetProperty(nameof(SuDocumentTemplate.ContractAmendmentDocumentType))
                 .GetString() == ContractAmendmentDocumentType.WaiveOrReducePenalty,
            ct);

        entity.AddDocumentHistory(WaiveOrReducePenaltyDocumentType.WaiveOrReducePenalty, (FileId)waiveOrReducePenaltyTemplateDocId, false);
        entity.AddDocumentHistory(WaiveOrReducePenaltyDocumentType.Approved, (FileId)approvedTemplateDocId, false);
    }
}