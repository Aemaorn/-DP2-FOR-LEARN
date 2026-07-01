namespace GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition.Abstract;

using System.ComponentModel;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;
using GreatFriends.ThaiBahtText;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public record CertificateRequisitionReplace(
    [property: Description("เลขที่ใบรับรอง")]
    string? CertificateNo,
    [property: Description("ชื่อสัญญา")] string ContractName,
    [property: Description("เลขที่สํญญา")] string ContractNumber,
    [property: Description("วันที่ลงนามสัญญา")]
    string ContractSignedDate,
    [property: Description("ข้อมูลคู่ค้าสัญญา")]
    ContractDraftInfoDetail ContractDraftInfoDetail,
    [property: Description("ผู้เห็นชอบ/อนุมัติ")]
    AcceptorReplace? Acceptor,
    [property: Description("วันที่ส่งเห็นชอบ")]
    string? AcceptorDate,
    [property: Description("วิธีการจัดซื้อจัดจ้าง ซื้อ/จ้าง/เช่า")]
    string SupplyMethodTypeLabel);

public record ContractDraftInfoDetail(
    [property: Description("ข้อมูลผู้ประกอบการ")]
    ContractVendor Vendor,
    [property: Description("ราคาที่ตกลง")] Agreement Agreement);

public record ContractVendor(
    string Name);

public record Agreement(
    [property: Description("ราคา(ตัวเลข)")]
    string TotalAmountFormat,
    [property: Description("ราคา(ข้อความ)")]
    string TotalAmountText);

public record AcceptorReplace(
    [property: Description("ลายเซ็นต์")] string Signature,
    [property: Description("ชื่อ-สกุล")] string FullName,
    [property: Description("ตำแหน่ง")] string FullPositionName);

public abstract partial class CertificateRequisitionEndpointBase<TRequest, TResponse>
{
    protected async Task UpdateDocumentTemplateAsync(
        CamCertificateRequisition certificateRequisition,
        CancellationToken ct,
        bool? isResetDocument = false)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var isAllPendingOrDraft = certificateRequisition.Acceptors
                                                        .Where(a =>
                                                            a is
                                                            {
                                                                IsUnableToPerformDuties: false,
                                                                IsActive: true,
                                                            })
                                                        .All(a => a.Status is AcceptorStatus.Pending or AcceptorStatus.Draft);

        var isApproved = certificateRequisition.Acceptors
                                               .Where(a =>
                                                   a is
                                                   {
                                                       IsUnableToPerformDuties: false,
                                                       IsActive: true,
                                                   })
                                               .Any(a => a.Status == AcceptorStatus.Approved);

        var isReplace = (certificateRequisition.Status, isAllPendingOrDraft, isApproved) switch
        {
            (CamCertificateRequisitionStatus.WaitingForCommitteeApproval, true, false) => true,
            (CamCertificateRequisitionStatus.Approved, false, true) => true,
            _ => false,
        };

        var lastedDocument = certificateRequisition.LastedDocumentHistory;

        if (lastedDocument is null)
        {
            this.ThrowError(
                "ไม่พบเอกสารใบรับรองผลงาน",
                StatusCodes.Status404NotFound);
        }

        FileId sourceFileId;

        if (isResetDocument.GetValueOrDefault())
        {
            var templateFileId = await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.CertificateRequisition &&
                    d.IsActive,
                ct);

            if (templateFileId is null)
            {
                this.ThrowError(
                    "ไม่พบแม่แบบเอกสารสำหรับใบขอหนังสือรับรอง.",
                    StatusCodes.Status404NotFound);
            }

            sourceFileId = templateFileId.Value;
        }
        else
        {
            sourceFileId = lastedDocument.FileId;
        }

        var dtoReplace = await this.MapReplaceAsync(certificateRequisition, ct);

        var documentTemplate = await documentService.CopyDocumentTemplateAsync(
            sourceFileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, dtoReplace),
            parentDirectory: $"{DocumentTemplateGroups.CertificateRequisition}/{dtoReplace.CertificateNo}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (documentTemplate is null)
        {
            this.ThrowError(
                "ไม่สามารถอัพเดทเอกสารขอใบรับรองได้",
                StatusCodes.Status500InternalServerError);
        }

        certificateRequisition.AddDocumentHistory(documentTemplate.Value, isReplace);
    }

    protected void RevertDocumentTemplateSectionAsync(CamCertificateRequisition certificateRequisition)
    {
        var lastedDocument = certificateRequisition.LastedReplacedDocument;

        if (lastedDocument is null)
        {
            this.ThrowError(
                "ไม่พบเอกสารใบรับรองผลงาน",
                StatusCodes.Status404NotFound);
        }

        certificateRequisition.AddDocumentHistory(lastedDocument.FileId);
    }

    protected async Task<CertificateRequisitionReplace> MapReplaceAsync(
        CamCertificateRequisition entity,
        CancellationToken ct)
    {
        var operationService = this.Resolve<IOperationService>();

        var acceptorDate =
            entity.Status == CamCertificateRequisitionStatus.Approved
                ? DateTime.UtcNow.ToThaiDateString()
                : null;

        var acceptor =
            entity.Status == CamCertificateRequisitionStatus.Approved
                ? entity.Acceptors.Where(e => e.IsBoardChairman())
                        .Select(DelegatorExtensions.DelegatorToAcceptor)
                        .Map(s => new AcceptorReplace(
                            s.Signature ?? string.Empty,
                            s.FullName,
                            s.PositionName))
                        .FirstOrDefault()
                : null;

        if (entity.ContractDraftVendor is null)
        {
            var budget = entity.Budget ?? 0m;
            return new CertificateRequisitionReplace(
                entity.CertificateNo.Value,
                entity.ContractName ?? string.Empty,
                entity.ContractNumber ?? string.Empty,
                entity.ContractSignedDate?.ToThaiDateString() ?? string.Empty,
                new ContractDraftInfoDetail(
                    new ContractVendor(entity.EntrepreneurName ?? string.Empty),
                    new Agreement(
                        budget.ToCurrencyStringWithComma(),
                        budget.ThaiBahtText())),
                acceptor,
                acceptorDate,
                entity.SupplyMethodType?.Label ?? string.Empty);
        }

        var contractVendor = entity.ContractDraftVendor;

        var vendor =
            await this.dbContext.SuVendors
                      .FirstOrDefaultAsync(
                          v => v.Id == contractVendor.Vendor.VendorId,
                          ct);

        var supplyMethodTypeLabel =
            contractVendor
                .ContractDraft
                .Procurement
                .SupplyMethodType?.Label ?? string.Empty;

        return new CertificateRequisitionReplace(
            entity.CertificateNo.Value,
            contractVendor.ContractName,
            contractVendor.ContractNumber,
            contractVendor.ContractSignedDate.ToThaiDateString(),
            new ContractDraftInfoDetail(
                new ContractVendor(
                    vendor?.EstablishmentName ?? string.Empty),
                new Agreement(
                    contractVendor.Budget.ToCurrencyStringWithComma(),
                    contractVendor.Budget.ThaiBahtText())),
            acceptor,
            acceptorDate,
            supplyMethodTypeLabel);
    }
}