namespace GHB.DP2.Application.Features.Plan.PlanAnnouncement.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Plan.PlanAnnouncement.DTO;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract partial class PlanAnnouncementEndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TResponse : IResult
    where TRequest : notnull
{
    private readonly Dp2DbContext dbContext;

    protected PlanAnnouncementEndpointBase(
        Dp2DbContext dbContext,
        ILogger logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    protected async ValueTask SetDefaultDocumentTemplate(Domain.Plan.PlanAnnouncement announcement, CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var documentApproval =
            await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.PlanAnnouncement &&
                    d.SupplyMethodCode == announcement.SupplyMethodCode &&
                    d.IsCancel == null &&
                    d.IsChange == null &&
                    d.AdditionalInfo == null,
                ct);

        if (documentApproval == null)
        {
            this.ThrowError(
                "ไม่พบเอกสารแบบฟอร์มการขออนุมัติแผนประจำปี",
                StatusCodes.Status404NotFound);
        }

        var documentPublished =
            await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.PlanAnnouncement &&
                    d.SupplyMethodCode == announcement.SupplyMethodCode &&
                    d.IsCancel == null &&
                    d.IsChange == null &&
                    d.AdditionalInfo!.RootElement
                     .GetProperty(nameof(SuDocumentTemplate.IsPublished))
                     .GetBoolean() == true,
                ct);

        if (documentPublished == null)
        {
            this.ThrowError(
                "ไม่พบเอกสารแบบฟอร์มการประกาศแผนประจำปี",
                StatusCodes.Status404NotFound);
        }

        announcement.AddDocumentHistory(
            PlanAnnouncementDocumentType.Approve,
            documentApproval.Value,
            false);
        announcement.AddDocumentHistory(
            PlanAnnouncementDocumentType.Announcement,
            documentPublished.Value,
            false);
    }

    private static AcceptorResponse[] SetAcceptors(PlanAnnouncementAcceptor[] acceptors)
        => [.. acceptors
           .Map(DelegatorExtensions.DelegatorToAcceptor)
           .Map(a => new AcceptorResponse(
               a.Id.Value,
               a.Type,
               a.UserId.Value,
               a.Sequence,
               a.FullName,
               a.PositionName,
               a.BusinessUnitName,
               a.Status,
               a.Remark,
               a.ActionAt,
               DelegateeUserId: a.Delegatee?.SuUserId.Value))
           .OrderBy(o => o.Sequence)];

    private static AssigneeResponse[] SetAssignee(PlanAnnouncementAssignee[] assignees)
        => [.. assignees
           .Map(DelegatorExtensions.DelegatorToAssignee)
           .Map(a => new AssigneeResponse(
               a.Id.Value,
               a.Group,
               a.Type,
               a.UserId.Value,
               a.Sequence,
               a.FullName,
               a.PositionName,
               a.BusinessUnitName,
               a.Status,
               a.Remark,
               a.ActionAt,
               a.Delegatee?.SuUserId.Value))
           .OrderBy(o => o.Sequence)];

    private static AssigneeResponse? SetAssigneeAnnouncement(
        PlanAnnouncementStatus status,
        PlanAnnouncementAssignee[] assignees)
    {
        if (status is PlanAnnouncementStatus.WaitingAnnouncement or PlanAnnouncementStatus.Announcement)
        {
            return
                assignees
                    .Where(w => w.Type == AssigneeType.Director)
                    .Map(DelegatorExtensions.DelegatorToAssignee)
                    .Map(a => new AssigneeResponse(
                        a.Id.Value,
                        a.Group,
                        a.Type,
                        a.UserId.Value,
                        a.Sequence,
                        a.FullName,
                        a.PositionName,
                        a.BusinessUnitName,
                        a.Status,
                        a.Remark,
                        a.ActionAt,
                        DelegateeUserId: a.Delegatee?.SuUserId.Value))
                    .FirstOrDefault();
        }

        return null;
    }

    protected async Task<GetPlanAnnouncementByIdResponse> MapToGetPlanAnnouncementResponse(PlanAnnouncement data, CancellationToken ct)
    {
        var planIds =
            data.AnnouncementSelectedInformations
                .Where(w => !w.Plan.IsActive)
                .Select(s => s.Plan.Id.Value)
                .ToArray();

        var planRef =
            await this.dbContext.Plans
                      .Where(w =>
                          w.ReferenceId != null &&
                          planIds.Contains((Guid)w.ReferenceId) &&
                          w.IsActive)
                      .ToListAsync(ct);

        var annualPlanData =
            data.AnnouncementSelectedInformations
                .GroupJoin(
                    planRef,
                    pr => pr.PlanId.Value,
                    pa => (Guid)pa.ReferenceId,
                    (pa, pr) => new { pa, pr })
                .SelectMany(
                    p => p.pr.DefaultIfEmpty(),
                    (pa, pr) => new PlanAnnouncementSelectedDto
                    {
                        Id = pa.pa.Id.Value,
                        RefId = pr?.Id,
                        PlanId = pa.pa.PlanId.Value,
                        PlanNumber = pa.pa.Plan.PlanNumber.Value,
                        PlanTitle = pa.pa.Plan.Name,
                        Budget = pa.pa.Plan.Budget,
                        DepartmentName = pa.pa.Plan.Department.Name,
                        SupplyMethodName = pa.pa.Plan.SupplyMethod.Label,
                        SupplyMethodTypeName = pa.pa.Plan.SupplyMethodType?.Label,
                        EgpNumber = pa.pa.Plan.EgpNumber,
                        IsCancel = pr?.IsCancel ?? false,
                        IsChange = pr?.IsChange ?? false,
                    }).OrderBy(x => x.PlanNumber);

        var assignees = data.Assignees
                            .OrderBy(a => a.Sequence)
                            .Select(DelegatorExtensions.DelegatorToAssignee).ToArray();

        var isReplacedApproval = data.DocumentHistories
                              .Any(d => d.DocumentType == PlanAnnouncementDocumentType.Approve && d.IsReplaced);

        var isReplacedAnnouncement = data.DocumentHistories
                             .Any(d => d.DocumentType == PlanAnnouncementDocumentType.Announcement && d.IsReplaced);

        var approveDocumentVersions = data.DocumentHistories
                                          .Where(d => d.DocumentType == PlanAnnouncementDocumentType.Approve)
                                          .OrderVersions()
                                          .Select((d, index) => new PlanAnnouncementDocumentVersionResponse(
                                              d.FileId.Value,
                                              d.Version,
                                              d.CreatedAt,
                                              d.CreatedByName ?? string.Empty,
                                              index == 0))
                                          .ToArray();

        var announcementDocumentVersions = data.DocumentHistories
                                               .Where(d => d.DocumentType == PlanAnnouncementDocumentType.Announcement)
                                               .OrderVersions()
                                               .Select((d, index) => new PlanAnnouncementDocumentVersionResponse(
                                                   d.FileId.Value,
                                                   d.Version,
                                                   d.CreatedAt,
                                                   d.CreatedByName ?? string.Empty,
                                                   index == 0))
                                               .ToArray();

        return new GetPlanAnnouncementByIdResponse(
            data.Id.Value,
            data.PlanAnnouncementNumber.Value,
            data.GroupEgpNumber,
            data.Telephone,
            data.Year,
            data.SupplyMethodCode.Value,
            data.Remark,
            data.AnnouncementTitle,
            data.AnnouncementDate,
            data.DocumentDate,
            data.Status,
            data.AnnouncementDocument?.FileId.Value,
            data.Document?.FileId.Value,
            [.. annualPlanData],
            [.. data.Attachments
                .GroupBy(
                    a => a.DocumentTypeCode,
                    (key, g) => new AttachmentsDto(
                        key.Value,
                        [.. g.Map(s => new FileAttachments(s.Id.Value, s.FileName, s.Sequence, s.IsPublic, s.AuditInfo.CreatedBy))]))],
            SetAssignee([.. data.Assignees]),
            SetAcceptors([.. data.Acceptors]),
            SetAssigneeAnnouncement(data.Status, [.. data.Assignees]),
            false,
            false,
            approveDocumentVersions,
            announcementDocumentVersions,
            data.AuditInfo.LastModifiedAt);
    }

    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        PlanAnnouncement planAnnouncement,
        PlanAnnouncementDocumentType documentType,
        FileId fileId,
        bool? isReplaced = false,
        CancellationToken ct = default)
    {
        var latestHistory = planAnnouncement.DocumentHistories
                                            .Where(d => d.DocumentType == documentType)
                                            .OrderVersions()
                                            .FirstOrDefault();

        if (latestHistory == null)
        {
            return null;
        }

        var newVersion = RunningDocumentVersion.IncrementDocumentVersion(
            latestHistory.Version,
            latestHistory.StatusState.ToString(),
            planAnnouncement.Status.ToString());

        var documentService = this.Resolve<IDocumentService>();
        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            fileId,
            parentDirectory: $"{DocumentTemplateGroups.PlanAnnouncement}/{planAnnouncement.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}_{documentType}.odt",
            cancellationToken: ct);

        if (!copiedFileId.HasValue)
        {
            return null;
        }

        planAnnouncement.AddDocumentHistory(documentType, copiedFileId.Value, isReplaced ?? false);

        var histories = planAnnouncement.DocumentHistories.ToHashSet();
        var newHistory = histories.OrderVersions().First(h => h.FileId == copiedFileId.Value);
        newHistory.SetVersion(newVersion);

        return copiedFileId.Value;
    }

    protected async ValueTask UpdateAndReplaceDocumentHistoryAsync(
        PlanAnnouncement planAnnouncement,
        PlanAnnouncementDocumentType documentType,
        bool? isReplaced = false,
        bool isReplaceNewDocument = false,
        CancellationToken ct = default)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var latestHistory = planAnnouncement.DocumentHistories
                                      .Where(d => d.DocumentType == documentType)
                                      .OrderVersions()
                                      .FirstOrDefault();

        if (latestHistory is not null)
        {
            var replaceDto =
                this.MapToPlanAnnouncementReplateAsync(planAnnouncement);

            var templateFileId = isReplaceNewDocument
                ? await this.GetDocumentTemplateFileIdAsync(
                    documentService, planAnnouncement, documentType, ct)
                : latestHistory.FileId;

            var newFileId = isReplaceNewDocument
                ? await documentService.CopyDocumentTemplateAsync(
                    templateFileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                    parentDirectory: $"{DocumentTemplateGroups.PlanAnnouncement}/{planAnnouncement.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}_{documentType}.odt",
                    cancellationToken: ct)
                : templateFileId;

            if (newFileId is null)
            {
                this.ThrowError("ไม่สามารถคัดลอกเอกสารร่างได้");
            }

            planAnnouncement.AddDocumentHistory(documentType, newFileId.Value, isReplaced ?? false);
        }
    }

    private async Task<FileId> GetDocumentTemplateFileIdAsync(
        IDocumentService documentService,
        PlanAnnouncement planAnnouncement,
        PlanAnnouncementDocumentType documentType,
        CancellationToken ct)
    {
        FileId? templateFileId;

        if (documentType == PlanAnnouncementDocumentType.Approve)
        {
            templateFileId = await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.PlanAnnouncement &&
                    d.SupplyMethodCode == planAnnouncement.SupplyMethodCode &&
                    d.IsCancel == null &&
                    d.IsChange == null &&
                    d.AdditionalInfo == null,
                ct);
        }
        else
        {
            templateFileId = await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.PlanAnnouncement &&
                    d.SupplyMethodCode == planAnnouncement.SupplyMethodCode &&
                    d.IsCancel == null &&
                    d.IsChange == null &&
                    d.AdditionalInfo!.RootElement
                     .GetProperty(nameof(SuDocumentTemplate.IsPublished))
                     .GetBoolean() == true,
                ct);
        }

        if (templateFileId == null)
        {
            this.ThrowError(
                $"ไม่พบเอกสารเทมเพลตสำหรับ {documentType}",
                StatusCodes.Status404NotFound);
        }

        return templateFileId.Value;
    }

    protected async Task<FileId> GetDocumentTemplateForResetAsync(
        PlanAnnouncement planAnnouncement,
        PlanAnnouncementDocumentType documentType,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();
        FileId? fileId = null;

        if (documentType == PlanAnnouncementDocumentType.Approve)
        {
            fileId = await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.PlanAnnouncement &&
                    d.SupplyMethodCode == planAnnouncement.SupplyMethodCode &&
                    d.IsCancel == null &&
                    d.IsChange == null &&
                    d.AdditionalInfo == null,
                parentDirectory: $"{DocumentTemplateGroups.PlanAnnouncement}/{planAnnouncement.Id}_Reset_{DateTime.UtcNow:yyyyMMddHHmmss}_{documentType}.odt",
                cancellationToken: ct);
        }
        else
        {
            fileId = await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.PlanAnnouncement &&
                    d.SupplyMethodCode == planAnnouncement.SupplyMethodCode &&
                    d.IsCancel == null &&
                    d.IsChange == null &&
                    d.AdditionalInfo!.RootElement
                     .GetProperty(nameof(SuDocumentTemplate.IsPublished))
                     .GetBoolean() == true,
                parentDirectory: $"{DocumentTemplateGroups.PlanAnnouncement}/{planAnnouncement.Id}_Reset_{DateTime.UtcNow:yyyyMMddHHmmss}_{documentType}.odt",
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

    protected async ValueTask UpdateAndReplaceDocumentTemplate(Domain.Plan.PlanAnnouncement announcement, CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var replaceDto =
            this.MapToPlanAnnouncementReplateAsync(announcement);

        var documentApproval =
            await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.PlanAnnouncement &&
                    d.SupplyMethodCode == announcement.SupplyMethodCode &&
                    d.IsCancel == null &&
                    d.IsChange == null &&
                    d.AdditionalInfo == null,
                ct);

        if (documentApproval == null)
        {
            this.ThrowError(
                "ไม่พบเอกสารแบบฟอร์มการขออนุมัติแผนประจำปี",
                StatusCodes.Status404NotFound);
        }

        var documentPublished =
            await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.PlanAnnouncement &&
                    d.SupplyMethodCode == announcement.SupplyMethodCode &&
                    d.IsCancel == null &&
                    d.IsChange == null &&
                    d.AdditionalInfo!.RootElement
                     .GetProperty(nameof(SuDocumentTemplate.IsPublished))
                     .GetBoolean() == true,
                ct);

        if (documentPublished == null)
        {
            this.ThrowError(
                "ไม่พบเอกสารแบบฟอร์มการประกาศแผนประจำปี",
                StatusCodes.Status404NotFound);
        }

        var approveParentDirectory =
            $"{DocumentTemplateGroups.PlanAnnouncement}/{announcement.PlanAnnouncementNumber}_{PlanAnnouncementDocumentType.Approve}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        var approveFileId =
            await documentService.CopyDocumentTemplateAsync(
                documentApproval.Value,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: approveParentDirectory,
                cancellationToken: ct);

        if (approveFileId is null)
        {
            this.ThrowError(
                "ไม่สามารถคัดลอกเอกสารแบบฟอร์มการขออนุมัติแผนประจำปีได้",
                StatusCodes.Status500InternalServerError);
        }

        var announcementParentDirectory =
            $"{DocumentTemplateGroups.PlanAnnouncement}/{announcement.PlanAnnouncementNumber}_{PlanAnnouncementDocumentType.Announcement}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        var announcementFileId =
            await documentService.CopyDocumentTemplateAsync(
                documentPublished.Value,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: announcementParentDirectory,
                cancellationToken: ct);

        if (announcementFileId is null)
        {
            this.ThrowError(
                "ไม่สามารถคัดลอกเอกสารแบบฟอร์มการประกาศแผนประจำปีได้",
                StatusCodes.Status500InternalServerError);
        }

        announcement.AddDocumentHistory(
            PlanAnnouncementDocumentType.Approve,
            approveFileId.Value,
            false);
        announcement.AddDocumentHistory(
            PlanAnnouncementDocumentType.Announcement,
            announcementFileId.Value,
            false);
    }
}