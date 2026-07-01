namespace GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn.Abstract;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn.Dto;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract partial class ContractGuaranteeReturnEndpoint<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;
    private readonly IFileServiceClient fileServiceClient;

    protected ContractGuaranteeReturnEndpoint(
        ILogger logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IOperationService operationService)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
        this.operationService = operationService;
    }

    protected async Task<CaContractDraftVendor> GetByIdAsync(ContractDraftVendorId contractDraftVendorId, CancellationToken ct = default)
    {
        var entity = await this.dbContext.CaContractDraftVendors
                               .Include(c => c.CmContractGuaranteeReturns)
                               .ThenInclude(r => r.Assignees)
                               .ThenInclude(a => a.Delegatee)
                               .Include(c => c.CmContractGuaranteeReturns)
                               .ThenInclude(r => r.Acceptors)
                               .ThenInclude(a => a.Delegatee)
                               .Include(c => c.CmContractGuaranteeReturns)
                               .ThenInclude(r => r.Acceptors).ThenInclude(cmContractGuaranteeReturnAcceptor => cmContractGuaranteeReturnAcceptor.CommitteePosition)
                               .Include(c => c.CmContractGuaranteeReturns)
                               .ThenInclude(r => r.Conditions)
                               .Include(c => c.CmContractGuaranteeReturns)
                               .ThenInclude(r => r.RequiredDocuments)
                               .Include(c => c.ContractInvitationVendors)
                               .ThenInclude(iv => iv.PurchaseOrderApprovalContract)
                               .ThenInclude(p => p.Entrepreneur)
                               .ThenInclude(e => e!.SuVendor)
                               .Include(c => c.ContractInvitationVendors)
                               .ThenInclude(iv => iv.PurchaseOrderApprovalContract)
                               .ThenInclude(p => p.PrincipleApprovalRentalEntrepreneurs)
                               .ThenInclude(e => e!.Vendor)
                               .Include(c => c.ContractType)
                               .Include(c => c.Template)
                               .Include(c => c.Delivery)
                               .ThenInclude(d => d.LeadTimeType)
                               .Include(caContractDraftVendor => caContractDraftVendor.DocumentHistories)
                               .Include(caContractDraftVendor => caContractDraftVendor.CmContractGuaranteeReturns)
                               .ThenInclude(cmContractGuaranteeReturn => cmContractGuaranteeReturn.DocumentHistories)
                               .Include(c => c.ContractDraft)
                               .ThenInclude(p => p.Procurement)
                               .Include(caContractDraftVendor => caContractDraftVendor.CmContractGuaranteeReturns)
                               .ThenInclude(cmContractGuaranteeReturn => cmContractGuaranteeReturn.Attachments)
                               .Include(caContractDraftVendor => caContractDraftVendor.CmContractGuaranteeReturns)
                               .ThenInclude(cmContractGuaranteeReturn => cmContractGuaranteeReturn.EmailAttachments)
                               .Include(c => c.DraftTermsConditions)
                               .ThenInclude(g => g.Guarantee)
                               .ThenInclude(g => g.Type)
                               .Include(c => c.DraftTermsConditions)
                               .ThenInclude(g => g.Guarantee)
                               .ThenInclude(g => g.Bank)
                               .Include(g => g.DeliveryAcceptances)
                               .ThenInclude(x => x.Periods)
                               .AsSplitQuery()
                               .SingleOrDefaultAsync(c => c.Id == contractDraftVendorId, ct);

        if (entity is null)
        {
            this.ThrowError($"ไม่พบข้อมูลสัญญารหัส {contractDraftVendorId}", StatusCodes.Status404NotFound);
        }

        return entity;
    }

    protected async Task UpsertAcceptors(CmContractGuaranteeReturn entity, AcceptorRequest[] requests, CmContractGuaranteeReturnStatus status, CancellationToken ct, UserId? sendToAcceptorId = null)
    {
        _ = entity.Acceptors.Where(w => !requests.Select(s => s.Id).Contains(w.Id.Value))
                  .Iter(s => entity.RemoveAcceptor(s));

        var userIds = requests.Select(r => r.UserId).ToList();
        var users = await this.ValidateUsersAsync(userIds, ct);

        var lastAssigneeUserId = entity.Assignees
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)a.UserId)
            .FirstOrDefault();

        var resolvedSendToAcceptorId = lastAssigneeUserId ?? sendToAcceptorId;

        _ = requests.Where(w => !w.Id.HasValue)
                    .Join(
                        users,
                        req => UserId.From(req.UserId),
                        usr => usr.Id,
                        (req, usr) =>
                        {
                            var acceptor = CmContractGuaranteeReturnAcceptor.Create(req.AcceptorType, usr, req.Sequence, status)
                                                                            .SetIsUnableToPerformDuties(req.IsUnableToPerformDuties ?? false);

                            acceptor.SetSendToAcceptorId(resolvedSendToAcceptorId);

                            if (!string.IsNullOrWhiteSpace(req.CommitteePositionsCode))
                            {
                                acceptor.SetCommitteePositionsCode(ParameterCode.From(req.CommitteePositionsCode));
                            }

                            return acceptor;
                        })
                    .Iter(r => entity.AddAcceptor(r));

        foreach (var existing in entity.Acceptors.ToList())
        {
            var match = requests.FirstOrDefault(e => e.UserId == existing.UserId && existing.Type == e.AcceptorType);

            if (match != null)
            {
                existing.SetSendToAcceptorId(resolvedSendToAcceptorId);
                existing.SetIsUnableToPerformDuties(match.IsUnableToPerformDuties ?? false)
                        .SetSequence(match.Sequence);

                if (existing.IsUnableToPerformDuties)
                {
                    existing.UnableToPerformDuties(existing.Remark);
                }
            }
        }
    }

    protected async Task UpsertAssignee(CmContractGuaranteeReturn entity, AssigneeRequest[] requests, CancellationToken cancellationToken = default, UserId? sendToAcceptorId = null)
    {
        _ = entity.Assignees.Where(w => !requests.Select(s => s.Id).Contains(w.Id.Value))
                  .Iter(s => entity.RemoveAssignee(s));

        var userIds = requests.Select(r => r.UserId).ToList();
        var users = await this.ValidateUsersAsync(userIds, cancellationToken);

        var lastAssigneeUserId = requests
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)UserId.From(a.UserId))
            .FirstOrDefault();

        var resolvedSendToAcceptorId = lastAssigneeUserId ?? sendToAcceptorId;

        _ = requests.Where(w => !w.Id.HasValue)
                    .Join(
                        users,
                        req => UserId.From(req.UserId),
                        usr => usr.Id,
                        (req, usr) =>
                        {
                            var assignee = CmContractGuaranteeReturnAssignee.Create(req.AssigneeGroup, req.AssigneeType, usr, req.Sequence);
                            assignee.SetSendToAcceptorId(resolvedSendToAcceptorId);
                            return assignee;
                        })
                    .Iter(r => entity.AddAssignee(r));

        foreach (var existing in entity.Assignees.ToList())
        {
            var match = requests.FirstOrDefault(e => e.UserId == existing.UserId);

            if (match != null)
            {
                existing.SetSequence(match.Sequence);
                existing.SetSendToAcceptorId(resolvedSendToAcceptorId);
            }
        }
    }

    protected void UpsertConditions(
        CmContractGuaranteeReturn entity,
        IEnumerable<ConditionRequest> requests)
    {
        var details = entity.Conditions ?? new List<CmContractGuaranteeReturnCondition>();
        var newEntities = requests.Select(dto =>
        {
            var existing = details.FirstOrDefault(c => c.Id.Value == dto.Id);

            if (existing != null)
            {
                existing.SetValue(
                    dto.Sequence,
                    dto.Description,
                    dto.IsSatisfied);

                return existing;
            }

            return CmContractGuaranteeReturnCondition.Create()
                                                     .SetValue(
                                                         dto.Sequence,
                                                         dto.Description,
                                                         dto.IsSatisfied);
        }).ToList();

        // Add new
        foreach (var toAdd in newEntities.Where(e => details.All(a => !a.Id.Equals(e.Id))))
        {
            entity.AddConditions(toAdd);
        }

        // Remove obsolete
        foreach (var toRemove in details.Where(a => !newEntities.Any(e => e.Id.Equals(a.Id))).ToList())
        {
            entity.RemoveConditions(toRemove);
        }
    }

    protected void UpsertRequiredDocument(
        CmContractGuaranteeReturn entity,
        IEnumerable<RequiredDocumentRequest> requests)
    {
        var details = entity.RequiredDocuments ?? new List<CmContractGuaranteeReturnRequiredDocument>();
        var newEntities = requests.Select(dto =>
        {
            var existing = details.FirstOrDefault(c => c.Id.Value == dto.Id);

            if (existing != null)
            {
                existing.SetValue(
                    dto.Sequence,
                    dto.DocumentName,
                    dto.IsSubmitted);

                return existing;
            }

            return CmContractGuaranteeReturnRequiredDocument.Create()
                                                            .SetValue(
                                                                dto.Sequence,
                                                                dto.DocumentName,
                                                                dto.IsSubmitted);
        }).ToList();

        // Add new
        foreach (var toAdd in newEntities.Where(e => details.All(a => !a.Id.Equals(e.Id))))
        {
            entity.AddRequiredDocuments(toAdd);
        }

        // Remove obsolete
        foreach (var toRemove in details.Where(a => !newEntities.Any(e => e.Id.Equals(a.Id))).ToList())
        {
            entity.RemoveRequiredDocuments(toRemove);
        }
    }

    private async Task<SuUser[]> ValidateUsersAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Map(UserId.From).ToArray();

        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Where(u => ids.Contains(u.Id))
                              .ToArrayAsync(cancellationToken);

        var missingIds = ids.Except(users.Map(u => u.Id)).ToArray();

        if (missingIds.Length > 0)
        {
            this.ThrowError($"User with ID {string.Join(", ", missingIds)} not found.", StatusCodes.Status404NotFound);
        }

        return users;
    }

    protected async Task UpsertAttachments(CmContractGuaranteeReturn entity, AttachmentsDtoWithId[] attachments)
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

        newFiles.Map(f => CmContractGuaranteeReturnAttachments.Create(ParameterCode.From(f.DocumentTypeCode), FileId.From(f.FileId), f.FileName, f.Sequence, f.IsPublic))
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

    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        CmContractGuaranteeReturn entity,
        CmContractGuaranteeReturnDocumentType documentType,
        FileId fileId,
        bool? isReplace = false,
        CancellationToken ct = default)
    {
        var latestHistory = entity.DocumentHistories
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
            entity.Status.ToString());

        var documentService = this.Resolve<IDocumentService>();

        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            fileId,
            parentDirectory: $"{DocumentTemplateGroups.CMGuaranteeReturn}/{entity.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (!copiedFileId.HasValue)
        {
            return null;
        }

        entity.AddDocumentHistory(documentType, copiedFileId.Value, isReplace ?? false);

        var newHistory = entity.DocumentHistories
            .Where(d => d.DocumentType == documentType)
            .OrderVersions()
            .First(h => h.FileId == copiedFileId.Value);
        newHistory.SetVersion(newVersion);

        return copiedFileId.Value;
    }
}