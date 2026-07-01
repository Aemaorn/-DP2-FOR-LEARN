namespace GHB.DP2.Application.Features.Report.ContractCompletionByQuarter.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.AspNetCore.Http;

public abstract partial class ContractCompletionByQuarterEndpoint<TRequest, TResponse>
{
    private async Task<FileId> ReplaceDocumentFromSourceAsync(
        RpContractCompletionByQuarter entity,
        FileId sourceFileId,
        UserId userId,
        bool hasCreator,
        bool hasAcceptor,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var replaceDto = await this.MapToReplaceDtoAsync(
            entity,
            userId,
            hasCreator: hasCreator,
            hasAcceptor: hasAcceptor,
            ct);

        var parentDirectory =
            $"{DocumentTemplateGroups.QuarterlyCompletion}/{entity.Id}_{RpContractCompletionByQuarterDocumentType.Completion}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        var copyFileId =
            await documentService.CopyDocumentTemplateAsync(
                sourceFileId,
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

    protected async Task ManageDocumentForSaveAsync(
        RpContractCompletionByQuarter entity,
        RpContractCompletionByQuarterStatus originalStatus,
        RpContractCompletionByQuarterStatus targetStatus,
        bool isReplaced,
        UserId userId,
        CancellationToken ct)
    {
        if (entity.LastDocument() is null)
        {
            return;
        }

        var isSameStatus = originalStatus == targetStatus;

        if (isSameStatus)
        {
            if (isReplaced)
            {
                // Case 1.1: replace from template
                var templateFileId = await this.GetDocumentTemplateByCriteria(entity.Quarter, ct);
                var newFileId = await this.ReplaceDocumentFromSourceAsync(
                    entity,
                    templateFileId,
                    userId,
                    hasCreator: false,
                    hasAcceptor: false,
                    ct);

                entity.AddDocumentHistory(newFileId, isReplace: false, incrementMajor: true);
            }
            else
            {
                // Case 1.2: copy latest doc without replace
                var latestDoc = entity.LastDocument()!;
                entity.AddDocumentHistory(latestDoc.FileId, isReplace: false, incrementMajor: false);
            }
        }
        else if (targetStatus == RpContractCompletionByQuarterStatus.WaitingApproval)
        {
            // Case 2: status transition -> WaitingApproval (2 steps)
            // Step 1: Replace latest doc without creator -> incrementMajor
            var latestDoc = entity.LastDocument()!;
            var firstFileId = await this.ReplaceDocumentFromSourceAsync(
                entity,
                latestDoc.FileId,
                userId,
                hasCreator: false,
                hasAcceptor: false,
                ct);

            entity.AddDocumentHistory(firstFileId, isReplace: false, incrementMajor: true);

            // Step 2: Replace step1 result with creator -> no incrementMajor
            var secondFileId = await this.ReplaceDocumentFromSourceAsync(
                entity,
                firstFileId,
                userId,
                hasCreator: true,
                hasAcceptor: false,
                ct);

            entity.AddDocumentHistory(secondFileId, isReplace: true, incrementMajor: false);
        }
        else
        {
            // Case 3: status transition -> Edit/Rejected (recall)
            var waDoc = entity.FirstWaitingApprovalNotReplacedInLatestMajor();

            if (waDoc is null)
            {
                this.ThrowError(
                    "ไม่พบเอกสารสถานะรออนุมัติ",
                    StatusCodes.Status404NotFound);
            }

            entity.AddDocumentHistory(waDoc.FileId, isReplace: false, incrementMajor: true);
        }
    }

    protected async Task ManageDocumentForApproveAsync(
        RpContractCompletionByQuarter entity,
        UserId userId,
        bool hasAcceptor,
        bool isReplace,
        CancellationToken ct)
    {
        var waDoc = entity.LastWaitingApprovalDocumentReplaced();

        if (waDoc is null)
        {
            this.ThrowError(
                "ไม่พบเอกสารสถานะรออนุมัติที่ Replace แล้ว",
                StatusCodes.Status404NotFound);
        }

        var newFileId = await this.ReplaceDocumentFromSourceAsync(
            entity,
            waDoc.FileId,
            userId,
            hasCreator: true,
            hasAcceptor: hasAcceptor,
            ct);

        entity.AddDocumentHistory(newFileId, isReplace: isReplace, incrementMajor: false);
    }

    protected Task ManageDocumentForRejectAsync(
        RpContractCompletionByQuarter entity)
    {
        var waDoc = entity.FirstWaitingApprovalNotReplacedInLatestMajor();

        if (waDoc is null)
        {
            this.ThrowError(
                "ไม่พบเอกสารสถานะรออนุมัติ",
                StatusCodes.Status404NotFound);
        }

        entity.AddDocumentHistory(waDoc.FileId, isReplace: false, incrementMajor: true);

        return Task.CompletedTask;
    }
}
