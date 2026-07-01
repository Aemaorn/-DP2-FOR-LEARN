namespace GHB.DP2.Application.Features.Report.AuditAndRevenue.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Report.RpAuditAndRevenue;
using GHB.DP2.Domain.SystemUtility;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public abstract partial class AuditAndRevenueEndpoint<TRequest, TResponse>
{
    protected async Task<GetAuditAndRevenueReplaceDto> GetAuditAndRevenueReplaceDto(
        RpAuditAndRevenue rpAuditAndRevenues,
        UserId userId,
        bool hasCreator = false,
        bool hasAcceptor = false,
        bool hasPublisher = false,
        CancellationToken ct = default)
    {
        var hasEditPermission = rpAuditAndRevenues.AuditInfo.CreatedBy == userId;
        var creatorReplace = hasCreator ? await GetCreatorReplaceAsync() : null;

        var details = rpAuditAndRevenues.Details?.Select(d =>
        {
            return new AuditAndRevenueDetailReplaceDto(
                d.Id.Value,
                d.CaContractDraftVendor.Id.Value,
                d.CaContractDraftVendor.ContractTypeCode.Value.ToString(),
                (d.CaContractDraftVendor.ContractType != null ? d.CaContractDraftVendor.ContractType?.Label : string.Empty)!,
                d.CaContractDraftVendor.ContractNumber,
                d.CaContractDraftVendor.ContractName,
                d.CaContractDraftVendor.ContractSignedDate.ToThaiDateString(),
                d.CaContractDraftVendor.Vendor.VendorInfo.EstablishmentName,
                d.CaContractDraftVendor.Budget.ToCurrencyStringWithComma(),
                d.CaContractDraftVendor.Budget.ThaiBahtText(),
                d.Description,
                GetOverdue(d.CaContractDraftVendor.ContractSignedDate),
                d.Sequence,
                d.Description ?? string.Empty);

            bool GetOverdue(DateTimeOffset? date) => date.HasValue && (DateTimeOffset.UtcNow - date.Value).TotalDays > 30;
        }) ?? [];

        var acceptor =
            hasAcceptor
                ? GetAcceptorReplace()
                : null;

        var acceptorDate =
            rpAuditAndRevenues.Status == RpAuditAndRevenueStatus.WaitingApproval
                ? rpAuditAndRevenues.DocumentDate.ToThaiDateString() ?? DateTimeOffset.UtcNow.ToThaiDateString()
                : null;

        var publisher =
            hasPublisher
                ? GetPublisherReplace()
                : null;

        var managers =
             await this.operationService.GetDefaultAcceptorPositionIgnorePrefixAsync(
                 SectionProcessType.ContractAmendment,
                 userId.Value,
                 0,
                 SupplyMethodConstant.Eighty,
                 SupplyMethodConstant.Eighty,
                 ct);

        var positionNamePrefix = this.operationService.AddPositionNamePrefix(managers);

        var sectionApproveName =
            positionNamePrefix.Select(m => new SectionApprove(m.PositionName));

        return new GetAuditAndRevenueReplaceDto(
            rpAuditAndRevenues.Id.Value,
            acceptorDate,
            rpAuditAndRevenues.DocumentNumber,
            rpAuditAndRevenues.DocumentDate.ToThaiDateString(),
            rpAuditAndRevenues.SignStartDate.ToThaiDateString(),
            rpAuditAndRevenues.SignEndDate.ToThaiDateString(),
            rpAuditAndRevenues.Status,
            publisher,
            creatorReplace,
            details.Count(),
            details,
            acceptor,
            hasEditPermission,
            sectionApproveName);

        async Task<CreatorReplace?> GetCreatorReplaceAsync()
        {
            var sendToCommitteeApproveByUser =
                await this.dbContext.SuUsers
                          .Include(suUser => suUser.Employee)
                          .ThenInclude(rawEmployee => rawEmployee.View)
                          .FirstOrDefaultAsync(u => u.Id == userId, CancellationToken.None);

            if (sendToCommitteeApproveByUser == null)
            {
                return null;
            }

            return new CreatorReplace(
                sendToCommitteeApproveByUser.Id.Value,
                "ผู้จัดทำ",
                sendToCommitteeApproveByUser.FullName,
                sendToCommitteeApproveByUser.Employee.View?.FullPositionName ?? string.Empty,
                string.Empty);
        }

        AuthorityReplace? GetPublisherReplace()
        {
            var approverList =
                rpAuditAndRevenues.Acceptors
                                  .Where(a => a.Type == AcceptorType.Approver)
                                  .Select(DelegatorExtensions.DelegatorToAcceptor)
                                  .ToArray();

            if (!approverList.Any())
            {
                return null;
            }

            var approverApproveAll =
                approverList.All(a => a.Status == AcceptorStatus.Approved);

            if (!approverApproveAll)
            {
                return null;
            }

            var publisherUser =
                approverList.MaxBy(a => a.Sequence)!;

            return
                new AuthorityReplace(
                    publisherUser.Delegatee != null ? publisherUser.SignatureDelegatee : publisherUser.Signature,
                    publisherUser.FullName,
                    publisherUser.PositionName);
        }

        AcceptorReplace[] GetAcceptorReplace()
        {
            AcceptorReplace[] acceptors =
            [
                .. rpAuditAndRevenues.Acceptors
                                     .Where(a => a.Type == AcceptorType.Approver)
                                     .Select(DelegatorExtensions.DelegatorToAcceptor)
                                     .Map(MapAcceptorReplace)
                                     .OrderBy(a => a.Sequence)
            ];

            if (acceptors.Any())
            {
                acceptors[^1] =
                    acceptors.Last() with { Action = "อนุมัติ" };
            }

            return [.. acceptors.Where(a => a.Status == AcceptorStatus.Approved)];
        }

        AcceptorReplace MapAcceptorReplace(RpAuditRevenueAcceptor acceptor)
        {
            return new AcceptorReplace(
                acceptor.UserId.Value,
                acceptor.Sequence,
                "เห็นชอบ",
                acceptor.FullName,
                acceptor.FullName,
                acceptor.User?.Employee.View?.FullPositionName ?? string.Empty,
                string.Empty,
                string.Empty,
                acceptor.Status);
        }
    }

    private async Task<FileId> GetTemplateFileIdAsync(
        RpAuditAndRevenueDocumentType documentType,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var templateCode = documentType switch
        {
            RpAuditAndRevenueDocumentType.AuditReport => "AuditReport",
            RpAuditAndRevenueDocumentType.AuditGeneralReport => "AuditGeneralReport",
            RpAuditAndRevenueDocumentType.RevenueReport => "RevenueReport",
            _ => throw new ArgumentOutOfRangeException(nameof(documentType)),
        };

        var templateFileId = await documentService.GetDocumentTemplateAsync(
            c =>
                c.Group == DocumentTemplateGroups.AuditAndRevenueReport &&
                c.AdditionalInfo!.RootElement
                 .GetProperty(nameof(SuDocumentTemplate.ContractTemplateCode))
                 .GetString() == templateCode,
            ct);

        if (templateFileId is null)
        {
            this.ThrowError(
                DocumentErrorMessages.TemplateNotFoundForReset,
                StatusCodes.Status404NotFound);
        }

        return templateFileId.Value;
    }

    private async Task<FileId> ReplaceDocumentFromSourceAsync(
        RpAuditAndRevenue entity,
        RpAuditAndRevenueDocumentType documentType,
        FileId sourceFileId,
        UserId userId,
        bool hasCreator,
        bool hasAcceptor,
        bool hasPublisher,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var replaceDto = await this.GetAuditAndRevenueReplaceDto(
            entity,
            userId,
            hasCreator: hasCreator,
            hasAcceptor: hasAcceptor,
            hasPublisher: hasPublisher,
            ct);

        var parentDirectory =
            $"{DocumentTemplateGroups.AuditAndRevenueReport}/{entity.Id}_{documentType}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

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
        RpAuditAndRevenue entity,
        RpAuditAndRevenueDocumentType documentType,
        RpAuditAndRevenueStatus originalStatus,
        RpAuditAndRevenueStatus targetStatus,
        bool isReplaced,
        UserId userId,
        CancellationToken ct)
    {
        if (entity.LastDocumentByType(documentType) is null)
        {
            return;
        }

        var isSameStatus = originalStatus == targetStatus;

        if (isSameStatus)
        {
            // Case 1: same status (Draft/Edit/Rejected)
            if (isReplaced)
            {
                // Case 1.1: replace from template
                var templateFileId = await this.GetTemplateFileIdAsync(documentType, ct);
                var newFileId = await this.ReplaceDocumentFromSourceAsync(
                    entity,
                    documentType,
                    templateFileId,
                    userId,
                    hasCreator: false,
                    hasAcceptor: false,
                    hasPublisher: false,
                    ct);

                entity.AddDocumentHistory(documentType, newFileId, isReplace: false, incrementMajor: true);
            }
            else
            {
                // Case 1.2: copy latest doc without replace
                var latestDoc = entity.LastDocumentByType(documentType)!;
                entity.AddDocumentHistory(documentType, latestDoc.FileId, isReplace: false, incrementMajor: false);
            }
        }
        else if (targetStatus == RpAuditAndRevenueStatus.WaitingApproval)
        {
            // Case 2.1: status transition → WaitingApproval (2 steps)
            // Step 1: Replace without creator → incrementMajor
            var latestDoc = entity.LastDocumentByType(documentType)!;
            var firstFileId = await this.ReplaceDocumentFromSourceAsync(
                entity,
                documentType,
                latestDoc.FileId,
                userId,
                hasCreator: false,
                hasAcceptor: false,
                hasPublisher: false,
                ct);

            entity.AddDocumentHistory(documentType, firstFileId, isReplace: false, incrementMajor: true);

            // Step 2: Replace with creator → no incrementMajor
            var secondFileId = await this.ReplaceDocumentFromSourceAsync(
                entity,
                documentType,
                firstFileId,
                userId,
                hasCreator: true,
                hasAcceptor: false,
                hasPublisher: false,
                ct);

            entity.AddDocumentHistory(documentType, secondFileId, isReplace: true, incrementMajor: false);
        }
        else
        {
            // Case 2.2: status transition → Edit/Rejected (recall)
            var waDoc = entity.FirstWaitingApprovalNotReplacedInLatestMajor(documentType);

            if (waDoc is null)
            {
                this.ThrowError(
                    "ไม่พบเอกสารสถานะรออนุมัติ",
                    StatusCodes.Status404NotFound);
            }

            entity.AddDocumentHistory(documentType, waDoc.FileId, isReplace: false, incrementMajor: true);
        }
    }

    protected async Task ManageDocumentForApproveAsync(
        RpAuditAndRevenue entity,
        RpAuditAndRevenueDocumentType documentType,
        UserId userId,
        bool hasAcceptor,
        bool hasPublisher,
        bool isReplace,
        CancellationToken ct)
    {
        var waDoc = entity.LastWaitingApprovalDocumentByTypeReplaced(documentType);

        if (waDoc is null)
        {
            this.ThrowError(
                "ไม่พบเอกสารสถานะรออนุมัติที่ Replace แล้ว",
                StatusCodes.Status404NotFound);
        }

        var newFileId = await this.ReplaceDocumentFromSourceAsync(
            entity,
            documentType,
            waDoc.FileId,
            userId,
            hasCreator: true,
            hasAcceptor: hasAcceptor,
            hasPublisher: hasPublisher,
            ct);

        entity.AddDocumentHistory(documentType, newFileId, isReplace: isReplace, incrementMajor: false);
    }

    protected Task ManageDocumentForRejectAsync(
        RpAuditAndRevenue entity,
        RpAuditAndRevenueDocumentType documentType)
    {
        var waDoc = entity.FirstWaitingApprovalNotReplacedInLatestMajor(documentType);

        if (waDoc is null)
        {
            this.ThrowError(
                "ไม่พบเอกสารสถานะรออนุมัติ",
                StatusCodes.Status404NotFound);
        }

        entity.AddDocumentHistory(documentType, waDoc.FileId, isReplace: false, incrementMajor: true);

        return Task.CompletedTask;
    }

    protected async Task ManageDocumentForCreateAsync(
        RpAuditAndRevenue entity,
        RpAuditAndRevenueDocumentType documentType,
        UserId userId,
        CancellationToken ct)
    {
        var latestDoc = entity.LastDocumentByType(documentType);

        if (latestDoc is null)
        {
            return;
        }

        var isWaitingApproval = entity.Status == RpAuditAndRevenueStatus.WaitingApproval;

        var newFileId = await this.ReplaceDocumentFromSourceAsync(
            entity,
            documentType,
            latestDoc.FileId,
            userId,
            hasCreator: isWaitingApproval,
            hasAcceptor: false,
            hasPublisher: false,
            ct);

        entity.AddDocumentHistory(
            documentType,
            newFileId,
            isReplace: false,
            incrementMajor: isWaitingApproval);
    }

    protected async ValueTask SetDefaultDocumentTemplate(RpAuditAndRevenue rpAuditAndRevenues, CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var aditReportTemplateDocId = await documentService.GetDocumentTemplateAsync(
            c =>
                c.Group == DocumentTemplateGroups.AuditAndRevenueReport &&
                c.AdditionalInfo!.RootElement
                 .GetProperty(nameof(SuDocumentTemplate.ContractTemplateCode))
                 .GetString() == "AuditReport",
            ct);

        var auditGeneralReportTemplateDocId = await documentService.GetDocumentTemplateAsync(
            c =>
                c.Group == DocumentTemplateGroups.AuditAndRevenueReport &&
                c.AdditionalInfo!.RootElement
                 .GetProperty(nameof(SuDocumentTemplate.ContractTemplateCode))
                 .GetString() == "AuditGeneralReport",
            ct);

        var revenueReportTemplateDocId = await documentService.GetDocumentTemplateAsync(
            c =>
                c.Group == DocumentTemplateGroups.AuditAndRevenueReport &&
                c.AdditionalInfo!.RootElement
                 .GetProperty(nameof(SuDocumentTemplate.ContractTemplateCode))
                 .GetString() == "RevenueReport",
            ct);

        rpAuditAndRevenues.AddDocumentHistory(RpAuditAndRevenueDocumentType.AuditReport, (FileId)aditReportTemplateDocId, false);
        rpAuditAndRevenues.AddDocumentHistory(RpAuditAndRevenueDocumentType.AuditGeneralReport, (FileId)auditGeneralReportTemplateDocId, false);
        rpAuditAndRevenues.AddDocumentHistory(RpAuditAndRevenueDocumentType.RevenueReport, (FileId)revenueReportTemplateDocId, false);
    }
}