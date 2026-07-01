namespace GHB.DP2.Domain.ContractAgreement.CaContractInvitation;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;

public partial class CaContractInvitationAcceptor : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public ContractInvitationId ContractInvitationId { get; init; }

    public virtual CaContractInvitation ContractInvitation { get; init; }

    public static CaContractInvitationAcceptor Create(
        ContractInvitationId contractInvitationId,
        AcceptorType type,
        SuUser user,
        int sequence,
        ContractInvitationStatus status)
    {
        var newAcceptor = new CaContractInvitationAcceptor
        {
            Id = AcceptorId.New(),
            ContractInvitationId = contractInvitationId,
        };

        if (user.Employee.View is null)
        {
            throw new ArgumentException("View is null");
        }

        newAcceptor
            .SetType(type)
            .SetUser(
                user.Id,
                user.EmployeeCode,
                user.Employee.View.FullName,
                user.Employee.View.FullPositionName,
                user.Employee.View.BusinessUnitName)
            .SetSequence(sequence)
            .SetActive();

        newAcceptor.Status = GetInitialStatus(status);

        return newAcceptor;
    }

    private static AcceptorStatus GetInitialStatus(
        ContractInvitationStatus status)
    {
        if (status == ContractInvitationStatus.WaitingApproval)
        {
            return AcceptorStatus.Pending;
        }

        return AcceptorStatus.Draft;
    }

    public bool IsCurrentApprover()
    {
        var pendingApprover = this.GetPendingActiveAcceptors();

        return this.IsCurrentApprover(pendingApprover);
    }

    private IOrderedEnumerable<CaContractInvitationAcceptor> GetPendingActiveAcceptors()
    {
        return
            this.ContractInvitation.Acceptors
                .Where(a =>
                    a.Type == this.Type &&
                    a is
                    {
                        IsActive: true,
                        Status: AcceptorStatus.Pending
                    })
                .OrderBy(a => a.Sequence);
    }

    private bool IsCurrentApprover(IOrderedEnumerable<CaContractInvitationAcceptor> acceptors)
    {
        var currentApprover = acceptors.FirstOrDefault();

        return
            currentApprover != null &&
            currentApprover.UserId == this.UserId;
    }

    public bool ArePreviousAcceptorsApproved(IEnumerable<CaContractInvitationAcceptor> acceptors)
    {
        var previousAcceptors =
            acceptors.Where(a =>
                         a.Type == this.Type &&
                         a.Sequence < this.Sequence &&
                         a.IsActive)
                     .ToList();

        return previousAcceptors.Count == 0 ||
               previousAcceptors.All(
                   a => a.Status == AcceptorStatus.Approved);
    }
}