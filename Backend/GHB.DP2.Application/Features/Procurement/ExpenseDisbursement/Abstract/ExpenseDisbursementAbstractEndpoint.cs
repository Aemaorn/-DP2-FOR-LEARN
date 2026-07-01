namespace GHB.DP2.Application.Features.Procurement.ExpenseDisbursement.Abstract;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Domain.Procurement.P79Clause2;
using GHB.DP2.Domain.Procurement.PExpenseDisbursement;
using GHB.DP2.Domain.Procurement.PPettyCashReimbursement;
using GHB.DP2.Domain.Procurement.Pw119;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GlAccountDto(
    Guid? Id,
    int Sequence,
    string SoId,
    string BudgetTypeCode,
    string GlAccountCode,
    string? ProjectNumber,
    decimal Amount);

public abstract class ExpenseDisbursementAbstractEndpoint<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    protected ExpenseDisbursementAbstractEndpoint(
        ILogger logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    protected async ValueTask<IEnumerable<AttachmentsDtoWithId>> GetAttachments(PExpenseDisbursement entity, UserId userId)
    {
        var attachments = entity.SourceType switch
        {
            PExpenseDisbursementSourceType.W119 => this.GetW119AttachmentsAsync(entity.SourceId, userId),
            PExpenseDisbursementSourceType.Clause79_2 => this.Get79Clause2AttachmentsAsync(entity.SourceId, userId),
            PExpenseDisbursementSourceType.ContractGuaranteeReturn => this.GetContractGuaranteeReturnAttachmentsAsync(entity.SourceId, userId),
            PExpenseDisbursementSourceType.PettyCashReimbursement => this.GetPettyCashReimbursementAttachmentsAsync(entity.SourceId, userId),
            _ => Task.FromResult(Enumerable.Empty<AttachmentsDtoWithId>()),
        };

        return await attachments;
    }

    private async Task<IEnumerable<AttachmentsDtoWithId>> GetW119AttachmentsAsync(Guid w119Id, UserId userId, CancellationToken ct = default)
    {
        var w119 = await this.dbContext.Pw119s
                             .Include(pw119 => pw119.Attachments)
                             .FirstOrDefaultAsync(p => p.Id == Pw119Id.From(w119Id), ct);

        if (w119 == null)
        {
            return new List<AttachmentsDtoWithId>();
        }

        var grouped = w119.Attachments.GroupBy(a => a.DocumentTypeCode);

        return grouped.Select(group => new AttachmentsDtoWithId(
            DocumentTypeCode: group.Key.ToString(),
            FileAttachments:
            [
                .. group.Select(attachment => new FileAttachmentsWithId(
                    Id: null,
                    FileId: attachment.Id.Value,
                    FileName: attachment.FileName,
                    Sequence: attachment.Sequence,
                    IsPublic: attachment.IsPublic,
                    CreatedBy: userId.Value))
            ]));
    }

    private async Task<IEnumerable<AttachmentsDtoWithId>> Get79Clause2AttachmentsAsync(Guid p79Clause2Id, UserId userId, CancellationToken ct = default)
    {
        var p79Clause2 = await this.dbContext.P79Clause2s
                                   .Include(pw119 => pw119.Attachments)
                                   .FirstOrDefaultAsync(p => p.Id == P79Clause2Id.From(p79Clause2Id), ct);

        if (p79Clause2 == null)
        {
            return new List<AttachmentsDtoWithId>();
        }

        var grouped = p79Clause2.Attachments.GroupBy(a => a.DocumentTypeCode);

        return grouped.Select(group => new AttachmentsDtoWithId(
            DocumentTypeCode: group.Key.ToString(),
            FileAttachments:
            [
                .. group.Select(attachment => new FileAttachmentsWithId(
                    Id: null,
                    FileId: attachment.Id.Value,
                    FileName: attachment.FileName,
                    Sequence: attachment.Sequence,
                    IsPublic: attachment.IsPublic,
                    CreatedBy: userId.Value))
            ]));
    }

    private async Task<IEnumerable<AttachmentsDtoWithId>> GetContractGuaranteeReturnAttachmentsAsync(Guid contractGuaranteeReturnId, UserId userId, CancellationToken ct = default)
    {
        var contractGuaranteeReturn = await this.dbContext.CmContractGuaranteeReturns
                                                .Include(pw119 => pw119.Attachments)
                                                .FirstOrDefaultAsync(p => p.Id == CmContractGuaranteeReturnId.From(contractGuaranteeReturnId), ct);

        if (contractGuaranteeReturn == null)
        {
            return new List<AttachmentsDtoWithId>();
        }

        var grouped = contractGuaranteeReturn.Attachments.GroupBy(a => a.DocumentTypeCode);

        return grouped.Select(group => new AttachmentsDtoWithId(
            DocumentTypeCode: group.Key.ToString(),
            FileAttachments:
            [
                .. group.Select(attachment => new FileAttachmentsWithId(
                    Id: null,
                    FileId: attachment.Id.Value,
                    FileName: attachment.FileName,
                    Sequence: attachment.Sequence,
                    IsPublic: attachment.IsPublic,
                    CreatedBy: userId.Value))
            ]));
    }

    private async Task<IEnumerable<AttachmentsDtoWithId>> GetPettyCashReimbursementAttachmentsAsync(Guid pettyCashReimbursementId, UserId userId, CancellationToken ct = default)
    {
        var pettyCashReimbursement = await this.dbContext.PPettyCashReimbursements
                                               .Include(pw119 => pw119.Attachments)
                                               .FirstOrDefaultAsync(p => p.Id == PPettyCashReimbursementId.From(pettyCashReimbursementId), ct);

        if (pettyCashReimbursement == null)
        {
            return new List<AttachmentsDtoWithId>();
        }

        var grouped = pettyCashReimbursement.Attachments.GroupBy(a => a.DocumentTypeCode);

        return grouped.Select(group => new AttachmentsDtoWithId(
            DocumentTypeCode: group.Key.ToString(),
            FileAttachments:
            [
                .. group.Select(attachment => new FileAttachmentsWithId(
                    Id: null,
                    FileId: attachment.Id.Value,
                    FileName: attachment.FileName,
                    Sequence: attachment.Sequence,
                    IsPublic: attachment.IsPublic,
                    CreatedBy: userId.Value))
            ]));
    }

    protected async Task UpsertAttachments(PExpenseDisbursement entity, IEnumerable<AttachmentsDtoWithId> attachments)
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

        var removeFileIds = entity.Attachments
                                  .Where(w => !fileList.Select(s => s.Id).Contains(w.Id.Value))
                                  .Map(s =>
                                  {
                                      entity.RemoveAttachment(s);

                                      return s.FileId;
                                  }).ToArray();

        foreach (var id in removeFileIds)
        {
            await this.fileServiceClient.DeleteAsync(id, CancellationToken.None);
        }

        fileList.Where(w => !w.Id.HasValue)
                .Map(f => PExpenseDisbursementAttachment.Create(ParameterCode.From(f.DocumentTypeCode), FileId.From(f.FileId), f.FileName, f.Sequence, f.IsPublic))
                .Iter(r => entity.AddAttachment(r));

        foreach (var existing in entity.Attachments)
        {
            var match = fileList
                        .Where(w => w.Id.HasValue)
                        .FirstOrDefault(e => e.Id == existing.Id.Value);

            if (match != null)
            {
                existing.SetIsPublic(match.IsPublic)
                        .SetSequence(match.Sequence)
                        .SetDocumentType(ParameterCode.From(match.DocumentTypeCode));
            }
        }
    }

    protected void UpsertDetails(
        PExpenseDisbursement entity,
        IEnumerable<GlAccountDto> requests)
    {
        var details = entity.GlAccounts ?? new List<PExpenseDisbursementGlAccount>();
        var newEntities = requests.Select(dto =>
        {
            var existing = details.FirstOrDefault(c => c.Id.Value == dto.Id);

            if (existing != null)
            {
                existing.SetValue(
                    dto.Sequence,
                    dto.SoId,
                    ParameterCode.From(dto.BudgetTypeCode),
                    ParameterCode.From(dto.GlAccountCode),
                    dto.ProjectNumber,
                    dto.Amount);

                return existing;
            }

            return PExpenseDisbursementGlAccount.Create()
                                                .SetValue(
                                                    dto.Sequence,
                                                    dto.SoId,
                                                    ParameterCode.From(dto.BudgetTypeCode),
                                                    ParameterCode.From(dto.GlAccountCode),
                                                    dto.ProjectNumber,
                                                    dto.Amount);
        }).ToList();

        // Add new
        foreach (var toAdd in newEntities.Where(e => details.All(a => !a.Id.Equals(e.Id))))
        {
            entity.AddGlAccount(toAdd);
        }

        // Remove obsolete
        foreach (var toRemove in details.Where(a => !newEntities.Any(e => e.Id.Equals(a.Id))).ToList())
        {
            entity.RemoveGlAccount(toRemove);
        }
    }
}