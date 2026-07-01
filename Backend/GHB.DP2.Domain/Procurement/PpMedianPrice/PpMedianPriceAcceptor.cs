namespace GHB.DP2.Domain.Procurement.PpMedianPrice;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Constants;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using System.Text.Json;

public partial class PpMedianPriceAcceptor : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public bool IsUnableToPerformDuties { get; private set; }

    public ParameterCode? CommitteePositionsCode { get; private set; }

    public virtual SuParameter? CommitteePosition { get; init; }

    public virtual PpMedianPrice MedianPrice { get; init; }

    public new PpMedianPriceAcceptor Clone()
    {
        var newAcceptor = new PpMedianPriceAcceptor
        {
            Id = AcceptorId.New(),
            IsUnableToPerformDuties = this.IsUnableToPerformDuties,
            CommitteePositionsCode = this.CommitteePositionsCode,
            CommitteePosition = this.CommitteePosition,
        };

        newAcceptor
            .SetType(this.Type)
            .SetUser(
                this.UserId,
                this.EmployeeCode,
                this.FullName,
                this.PositionName,
                this.BusinessUnitName)
            .SetDelegatee(this.DelegateeId)
            .SetSequence(this.Sequence)
            .SetActive()
            .Draft();

        return newAcceptor;
    }

    public bool ArePreviousAcceptorsApproved(IEnumerable<PpMedianPriceAcceptor> acceptors)
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

        if (this.MedianPrice.Status == MedianPriceStatus.WaitingCommitteeApproval)
        {
            return this.IsCurrentCommitteeApprover(pendingActiveAcceptors);
        }

        return this.IsCurrentRegularApprover(pendingActiveAcceptors);
    }

    private IOrderedEnumerable<PpMedianPriceAcceptor> GetPendingActiveAcceptorsWithSameType()
    {
        return this.MedianPrice.Acceptors
                   .Where(a =>
                       a.Type == this.Type &&
                       a is { IsActive: true, Status: AcceptorStatus.Pending, IsUnableToPerformDuties: false })
                   .OrderBy(a => a.Sequence);
    }

    private bool IsCurrentCommitteeApprover(IEnumerable<PpMedianPriceAcceptor> acceptors)
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

    private bool IsCurrentRegularApprover(IOrderedEnumerable<PpMedianPriceAcceptor> acceptors)
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

    public PpMedianPriceAcceptor SetCommitteePositionsCode(
        ParameterCode? committeePositionsCode)
    {
        this.CommitteePositionsCode = committeePositionsCode;

        return this;
    }

    public PpMedianPriceAcceptor SetIsUnableToPerformDuties(bool isUnableToPerformDuties)
    {
        this.IsUnableToPerformDuties = isUnableToPerformDuties;

        return this;
    }

    public static PpMedianPriceAcceptor Create(
        AcceptorType type,
        SuUser user,
        int sequence,
        BusinessUnitId workBusinessUnitId)
    {
        return Create(
            AcceptorId.New(),
            type,
            user,
            sequence,
            workBusinessUnitId);
    }

    public static PpMedianPriceAcceptor Create(
        AcceptorId id,
        AcceptorType type,
        SuUser user,
        int sequence,
        BusinessUnitId workBusinessUnitId)
    {
        var acceptor = new PpMedianPriceAcceptor
        {
            Id = id,
            IsUnableToPerformDuties = false,
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
                        user.Employee.ConvertPositionName(workBusinessUnitId),
                        user.Employee.View.BusinessUnitName)
                    .SetSequence(sequence)
                    .SetActive()
                    .Draft();

        return acceptor;
    }
}