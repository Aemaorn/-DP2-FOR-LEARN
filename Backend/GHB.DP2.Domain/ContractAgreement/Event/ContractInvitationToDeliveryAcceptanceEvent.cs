namespace GHB.DP2.Domain.ContractAgreement.Event;

using Codehard.Common.DomainModel;
using FastEndpoints;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;

public record ContractInvitationToDeliveryAcceptanceEvent(
    ContractInvitationId Id,
    DateTimeOffset Timestamp) : IDomainEvent<ContractInvitationId>, IEvent
{
    public static ContractInvitationToDeliveryAcceptanceEvent Create(ContractInvitationId id)
    {
        return new ContractInvitationToDeliveryAcceptanceEvent(id, DateTimeOffset.UtcNow);
    }
}