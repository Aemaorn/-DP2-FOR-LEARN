namespace GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;

using System.Text.Json;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Constants;
using GHB.DP2.Domain.SystemUtility;

public class CamContractAmendmentExtendChangeAcceptor : AcceptorInfoEntity
{
    public override AcceptorId Id { get; init; }

    public bool IsUnableToPerformDuties { get; private set; }

    public ParameterCode? CommitteePositionsCode { get; private set; }

    public virtual SuParameter? CommitteePosition { get; init; }

    public virtual CamContractAmendmentExtendChange CamContractAmendmentExtendChange { get; init; }

    public static CamContractAmendmentExtendChangeAcceptor Create(
        AcceptorType type,
        SuUser user,
        int sequence,
        ContractAmendmentExtendChangeStatus status)
    {
        var acceptor = new CamContractAmendmentExtendChangeAcceptor
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

        acceptor.Status = GetInitialStatus(status);

        return acceptor;
    }

    public CamContractAmendmentExtendChangeAcceptor SetIsUnableToPerformDuties(bool isUnableToPerformDuties)
    {
        this.IsUnableToPerformDuties = isUnableToPerformDuties;

        return this;
    }

    public CamContractAmendmentExtendChangeAcceptor SetCommitteePositionsCode(ParameterCode? committeePositionsCode)
    {
        this.CommitteePositionsCode = committeePositionsCode;

        return this;
    }

    public bool IsCurrentApprover()
    {
        var pendingActiveAcceptors =
            this.GetPendingActiveAcceptorsWithSameType();

        if (this.CamContractAmendmentExtendChange.Status == ContractAmendmentExtendChangeStatus.WaitingCommitteeApproval)
        {
            return this.IsCurrentCommitteeApprover(pendingActiveAcceptors);
        }

        return this.IsCurrentRegularApprover(pendingActiveAcceptors);
    }

    private static AcceptorStatus GetInitialStatus(ContractAmendmentExtendChangeStatus status)
    {
        return status == ContractAmendmentExtendChangeStatus.WaitingApproval
            ? AcceptorStatus.Pending
            : AcceptorStatus.Draft;
    }

    private IOrderedEnumerable<CamContractAmendmentExtendChangeAcceptor> GetPendingActiveAcceptorsWithSameType()
    {
        return this.CamContractAmendmentExtendChange.Acceptors
                   .Where(a =>
                       a.Type == this.Type &&
                       a is { IsActive: true, Status: AcceptorStatus.Pending, IsUnableToPerformDuties: false })
                   .OrderBy(a => a.Sequence);
    }

    private bool IsCurrentCommitteeApprover(IEnumerable<CamContractAmendmentExtendChangeAcceptor> acceptors)
    {
        var ppMedianPriceAcceptors =
            acceptors.ToArray();
        var hasNonChairmanAcceptors =
            ppMedianPriceAcceptors.Any(a => !a.IsBoardChairman());

        return ppMedianPriceAcceptors.Any(a =>
            a.UserId == this.UserId &&
            !a.IsUnableToPerformDuties &&
            (hasNonChairmanAcceptors ? !a.IsBoardChairman() : a.IsBoardChairman()));
    }

    private bool IsCurrentRegularApprover(IOrderedEnumerable<CamContractAmendmentExtendChangeAcceptor> acceptors)
    {
        var currentApprover = acceptors.FirstOrDefault();

        return currentApprover != null && currentApprover.UserId == this.UserId;
    }

    public bool IsBoardChairman()
    {
        if (this.CommitteePosition is null)
        {
            return false;
        }

        var committeePosition =
            this.CommitteePosition.Values
                .GetValueOrDefault(PositionOnBoard.IsBoardChairmanKey);

        if (committeePosition is null)
        {
            return false;
        }

        if (committeePosition.Value is null)
        {
            return false;
        }

        var isBoardChairmanJson = (JsonElement)committeePosition.Value;

        return isBoardChairmanJson.ToBoolean();
    }
}