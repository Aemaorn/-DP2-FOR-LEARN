namespace GHB.DP2.Application.Features.Procurement.PPettyCashReimbursement.Abstract;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPettyCash;
using GHB.DP2.Domain.Procurement.PPettyCashReimbursement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record PettyCashReimbursementItemDto(
    Guid? Id,
    int Sequence,
    Guid PettyCashGlAccountId);

public abstract class PPettyCashReimbursementAbstractEndpoint<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    protected PPettyCashReimbursementAbstractEndpoint(
        ILogger logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    protected async Task UpsertAcceptors(
        PPettyCashReimbursement entity,
        IEnumerable<AcceptorRequest> requests,
        PPettyCashReimbursementStatus targetStatus,
        CancellationToken ct = default,
        UserId? sendToAcceptorId = null)
    {
        _ = entity.Acceptors.Where(w => !requests.Select(s => s.Id).Contains(w.Id.Value))
                  .Iter(s => entity.RemoveAcceptor(s));

        var userIds = requests.Select(r => r.UserId).ToList();
        var users = await this.ValidateUsersAsync(userIds, ct);

        _ = requests.Where(w => !w.Id.HasValue)
                    .Join(
                        users,
                        req => UserId.From(req.UserId),
                        usr => usr.Id,
                        (req, usr) =>
                        {
                            var newAcceptor = PPettyCashReimbursementAcceptor.Create(req.AcceptorType, usr, req.Sequence);

                            var status = req.AcceptorType switch
                            {
                                AcceptorType.AccountingApprover => AcceptorStatus.Pending,
                                AcceptorType.AccountingOperator => AcceptorStatus.Pending,
                                _ => AcceptorStatus.Draft,
                            };

                            newAcceptor.SetStatus(status);
                            newAcceptor.SetSendToAcceptorId(sendToAcceptorId);

                            return newAcceptor;
                        })
                    .Iter(r => entity.AddAcceptor(r));

        foreach (var existing in entity.Acceptors)
        {
            var match = requests.FirstOrDefault(e => e.UserId == existing.UserId && existing.Type == e.AcceptorType);

            if (match != null)
            {
                existing.SetSequence(match.Sequence)
                        .SetActive();
                existing.SetSendToAcceptorId(sendToAcceptorId);

                if ((match.AcceptorType == AcceptorType.AccountingApprover || match.AcceptorType == AcceptorType.AccountingOperator) &&
                    targetStatus is PPettyCashReimbursementStatus.WaitingAccountingApproval)
                {
                    existing.SetStatus(AcceptorStatus.Pending);
                }
            }
        }

        if (targetStatus is PPettyCashReimbursementStatus.WaitingApproval)
        {
            entity.Acceptors.Iter(r => r.Pending());
        }

        if (entity.Status == PPettyCashReimbursementStatus.WaitingAccountingApproval)
        {
            var accountingApprovers = entity.Acceptors
                                            .Where(a => !a.IsDeleted && (a.Type == AcceptorType.AccountingApprover || a.Type == AcceptorType.AccountingOperator))
                                            .OrderBy(a => a.Type == AcceptorType.AccountingOperator ? 0 : 1)
                                            .ThenBy(a => a.Sequence)
                                            .ToList();

            if (accountingApprovers.Any())
            {
                foreach (var approver in entity.Acceptors.Where(a => !a.IsDeleted))
                {
                    approver.SetCurrent(false);
                }

                var firstPending = accountingApprovers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);
                if (firstPending != null)
                {
                    var firstSeq = firstPending.Sequence;
                    foreach (var a in accountingApprovers.Where(a => a.Sequence == firstSeq && a.Status == AcceptorStatus.Pending))
                    {
                        a.SetCurrent(true);
                    }
                }
            }
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

    protected async Task UpsertItems(
        PPettyCashReimbursement entity,
        IEnumerable<PettyCashReimbursementItemDto> requests,
        CancellationToken ct = default)
    {
        var items = entity.Items?.ToList() ?? new List<PPettyCashReimbursementItems>();
        var newEntities = new List<PPettyCashReimbursementItems>();

        foreach (var dto in requests.OrderBy(r => r.Sequence))
        {
            PettyCashGLAccountId glId = PettyCashGLAccountId.From(dto.PettyCashGlAccountId);
            var gl = await this.dbContext.PPettyCashGLAccounts
                               .SingleOrDefaultAsync(g => g.Id == glId, ct);

            if (gl == null)
            {
                this.ThrowError($"Pw119 GL Account with ID {dto.PettyCashGlAccountId} not found.", StatusCodes.Status404NotFound);
            }

            if (dto.Id.HasValue)
            {
                var existing = items.FirstOrDefault(i => i.Id.Value == dto.Id.Value);

                if (existing != null)
                {
                    existing.SetValue(dto.Sequence, gl!);
                    newEntities.Add(existing);

                    continue;
                }
            }

            var created = PPettyCashReimbursementItems
                          .Create()
                          .SetValue(dto.Sequence, gl);
            newEntities.Add(created);
        }

        // Add new
        foreach (var toAdd in newEntities.Where(e => items.All(a => a.Id != e.Id)))
        {
            entity.AddDetail(toAdd);
        }

        // Remove obsolete
        var removeList = items.Where(a => newEntities.All(e => e.Id != a.Id)).ToList();

        foreach (var rem in removeList)
        {
            entity.RemoveDetail(rem);
        }
    }

    protected async Task UpsertAttachments(PPettyCashReimbursement entity, AttachmentsDtoWithId[] attachments)
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

        newFiles.Map(f => PPettyCashReimbursementAttachments.Create(ParameterCode.From(f.DocumentTypeCode), FileId.From(f.FileId), f.FileName, f.Sequence, f.IsPublic))
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