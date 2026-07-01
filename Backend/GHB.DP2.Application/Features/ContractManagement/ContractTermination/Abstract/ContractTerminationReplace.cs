namespace GHB.DP2.Application.Features.ContractManagement.ContractTermination.Abstract;

using Codehard.Common.Extensions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Linq.Expressions;

public abstract partial class ContractTerminationEndpoint<TRequest, TResponse>
{
    private const string ParentDirectory = $"{DocumentTemplateGroups.CMTermination}";

    public record ContractTerminationReplaceDocumentDto(
        [property: Description("เลขที่สัญญา")] string ContractNumber,
        [property: Description("ชื่อสัญญา")] string ContractName,
        [property: Description("รายละเอียดสัญญา")]
        ContractDraftInfoReplaceDto? ContractDraftInfoDetail,
        IEnumerable<ContractTerminationAcceptorReplace>? Acceptors,
        TerminationDetailReplaceDto? ContractTermination);

    public record TerminationDetailReplaceDto(
        string? TerminateTypeName,
        string? TerminateReasonDetail);

    private static Expression<Func<SuDocumentTemplate, bool>> GetDocumentTemplateCondition(ParameterCode supplyMethodCode) =>
        c => c.Group == DocumentTemplateGroups.CMTermination &&
             c.SupplyMethodCode == supplyMethodCode;

    protected record TerminationDocumentOptions(
        bool IsReplace,
        bool HasCreator,
        bool HasAcceptor,
        bool MarkAsReplaced,
        bool HasComment = false);

    protected async Task<ContractVendorTerminationReplaceDto> GetContractVendorTerminalMappingDtoAsync(
        CaContractDraftVendor entity,
        CmContractTermination termination,
        Delivery delivery,
        SuVendor suVendor,
        Guid userId,
        bool hasCreator = false,
        bool hasAcceptor = false,
        bool hasComment = false,
        CancellationToken ct = default)
    {
        var lastedContractGuaranteeReturn = termination.DocumentHistories
                                                       .Where(d => d.DocumentType == CmContractTerminationDocumentType.ContractTermination)
                                                       .OrderVersions()
                                                       .FirstOrDefault();

        var lastAcceptors =
            hasAcceptor
                ? termination.Acceptors
                             .Where(a =>
                                 a is { Status: AcceptorStatus.Approved, Type: AcceptorType.Approver })
                             .OrderBy(a => a.Sequence)
                             .LastOrDefault()
                : new CmContractTerminationAcceptor();

        IEnumerable<ContractTerminationAcceptorReplace>? acceptor =
            hasAcceptor
                ? termination.Acceptors
                             .Where(a =>
                                 a is { Status: AcceptorStatus.Approved, Type: AcceptorType.Approver })
                             .Select(DelegatorExtensions.DelegatorToAcceptor)
                             .OrderBy(a => a.Sequence)
                             .Select(a =>
                             {
                                 var action =
                                     (a.Status, lastAcceptors == a) switch
                                     {
                                         (AcceptorStatus.Approved, false) => "เห็นชอบ",
                                         (AcceptorStatus.Approved, true) => "อนุมัติ",
                                         _ => "ไมเห็นชอบ",
                                     };

                                 return new ContractTerminationAcceptorReplace(
                                     action,
                                     a.FullName,
                                     a.PositionName,
                                     string.Empty);
                             })
                : null;

        if (hasAcceptor && !termination.IsProposedApprover)
        {
            acceptor = new List<ContractTerminationAcceptorReplace>
            {
                new(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty),
            };
        }

        var creator =
            hasCreator
                ? await this.dbContext.SuUsers
                            .Where(u => u.Id == UserId.From(userId))
                            .Select(u => new ContractTerminationCreatorDto(
                                "ผู้จัดทำ",
                                u.Employee.Signature,
                                u.FullName,
                                u.Employee.View!.FullPositionName))
                            .FirstOrDefaultAsync(ct)
                : null;

        if (termination.TerminateTypeNavigation == null)
        {
            await this.dbContext.Entry(termination)
                      .Reference(r => r.TerminateTypeNavigation)
                      .LoadAsync(CancellationToken.None);
        }

        var terminateTypeLabel = termination.TerminateType == ParameterCode.From("CTR004") ? termination.TerminateReasonOther : termination.TerminateTypeNavigation?.Label;

        var acceptorDate =
            termination.Status is not (CmContractTerminationStatus.Draft or CmContractTerminationStatus.Rejected)
                ? DateTimeOffset.Now.ToThaiDateString(includeBuddhistEra: true)
                : null;

        var jorPorComments = hasComment
            ? termination.Assignees
                         .Where(a => !string.IsNullOrWhiteSpace(a.Remark))
                         .OrderBy(a => a.ActionAt)
                         .Select(a => new JorPorCommentDto(
                             "ให้ความเห็น",
                             a.FullName,
                             a.PositionName,
                             a.Remark))
                         .FirstOrDefault()
            : null;

        var terminationDto = new ContractTerminationReplaceDto(
            termination.Id.Value,
            termination.TerminateDate,
            terminateTypeLabel ?? string.Empty,
            termination.TerminateReason,
            termination.TerminateReasonDetail,
            termination.Status,
            lastedContractGuaranteeReturn?.FileId.Value,
            termination.Assignees
                       .OrderBy(a => a.Sequence)
                       .Select(a => new ContractTerminationAssigneeResponse(
                           a.Status.ToString(),
                           a.FullName,
                           a.PositionName,
                           a.DelegateeId.Value.ToString())),
            acceptor,
            creator);

        return new ContractVendorTerminationReplaceDto(
            entity.Id,
            suVendor.TaxpayerIdentificationNo,
            suVendor.EstablishmentName,
            suVendor.Email,
            entity.ContractNumber,
            entity.PoNumber,
            entity.Budget,
            entity.ContractName,
            entity.ContractType?.Label,
            entity.Template?.Label,
            entity.ContractSignedDate,
            delivery?.LeadTime,
            delivery?.LeadTimeTypeCode,
            delivery?.LeadTimeType?.Label,
            delivery?.Date,
            terminationDto,
            ContractDraftInfoReplaceDto.FromEntity(entity),
            acceptorDate,
            jorPorComments,
            entity.ContractDraftNumber.Value,
            entity.ContractSignedDate?.ToThaiDateString(includeBuddhistEra: true));
    }

    private async Task<ContractTerminationReplaceDocumentDto> MapReplaceDocumentDto(
        CaContractDraftVendor entity,
        CmContractTermination termination,
        bool hasAcceptor,
        bool hasTerminateInfo = true)
    {
        IEnumerable<ContractTerminationAcceptorReplace>? acceptors = null;

        if (hasAcceptor)
        {
            acceptors = termination.IsProposedApprover switch
            {
                true => termination.Acceptors
                                   .Where(r => r is
                                   { Type: AcceptorType.Approver, Status: AcceptorStatus.Approved })
                                   .Select(DelegatorExtensions.DelegatorToAcceptor)
                                   .OrderBy(a => a.Sequence)
                                   .Select(a => new ContractTerminationAcceptorReplace(
                                       "อนุมัติ",
                                       $"({a.FullName})",
                                       a.PositionName,
                                       string.Empty))
                                   .ToList(),
                false => new List<ContractTerminationAcceptorReplace>
                {
                    new(string.Empty, string.Empty, string.Empty, string.Empty),
                },
            };
        }

        if (!hasTerminateInfo)
        {
            return new ContractTerminationReplaceDocumentDto(
                entity.ContractNumber,
                entity.ContractName,
                ContractDraftInfoReplaceDto.FromEntity(entity),
                acceptors,
                new TerminationDetailReplaceDto(
                    null,
                    termination.TerminateReasonDetail));
        }

        if (termination.TerminateTypeNavigation == null)
        {
            await this.dbContext.Entry(termination)
                      .Reference(r => r.TerminateTypeNavigation)
                      .LoadAsync(CancellationToken.None);
        }

        var terminateTypeLabel = termination.TerminateType == ParameterCode.From("CTR004") ? termination.TerminateReasonOther : termination.TerminateTypeNavigation?.Label;

        var terminateDetail = new TerminationDetailReplaceDto(
            terminateTypeLabel ?? null,
            termination.TerminateReasonDetail);

        return new ContractTerminationReplaceDocumentDto(
            entity.ContractNumber,
            entity.ContractName,
            ContractDraftInfoReplaceDto.FromEntity(entity),
            acceptors,
            terminateDetail);
    }

    protected async ValueTask CreateDocumentAsync(
        CaContractDraftVendor entity,
        CmContractTermination termination,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var contractTerminationTemplateDocId = await documentService.GetDocumentTemplateAsync(
            GetDocumentTemplateCondition(entity.ContractDraft.Procurement.SupplyMethodCode),
            ct);

        if (!contractTerminationTemplateDocId.HasValue)
        {
            this.ThrowError($"ไม่พบแม่แบบเอกสาร", StatusCodes.Status404NotFound);
        }

        termination.AddDocumentHistory(contractTerminationTemplateDocId.Value, incrementMajor: true);

        var replaceDto = await this.MapReplaceDocumentDto(entity, termination, false, false);

        var newFileId =
            await documentService.CopyDocumentTemplateAsync(
                contractTerminationTemplateDocId.Value,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: $"{ParentDirectory}/{entity.ContractNumber}/{CmContractTerminationDocumentType.ContractTermination}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);

        if (newFileId is null)
        {
            this.ThrowError("การคัดลอกเอกสารล้มเหลว", StatusCodes.Status500InternalServerError);
        }

        termination.AddDocumentHistory(newFileId.Value);
    }

    protected async ValueTask ResetDocumentFromTemplateAsync(
        CaContractDraftVendor entity,
        CmContractTermination termination,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var templateDocId = await documentService.GetDocumentTemplateAsync(
            GetDocumentTemplateCondition(entity.ContractDraft.Procurement.SupplyMethodCode),
            ct);

        if (!templateDocId.HasValue)
        {
            this.ThrowError("ไม่พบแม่แบบเอกสาร", StatusCodes.Status404NotFound);
        }

        var replaceDto = await this.MapReplaceDocumentDto(entity, termination, false);

        var newFileId =
            await documentService.CopyDocumentTemplateAsync(
                templateDocId.Value,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: $"{ParentDirectory}/{entity.ContractNumber}/{CmContractTerminationDocumentType.ContractTermination}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);

        if (newFileId is null)
        {
            this.ThrowError("การคัดลอกเอกสารล้มเหลว", StatusCodes.Status500InternalServerError);
        }

        termination.AddDocumentHistory(newFileId.Value, incrementMajor: true);
    }

    protected async ValueTask CopyCheckpointDocumentAsync(
        CmContractTermination termination,
        CmContractTerminationDocumentHistory checkpointDocument,
        CancellationToken ct,
        bool isReplace = false,
        bool incrementMajor = true)
    {
        var documentService = this.Resolve<IDocumentService>();

        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            checkpointDocument.FileId,
            parentDirectory: $"{ParentDirectory}/{termination.Id}_{CmContractTerminationDocumentType.ContractTermination}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (!copiedFileId.HasValue)
        {
            this.ThrowError("การคัดลอกเอกสารล้มเหลว", StatusCodes.Status500InternalServerError);
        }

        termination.AddDocumentHistory(copiedFileId.Value, isReplace: isReplace, incrementMajor: incrementMajor);
    }

    private record FinalDocumentStampDto(
        [property: Description("วันที่อนุมัติ")]
        string AcceptorDate,
        IEnumerable<ContractTerminationAcceptorReplace>? Acceptors);

    protected async ValueTask StampAcceptorDateAsync(
        CmContractTermination termination,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var latestDoc = termination.DocumentHistories
                                   .Where(d => d.DocumentType == CmContractTerminationDocumentType.ContractTermination)
                                   .OrderVersions()
                                   .FirstOrDefault();

        if (latestDoc is null)
        {
            return;
        }

        IEnumerable<ContractTerminationAcceptorReplace>? acceptors =
            !termination.IsProposedApprover
                ? new List<ContractTerminationAcceptorReplace>
                {
                    new(string.Empty, string.Empty, string.Empty, string.Empty),
                }
                : null;

        var stampDto = new FinalDocumentStampDto(
            termination.Status is not (CmContractTerminationStatus.Draft or CmContractTerminationStatus.Rejected) ? DateTimeOffset.UtcNow.ToThaiDateString(includeBuddhistEra: true) : string.Empty,
            acceptors);

        var newFileId = await documentService.CopyDocumentTemplateAsync(
            latestDoc.FileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, stampDto),
            parentDirectory: $"{ParentDirectory}/{termination.Id}_{CmContractTerminationDocumentType.ContractTermination}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (newFileId is null)
        {
            this.ThrowError("การคัดลอกเอกสารล้มเหลว", StatusCodes.Status500InternalServerError);
        }

        termination.AddDocumentHistory(newFileId.Value, isReplace: true, incrementMajor: true);
    }

    protected async Task ReplaceAcceptorsAsync(
        CaContractDraftVendor entity,
        CmContractTermination termination,
        CancellationToken ct = default)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var document = termination.IsProposedApprover ? termination.LastedWaitingApprovalDocument : termination.LastedWaitingCommentDocument;

        if (document is null)
        {
            this.ThrowError(
                $"ไม่พบเอกสารร่าง ที่ต้องการอัปโหลด",
                StatusCodes.Status404NotFound);
        }

        var replaceDto = await this.MapReplaceDocumentDto(entity, termination, true);

        var parentDirectory =
            $"{DocumentTemplateGroups.CMTermination}/{termination.Id}_{CmContractTerminationDocumentType.ContractTermination}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        var copyFileId =
            await documentService.CopyDocumentTemplateAsync(
                document.FileId,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: parentDirectory,
                cancellationToken: ct);

        if (copyFileId is null)
        {
            this.ThrowError(
                DocumentErrorMessages.CopyDocumentFailed,
                StatusCodes.Status500InternalServerError);
        }

        termination.AddDocumentHistory(copyFileId.Value);
    }

    protected async ValueTask UpdateDocumentAsync(
        CaContractDraftVendor entity,
        CmContractTermination termination,
        Delivery delivery,
        SuVendor suVendor,
        Guid userId,
        TerminationDocumentOptions options,
        CancellationToken cancellationToken = default,
        CmContractTerminationDocumentHistory? overrideSourceDocument = null,
        bool hasComment = false)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var lastedDraftOrRejectedDocument = termination.LastedDraftOrRejectedDocument;

        var lastedApprovalDocument = overrideSourceDocument
            ?? (options.HasAcceptor
                ? termination.LastedWaitingApprovalDocument
                : lastedDraftOrRejectedDocument);

        if (lastedApprovalDocument is null)
        {
            this.ThrowError(
                $"ไม่พบเอกสารร่าง ที่ต้องการอัปโหลด",
                StatusCodes.Status404NotFound);
        }

        var approvalFileId = await ReplaceDocument(lastedApprovalDocument.FileId);

        termination.AddDocumentHistory(
            approvalFileId,
            options.MarkAsReplaced);

        return;

        async Task<FileId> ReplaceDocument(FileId fileId)
        {
            if (!options.IsReplace)
            {
                return fileId;
            }

            var replaceDto =
                await this.GetContractVendorTerminalMappingDtoAsync(entity, termination, delivery, suVendor, userId, options.HasCreator, options.HasAcceptor, hasComment, cancellationToken);

            var parentDirectory =
                $"{DocumentTemplateGroups.CMTermination}/{termination.Id}_{CmContractTerminationDocumentType.ContractTermination}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

            var copyFileId =
                await documentService.CopyDocumentTemplateAsync(
                    fileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                    parentDirectory: parentDirectory,
                    cancellationToken: cancellationToken);

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