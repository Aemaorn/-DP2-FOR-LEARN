namespace GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;

using System.Text.Json;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Constants;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;

public partial class CmContractGuaranteeReturnAcceptor : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public bool IsUnableToPerformDuties { get; private set; }

    public ParameterCode? CommitteePositionsCode { get; private set; }

    public virtual SuParameter? CommitteePosition { get; init; }

    public virtual CmContractGuaranteeReturn CmContractGuaranteeReturn { get; init; }

    public record AcceptorInfoData(
        AcceptorType Type,
        UserId UserId,
        EmployeeCode EmployeeCode,
        string FullName,
        string FullPositionName,
        string BusinessUnitName,
        int Sequence
    );

    public static CmContractGuaranteeReturnAcceptor Create(
        AcceptorType type,
        SuUser user,
        int sequence,
        CmContractGuaranteeReturnStatus status)
    {
        var acceptor = new CmContractGuaranteeReturnAcceptor
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

    public bool ArePreviousAcceptorsApproved(IEnumerable<CmContractGuaranteeReturnAcceptor> acceptors)
    {
        // Get all active acceptors of the same type with lower sequence numbers
        var previousAcceptors =
            acceptors.Where(a =>
                         a.Type == this.Type &&
                         a.Sequence < this.Sequence &&
                         a.IsActive)
                     .ToList();

        return previousAcceptors.Count == 0 ||
               previousAcceptors.All(a => a.Status == AcceptorStatus.Approved);
    }

    public bool IsCurrentApprover()
    {
        var pendingActiveAcceptors =
            this.GetPendingActiveAcceptorsWithSameType();

        if (this.CmContractGuaranteeReturn.Status == CmContractGuaranteeReturnStatus.WaitingCommitteeApproval)
        {
            return this.IsCurrentCommitteeApprover(pendingActiveAcceptors);
        }

        return this.IsCurrentRegularApprover(pendingActiveAcceptors);
    }

    private IOrderedEnumerable<CmContractGuaranteeReturnAcceptor> GetPendingActiveAcceptorsWithSameType()
    {
        return this.CmContractGuaranteeReturn.Acceptors
                   .Where(a =>
                       a.Type == this.Type &&
                       a is { IsActive: true, Status: AcceptorStatus.Pending, IsUnableToPerformDuties: false })
                   .OrderBy(a => a.Sequence);
    }

    private bool IsCurrentCommitteeApprover(IEnumerable<CmContractGuaranteeReturnAcceptor> acceptors)
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

    private bool IsCurrentRegularApprover(IOrderedEnumerable<CmContractGuaranteeReturnAcceptor> acceptors)
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

    public CmContractGuaranteeReturnAcceptor SetCommitteePositionsCode(
        ParameterCode? committeePositionsCode)
    {
        this.CommitteePositionsCode = committeePositionsCode;

        return this;
    }

    public CmContractGuaranteeReturnAcceptor SetIsUnableToPerformDuties(bool isUnableToPerformDuties)
    {
        this.IsUnableToPerformDuties = isUnableToPerformDuties;

        return this;
    }

    public Unit Update(
        AcceptorInfoData info,
        AcceptorStatus status)
    {
        this.SetType(info.Type)
            .SetUser(
                info.UserId,
                info.EmployeeCode,
                info.FullName,
                info.FullPositionName,
                info.BusinessUnitName)
            .SetSequence(info.Sequence)
            .SetStatus(status)
            .SetActive();

        return unit;
    }

    public Unit SetAcceptorStatus(AcceptorStatus status)
    {
        this.Status = status;

        return unit;
    }

    private static AcceptorStatus GetInitialStatus(CmContractGuaranteeReturnStatus status)
    {
        if (status == CmContractGuaranteeReturnStatus.WaitingCommitteeApproval)
        {
            return AcceptorStatus.Pending;
        }

        return AcceptorStatus.Draft;
    }
}