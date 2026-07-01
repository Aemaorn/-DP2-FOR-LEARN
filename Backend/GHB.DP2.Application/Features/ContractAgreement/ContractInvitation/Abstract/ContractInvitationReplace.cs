namespace GHB.DP2.Application.Features.ContractAgreement.ContractInvitation.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.SystemUtility;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public abstract partial class ContractInvitationEndpointBase<TRequest, TResponse>
{
    protected CaContractInvitationVendors GetVendorReplaceById(
        CaContractInvitation contractInvitationExisting,
        Guid vendorId)
    {
        var vendor =
            contractInvitationExisting
                .Vendors
                .OrderBy(v =>
                    contractInvitationExisting.Procurement.Type is ProcurementType.Procurement
                        ? v.PurchaseOrderApprovalContract.Budget?.Sequence
                        : v.PurchaseOrderApprovalContract.PrincipleApprovalRentalBudget?.Sequence)
                .ThenBy(o => o.PurchaseOrderApprovalContract.Sequence)
                .FirstOrDefault(v => v.Id == ContractInvitationVendorsId.From(vendorId));

        if (vendor is null)
        {
            this.ThrowError($"ไม่พบข้อมูลผู้ขายที่มีรหัส {vendorId}", StatusCodes.Status404NotFound);
        }

        return vendor;
    }

    protected async Task<decimal> GetRevenueStampRateAsync(CancellationToken ct)
    {
        var parameterCode = ParameterCode.From(RevStampConstant.RevStamp001);

        var revenueStamp =
            await this.dbContext.SuParameters
                      .AsNoTracking()
                      .FirstOrDefaultAsync(
                          p => p.Code == parameterCode, ct);

        if (revenueStamp is null)
        {
            return 0.00m;
        }

        var revStamp001Values =
            revenueStamp?.Values
                        .FirstOrDefault(v => v.Key == "Values")
                        .Value
                        .Value?.ToString();

        if (!decimal.TryParse(revStamp001Values, out var revenueStampRate))
        {
            revenueStampRate = 0.00m;
        }

        return revenueStampRate;
    }

    protected async Task<ContractInvitationVendorReplaceDto> MapToInvitationVendorReplace(
        Guid contractInvitationId,
        Guid vendorId,
        Guid procurementId,
        bool hasAcceptor,
        CancellationToken ct)
    {
        var contractInvitationExisting =
            await this.GetById(
                ContractInvitationId.From(contractInvitationId),
                ProcurementId.From(procurementId),
                ct);

        var supplyMethodSpecialType =
            contractInvitationExisting.Procurement
                                      .SupplyMethodSpecialType;

        var vendor =
            this.GetVendorReplaceById(
                contractInvitationExisting,
                vendorId);

        var revenueStampRate =
            await this.GetRevenueStampRateAsync(ct);

        var vendorMapResult =
            await this.MapToInvitationVendorReplaceAsync(
                contractInvitationExisting.Procurement.Type,
                vendor,
                revenueStampRate,
                supplyMethodSpecialType,
                hasAcceptor,
                ct);

        return vendorMapResult;
    }

    private async Task<ContractInvitationVendorReplaceDto> MapToInvitationVendorReplaceAsync(
        ProcurementType type,
        CaContractInvitationVendors ciVendor,
        decimal revenueStampRate,
        SuParameter? supplyMethodSpecialType,
        bool hasAcceptor,
        CancellationToken ct)
    {
        var vendor = type switch
        {
            ProcurementType.Procurement => ciVendor.PurchaseOrderApprovalContract.Entrepreneur != null ? ciVendor.PurchaseOrderApprovalContract.Entrepreneur?.SuVendor : ciVendor.PurchaseOrderApprovalContract.PPurchaseOrderApprovalEntrepreneurs?.Vendor,
            ProcurementType.Rent => ciVendor.PurchaseOrderApprovalContract.PrincipleApprovalRentalEntrepreneurs?.Vendor,
            _ => null,
        };

        var documentId =
            ciVendor.DocumentHistories
                    .OrderVersions()
                    .FirstOrDefault();

        var dateNowSource = ciVendor.DocumentDate ?? DateTimeOffset.UtcNow;

        var monthYearNow =
            dateNowSource.ToThaiDateString(format: "MMMM yyyy");

        var dateNow =
            dateNowSource.ToThaiDateString(format: "dd MMMM yyyy");

        var acceptor = await this.operationService.GetDefaultJorPorDirectorAsync(ct);

        var acceptorReplace =
            hasAcceptor && acceptor is not null
                ? new AcceptorReplaceDto(
                    acceptor.Signature,
                    acceptor.FullName,
                    acceptor.FullPositionName)
                : null;

        var assigneeName =
            ciVendor.ContractInvitation
                    .Procurement
                    .PurchaseOrderApprovals.FirstOrDefault()?
                    .Assignees.Select(DelegatorExtensions.DelegatorToAssignee)
                    .MaxBy(a => a.Sequence)?
                    .FullName ?? string.Empty;

        var revenueStampAmount = ciVendor.AgreedPrice * (revenueStampRate / 100);

        var contractGuaranteePercent =
            ciVendor.ContractGuaranteePercent is null
                ? "..................."
                : ciVendor.ContractGuaranteePercent.Value % 1 == 0
                    ? ((int)ciVendor.ContractGuaranteePercent.Value).ToString()
                    : ciVendor.ContractGuaranteePercent.ToCurrencyStringWithComma();

        var contractGuarantee = ciVendor.HasContractGuarantee
                                ? string.Format(
                                   "และหลักประกันสัญญาร้อยละ {0} ของราคาทั้งหมด ตามสัญญา เป็นจำนวนเงิน {1} บาท ({2})",
                                   contractGuaranteePercent,
                                   ciVendor.GuaranteeAmount.ToCurrencyStringWithComma(),
                                   ciVendor.GuaranteeAmount.ThaiBahtText())
                                : string.Empty;

        var result =
            new ContractInvitationVendorReplaceDto(
                ciVendor.Id,
                ciVendor.PurchaseOrderApprovalContractId,
                documentId?.FileId.Value,
                vendor != null ? vendor.EstablishmentName : string.Empty,
                ciVendor.Email,
                ciVendor.ContractName,
                ciVendor.PoNumber,
                ciVendor.ContractNumber,
                ciVendor.AgreedPrice.ToCurrencyStringWithComma(),
                ciVendor.AgreedPrice.ThaiBahtText(),
                ciVendor.HasContractGuarantee,
                contractGuarantee,
                contractGuaranteePercent,
                ciVendor.GuaranteeAmount.ToCurrencyStringWithComma() ?? "...................",
                ciVendor.GuaranteeAmount is not null ? ciVendor.GuaranteeAmount.ThaiBahtText() : "...................",
                ciVendor.ContractOfficerName,
                ciVendor.ContractOfficerPhone,
                ciVendor.ContractOfficerEmail,
                ciVendor.EgpResult ?? false,
                ciVendor.EgpRemark,
                ciVendor.EgpDate,
                vendor != null ? MapToEntrepreneurReplace(vendor) : null,
                monthYearNow,
                dateNow,
                supplyMethodSpecialType?.Label ?? string.Empty,
                acceptorReplace,
                assigneeName,
                revenueStampAmount.ToCurrencyStringWithComma(),
                revenueStampAmount.ThaiBahtText());

        return result;
    }

    protected static VendorInfoReplaceDto MapToEntrepreneurReplace(SuVendor vendor)
    {
        return
            new VendorInfoReplaceDto(
                vendor.Id,
                vendor.Nationality,
                vendor.Type,
                vendor.EntrepreneurType,
                vendor.EntrepreneurTypeInfo.Label,
                vendor.TaxpayerIdentificationNo,
                vendor.EstablishmentName,
                vendor.Tel,
                vendor.Fax,
                vendor.SapVendorNumber,
                vendor.SapBranchNumber,
                vendor.Email);
    }

    protected async ValueTask UpdateDocumentAsync(
        CaContractInvitationVendors vendor,
        bool isResetDocument,
        bool hasAcceptor,
        ContractInvitationStatus status,
        CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var lastedDraftContractDraftDocument = vendor.LastedDraftContractDraftDocument;

        if (lastedDraftContractDraftDocument is null)
        {
            this.ThrowError(
                $"ไม่พบเอกสารร่างที่ต้องการอัปโหลด",
                StatusCodes.Status404NotFound);
        }

        var contractInvitationFileId =
            await ReplaceDocument(
                lastedDraftContractDraftDocument.FileId,
                CaContractInvitationDocumentType.ContractInvitation);

        this.UpdateDocumentHistory(
            CaContractInvitationDocumentType.ContractInvitation,
            vendor,
            status,
            contractInvitationFileId);

        return;

        async Task<FileId> ReplaceDocument(
            FileId fileId,
            CaContractInvitationDocumentType documentType)
        {
            if (isResetDocument)
            {
                fileId =
                    await this.GetDocumentTemplateByCriteria(
                        vendor.DocumentTemplateCode.Value,
                        vendor.HasContractGuarantee,
                        ct);
            }

            var procurementType =
                vendor.ContractInvitation
                      .Procurement.Type;

            var supplyMethodSpecialType =
                vendor.ContractInvitation
                      .Procurement
                      .SupplyMethodSpecialType;

            var revenueStampRate =
                await this.GetRevenueStampRateAsync(ct);

            var replaceDto =
                await this.MapToInvitationVendorReplaceAsync(
                    procurementType,
                    vendor,
                    revenueStampRate,
                    supplyMethodSpecialType,
                    hasAcceptor,
                    ct);

            var parentDirectory =
                $"{DocumentTemplateGroups.PlanAnnouncement}/{vendor.Id}_{documentType.ToString()}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

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

    protected void UpdateDocumentHistory(
        CaContractInvitationDocumentType documentType,
        CaContractInvitationVendors contractInvitationVendors,
        ContractInvitationStatus contractInvitationStatus,
        FileId fileId,
        bool? isReplace = false)
    {
        var lastHistory = contractInvitationVendors.DocumentHistories
                                                   .OrderVersions()
                                                   .FirstOrDefault();

        if (lastHistory != null)
        {
            // var newVersion = RunningDocumentVersion.IncrementDocumentVersion(
            //    lastHistory.Version,
            //    lastHistory.StatusState.ToString(),
            //    contractInvitationStatus.ToString());
            var incrementMajor =
                lastHistory.StatusState != contractInvitationStatus;

            var newVersion =
                contractInvitationVendors.DocumentHistories
                    .NextVersion(incrementMajor);

            var addNewHistory = CaContractInvitationVendorsDocumentHistory.Create(
                documentType,
                contractInvitationStatus,
                newVersion,
                fileId,
                isReplace);

            contractInvitationVendors.AddDocumentHistory(addNewHistory);

            return;
        }

        var addHistory = CaContractInvitationVendorsDocumentHistory.Create(
            documentType,
            contractInvitationStatus,
            "1.0",
            fileId,
            isReplace);

        contractInvitationVendors.AddDocumentHistory(addHistory);
    }
}