namespace GHB.DP2.Domain.ContractAgreement.Event;

using Codehard.Common.DomainModel;
using FastEndpoints;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;

public record ContractDraftApproveEvent(
    ContractDraftId Id,
    DateTimeOffset Timestamp) : IDomainEvent<ContractDraftId>, IEvent
{
    public static ContractDraftApproveEvent Create(ContractDraftId id)
    {
        return new ContractDraftApproveEvent(id, DateTimeOffset.UtcNow);
    }
}