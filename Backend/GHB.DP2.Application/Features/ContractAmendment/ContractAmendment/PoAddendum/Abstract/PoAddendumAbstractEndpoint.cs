namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoAddendum.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoAddendum;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record PaymentTermRequest(
    Guid? Id,
    string Title,
    int PaymentTermNo,
    int LeadTime,
    DateTimeOffset DeliveryDate,
    decimal InstallmentPercentage,
    decimal Amount,
    decimal AdvanceDeductionAmount,
    decimal PerformanceDeductionAmount,
    string Description,
    int Sequence);

public abstract partial class PoAddendumAbstractEndpoint<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;

    protected PoAddendumAbstractEndpoint(
        ILogger logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    protected async Task UpsertAcceptors(CamContractAmendmentPoAddendum entity, AcceptorRequest[] requests, CamContractAmendmentPoAddendumStatus status, CancellationToken ct)
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
                            var acceptor = CamContractAmendmentPoAddendumAcceptor.Create(req.AcceptorType, usr, req.Sequence, status).SetIsUnableToPerformDuties(req.IsUnableToPerformDuties ?? false);

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
                existing
                    .SetIsUnableToPerformDuties(match.IsUnableToPerformDuties ?? false)
                    .SetSequence(match.Sequence);

                switch (entity.Status)
                {
                    case CamContractAmendmentPoAddendumStatus.Draft or CamContractAmendmentPoAddendumStatus.Rejected when status == CamContractAmendmentPoAddendumStatus.WaitingCommitteeApproval:
                        existing.SetStatus(AcceptorStatus.Pending);

                        break;
                }
            }
        }
    }

    protected async Task UpsertAssignee(CamContractAmendmentPoAddendum entity, AssigneeRequest[] requests, CancellationToken cancellationToken = default)
    {
        _ = entity.Assignees.Where(w => !requests.Select(s => s.Id).Contains(w.Id.Value))
                  .Iter(s => entity.RemoveAssignee(s));

        var userIds = requests.Select(r => r.UserId).ToList();
        var users = await this.ValidateUsersAsync(userIds, cancellationToken);

        _ = requests.Where(w => !w.Id.HasValue)
                    .Join(
                        users,
                        req => UserId.From(req.UserId),
                        usr => usr.Id,
                        (req, usr) => CamContractAmendmentPoAddendumAssignee.Create(req.AssigneeGroup, req.AssigneeType, usr, req.Sequence))
                    .Iter(r => entity.AddAssignee(r));

        foreach (var existing in entity.Assignees.ToList())
        {
            var match = requests.FirstOrDefault(e => e.UserId == existing.UserId);

            if (match != null)
            {
                existing.SetSequence(match.Sequence);
            }
        }
    }

    protected void UpsertPaymentTerm(
        CamContractAmendmentPoAddendum entity,
        IEnumerable<PaymentTermRequest> requests)
    {
        var details = entity.PaymentTerms ?? new List<CamContractAmendmentPoAddendumPaymentTerm>();
        var newEntities = requests.Select(dto =>
        {
            var existing = details.FirstOrDefault(c => c.Id.Value == dto.Id);

            if (existing != null)
            {
                var values = new CamContractAmendmentPoAddendumPaymentTerm.PaymentTermValues(
                    dto.Title,
                    dto.PaymentTermNo,
                    dto.LeadTime,
                    dto.DeliveryDate,
                    dto.InstallmentPercentage,
                    dto.Amount,
                    dto.AdvanceDeductionAmount,
                    dto.PerformanceDeductionAmount,
                    dto.Description,
                    dto.Sequence);
                existing.SetValues(values);

                return existing;
            }

            var newValues = new CamContractAmendmentPoAddendumPaymentTerm.PaymentTermValues(
                dto.Title,
                dto.PaymentTermNo,
                dto.LeadTime,
                dto.DeliveryDate,
                dto.InstallmentPercentage,
                dto.Amount,
                dto.AdvanceDeductionAmount,
                dto.PerformanceDeductionAmount,
                dto.Description,
                dto.Sequence);

            return CamContractAmendmentPoAddendumPaymentTerm.Create()
                                                            .SetValues(newValues);
        }).ToList();

        // Add new
        foreach (var toAdd in newEntities.Where(e => details.All(a => !a.Id.Equals(e.Id))))
        {
            entity.AddPaymentTerm(toAdd);
        }

        // Remove obsolete
        foreach (var toRemove in details.Where(a => !newEntities.Any(e => e.Id.Equals(a.Id))).ToList())
        {
            entity.RemovePaymentTerm(toRemove);
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

    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        CamContractAmendmentPoAddendum entity,
        CamContractAmendmentPoAddendumDocumentType documentType,
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
            parentDirectory: $"{DocumentTemplateGroups.CAMPoAddendum}/{entity.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
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