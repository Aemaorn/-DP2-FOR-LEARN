namespace GHB.DP2.Application.Features.Plan.Plan.Abstract;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Plan.Dto;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

public abstract partial class PlanEndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TResponse : IResult
    where TRequest : notnull
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    protected PlanEndpointBase(
        ILogger logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    protected bool PlanDocumentCondition(Plan plan)
    {
        return (plan.Type == PlanType.InYearPlan && plan.Budget > 500000) || this.AnnualPlanDocumentCondition(plan);
    }

    protected bool AnnualPlanDocumentCondition(Plan plan)
    {
        return plan.Type == PlanType.AnnualPlan && plan.Budget > 500000 && (plan.IsCancel || plan.IsChange);
    }

    protected async Task SetDefaultDocumentTemplate(Plan planData, CancellationToken ct)
    {
        if (planData.Budget > 500000)
        {
            // Plan document
            var documentService =
                this.Resolve<IDocumentService>();

            var planTemplate = await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.Plan &&
                    d.SupplyMethodCode == planData.SupplyMethodCode &&
                    (d.BudgetForDocument.Min <= planData.Budget &&
                     (d.BudgetForDocument.Max >= planData.Budget || d.BudgetForDocument.Max == null)) &&
                    (d.IsChange == null || d.IsChange == planData.IsChange) &&
                    (d.IsCancel == null || d.IsCancel == planData.IsCancel) &&
                    d.AdditionalInfo == null,
                ct);

            if (planTemplate == null)
            {
                this.ThrowError(
                    "ไม่พบเทมเพลตเอกสารแผนที่ตรงกับเงื่อนไข",
                    StatusCodes.Status404NotFound);
            }

            planData.AddDocumentHistory(PlanDocumentType.Plan, planTemplate.Value);

            // Plan announcement document
            var planAnnouncementTemplate =
                await documentService.GetDocumentTemplateAsync(
                    d =>
                        d.Group == DocumentTemplateGroups.Plan &&
                        d.SupplyMethodCode == planData.SupplyMethodCode &&
                        (d.BudgetForDocument.Min <= planData.Budget &&
                         (d.BudgetForDocument.Max >= planData.Budget || d.BudgetForDocument.Max == null)) &&
                        (d.IsChange == null || d.IsChange == planData.IsChange) &&
                        (d.IsCancel == null || d.IsCancel == planData.IsCancel) &&
                        d.AdditionalInfo!.RootElement
                         .GetProperty(nameof(SuDocumentTemplate.IsPublished))
                         .GetBoolean(),
                    ct);

            if (planAnnouncementTemplate == null)
            {
                this.ThrowError(
                    "ไม่พบเทมเพลตประกาศแผนที่ตรงกับเงื่อนไข",
                    StatusCodes.Status404NotFound);
            }

            planData.AddDocumentHistory(
                PlanDocumentType.Announcement,
                planAnnouncementTemplate.Value);
        }
        else
        {
            _ =
                planData.DocumentHistories
                        .Where(w =>
                            w.DocumentType == PlanDocumentType.Announcement)
                        .Select(s => s.Id)
                        .Iter(d =>
                            planData.RemoveDocumentHistory(d));
        }
    }

    protected static GetPlanResponse MapToGetPlanResponse(Plan data, bool isCurrentCancelOrChange, bool isProcurement)
    {
        var assignees = data.Assignees
                            .OrderBy(a => a.Sequence)
                            .Select(DelegatorExtensions.DelegatorToAssignee).ToArray();

        var planDocumentVersions = data.DocumentHistories
                                       .Where(d => d.DocumentType == PlanDocumentType.Plan)
                                       .OrderVersions()
                                       .Select((d, index) => new PlanDocumentVersionResponse(
                                           d.FileId.Value,
                                           d.Version,
                                           d.CreatedAt,
                                           d.CreatedByName ?? string.Empty,
                                           index == 0))
                                       .ToArray();

        var planAnnouncementDocumentVersions = data.DocumentHistories
                                                   .Where(d => d.DocumentType == PlanDocumentType.Announcement)
                                                   .OrderVersions()
                                                   .Select((d, index) => new PlanDocumentVersionResponse(
                                                       d.FileId.Value,
                                                       d.Version,
                                                       d.CreatedAt,
                                                       d.CreatedByName ?? string.Empty,
                                                       index == 0))
                                                   .ToArray();

        return new GetPlanResponse(
            data.Id,
            data.Status,
            data.PlanNumber.Value,
            data.DepartmentId,
            data.Type,
            data.SupplyMethodCode,
            data.SupplyMethodTypeCode,
            data.SupplyMethodSpecialTypeCode,
            data.BudgetYear,
            data.Name,
            data.Budget,
            data.ExpectingProcurementAt,
            data.DocumentDate,
            data.Remark ?? string.Empty,
            data.Telephone,
            data.IsStock,
            data.AssignSegmentCode,
            data.GroupEgpNumber,
            data.EgpNumber,
            data.IsCommercialMaterial,
            data.Document?.FileId.Value,
            false,
            data.AnnouncementDocument?.FileId.Value,
            false,
            [.. data.Acceptors
                .Select(DelegatorExtensions.DelegatorToAcceptor)
                .OrderBy(a => a.Sequence)
                .Select(a => new AcceptorResponse(
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
                    DelegateeUserId: a.Delegatee?.SuUserId.Value))],
            [.. assignees.Select(a => new AssigneeResponse(
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
                     DelegateeUserId: a.Delegatee?.SuUserId.Value))],
            [.. data.Attachments
                .OrderBy(o => o.Sequence)
                .GroupBy(
                    a => a.DocumentTypeCode,
                    (key, g) => new AttachmentsDtoWithId(
                        key.Value,
                        [.. g.Select(s => new FileAttachmentsWithId(s.Id.Value, s.FileId.Value, s.FileName, s.Sequence, s.IsPublic, s.AuditInfo.CreatedBy))]))],
            data.AuditInfo.CreatedBy,
            SetAssigneeAnnouncement(data.Status, assignees),
            data.IsChange,
            data.IsCancel,
            data.ChangeReason,
            data.CancelReason,
            data.RemarkClosed,
            isCurrentCancelOrChange,
            isProcurement,
            planDocumentVersions,
            planAnnouncementDocumentVersions,
            data.AuditInfo.LastModifiedAt);
    }

    private async Task<(string Budget, string BudgetText, ChangePlanReplace? ChangePlan)> PlanChangeAsync(
        Plan plan,
        CancellationToken cancellationToken)
    {
        if (!plan.IsChange)
        {
            return (
                plan.Budget.ToCurrencyStringWithComma(),
                plan.Budget.ThaiBahtText(),
                null);
        }

        var planOld =
            await this.dbContext.Plans
                      .FirstOrDefaultAsync(
                          p => p.Id == plan.ReferenceId,
                          cancellationToken);

        if (planOld is null)
        {
            this.ThrowError(
                $"ไม่พบแผนที่อ้างอิง ID {plan.ReferenceId}",
                StatusCodes.Status404NotFound);
        }

        var changePlan = new ChangePlanReplace(
            planOld.Name,
            planOld.Budget.ToCurrencyStringWithComma(),
            planOld.ExpectingProcurementAt.ToOffset(TimeSpan.FromHours(7)).ToThaiDateString(format: "MMMM yyyy"),
            plan.Name,
            plan.Budget.ToCurrencyStringWithComma(),
            plan.ExpectingProcurementAt.ToOffset(TimeSpan.FromHours(7)).ToThaiDateString(format: "MMMM yyyy"));

        return (
            planOld.Budget.ToCurrencyStringWithComma(),
            planOld.Budget.ThaiBahtText(),
            changePlan);
    }

    private static AssigneeResponse? SetAssigneeAnnouncement(PlanStatus status, PlanAssignee[] assignees)
    {
        if (status is PlanStatus.WaitingAnnouncement or PlanStatus.Announcement or PlanStatus.CancelPlan)
        {
            return assignees.Where(w => w.Type == AssigneeType.Director)
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

    protected async Task UpsertAcceptors(Plan entity, AcceptorRequest[] requests, PlanStatus status, CancellationToken ct)
    {
        _ = entity.Acceptors
                  .Where(w => !requests.Select(s => s.Id).Contains(w.Id.Value))
                  .Iter(s => entity.RemoveAcceptor(s));

        var userIds = requests.Map(a => a.UserId)
                              .Map(UserId.From)
                              .ToArray();

        var users = await this.dbContext.SuUsers
                              .Include(suUser => suUser.Employee)
                              .ThenInclude(rawEmployee => rawEmployee.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(ct);
        var userExists
            = userIds.Except(users.Map(u => u.Id)).ToArray();

        if (userExists.Length > 0)
        {
            this.ThrowError(
                $"User with ID {string.Join(", ", userExists)} not found.",
                StatusCodes.Status404NotFound);
        }

        _ = requests.Where(w => !w.Id.HasValue)
                    .Join(
                        users,
                        req => UserId.From(req.UserId),
                        usr => usr.Id,
                        (req, usr) => PlanAcceptor.Create(req.AcceptorType, usr, req.Sequence, entity.DepartmentId))
                    .Iter(r => entity.AddAcceptor(r));

        var currentUserId = Guid.TryParse(
            this.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
            out var parsedUserId)
            ? UserId.From(parsedUserId)
            : (UserId?)null;

        var lastAssigneeUserId = entity.Assignees
            .Where(a => a.Type == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)a.UserId)
            .FirstOrDefault();

        foreach (var existing in entity.Acceptors)
        {
            var match = requests.FirstOrDefault(e => e.UserId == existing.UserId && existing.Type == e.AcceptorType);

            if (match != null)
            {
                existing.SetSequence(match.Sequence)
                        .SetActive()
                        .SetSendToAcceptorId(
                            lastAssigneeUserId
                            ?? currentUserId);

                switch (entity.Status)
                {
                    case PlanStatus.DraftPlan or PlanStatus.RejectPlan when status == PlanStatus.WaitingApprovePlan:
                        existing.SetStatus(AcceptorStatus.Pending);

                        break;
                }
            }
        }
    }

    protected async Task UpsertAttachments(Plan entity, AttachmentsDtoWithId[] attachments)
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
                       }))
                       .ToArray();

        var incomingFileIds = fileList.Select(f => FileId.From(f.FileId)).ToHashSet();
        var existingFileIds = entity.Attachments.Select(a => a.FileId).ToHashSet();

        var removedAttachments = entity.Attachments
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

        newFiles.Select(f => PlanAttachments.Create(ParameterCode.From(f.DocumentTypeCode), FileId.From(f.FileId), f.FileName, f.Sequence, f.IsPublic))
                .Iter(r => entity.AddAttachment(r));

        if (newFiles.Length > 0)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.UploadFile,
                ActivityLogActionTypeConstant.UploadFile,
                nameof(entity.Status),
                string.Join(", ", newFiles.Select(f => f.FileName))));
        }

        foreach (var existing in entity.Attachments)
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

    protected async Task ValidateDocumentTypeCode(AttachmentsDtoWithId[] attachments, CancellationToken ct)
    {
        var docTypeCodes = attachments.Select(s => s.DocumentTypeCode)
                                      .Where(w => !string.IsNullOrWhiteSpace(w))
                                      .Select(ParameterCode.From)
                                      .ToArray();

        var docType = await this.dbContext.SuParameters
                                .Where(x => docTypeCodes.Contains(x.Code))
                                .ToArrayAsync(ct);

        var missingDocumentTypes = docTypeCodes
                                   .Except(docType.Select(dt => dt.Code))
                                   .ToArray();

        if (missingDocumentTypes.Any())
        {
            this.ThrowError(
                $"ไม่พบประเภทไฟล์",
                StatusCodes.Status404NotFound);
        }
    }

    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        Plan planData,
        PlanDocumentType documentType,
        FileId fileId,
        bool? isReplaced = false,
        CancellationToken ct = default)
    {
        var latestHistory = planData.DocumentHistories
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
            planData.Status.ToString());

        var documentService = this.Resolve<IDocumentService>();
        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            fileId,
            parentDirectory: $"{DocumentTemplateGroups.Plan}/{planData.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}_{documentType}.odt",
            cancellationToken: ct);

        if (!copiedFileId.HasValue)
        {
            return null;
        }

        planData.AddDocumentHistory(documentType, copiedFileId.Value, isReplaced ?? false);

        var histories = planData.DocumentHistories.ToHashSet();
        var newHistory = histories.OrderVersions().First(h => h.FileId == copiedFileId.Value);
        newHistory.SetVersion(newVersion);

        return copiedFileId.Value;
    }

    protected async Task<FileId> GetDocumentTemplateForResetAsync(
        Plan planData,
        PlanDocumentType documentType,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();
        FileId? fileId = null;

        if (documentType == PlanDocumentType.Plan)
        {
            fileId = await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.Plan &&
                    d.SupplyMethodCode == planData.SupplyMethodCode &&
                    (d.BudgetForDocument.Min <= planData.Budget &&
                     (d.BudgetForDocument.Max >= planData.Budget || d.BudgetForDocument.Max == null)) &&
                    (d.IsChange == null || d.IsChange == planData.IsChange) &&
                    (d.IsCancel == null || d.IsCancel == planData.IsCancel) &&
                    d.AdditionalInfo == null,
                parentDirectory: $"{DocumentTemplateGroups.Plan}/{planData.Id}_Reset_{DateTime.UtcNow:yyyyMMddHHmmss}_{documentType}.odt",
                cancellationToken: ct);
        }
        else
        {
            fileId = await documentService.GetDocumentTemplateAsync(
                d =>
                    d.Group == DocumentTemplateGroups.Plan &&
                    d.SupplyMethodCode == planData.SupplyMethodCode &&
                    (d.BudgetForDocument.Min <= planData.Budget &&
                     (d.BudgetForDocument.Max >= planData.Budget || d.BudgetForDocument.Max == null)) &&
                    (d.IsChange == null || d.IsChange == planData.IsChange) &&
                    (d.IsCancel == null || d.IsCancel == planData.IsCancel) &&
                    d.AdditionalInfo!.RootElement
                     .GetProperty(nameof(SuDocumentTemplate.IsPublished))
                     .GetBoolean(),
                parentDirectory: $"{DocumentTemplateGroups.Plan}/{planData.Id}_Reset_{DateTime.UtcNow:yyyyMMddHHmmss}_{documentType}.odt",
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
}