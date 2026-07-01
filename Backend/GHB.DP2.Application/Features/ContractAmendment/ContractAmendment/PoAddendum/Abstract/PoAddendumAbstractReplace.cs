namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoAddendum.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoAddendum;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public abstract partial class PoAddendumAbstractEndpoint<TRequest, TResponse>
{
    protected async Task<GetPoAddendumResponseReplaceDto> GetGetPoAddendumResponseReplaceDtoAsync(
        CamContractAmendment cam,
        CamContractAmendmentPoAddendum po,
        UserId userId,
        bool hasCreator = false,
        bool hasAcceptor = false,
        CancellationToken ct = default)
    {
        var oldPaymentTerms = cam.ContractDraftVendor.PaymentTerms
                                 .OrderBy(p => p.Sequence)
                                 .Select(p => new PaymentTermReplaceDto(
                                     p.Id.Value,
                                     p.PaymentTermNo,
                                     p.LeadTime,
                                     p.DeliveryDate,
                                     p.InstallmentPercentage,
                                     p.Amount,
                                     p.AdvanceDeductionAmount,
                                     p.PerformanceDeductionAmount,
                                     p.Description,
                                     p.Sequence));

        var newPaymentTerms = po.PaymentTerms.OrderBy(p => p.Sequence)
                                .Select(p => new PaymentTermReplaceDto(
                                    p.Id.Value,
                                    p.PaymentTermNo,
                                    p.LeadTime,
                                    p.DeliveryDate,
                                    p.InstallmentPercentage,
                                    p.Amount,
                                    p.AdvanceDeductionAmount,
                                    p.PerformanceDeductionAmount,
                                    p.Description,
                                    p.Sequence));

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
                ? new PoAddendumPoApproverReplaceDto("ผู้จัดทำ", user?.Employee.View?.FullName, user?.Employee.View?.FullPositionName)
                : null;

        var lastAcceptors =
            hasAcceptor
                ? po.Acceptors
                    .Where(a => a.Type == AcceptorType.Approver)
                    .OrderBy(a => a.Sequence)
                    .LastOrDefault()
                : null;

        var acceptors =
            hasAcceptor
                ? [.. po.Acceptors
                    .Where(a => a.Status == AcceptorStatus.Approved)
                    .Where(a => a is { Type: AcceptorType.Approver, IsUnableToPerformDuties: false })
                    .OrderBy(a => a.Sequence)
                    .Select(a => MapToAcceptorReplace(a, lastAcceptors))]
                : new List<PoAddendumAcceptorReplaceDto>();

        var assignees = po.Assignees
                          .Where(a => !a.IsDeleted)
                          .OrderBy(a => a.Sequence)
                          .Select(a => new PoAddendumAssigneeReplaceDto(
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
                              a.ActionAt));

        var oldVendor = cam.ContractDraftVendor.Vendor?.VendorInfo;
        var oldVendorVendorInfo = oldVendor != null ? new PoAddendumVendorInfoReplaceDto(oldVendor.Id.Value, oldVendor.TaxpayerIdentificationNo, oldVendor.EstablishmentName, oldVendor.Email) : null;

        var vi2 = po.Vendor;
        var vendorInfo2 = vi2 != null ? new PoAddendumVendorInfoReplaceDto(vi2.Id.Value, vi2.TaxpayerIdentificationNo, vi2.EstablishmentName, vi2.Email) : null;

        return new GetPoAddendumResponseReplaceDto(
            po.Id,
            po.CamContractAmendmentId,
            new ContractInfoReplaceDto(
                cam.ContractDraftVendor.ContractNumber,
                oldVendorVendorInfo?.VendorId,
                oldVendorVendorInfo != null ? oldVendorVendorInfo.EstablishmentName : string.Empty,
                string.Empty,
                cam.ContractDraftVendor.PoNumber),
            new ContractInfoReplaceDto(
                po.ContractNumber,
                po.Vendor.Id.Value,
                po.Vendor.EstablishmentName,
                po.SapNumber,
                po.PoNumber),
            PoAddendumVendorReplaceDto.FromEntity(cam.ContractDraftVendor),
            po.Status,
            vendorInfo2,
            creator,
            acceptors,
            assignees,
            newPaymentTerms,
            oldPaymentTerms);

        PoAddendumAcceptorReplaceDto MapToAcceptorReplace(CamContractAmendmentPoAddendumAcceptor acceptor, CamContractAmendmentPoAddendumAcceptor? lastedAcceptor)
        {
            var action =
                (acceptor.Status, lastedAcceptor == acceptor) switch
                {
                    (AcceptorStatus.Approved, false) => "เห็นชอบ",
                    (AcceptorStatus.Approved, true) => "อนุมัติ",
                    _ => "ไมเห็นชอบ",
                };

            return new PoAddendumAcceptorReplaceDto(
                acceptor.Id.Value,
                acceptor.Type,
                acceptor.UserId.Value,
                acceptor.Sequence,
                action,
                acceptor.FullName,
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
        CamContractAmendment cam,
        CamContractAmendmentPoAddendum po,
        UserId userId,
        bool isReplace,
        bool hasCreator = false,
        bool hasAcceptor = false,
        CancellationToken ct = default)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var lastedContractAddendumDocument =
            hasAcceptor
                ? po.LastedContractAddendumWaitingCommitteeApprovalDocument
                : po.LastedContractAddendumDraftDocument;

        var lastedContractAmendmentRequestDocument =
            hasAcceptor
                ? po.LastedContractAmendmentRequestWaitingCommitteeApprovalDocument
                : po.LastedContractAmendmentRequestDraftDocument;

        if (lastedContractAddendumDocument is null || lastedContractAmendmentRequestDocument is null)
        {
            this.ThrowError(
                $"ไม่พบเอกสารร่าง ที่ต้องการอัปโหลด",
                StatusCodes.Status404NotFound);
        }

        var approvalFileId = await ReplaceDocument(lastedContractAddendumDocument.FileId);
        var contractFileId = await ReplaceDocument(lastedContractAmendmentRequestDocument.FileId);

        po.AddDocumentHistory(
            CamContractAmendmentPoAddendumDocumentType.ContractAddendum,
            approvalFileId,
            hasAcceptor);

        po.AddDocumentHistory(
            CamContractAmendmentPoAddendumDocumentType.ContractAmendmentRequest,
            contractFileId,
            hasAcceptor);

        return;

        async Task<FileId> ReplaceDocument(FileId fileId)
        {
            if (!isReplace)
            {
                return fileId;
            }

            var replaceDto = await this.GetGetPoAddendumResponseReplaceDtoAsync(cam, po, userId, hasCreator: hasCreator, hasAcceptor: hasAcceptor, ct);

            var parentDirectory =
                $"{DocumentTemplateGroups.AddendumList}/{po.Id}_{CmContractTerminationDocumentType.ContractTermination}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

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

    protected async ValueTask SetDefaultDocumentTemplate(CamContractAmendmentPoAddendum po, CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var contractAddendumTemplateDocId = await documentService.GetDocumentTemplateAsync(
            c =>
                c.Group == DocumentTemplateGroups.AddendumList &&
                EF.Functions.JsonExists(c.AdditionalInfo!.RootElement, nameof(SuDocumentTemplate.IsApproval)) == false &&
                c.AdditionalInfo!.RootElement
                 .GetProperty(nameof(SuDocumentTemplate.ContractAmendmentDocumentType))
                 .GetString() == ContractAmendmentDocumentType.AppendNewPurchaseOrder,
            ct);

        var contractAmendmentRequestTemplateDocId = await documentService.GetDocumentTemplateAsync(
            c =>
                c.Group == DocumentTemplateGroups.AddendumList &&
                c.AdditionalInfo!.RootElement
                 .GetProperty(nameof(SuDocumentTemplate.IsApproval))
                 .GetBoolean() == true &&
                c.AdditionalInfo!.RootElement
                 .GetProperty(nameof(SuDocumentTemplate.ContractAmendmentDocumentType))
                 .GetString() == ContractAmendmentDocumentType.AppendNewPurchaseOrder,
            ct);

        po.AddDocumentHistory(CamContractAmendmentPoAddendumDocumentType.ContractAddendum, (FileId)contractAddendumTemplateDocId, false);
        po.AddDocumentHistory(CamContractAmendmentPoAddendumDocumentType.ContractAmendmentRequest, (FileId)contractAmendmentRequestTemplateDocId, false);
    }
}