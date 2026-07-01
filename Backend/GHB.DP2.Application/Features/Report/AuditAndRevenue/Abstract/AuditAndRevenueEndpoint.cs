namespace GHB.DP2.Application.Features.Report.AuditAndRevenue.Abstract;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.Report.RpAuditAndRevenue;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public abstract partial class AuditAndRevenueEndpoint<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;
    private readonly IOperationService operationService;

    protected AuditAndRevenueEndpoint(
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

    protected void UpsertAcceptors(RpAuditAndRevenue entity, IEnumerable<AcceptorRequest> requests, RpAuditAndRevenueStatus status, UserId? sendToAcceptorId = null)
    {
        var acceptors = entity.Acceptors ?? [];
        var newEntities = requests.Select(dto =>
        {
            var existing = acceptors.FirstOrDefault(c => dto.Id == c.Id.Value);

            if (existing != null)
            {
                existing.SetType(existing.Type)
                        .SetUser(existing.UserId, existing.EmployeeCode, existing.FullName, existing.PositionName, existing.BusinessUnitName)
                        .SetSequence(dto.Sequence);

                existing.SetSendToAcceptorId(sendToAcceptorId);

                switch (entity.Status)
                {
                    case RpAuditAndRevenueStatus.Draft or RpAuditAndRevenueStatus.Rejected or RpAuditAndRevenueStatus.Edit when
                        status == RpAuditAndRevenueStatus.WaitingApproval:
                        existing.SetStatus(AcceptorStatus.Pending);

                        break;
                }

                return existing;
            }

            var users = this.dbContext.SuUsers.SingleOrDefault(u => u.Id == UserId.From(dto.UserId));

            if (users == null)
            {
                this.ThrowError($"User with ID {dto.UserId} not found.", StatusCodes.Status404NotFound);
            }

            var newAcceptorPending = RpAuditRevenueAcceptor.Create(dto.AcceptorType, users, dto.Sequence);

            newAcceptorPending.SetSendToAcceptorId(sendToAcceptorId);

            if (status == RpAuditAndRevenueStatus.WaitingApproval)
            {
                newAcceptorPending.SetStatus(AcceptorStatus.Pending);
            }

            return newAcceptorPending;
        }).ToList();

        // Add new
        foreach (var toAdd in newEntities.Where(e => acceptors.All(a => a.Id != e.Id)))
        {
            entity.AddAcceptor(toAdd);
        }

        // Remove obsolete
        var toRemoveList = acceptors.Where(a => !newEntities.Any(e => e.Id == a.Id)).ToList();

        foreach (var toRemove in toRemoveList)
        {
            entity.RemoveAcceptor(toRemove);
        }
    }

    protected void UpsertDetails(
        RpAuditAndRevenue entity,
        IEnumerable<AuditAndRevenueDetailDto> requests)
    {
        var details = entity.Details ?? new List<RpAuditAndRevenueDetail>();
        var newEntities = requests.Select(dto =>
        {
            var contractDraftVendor = this.dbContext.CaContractDraftVendors.SingleOrDefault(u => u.Id == ContractDraftVendorId.From(dto.CaContractDraftVendorId));

            if (contractDraftVendor == null)
            {
                this.ThrowError($"Contract Draft Vendor with ID {dto.CaContractDraftVendorId} not found.", StatusCodes.Status404NotFound);
            }

            var existing = details.FirstOrDefault(c => c.Id.Value == dto.Id);

            if (existing != null)
            {
                existing.SetValue(
                    dto.Sequence,
                    dto.Description,
                    contractDraftVendor);

                return existing;
            }

            return RpAuditAndRevenueDetail.Create()
                                                .SetValue(
                                                    dto.Sequence,
                                                    dto.Description,
                                                    contractDraftVendor);
        }).ToList();

        // Add new
        foreach (var toAdd in newEntities.Where(e => details.All(a => !a.Id.Equals(e.Id))))
        {
            entity.AddDetail(toAdd);
        }

        // Remove obsolete
        foreach (var toRemove in details.Where(a => !newEntities.Any(e => e.Id.Equals(a.Id))).ToList())
        {
            entity.RemoveDetail(toRemove);
        }
    }

    protected async Task UpsertAttachments(RpAuditAndRevenue entity, AttachmentsDtoWithId[] attachments)
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

        newFiles.Map(f => RpAuditAndRevenueAttachment.Create(ParameterCode.From(f.DocumentTypeCode), FileId.From(f.FileId), f.FileName, f.Sequence, f.IsPublic))
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
}