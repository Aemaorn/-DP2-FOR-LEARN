namespace GHB.DP2.Domain.ContractAgreement.Event;

using Codehard.Common.DomainModel;
using FastEndpoints;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;

public record ContractInvitationApproveEvent(
    ContractInvitationId Id,
    DateTimeOffset Timestamp) : IDomainEvent<ContractInvitationId>, IEvent
{
    public static ContractInvitationApproveEvent Create(ContractInvitationId id)
    {
        return new ContractInvitationApproveEvent(id, DateTimeOffset.UtcNow);
    }
}