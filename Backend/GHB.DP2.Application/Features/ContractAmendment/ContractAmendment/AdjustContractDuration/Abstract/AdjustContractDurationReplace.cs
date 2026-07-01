namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration.Dto;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public abstract partial class AdjustContractDurationEndpointBase<TRequest, TResponse>
{
    protected async Task<GetAdjustContractDurationReplaceDto> GetAdjustContractDurationReplaceMappingDto(
        CamContractAmendmentExtendChange extendChange,
        UserId? userId = null,
        bool hasCreator = false,
        bool hasAcceptor = false,
        CancellationToken ct = default)
    {
        var adjustContractDurationOld = AdjustContractDurationInfo.Map(extendChange.CamContractAmendment.ContractDraftVendor);

        var adjustContractDurationNew = AdjustContractDurationInfo.Map(extendChange);

        var user = await this.dbContext.SuUsers
                             .Include(suUser => suUser.Employee)
                             .ThenInclude(rawEmployee => rawEmployee.View)
                             .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (hasCreator && user is null)
        {
            this.ThrowError("User not found", StatusCodes.Status404NotFound);
        }

        var creator =
            hasCreator
                ? new AdjustContractDurationApproverReplaceDto("ผู้จัดทำ", user?.Employee.View?.FullName, user?.Employee.View?.FullPositionName)
                : null;

        var lastAcceptors =
            hasAcceptor
                ? extendChange.Acceptors
                              .Where(a => a.Type == AcceptorType.Approver)
                              .OrderBy(a => a.Sequence)
                              .LastOrDefault()
                : null;

        var acceptors =
            hasAcceptor
                ? [.. extendChange.Acceptors
                              .Where(a => a.Status == AcceptorStatus.Approved)
                              .Where(a => a is { Type: AcceptorType.Approver, IsUnableToPerformDuties: false })
                              .OrderBy(a => a.Sequence)
                              .Select(a => MapToAcceptorReplace(a, lastAcceptors))]
                : new List<AdjustContractDurationAcceptorReplaceDto>();

        var assignees =
            extendChange.Assignees
                        .OrderBy(o => o.Sequence)
                        .Select(a => new AdjustContractDurationAssigneeReplaceDto(
                            a.Id.Value,
                            a.Group,
                            a.Type,
                            a.UserId.Value,
                            a.Sequence,
                            a.User.FullName,
                            a.PositionName,
                            a.BusinessUnitName,
                            a.Status,
                            a.Remark,
                            a.ActionAt)).ToArray();

        return new GetAdjustContractDurationReplaceDto(
            extendChange.Id.Value,
            extendChange.CamContractAmendmentId.Value,
            AdjustContractDurationVendorReplaceDto.FromEntity(extendChange.CamContractAmendment.ContractDraftVendor),
            adjustContractDurationOld,
            adjustContractDurationNew,
            creator,
            acceptors,
            assignees,
            extendChange.Status);

        AdjustContractDurationAcceptorReplaceDto MapToAcceptorReplace(CamContractAmendmentExtendChangeAcceptor acceptor, CamContractAmendmentExtendChangeAcceptor? lastedAcceptor)
        {
            var action =
                (acceptor.Status, lastedAcceptor == acceptor) switch
                {
                    (AcceptorStatus.Approved, false) => "เห็นชอบ",
                    (AcceptorStatus.Approved, true) => "อนุมัติ",
                    _ => "ไมเห็นชอบ",
                };

            return new AdjustContractDurationAcceptorReplaceDto(
                acceptor.Id.Value,
                acceptor.Type,
                acceptor.UserId.Value,
                acceptor.Sequence,
                action,
                acceptor.User.FullName,
                acceptor.PositionName,
                acceptor.BusinessUnitName,
                acceptor.Status,
                acceptor.Remark,
                acceptor.ActionAt,
                acceptor.CommitteePositionsCode.HasValue ? (string)acceptor.CommitteePositionsCode : string.Empty,
                acceptor.CommitteePosition?.Label ?? string.Empty,
                acceptor.IsUnableToPerformDuties,
                string.Empty,
                acceptor.DelegateeId?.Value,
                acceptor.IsCurrentApprover());
        }
    }

    protected async ValueTask UpdateDocumentAsync(
        CamContractAmendmentExtendChange entity,
        UserId userId,
        bool isReplace,
        bool hasCreator = false,
        bool hasAcceptor = false,
        CancellationToken ct = default)
    {
        var documentService = this.Resolve<IDocumentService>();

        var lastedExtendChangeDocument =
            hasAcceptor
                ? entity.LastedWaitingCommitteeApprovalExtendChangeDocument
                : entity.LastedDraftExtendChangeDocument;

        var lastedApprovedDocument =
            hasAcceptor
                ? entity.LastedWaitingCommitteeApprovalApprovedRequestDocument
                : entity.LastedDraftApprovedRequestDocument;

        if (lastedExtendChangeDocument is null || lastedApprovedDocument is null)
        {
            this.ThrowError(
                "ไม่พบเอกสารร่าง ที่ต้องการอัปโหลด",
                StatusCodes.Status404NotFound);
        }

        var waiveOrReducePenaltyFileId = await ReplaceDocument(lastedExtendChangeDocument.FileId, ExtendChangeAcceptorDocumentType.ExtendChange);
        var approvedFileId = await ReplaceDocument(lastedApprovedDocument.FileId, ExtendChangeAcceptorDocumentType.Approved);

        entity.AddDocumentHistory(
            ExtendChangeAcceptorDocumentType.ExtendChange,
            waiveOrReducePenaltyFileId,
            hasAcceptor);

        entity.AddDocumentHistory(
            ExtendChangeAcceptorDocumentType.Approved,
            approvedFileId,
            hasAcceptor);

        return;

        async Task<FileId> ReplaceDocument(FileId fileId, ExtendChangeAcceptorDocumentType documentType)
        {
            if (!isReplace)
            {
                return fileId;
            }

            var replaceDto = await this.GetAdjustContractDurationReplaceMappingDto(entity, userId, hasCreator, hasAcceptor, ct);

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

    protected async ValueTask SetDefaultDocumentTemplate(CamContractAmendmentExtendChange entity, CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var extendChangeTemplateDocId = await documentService.GetDocumentTemplateAsync(
            c =>
                c.Group == DocumentTemplateGroups.AddendumList &&
                EF.Functions.JsonExists(c.AdditionalInfo!.RootElement, nameof(SuDocumentTemplate.IsApproval)) == false &&
                c.AdditionalInfo!.RootElement
                 .GetProperty(nameof(SuDocumentTemplate.ContractAmendmentDocumentType))
                 .GetString() == ContractAmendmentDocumentType.AdjustContractDuration,
            ct);

        var approvedTemplateDocId = await documentService.GetDocumentTemplateAsync(
            c =>
                c.Group == DocumentTemplateGroups.AddendumList &&
                c.AdditionalInfo!.RootElement
                 .GetProperty(nameof(SuDocumentTemplate.IsApproval))
                 .GetBoolean() == true &&
                c.AdditionalInfo!.RootElement
                 .GetProperty(nameof(SuDocumentTemplate.ContractAmendmentDocumentType))
                 .GetString() == ContractAmendmentDocumentType.AdjustContractDuration,
            ct);

        entity.AddDocumentHistory(ExtendChangeAcceptorDocumentType.ExtendChange, (FileId)extendChangeTemplateDocId, false);
        entity.AddDocumentHistory(ExtendChangeAcceptorDocumentType.Approved, (FileId)approvedTemplateDocId, false);
    }
}