namespace GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoSap;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;

public class CamContractAmendmentPoSapAcceptor : AcceptorInfoEntity
{
    public override AcceptorId Id { get; init; }

    public virtual CamContractAmendmentPoSap CamContractAmendmentPoSap { get; init; }

    public static CamContractAmendmentPoSapAcceptor Create(
        AcceptorType type,
        SuUser user,
        int sequence)
    {
        var acceptor = new CamContractAmendmentPoSapAcceptor
        {
            Id = AcceptorId.New(),
        };

        if (user.Employee.View is null)
        {
            throw new ArgumentException("View is null");
        }

        _ = acceptor.SetType(type)
                    .SetUser(
                        user.Id,
                        user.EmployeeCode,
                        user.Employee.View.FullName,
                        user.Employee.View.FullPositionName,
                        user.Employee.View.BusinessUnitName)
                    .SetSequence(sequence)
                    .SetActive();

        return acceptor;
    }

    public bool IsCurrentApprover()
    {
        var pendingActiveAcceptors =
            this.GetPendingActiveAcceptorsWithSameType();

        return this.IsCurrentRegularApprover(pendingActiveAcceptors);
    }

    private IOrderedEnumerable<CamContractAmendmentPoSapAcceptor> GetPendingActiveAcceptorsWithSameType()
    {
        return this.CamContractAmendmentPoSap.Acceptors
                   .Where(a =>
                       a.Type == this.Type &&
                       a is { IsActive: true, Status: AcceptorStatus.Pending })
                   .OrderBy(a => a.Sequence);
    }

    private bool IsCurrentRegularApprover(IOrderedEnumerable<CamContractAmendmentPoSapAcceptor> acceptors)
    {
        var currentApprover = acceptors.FirstOrDefault();

        return currentApprover != null && currentApprover.UserId == this.UserId;
    }
}