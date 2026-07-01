namespace GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoAddendum;

using System.Text.Json;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Constants;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;

public class CamContractAmendmentPoAddendumAcceptor : AcceptorInfoEntity
{
    public override AcceptorId Id { get; init; }

    public bool IsUnableToPerformDuties { get; private set; }

    public ParameterCode? CommitteePositionsCode { get; private set; }

    public virtual SuParameter? CommitteePosition { get; init; }

    public virtual ContractAmendment.CamContractAmendmentPoAddendum.CamContractAmendmentPoAddendum CamContractAmendmentPoAddendum { get; init; }

    public record AcceptorInfoData(
        AcceptorType Type,
        UserId UserId,
        EmployeeCode EmployeeCode,
        string FullName,
        string FullPositionName,
        string BusinessUnitName,
        int Sequence
    );

    public static CamContractAmendmentPoAddendumAcceptor Create(
        AcceptorType type,
        SuUser user,
        int sequence,
        CamContractAmendmentPoAddendumStatus status)
    {
        var acceptor = new CamContractAmendmentPoAddendumAcceptor
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

    public CamContractAmendmentPoAddendumAcceptor SetIsUnableToPerformDuties(bool isUnableToPerformDuties)
    {
        this.IsUnableToPerformDuties = isUnableToPerformDuties;

        return this;
    }

    private static AcceptorStatus GetInitialStatus(CamContractAmendmentPoAddendumStatus status)
    {
        if (status == CamContractAmendmentPoAddendumStatus.WaitingCommitteeApproval)
        {
            return AcceptorStatus.Pending;
        }

        return AcceptorStatus.Draft;
    }

    public CamContractAmendmentPoAddendumAcceptor SetCommitteePositionsCode(
        ParameterCode? committeePositionsCode)
    {
        this.CommitteePositionsCode = committeePositionsCode;

        return this;
    }

    public bool IsCurrentApprover()
    {
        var pendingActiveAcceptors =
            this.GetPendingActiveAcceptorsWithSameType();

        if (this.CamContractAmendmentPoAddendum.Status == CamContractAmendmentPoAddendumStatus.WaitingCommitteeApproval)
        {
            return this.IsCurrentCommitteeApprover(pendingActiveAcceptors);
        }

        return this.IsCurrentRegularApprover(pendingActiveAcceptors);
    }

    private IOrderedEnumerable<CamContractAmendmentPoAddendumAcceptor> GetPendingActiveAcceptorsWithSameType()
    {
        return this.CamContractAmendmentPoAddendum.Acceptors
                   .Where(a =>
                       a.Type == this.Type &&
                       a is { IsActive: true, Status: AcceptorStatus.Pending, IsUnableToPerformDuties: false })
                   .OrderBy(a => a.Sequence);
    }

    private bool IsCurrentCommitteeApprover(IEnumerable<CamContractAmendmentPoAddendumAcceptor> acceptors)
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

    private bool IsCurrentRegularApprover(IOrderedEnumerable<CamContractAmendmentPoAddendumAcceptor> acceptors)
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