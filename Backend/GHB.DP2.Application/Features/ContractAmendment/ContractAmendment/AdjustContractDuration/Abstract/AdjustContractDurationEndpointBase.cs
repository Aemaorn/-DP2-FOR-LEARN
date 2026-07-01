namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration.Dto;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract partial class AdjustContractDurationEndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;

    protected AdjustContractDurationEndpointBase(ILogger logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    protected Dp2DbContext DbContext => this.dbContext;

    protected async Task UpsertAcceptors(
        CamContractAmendmentExtendChange entity,
        AcceptorRequest[] requests,
        ContractAmendmentExtendChangeStatus status,
        CancellationToken ct)
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
                            var acceptor = CamContractAmendmentExtendChangeAcceptor
                                           .Create(req.AcceptorType, usr, req.Sequence, status)
                                           .SetIsUnableToPerformDuties(req.IsUnableToPerformDuties ?? false);

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
                    case ContractAmendmentExtendChangeStatus.Draft or
                        ContractAmendmentExtendChangeStatus.Rejected when status == ContractAmendmentExtendChangeStatus.WaitingCommitteeApproval:
                        existing.SetStatus(AcceptorStatus.Pending);

                        break;
                }
            }
        }
    }

    protected async Task UpsertAssignee(
        CamContractAmendmentExtendChange entity,
        AssigneeRequest[] requests,
        CancellationToken cancellationToken = default)
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
                        (req, usr) =>
                            CamContractAmendmentExtendChangeAssignee
                                .Create(req.AssigneeGroup, req.AssigneeType, usr, req.Sequence))
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

    protected void UpsertPaymentTerms(
        CamContractAmendmentExtendChange entity,
        IEnumerable<AdjustContractDurationPaymentTermInfo> paymentTermsRequest)
    {
        var paymentTerms =
            paymentTermsRequest
                .Map(pt => pt.MapToEntity())
                .ToHashSet();

        _ = entity.PaymentTerms
                  .Join(
                      paymentTerms,
                      existing => existing.Id,
                      updated => updated.Id,
                      (existing, updated) =>
                      {
                          existing
                              .SetLeadTime(updated.LeadTime)
                              .SetDeliveryDate(updated.DeliveryDate)
                              .SetInstallmentPercent(updated.InstallmentPercent)
                              .SetAmount(updated.Amount)
                              .SetAdvanceDeductionAmount(updated.AdvanceDeductionAmount)
                              .SetPerformanceDeductionAmount(updated.PerformanceDeductionAmount)
                              .SetDescription(updated.Description);

                          return existing;
                      })
                  .ToHashSet();

        _ = entity.PaymentTerms
                  .Where(pt => !paymentTerms.Select(p => p.Id).Contains(pt.Id))
                  .Iter(pt => entity.RemovePaymentTerm(pt));

        _ = paymentTerms
            .Where(pt => !entity.PaymentTerms.Select(p => p.Id).Contains(pt.Id))
            .Iter(pt => entity.AddPaymentTerm(pt));
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
        CamContractAmendmentExtendChange entity,
        ExtendChangeAcceptorDocumentType documentType,
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
            parentDirectory: $"{DocumentTemplateGroups.CAMExtendChange}/{entity.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
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