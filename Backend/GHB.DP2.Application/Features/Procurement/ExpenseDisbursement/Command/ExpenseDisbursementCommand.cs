namespace GHB.DP2.Application.Features.Procurement.ExpenseDisbursement.Command;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Domain.Procurement.PExpenseDisbursement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public record ExpenseDisbursementCommand(
    PExpenseDisbursementSourceType SourceType,
    Guid SourceId,
    IEnumerable<AttachmentsDtoWithId> Attachments,
    UserId UserId) : ICommand<bool>;

public class ExpenseDisbursementCommandHandler : ICommandHandler<ExpenseDisbursementCommand, bool>
{
    private readonly IServiceProvider serviceProvider;

    public ExpenseDisbursementCommandHandler(
        IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task<bool> ExecuteAsync(ExpenseDisbursementCommand command, CancellationToken ct)
    {
        await using var scope = this.serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Dp2DbContext>();
        var fileServiceClient = scope.ServiceProvider.GetRequiredService<IFileServiceClient>();

        var entity = await dbContext.PExpenseDisbursements
                                    .Where(p => p.Id == PExpenseDisbursementId.From(command.SourceId) && p.SourceType == command.SourceType)
                                    .FirstOrDefaultAsync(ct);

        if (entity is null)
        {
            return false;
        }

        await UpsertAttachments(fileServiceClient, entity, command.Attachments);
        await dbContext.SaveChangesAsync(ct);

        return true;
    }

    private static async Task UpsertAttachments(IFileServiceClient fileServiceClient, PExpenseDisbursement entity, IEnumerable<AttachmentsDtoWithId> attachments)
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
            await fileServiceClient.DeleteAsync(id, CancellationToken.None);
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
}