namespace GHB.DP2.Application.Features.ContractAmendment.CertificateRequisition.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract partial class CertificateRequisitionEndpointBase<TRequest, TResponse>
    : EndpointBase<TRequest, TResponse>
    where TResponse : IResult
    where TRequest : notnull
{
    private readonly Dp2DbContext dbContext;

    protected CertificateRequisitionEndpointBase(
        Dp2DbContext dbContext,
        ILogger logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    protected async Task<CamCertificateRequisition?> GetById(
        ContractDraftVendorId contractDraftVendorId,
        CamCertificateRequisitionId id,
        CancellationToken ct)
    {
        return await this.dbContext.CamCertificateRequisitions
                         .Include(cr => cr.Acceptors)
                         .Include(cr => cr.ContractDraftVendor!)
                         .ThenInclude(dv => dv.ContractDraft)
                         .ThenInclude(cd => cd.Procurement)
                         .ThenInclude(p => p.SupplyMethodType)
                         .FirstOrDefaultAsync(
                             da =>
                                 da.Id == id &&
                                 da.ContractDraftVendorId == contractDraftVendorId,
                             ct);
    }

    protected async Task<CamCertificateRequisition?> GetById(
        CamCertificateRequisitionId id,
        CancellationToken ct)
    {
        return await this.dbContext.CamCertificateRequisitions
                         .Include(cr => cr.Acceptors)
                         .Include(cr => cr.ContractDraftVendor!)
                         .ThenInclude(dv => dv.ContractDraft)
                         .ThenInclude(cd => cd.Procurement)
                         .ThenInclude(p => p.SupplyMethodType)
                         .FirstOrDefaultAsync(da => da.Id == id, ct);
    }

    protected void ValidateUsers(
        SuUser[] users,
        UserId[] userIds)
    {
        var foundUserIds =
            users.Select(u => u.Id)
                 .ToArray();

        var missingUserIds =
            userIds.Except(foundUserIds)
                   .ToArray();

        if (missingUserIds.Length > 0)
        {
            this.ThrowError(
                $"ไม่พบข้อมูลผู้ใช้งาน {string.Join(", ", missingUserIds)}.",
                StatusCodes.Status404NotFound);
        }
    }

    protected async Task SetDefaultDocumentTemplateAsync(CamCertificateRequisition certificateRequisition, CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var documentTemplate =
            await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.CertificateRequisition &&
                    d.IsActive,
                ct);

        if (documentTemplate is null)
        {
            this.ThrowError(
                "ไม่พบแม่แบบเอกสารสำหรับใบขอหนังสือรับรอง.",
                StatusCodes.Status404NotFound);
        }

        certificateRequisition
            .AddDocumentHistory(documentTemplate.Value);

        await this.UpdateDocumentTemplateAsync(certificateRequisition, ct);
    }

    protected async Task<GetById.DeliveryAcceptancePeriodInfoDto[]> GetDeliveryAcceptancePeriods(CancellationToken ct)
    {
        var deliveryAcceptancePeriod =
            await this.dbContext.CmDeliveryAcceptancePeriods
                      .Include(p => p.CmDeliveryAcceptance)
                      .Where(p =>
                          p.CmDeliveryAcceptance.Status == CmDeliveryAcceptanceStatus.Completed)
                      .ToArrayAsync(ct)
                      .Map(res => res.Select((p, index) =>
                                         new GetById.DeliveryAcceptancePeriodInfoDto(
                                             p.CmDeliveryAcceptanceId,
                                             p.Id,
                                             p.Status,
                                             index + 1,
                                             null,
                                             null,
                                             null,
                                             null,
                                             p.AcceptanceDate))
                                     .OrderBy(p => p.Sequence)
                                     .ToArray());

        if (!deliveryAcceptancePeriod.Any())
        {
            this.ThrowError(
                $"ไม่พบข้อมูลการส่งมอบตรวจรับ",
                StatusCodes.Status404NotFound);
        }

        return deliveryAcceptancePeriod;
    }

    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        CamCertificateRequisition entity,
        FileId fileId,
        bool? isReplace = false,
        CancellationToken ct = default)
    {
        var latestHistory = entity.DocumentHistories
                                   .OrderVersions()
                                   .FirstOrDefault();

        if (latestHistory == null)
        {
            return null;
        }

        var newVersion = RunningDocumentVersion.IncrementDocumentVersion(
            latestHistory.Version,
            latestHistory.StatusState.ToString(),
            entity.Status.ToString());

        var documentService = this.Resolve<IDocumentService>();

        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            fileId,
            parentDirectory: $"{DocumentTemplateGroups.CertificateRequisition}/{entity.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (!copiedFileId.HasValue)
        {
            return null;
        }

        entity.AddDocumentHistory(copiedFileId.Value, isReplace ?? false);

        var newHistory = entity.DocumentHistories
            .OrderVersions()
            .First(h => h.FileId == copiedFileId.Value);
        newHistory.SetVersion(newVersion);

        return copiedFileId.Value;
    }
}