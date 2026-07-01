namespace GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;

using System.Text.Json;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Constants;
using GHB.DP2.Domain.SystemUtility;

public class CamContractAmendmentWaiveOrReducePenaltyAcceptor : AcceptorInfoEntity
{
    public override AcceptorId Id { get; init; }

    public bool IsUnableToPerformDuties { get; private set; }

    public ParameterCode? CommitteePositionsCode { get; private set; }

    public virtual SuParameter? CommitteePosition { get; init; }

    public virtual CamContractAmendmentWaiveOrReducePenalty CamContractAmendmentWaiveOrReducePenalty { get; init; }

    public static CamContractAmendmentWaiveOrReducePenaltyAcceptor Create(
        AcceptorType type,
        SuUser user,
        int sequence,
        CamContractAmendmentWaiveOrReducePenaltyStatus status)
    {
        var acceptor = new CamContractAmendmentWaiveOrReducePenaltyAcceptor
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

    public CamContractAmendmentWaiveOrReducePenaltyAcceptor SetIsUnableToPerformDuties(bool isUnableToPerformDuties)
    {
        this.IsUnableToPerformDuties = isUnableToPerformDuties;

        return this;
    }

    public CamContractAmendmentWaiveOrReducePenaltyAcceptor SetCommitteePositionsCode(ParameterCode? committeePositionsCode)
    {
        this.CommitteePositionsCode = committeePositionsCode;

        return this;
    }

    public bool IsCurrentApprover()
    {
        var pendingActiveAcceptors =
            this.GetPendingActiveAcceptorsWithSameType();

        if (this.CamContractAmendmentWaiveOrReducePenalty.Status == CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingCommitteeApproval)
        {
            return this.IsCurrentCommitteeApprover(pendingActiveAcceptors);
        }

        return this.IsCurrentRegularApprover(pendingActiveAcceptors);
    }

    private static AcceptorStatus GetInitialStatus(CamContractAmendmentWaiveOrReducePenaltyStatus status)
    {
        return status == CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingApproval
            ? AcceptorStatus.Pending
            : AcceptorStatus.Draft;
    }

    private IOrderedEnumerable<CamContractAmendmentWaiveOrReducePenaltyAcceptor> GetPendingActiveAcceptorsWithSameType()
    {
        return this.CamContractAmendmentWaiveOrReducePenalty.Acceptors
                   .Where(a =>
                       a.Type == this.Type &&
                       a is { IsActive: true, Status: AcceptorStatus.Pending, IsUnableToPerformDuties: false })
                   .OrderBy(a => a.Sequence);
    }

    private bool IsCurrentCommitteeApprover(IEnumerable<CamContractAmendmentWaiveOrReducePenaltyAcceptor> acceptors)
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

    private bool IsCurrentRegularApprover(IOrderedEnumerable<CamContractAmendmentWaiveOrReducePenaltyAcceptor> acceptors)
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