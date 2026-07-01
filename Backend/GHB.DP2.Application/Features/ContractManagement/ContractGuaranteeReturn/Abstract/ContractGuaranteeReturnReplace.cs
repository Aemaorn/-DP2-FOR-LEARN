namespace GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.SystemUtility;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;

public abstract partial class ContractGuaranteeReturnEndpoint<TRequest, TResponse>
{
    protected async Task<ContractVendorGuaranteeReturnReplaceDto> GetContractGuaranteeReturnMappingDtoAsync(
        CaContractDraftVendor entity,
        CmContractGuaranteeReturn guarantee,
        SuVendor suVendor,
        bool hasCreator = false,
        bool hasAcceptor = false,
        bool hasPublisher = false,
        bool hasCommittee = false,
        CancellationToken ct = default)
    {
        var lastedApprovalCmContractGuaranteeReturn = guarantee.DocumentHistories
                                                               .Where(d => d.DocumentType == CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn)
                                                               .OrderVersions()
                                                               .FirstOrDefault();

        var lastedContractGuaranteeReturnResult = guarantee.DocumentHistories
                                                           .Where(d => d.DocumentType == CmContractGuaranteeReturnDocumentType.CmContractGuaranteeReturnResule)
                                                           .OrderVersions()
                                                           .FirstOrDefault();

        var lastAcceptors =
            hasAcceptor
                ? guarantee.Acceptors
                           .Where(a =>
                               a is { Type: AcceptorType.Approver })
                           .OrderBy(a => a.Sequence)
                           .LastOrDefault()
                : new CmContractGuaranteeReturnAcceptor();

        var acceptor =
            hasAcceptor
                ? guarantee.Acceptors
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

                               return new ContractGuaranteeReturnAcceptorReplace(
                                   action,
                                   a.FullName,
                                   a.PositionName,
                                   string.Empty);
                           })
                : new List<ContractGuaranteeReturnAcceptorReplace>();

        var committee =
            hasCommittee
                ? guarantee.Acceptors
                           .Where(a =>
                               a is { Status: AcceptorStatus.Approved, Type: AcceptorType.AcceptanceCommittee })
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

                               return new ContractGuaranteeReturnCommitteeReplace(
                                   action,
                                   a.FullName,
                                   a.PositionName,
                                   string.Empty);
                           })
                : new List<ContractGuaranteeReturnCommitteeReplace>();

        var processType = SectionProcessType.ContractGuaranteeReturn;

        var lastedAssignee = guarantee.Assignees
                                      .Select(DelegatorExtensions.DelegatorToAssignee)
                                      .LastOrDefault(x => x.Type == AssigneeType.Assignee);

        var lastCommitteeAssignee = guarantee.Acceptors
                                             .LastOrDefault(x => x.Type == AcceptorType.AcceptanceCommittee);

        var managers = await this.operationService.GetDefaultAcceptorPositionAsync(
            processType,
            lastedAssignee?.UserId.Value ?? lastCommitteeAssignee?.UserId.Value ?? Guid.Empty,
            guarantee.ReturnAmount,
            "SMethod002",
            null,
            ct);

        var sectionApproveName = this.operationService.AddPositionNamePrefix(managers)
            .Select(m => new SectionApprove(m.PositionName))
            .DefaultIfEmpty(new SectionApprove(string.Empty));

        var creator =
            hasCreator && lastedAssignee is not null
                ? await this.dbContext.SuUsers
                            .Where(u => u.Id == lastedAssignee.UserId)
                            .Select(u => new ContractGuaranteeReturnCreatorDto(
                                "ผู้จัดทำ",
                                u.Employee.Signature,
                                u.FullName,
                                u.Employee.View!.FullPositionName))
                            .FirstOrDefaultAsync(ct)
                : null;

        var publisher = guarantee.Acceptors
                                 .Where(a => a.CommitteePositionsCode == ParameterCode.From("PosBoard001"))
                                 .OrderBy(a => a.Sequence)
                                 .Select(a =>
                                     new ContractGuaranteeReturnAcceptorReplace(
                                         string.Empty,
                                         a.FullName,
                                         a.CommitteePosition?.Label ?? string.Empty,
                                         string.Empty))
                                 .FirstOrDefault();

        var defaultPublisher =
            publisher != null
                ? publisher
                : guarantee.Acceptors
                           .Where(a => a.CommitteePositionsCode == ParameterCode.From("PosBoardInsp001"))
                           .OrderBy(a => a.Sequence)
                           .Select(a =>
                               new ContractGuaranteeReturnAcceptorReplace(
                                   string.Empty,
                                   a.FullName,
                                   a.CommitteePosition?.Label ?? string.Empty,
                                   string.Empty))
                           .FirstOrDefault();

        var dtoGuarantee = new ContractGuaranteeReturnReplaceDto(
            guarantee.Id.Value,
            guarantee.GuaranteeReturnDate.ToThaiDateString(includeBuddhistEra: false),
            guarantee.ReturnAmount,
            guarantee.IsDeducted,
            guarantee.DeductedAmount,
            guarantee.NetReturnAmount,
            guarantee.AdditionalComment,
            guarantee.Status,
            lastedApprovalCmContractGuaranteeReturn?.FileId.Value,
            lastedContractGuaranteeReturnResult?.FileId.Value,
            guarantee.Assignees
                     .OrderBy(a => a.Sequence)
                     .Select(a => new ContractGuaranteeReturnAssigneeResponse(
                         a.Status.ToString(),
                         a.FullName,
                         a.PositionName,
                         a.DelegateeId.Value.ToString())),
            acceptor,
            committee,
            creator,
            hasPublisher ? defaultPublisher : null,
            guarantee.Conditions
                     .OrderBy(o => o.Sequence)
                     .Select(c => new ConditionReplaceDto(c.Id.Value, c.Sequence, c.Description, c.IsSatisfied)),
            guarantee.RequiredDocuments
                     .OrderBy(o => o.Sequence)
                     .Select(d => new RequiredDocumentReplaceDto(d.Id.Value, d.Sequence, d.DocumentName, d.IsSubmitted)),
            [
                .. guarantee.Attachments
                            .OrderBy(o => o.Sequence)
                            .GroupBy(
                                a => a.DocumentTypeCode,
                                (key, g) => new AttachmentsDtoWithId(
                                    key.Value,
                                    [.. g.Select(s => new FileAttachmentsWithId(s.Id.Value, s.FileId.Value, s.FileName, s.Sequence, s.IsPublic, s.AuditInfo.CreatedBy))]))
            ]);

        var deliveryAcceptancesData = await this.dbContext.CmDeliveryAcceptances
                                                .Include(x => x.Periods)
                                                .ThenInclude(p => p.PaymentTerms)
                                                .Where(p => p.SourceType == SourceType.ContractDraftVendor && p.RefId == entity.Id.Value)
                                                .FirstOrDefaultAsync(ct);

        var lastPeriodPaymentTerm = deliveryAcceptancesData?
                                    .Periods.SelectMany(x => x.PaymentTerms.Select(p => new { Period = x, PaymentTerm = p }))
                                    .OrderByDescending(x => x.PaymentTerm.PaymentTerm)
                                    .FirstOrDefault();

        var receiveDate = lastPeriodPaymentTerm?.Period.Acceptors
                                               .Where(a => a is { Type: AcceptorType.Approver, Status: AcceptorStatus.Approved })
                                               .OrderBy(a => a.Sequence)
                                               .LastOrDefault()
                                               ?.ActionAt?.ToThaiDateString();

        var paidDate = lastPeriodPaymentTerm?.Period.DisbursementDate?.ToThaiDateString();

        var isReplaceAcceptorDate = guarantee.Status == CmContractGuaranteeReturnStatus.WaitingAcceptance || guarantee.Status == CmContractGuaranteeReturnStatus.Approved;

        var warrantyParts = new List<string>();

        if (entity.DraftTermsConditions.Warranty.WarrantyPeriod?.Year > 0)
        {
            warrantyParts.Add($"{entity.DraftTermsConditions.Warranty.WarrantyPeriod.Year} ปี");
        }

        if (entity.DraftTermsConditions.Warranty.WarrantyPeriod?.Month > 0)
        {
            warrantyParts.Add($"{entity.DraftTermsConditions.Warranty.WarrantyPeriod.Month} เดือน");
        }

        if (entity.DraftTermsConditions.Warranty.WarrantyPeriod?.Day > 0)
        {
            warrantyParts.Add($"{entity.DraftTermsConditions.Warranty.WarrantyPeriod.Day} วัน");
        }

        var warranty = string.Join(" ", warrantyParts);

        var warrantyDueDate = entity.DraftTermsConditions.Warranty.WarrantyEndDate.ToThaiDateString();

        var bankOrderDescription = guarantee.ReturnAmount <= 1000000m
            ? "ตามคำสั่งธนาคารฯ ที่ 285/2552 เรื่อง ระเบียบปฏิบัติงานการบริหารงานทั่วไป ข้อ 1.24.2 กรณีที่หลักประกันสัญญามีวงเงินไม่เกิน 1,000,000.-บาท อยู่ในอำนาจ ผู้อำนวยการฝ่ายจัดหาและการพัสดุ อนุมัติได้"
            : "ตามคำสั่งธนาคาร ที่ 285/2552 เรื่อง ระเบียบปฏิบัติงานการบริหารงานทั่วไป ข้อ 1.24.3 กรณีที่หลักประกันสัญญามีวงเงินเกิน 1,000,000.00 บาท อยู่ในอำนาจรองกรรมการผู้จัดการ หรือ ผู้ช่วยกรรมการผู้จัดการที่ควบคุม กำกับ ดูแลฝ่ายจัดหาและการพัสดุ อนุมัติได้";

        return new ContractVendorGuaranteeReturnReplaceDto(
            entity.Id,
            suVendor.TaxpayerIdentificationNo,
            suVendor.EstablishmentName,
            suVendor.Email,
            entity.ContractNumber,
            entity.PoNumber,
            entity.Budget.ToCurrencyStringWithComma(),
            entity.Budget.ThaiBahtText(),
            entity.ContractName,
            entity.ContractType?.Label,
            entity.Template?.Label,
            entity.ContractSignedDate.ToThaiDateString(),
            entity.Delivery?.LeadTime,
            entity.Delivery?.LeadTimeTypeCode,
            entity.Delivery?.LeadTimeType?.Label,
            entity.Delivery?.Date.ToThaiDateString(),
            dtoGuarantee,
            ContractDraftInfoReplaceDto.FromEntity(entity),
            guarantee.ContractDescription,
            guarantee.ProofOfPaymentDescription,
            guarantee.GuranteeDescription,
            warranty,
            warrantyDueDate,
            receiveDate,
            paidDate,
            isReplaceAcceptorDate ? entity.DocumentDate?.ToThaiDateString() ?? DateTimeOffset.UtcNow.ToThaiDateString() : null,
            creator,
            acceptor,
            sectionApproveName,
            bankOrderDescription);
    }

    protected record GuaranteeReturnDocumentOptions(
        bool IsApprovalReplace,
        bool IsResultReplace,
        bool HasCreator = false,
        bool HasAcceptor = false,
        bool HasPublisher = false,
        bool HasCommittee = false,
        bool IsMarkReplaced = false);

    protected async ValueTask<(FileId? ApprovalFileId, FileId? ReturnFileId)> UpdateDocumentAsync(
        CaContractDraftVendor entity,
        CmContractGuaranteeReturn guarantee,
        SuVendor suVendor,
        GuaranteeReturnDocumentOptions options,
        CancellationToken ct = default)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var supplyMethodCode = entity.ContractDraft.Procurement.SupplyMethodCode;

        var lastedApprovalApprovalDocument = guarantee.GetApprovalDocumentForStatus(guarantee.Status);

        var lastedContractDocument = guarantee.GetResultDocumentForStatus(guarantee.Status);

        if (lastedApprovalApprovalDocument is null || lastedContractDocument is null)
        {
            this.ThrowError(
                $"ไม่พบเอกสารร่าง ที่ต้องการอัปโหลด",
                StatusCodes.Status404NotFound);
        }

        var approvalFileId = await ReplaceDocument(
            lastedApprovalApprovalDocument.FileId,
            CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn,
            options.IsApprovalReplace);
        var contractFileId = await ReplaceDocument(
            lastedContractDocument.FileId,
            CmContractGuaranteeReturnDocumentType.CmContractGuaranteeReturnResule,
            options.IsResultReplace);

        var isDocumentReplaced = options.IsMarkReplaced;

        guarantee.AddDocumentHistory(
            CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn,
            approvalFileId,
            isDocumentReplaced);

        guarantee.AddDocumentHistory(
            CmContractGuaranteeReturnDocumentType.CmContractGuaranteeReturnResule,
            contractFileId,
            isDocumentReplaced);

        return (approvalFileId, contractFileId);

        async Task<FileId> ReplaceDocument(FileId fileId, CmContractGuaranteeReturnDocumentType documentType, bool isReplace)
        {
            var replaceDto =
                await this.GetContractGuaranteeReturnMappingDtoAsync(entity, guarantee, suVendor, options.HasCreator, options.HasAcceptor, options.HasPublisher, options.HasCommittee, ct);

            var sourceFileId = isReplace
                ? await this.GetDocumentTemplateByTypeAsync(documentType, supplyMethodCode, ct)
                : fileId;

            var parentDirectory =
                $"{DocumentTemplateGroups.CMCltr}/{guarantee.Id}_{CmContractTerminationDocumentType.ContractTermination}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

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
    }

    protected async ValueTask SetDefaultDocumentTemplate(CmContractGuaranteeReturn contractGuaranteeReturn, ParameterCode supplyMethodCode, CancellationToken ct)
    {
        var approvalTemplateDocId = await this.GetDocumentTemplateByTypeAsync(
            CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn,
            supplyMethodCode,
            ct);

        var contractGuaranteeReturnTemplateDocId = await this.GetDocumentTemplateByTypeAsync(
            CmContractGuaranteeReturnDocumentType.CmContractGuaranteeReturnResule,
            supplyMethodCode,
            ct);

        contractGuaranteeReturn.AddDocumentHistory(CmContractGuaranteeReturnDocumentType.CmContractGuaranteeReturnResule, contractGuaranteeReturnTemplateDocId, false);
        contractGuaranteeReturn.AddDocumentHistory(CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn, approvalTemplateDocId, false);
    }

    protected async Task<FileId> GetDocumentTemplateByTypeAsync(
        CmContractGuaranteeReturnDocumentType documentType,
        ParameterCode supplyMethodCode,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var contractTemplateCode = documentType switch
        {
            CmContractGuaranteeReturnDocumentType.ApprovalCmContractGuaranteeReturn => CMDocumentTemplatesConstant.CMGuaranteeReturnApprovalRequest,
            CmContractGuaranteeReturnDocumentType.CmContractGuaranteeReturnResule => CMDocumentTemplatesConstant.CMGuaranteeReturnResule,
            _ => CMDocumentTemplatesConstant.CMGuaranteeReturnApprovalRequest,
        };

        var templateFileId = await documentService.GetDocumentTemplateAsync(
            c => c.Group == DocumentTemplateGroups.CMCltr &&
                 c.SupplyMethodCode == supplyMethodCode &&
                 c.AdditionalInfo!.RootElement
                  .GetProperty(nameof(SuDocumentTemplate.ContractTemplateCode))
                  .GetString() == contractTemplateCode,
            ct);

        if (templateFileId == null)
        {
            this.ThrowError(
                DocumentErrorMessages.DocumentTemplateNotFound,
                StatusCodes.Status404NotFound);
        }

        return (FileId)templateFileId;
    }
}