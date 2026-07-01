namespace GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Abstract;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Dto;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract partial class ContractDraftEndpointBase<TRequest, TResponse> : TransactionalEndpointBase<TRequest, TResponse>
    where TResponse : IResult
    where TRequest : notnull
{
    private static readonly IReadOnlyDictionary<string, string> ContractTemplateCodeMap = new Dictionary<string, string>
    {
        ["CMRentalTpl001"] = "CARentArea80",
        ["CMRentalTpl002"] = "CARentBuilding80",
        ["CMRentalTpl003"] = "CARentParking80",
        ["CMRentalTpl004"] = "CARentBillboard80",
    };

    private static class ErrorMessages
    {
        public const string ProcurementNotFound = "ไม่พบข้อมูลการจัดซื้อจัดจ้าง";
        public const string ContractDraftNotFound = "ไม่พบข้อมูลร่างสัญญา";
        public const string ContractTemplateNotFound = "ไม่พบเอกสารแม่แบบสำหรับร่างสัญญา";
        public const string ApprovalTemplateNotFound = "ไม่พบเอกสารแม่แบบสำหรับร่างสัญญาอนุมัติ";
        public const string ConfidentialTemplateNotFound = "ไม่พบเอกสารแม่แบบสำหรับร่างสัญญาความลับ";
        public const string ContractDraftCopyFailed = "ไม่สามารถคัดลอกเอกสารแม่แบบร่างสัญญาได้";
        public const string ApprovalDraftCopyFailed = "ไม่สามารถคัดลอกเอกสารแม่แบบร่างสัญญาอนุมัติได้";
        public const string ConfidentialDraftCopyFailed = "ไม่สามารถคัดลอกเอกสารแม่แบบร่างสัญญาความลับได้";
    }

    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;
    private readonly ICommandTextService commandTextService;
    private readonly IFileServiceClient fileServiceClient;

    protected ContractDraftEndpointBase(
        ILogger logger,
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
        this.commandTextService = commandTextService;
        this.fileServiceClient = fileServiceClient;
    }

    protected async Task ValidateProcurementAsync(Guid procurementId, CancellationToken ct)
    {
        var procurementExists = await this.dbContext
                                          .Procurements
                                          .AnyAsync(p => p.Id == ProcurementId.From(procurementId), ct);

        if (!procurementExists)
        {
            this.ThrowError(
                ErrorMessages.ProcurementNotFound,
                StatusCodes.Status404NotFound);
        }
    }

    protected async Task<CaContractDraft> QueryContractDraftsAsync(
        Guid procurementId,
        Guid? contractDraftId = null,
        CancellationToken ct = default)
    {
        var contractDraft = await this.dbContext
                                      .CaContractDrafts
                                      .Include(c => c.Vendors)
                                      .ThenInclude(v => v.Attachments)
                                      .Include(c => c.Vendors)
                                      .ThenInclude(v => v.PaymentTerms)
                                      .Include(c => c.Vendors)
                                      .ThenInclude(v => v.DraftTermsConditions)
                                      .Include(c => c.Vendors)
                                      .ThenInclude(r => r.Vendor)
                                      .ThenInclude(r => r.VendorInfo)
                                      .AsSingleQuery()
                                      .Where(c => c.ProcurementId == ProcurementId.From(procurementId))
                                      .WhereIfTrue(
                                          !contractDraftId.IsNull(),
                                          c => c.Id == ContractDraftId.From(contractDraftId.Value))
                                      .FirstOrDefaultAsync(ct);

        if (contractDraft == null)
        {
            this.ThrowError(
                ErrorMessages.ContractDraftNotFound,
                StatusCodes.Status404NotFound);
        }

        return contractDraft;
    }

    private static string? GetMappedContractTemplateCode(string? templateCode)
    {
        if (templateCode == null)
        {
            return templateCode;
        }

        return ContractTemplateCodeMap.GetValueOrDefault(templateCode, templateCode);
    }

    private static bool CheckIsConfidential(IEnumerable<SuParameter> parameters)
    {
        var checkResult = parameters.Any(c => c.Values.TryGetValue("IsPDPA", out var isPdpaValue) && isPdpaValue.Value?.ToString() == "True");

        return checkResult;
    }

    private async Task<FileId?> ProcessDocumentCopy(
        FileId fileId,
        string fileName,
        bool isChange,
        CancellationToken cancellationToken)
    {
        if (!isChange)
        {
            return fileId;
        }

        var documentService = this.Resolve<IDocumentService>();

        return await documentService.CopyDocumentTemplateAsync(
            fileId,
            parentDirectory: $"{DocumentTemplateGroups.CA}/{fileName}",
            cancellationToken: cancellationToken);
    }

    protected async Task AddDocumentTemplateAsync(CaContractDraftVendor vendor)
    {
        var documentService = this.Resolve<IDocumentService>();
        var supplyMethodCode = vendor.ContractDraft.Procurement.SupplyMethodCode;
        var contractTemplateCode = (string?)vendor.TemplateCode;
        var mappedTemplateCode = GetMappedContractTemplateCode(contractTemplateCode);

        // Add main contract template
        var contractTemplate = await this.GetContractTemplateAsync(documentService, vendor, supplyMethodCode.Value, contractTemplateCode, mappedTemplateCode);
        AddDocumentToVendor(vendor, CaContractDraftVendorDocumentType.ContractDraft, contractTemplate);

        // Add approval template
        var approvalTemplate = await this.GetApprovalTemplateAsync(documentService, supplyMethodCode.Value);
        AddDocumentToVendor(vendor, CaContractDraftVendorDocumentType.ApprovalContractDraft, approvalTemplate);
    }

    protected async Task AddConfidentialDocumentTemplateAsync(CaContractDraftVendor vendor)
    {
        var documentService = this.Resolve<IDocumentService>();
        var supplyMethodCode = vendor.ContractDraft.Procurement.SupplyMethodCode;

        // Check if confidential template is needed
        var isConfidential = await this.CheckIfConfidentialTemplateNeededAsync(vendor);

        // Add confidential template if needed
        if (isConfidential)
        {
            var confidentialTemplate = await this.GetConfidentialTemplateAsync(documentService, supplyMethodCode.Value);
            AddDocumentToVendor(vendor, CaContractDraftVendorDocumentType.ConfidentialContractDraft, confidentialTemplate);
        }
    }

    private async Task<bool> CheckIfConfidentialTemplateNeededAsync(CaContractDraftVendor vendor)
    {
        var attachmentTypes = vendor.Attachments.Select(a => a.TypeCode).ToArray();
        var parameters = await this.dbContext
                                   .SuParameters
                                   .Where(p => attachmentTypes.Contains(p.Code))
                                   .ToListAsync(CancellationToken.None);

        return CheckIsConfidential(parameters);
    }

    private async Task<FileId> GetContractTemplateAsync(
        IDocumentService documentService,
        CaContractDraftVendor vendor,
        string supplyMethodCode,
        string? contractTemplateCode,
        string? mappedTemplateCode)
    {
        var template = vendor.ContractDraft.Procurement.Type == ProcurementType.Procurement
            ? await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.CA &&
                    d.SupplyMethodCode == ParameterCode.From(supplyMethodCode) &&
                    d.AdditionalInfo!.RootElement
                     .GetProperty(nameof(SuDocumentTemplate.ContractTemplateCode))
                     .GetString() == contractTemplateCode,
                CancellationToken.None)
            : await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.CA &&
                    d.SupplyMethodCode == ParameterCode.From(supplyMethodCode) &&
                    d.Code == mappedTemplateCode,
                CancellationToken.None);

        if (template == null)
        {
            this.ThrowError(ErrorMessages.ContractTemplateNotFound, StatusCodes.Status404NotFound);
        }

        return template.Value;
    }

    private async Task<FileId> GetApprovalTemplateAsync(IDocumentService documentService, string supplyMethodCode)
    {
        var template = await documentService.GetDocumentTemplateAsync(
            d =>
                d.Group == DocumentTemplateGroups.CA &&
                d.SupplyMethodCode == ParameterCode.From(supplyMethodCode) &&
                d.AdditionalInfo!.RootElement
                 .GetProperty(nameof(SuDocumentTemplate.IsApproval))
                 .GetBoolean(),
            CancellationToken.None);

        if (template == null)
        {
            this.ThrowError(ErrorMessages.ApprovalTemplateNotFound, StatusCodes.Status404NotFound);
        }

        return template.Value;
    }

    private async Task<FileId> GetConfidentialTemplateAsync(IDocumentService documentService, string supplyMethodCode)
    {
        var template = await documentService.GetDocumentTemplateAsync(
            d =>
                d.Group == DocumentTemplateGroups.CA &&
                d.SupplyMethodCode == ParameterCode.From(supplyMethodCode) &&
                d.AdditionalInfo!.RootElement
                 .GetProperty(nameof(SuDocumentTemplate.IsConfidential))
                 .GetBoolean(),
            CancellationToken.None);

        if (template == null)
        {
            this.ThrowError(ErrorMessages.ConfidentialTemplateNotFound, StatusCodes.Status404NotFound);
        }

        return template.Value;
    }

    protected record DocumentUpdateOptions(
        Guid? DocumentContractId,
        bool IsDocumentContractIdReplace,
        Guid? DocumentApprovalId,
        bool IsDocumentApprovalIdReplace,
        Guid? DocumentConfidentialId,
        bool IsDocumentConfidentialIdReplace);

    private static void AddDocumentToVendor(
        CaContractDraftVendor vendor,
        CaContractDraftVendorDocumentType documentType,
        FileId fileId)
    {
        vendor.AddDocumentHistory(documentType, fileId, false);
    }

    protected async Task UpdateDocumentTemplateAsync(
        CaContractDraftVendor vendor,
        DocumentUpdateOptions options,
        bool isChange = false,
        CancellationToken cancellationToken = default)
    {
        await this.ProcessDocumentUpdateAsync(
            vendor.ContractDraftDocument,
            options.DocumentContractId,
            options.IsDocumentContractIdReplace,
            vendor,
            nameof(vendor.ContractDraftDocument),
            ErrorMessages.ContractDraftCopyFailed,
            isChange,
            cancellationToken);

        await this.ProcessDocumentUpdateAsync(
            vendor.ApprovedDocument,
            options.DocumentApprovalId,
            options.IsDocumentApprovalIdReplace,
            vendor,
            nameof(vendor.ApprovedDocument),
            ErrorMessages.ApprovalDraftCopyFailed,
            isChange,
            cancellationToken);

        await this.ProcessDocumentUpdateAsync(
            vendor.ConfidentialDocument,
            options.DocumentConfidentialId,
            options.IsDocumentConfidentialIdReplace,
            vendor,
            nameof(vendor.ConfidentialDocument),
            ErrorMessages.ConfidentialDraftCopyFailed,
            isChange,
            cancellationToken);
    }

    private async Task ProcessDocumentUpdateAsync(
        CaContractDraftVendorDocumentHistory? document,
        Guid? replacementDocumentId,
        bool isReplace,
        CaContractDraftVendor vendor,
        string documentTypeName,
        string errorMessage,
        bool isChange,
        CancellationToken cancellationToken)
    {
        if (document is null)
        {
            return;
        }

        if (ShouldReplaceDocument(isReplace, replacementDocumentId))
        {
            AddDocumentHistoryForReplacement(vendor, document, replacementDocumentId!.Value, isReplace);

            return;
        }

        await this.ProcessDocumentCopyAndAddHistory(vendor, document, documentTypeName, errorMessage, isChange, cancellationToken);
    }

    private static bool ShouldReplaceDocument(bool isReplace, Guid? replacementDocumentId) =>
        isReplace && replacementDocumentId.HasValue;

    private static void AddDocumentHistoryForReplacement(
        CaContractDraftVendor vendor,
        CaContractDraftVendorDocumentHistory document,
        Guid replacementDocumentId,
        bool isReplace) =>
        vendor.AddDocumentHistory(
            document.DocumentType,
            FileId.From(replacementDocumentId),
            isReplace);

    private async Task ProcessDocumentCopyAndAddHistory(
        CaContractDraftVendor vendor,
        CaContractDraftVendorDocumentHistory document,
        string documentTypeName,
        string errorMessage,
        bool isChange,
        CancellationToken cancellationToken)
    {
        var fileName = $"{DocumentTemplateGroups.CA}/{documentTypeName}_{vendor.ContractNumber}.odt";
        var fileId = await this.ProcessDocumentCopy(document.FileId, fileName, isChange, cancellationToken);

        if (fileId is null)
        {
            this.ThrowError(errorMessage, StatusCodes.Status500InternalServerError);
        }

        vendor.AddDocumentHistory(document.DocumentType, fileId.Value, false);
    }

    protected async Task UpsertAttachments(CaContractDraftVendor entity, EntrepreneurResponseAttachment[] attachments)
    {
        var fileList = attachments
                       .SelectMany(r => r.FileAttachments.Select(f => new
                       {
                           f.Id,
                           r.DocumentTypeCode,
                           f.FileId,
                           f.FileName,
                           f.Sequence,
                           f.IsPublic,
                           f.Type,
                       }))
                       .ToArray();

        var incomingFileIds = fileList.Select(f => FileId.From(f.FileId)).ToHashSet();
        var existingFileIds = entity.CheckerAttachment.Select(a => a.FileId).ToHashSet();

        var removedAttachments = entity.CheckerAttachment
                                       .Where(a => !incomingFileIds.Contains(a.FileId))
                                       .ToArray();

        foreach (var attachment in removedAttachments)
        {
            entity.RemoveAttachment(attachment);
            await this.fileServiceClient.DeleteAsync(attachment.FileId, CancellationToken.None);
        }

        if (removedAttachments.Length > 0)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.DeleteFile,
                ActivityLogActionTypeConstant.DeleteFile,
                nameof(entity.Status),
                string.Join(", ", removedAttachments.Select(a => a.FileName))));
        }

        var newFiles = fileList.Where(f => !existingFileIds.Contains(FileId.From(f.FileId))).ToArray();

        newFiles.Map(f => CaContractDraftVendorCheckerAttachments.Create(
                    ParameterCode.From(f.DocumentTypeCode),
                    FileId.From(f.FileId),
                    f.FileName,
                    f.Type,
                    f.Sequence,
                    f.IsPublic))
                .Iter(r => entity.AddAttachment(r));

        if (newFiles.Length > 0)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.UploadFile,
                ActivityLogActionTypeConstant.UploadFile,
                nameof(entity.Status),
                string.Join(", ", newFiles.Select(f => f.FileName))));
        }

        foreach (var existing in entity.CheckerAttachment)
        {
            var match = fileList.FirstOrDefault(f => FileId.From(f.FileId) == existing.FileId);

            if (match != null)
            {
                existing.SetIsPublic(match.IsPublic)
                        .SetSequence(match.Sequence)
                        .SetDocumentType(ParameterCode.From(match.DocumentTypeCode));
            }
        }
    }

    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        CaContractDraftVendor vendor,
        CaContractDraftVendorDocumentType documentType,
        FileId fileId,
        bool? isReplaced = false,
        CancellationToken ct = default)
    {
        var latestHistory = vendor.DocumentHistories
                                  .Where(d => d.DocumentType == documentType)
                                  .OrderVersions()
                                  .FirstOrDefault();

        if (latestHistory == null)
        {
            return null;
        }

        var documentService = this.Resolve<IDocumentService>();
        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            fileId,
            parentDirectory: $"{DocumentTemplateGroups.CA}/{vendor.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}_{documentType}.odt",
            cancellationToken: ct);

        if (!copiedFileId.HasValue)
        {
            return null;
        }

        vendor.AddDocumentHistory(documentType, copiedFileId.Value, isReplaced);

        return copiedFileId.Value;
    }

    protected async Task ManageDocumentHistoryAsync(
        CaContractDraftVendor contractDraftVendor,
        CaContractDraftVendorDocumentType documentType,
        bool isReplace,
        CancellationToken ct)
    {
        if (contractDraftVendor.LastedDocumentByType(documentType) is null)
        {
            return;
        }

        var isConfidential = await this.CheckIfConfidentialTemplateNeededAsync(contractDraftVendor);

        if (documentType is CaContractDraftVendorDocumentType.ConfidentialContractDraft && !isConfidential)
        {
            contractDraftVendor.RemoveConfidentialDocument();

            return;
        }

        var documentService = this.Resolve<IDocumentService>();

        if (isReplace)
        {
            var supplyMethodCode = contractDraftVendor.ContractDraft.Procurement.SupplyMethodCode;
            var templateFileId = await this.GetDocumentTemplateForResetAsync(
                contractDraftVendor,
                documentType,
                supplyMethodCode,
                ct);

            var purchaseOrder = await this.dbContext
                                          .PPurchaseOrder
                                          .FirstOrDefaultAsync(
                                              v => v.ProcurementId == contractDraftVendor.ContractDraft.ProcurementId,
                                              ct);

            var currentUserId = Guid.TryParse(this.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var parsedUserId) ? parsedUserId : Guid.Empty;
            var user = await this.dbContext.SuUsers
                                 .Include(u => u.Employee)
                                 .ThenInclude(e => e.View)
                                 .Where(u => u.Id == UserId.From(currentUserId))
                                 .FirstOrDefaultAsync(ct);

            var creator = user != null
                ? new CreatorResponse(
                    "ผู้จัดทำ",
                    user.Employee.View?.FullName,
                    user.Employee.View?.FullPositionName)
                : null;

            await this.LoadNavigationPropertiesForReplaceAsync(contractDraftVendor, ct);

            var location = await this.GetLocationDtoByVendor(contractDraftVendor, ct);

            var replaceDto = GetVendorReplaceDto.FromEntity(
                contractDraftVendor,
                commandText: null,
                purchaseOrder,
                creator,
                hasCreator: creator != null,
                hasAcceptor: false,
                location: location);

            var parentDirectory =
                $"{DocumentTemplateGroups.CA}/{contractDraftVendor.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}_{documentType}.odt";

            // 6. Copy template document WITH placeholder replacement
            var newFileId = await documentService.CopyDocumentTemplateAsync(
                templateFileId,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: parentDirectory,
                cancellationToken: ct);

            if (newFileId is null)
            {
                this.ThrowError(
                    DocumentErrorMessages.CopyDocumentFailed,
                    StatusCodes.Status500InternalServerError);
            }

            contractDraftVendor.AddDocumentHistory(documentType, newFileId.Value, true, incrementMajor: true);

            return;
        }

        var lastedDocument = contractDraftVendor.LastedDocumentByType(documentType);

        if (lastedDocument is null)
        {
            return;
        }

        contractDraftVendor.AddDocumentHistory(documentType, lastedDocument.FileId!, false);
    }

    protected async Task<FileId> GetDocumentTemplateForResetAsync(
        CaContractDraftVendor vendor,
        CaContractDraftVendorDocumentType documentType,
        ParameterCode supplyMethodCode,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();
        FileId? fileId = null;

        if (documentType == CaContractDraftVendorDocumentType.ContractDraft)
        {
            var contractTemplateCode = (string?)vendor.TemplateCode;
            var mappedTemplateCode = GetMappedContractTemplateCode(contractTemplateCode);

            fileId = vendor.ContractDraft.Procurement.Type == ProcurementType.Procurement
                ? await documentService.GetDocumentTemplateAsync(
                    d =>
                        d.Group == DocumentTemplateGroups.CA &&
                        d.SupplyMethodCode == supplyMethodCode &&
                        d.AdditionalInfo!.RootElement
                         .GetProperty(nameof(SuDocumentTemplate.ContractTemplateCode))
                         .GetString() == contractTemplateCode,
                    parentDirectory: $"{DocumentTemplateGroups.CA}/{vendor.Id}_Reset_{DateTime.UtcNow:yyyyMMddHHmmss}_{documentType}.odt",
                    cancellationToken: ct)
                : await documentService.GetDocumentTemplateAsync(
                    d =>
                        d.Group == DocumentTemplateGroups.CA &&
                        d.SupplyMethodCode == supplyMethodCode &&
                        d.Code == mappedTemplateCode,
                    parentDirectory: $"{DocumentTemplateGroups.CA}/{vendor.Id}_Reset_{DateTime.UtcNow:yyyyMMddHHmmss}_{documentType}.odt",
                    cancellationToken: ct);
        }
        else if (documentType == CaContractDraftVendorDocumentType.ApprovalContractDraft)
        {
            fileId = await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.CA &&
                    d.SupplyMethodCode == supplyMethodCode &&
                    d.AdditionalInfo!.RootElement
                     .GetProperty(nameof(SuDocumentTemplate.IsApproval))
                     .GetBoolean(),
                parentDirectory: $"{DocumentTemplateGroups.CA}/{vendor.Id}_Reset_{DateTime.UtcNow:yyyyMMddHHmmss}_{documentType}.odt",
                cancellationToken: ct);
        }
        else if (documentType == CaContractDraftVendorDocumentType.ConfidentialContractDraft)
        {
            fileId = await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.CA &&
                    d.SupplyMethodCode == supplyMethodCode &&
                    d.AdditionalInfo!.RootElement
                     .GetProperty(nameof(SuDocumentTemplate.IsConfidential))
                     .GetBoolean(),
                parentDirectory: $"{DocumentTemplateGroups.CA}/{vendor.Id}_Reset_{DateTime.UtcNow:yyyyMMddHHmmss}_{documentType}.odt",
                cancellationToken: ct);
        }

        if (fileId == null)
        {
            this.ThrowError(
                $"ไม่พบเอกสารเทมเพลตสำหรับ {documentType}",
                StatusCodes.Status404NotFound);
        }

        return (FileId)fileId;
    }

    protected async Task LoadNavigationPropertiesForReplaceAsync(
        CaContractDraftVendor vendor,
        CancellationToken ct)
    {
        var parameterCodes = new List<ParameterCode>();

        var guarantee = vendor.DraftTermsConditions.Guarantee;

        if (guarantee?.TypeCode != null)
        {
            parameterCodes.Add(guarantee.TypeCode.Value);
        }

        if (guarantee?.BankCode != null)
        {
            parameterCodes.Add(guarantee.BankCode.Value);
        }

        var penalty = vendor.DraftTermsConditions.Penalty;

        if (penalty?.TypeCode != null)
        {
            parameterCodes.Add(penalty.TypeCode.Value);
        }

        if (penalty?.RateTypeCode != null)
        {
            parameterCodes.Add(penalty.RateTypeCode.Value);
        }

        var payment = vendor.Payment;

        if (payment?.TypeCode != null)
        {
            parameterCodes.Add(payment.TypeCode.Value);
        }

        if (payment?.RedeliveryDateCode != null)
        {
            parameterCodes.Add(payment.RedeliveryDateCode.Value);
        }

        if (parameterCodes.Count == 0)
        {
            return;
        }

        var parameters = await this.dbContext.SuParameters
                                   .Where(p => parameterCodes.Contains(p.Code))
                                   .ToDictionaryAsync(p => p.Code, ct);

        var termsConditionsEntry = this.dbContext.Entry(vendor.DraftTermsConditions);
        var guaranteeEntry = guarantee != null ? termsConditionsEntry.Reference(t => t.Guarantee).TargetEntry : null;
        var penaltyEntry = penalty != null ? termsConditionsEntry.Reference(t => t.Penalty).TargetEntry : null;
        var paymentEntry = payment != null ? this.dbContext.Entry(vendor).Reference(v => v.Payment).TargetEntry : null;

        if (guaranteeEntry != null && guarantee?.TypeCode != null && parameters.TryGetValue(guarantee.TypeCode.Value, out var guaranteeType))
        {
            guaranteeEntry.Reference(g => g.Type).CurrentValue = guaranteeType;
        }

        if (guaranteeEntry != null && guarantee?.BankCode != null && parameters.TryGetValue(guarantee.BankCode.Value, out var guaranteeBank))
        {
            guaranteeEntry.Reference(g => g.Bank).CurrentValue = guaranteeBank;
        }

        if (penaltyEntry != null && penalty?.TypeCode != null && parameters.TryGetValue(penalty.TypeCode.Value, out var penaltyType))
        {
            penaltyEntry.Reference(p => p.Type).CurrentValue = penaltyType;
        }

        if (penaltyEntry != null && penalty?.RateTypeCode != null && parameters.TryGetValue(penalty.RateTypeCode.Value, out var penaltyRateType))
        {
            penaltyEntry.Reference(p => p.RateType).CurrentValue = penaltyRateType;
        }

        if (paymentEntry != null && payment?.TypeCode != null && parameters.TryGetValue(payment.TypeCode.Value, out var paymentType))
        {
            paymentEntry.Reference(p => p.Type).CurrentValue = paymentType;
        }

        if (paymentEntry != null && payment?.RedeliveryDateCode != null && parameters.TryGetValue(payment.RedeliveryDateCode.Value, out var redeliveryDate))
        {
            paymentEntry.Reference(p => p.RedeliveryDate).CurrentValue = redeliveryDate;
        }
    }

    protected async Task<LocationDto?> GetLocationDtoByVendor(
        CaContractDraftVendor? vendor,
        CancellationToken ct)
    {
        if (vendor is null)
        {
            return null;
        }

        var provinceName = await this.dbContext
                                     .RawProvinces
                                     .Where(r => r.IsActive && r.Code == vendor.Vendor.RawProvinceCode)
                                     .Select(r => r.NameTh)
                                     .FirstOrDefaultAsync(ct);

        var districtName = await this.dbContext
                                     .RawDistricts
                                     .Where(r => r.IsActive && r.Code == vendor.Vendor.RawDistrictCode)
                                     .Select(r => r.NameTh)
                                     .FirstOrDefaultAsync(ct);

        var subDistrictName = await this.dbContext
                                        .RawSubDistricts
                                        .Where(r => r.IsActive && r.Code == vendor.Vendor.RawSubDistrictCode)
                                        .Select(r => r.NameTh)
                                        .FirstOrDefaultAsync(ct);

        return new LocationDto(provinceName, districtName, subDistrictName);
    }
}