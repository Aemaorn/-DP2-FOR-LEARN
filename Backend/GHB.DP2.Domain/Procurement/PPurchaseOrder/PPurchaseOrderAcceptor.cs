namespace GHB.DP2.Domain.Procurement.PPurchaseOrder;

using System.Text.Json;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Constants;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;

public partial class PPurchaseOrderAcceptor : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public bool IsUnableToPerformDuties { get; private set; }

    public ParameterCode? CommitteePositionsCode { get; private set; }

    public virtual SuParameter? CommitteePosition { get; init; }

    public virtual PPurchaseOrder PPurchaseOrder { get; init; }

    public bool ArePreviousAcceptorsApproved(IEnumerable<PPurchaseOrderAcceptor> acceptors)
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
        if (this.PPurchaseOrder == null)
        {
            return false;
        }

        var pendingActiveAcceptors =
            this.GetPendingActiveAcceptorsWithSameType();

        if (this.PPurchaseOrder.Status == PurchaseOrderStatus.WaitingCommitteeApproval)
        {
            return this.IsCurrentCommitteeApprover(pendingActiveAcceptors);
        }

        return this.IsCurrentRegularApprover(pendingActiveAcceptors);
    }

    private IOrderedEnumerable<PPurchaseOrderAcceptor> GetPendingActiveAcceptorsWithSameType()
    {
        return this.PPurchaseOrder.Acceptors
                   .Where(a =>
                       a.Type == this.Type &&
                       a.IsActive &&
                       a.Status == AcceptorStatus.Pending)
                   .OrderBy(a => a.Sequence);
    }

    private bool IsCurrentCommitteeApprover(IEnumerable<PPurchaseOrderAcceptor> acceptors)
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

    private bool IsCurrentRegularApprover(IOrderedEnumerable<PPurchaseOrderAcceptor> acceptors)
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

    public PPurchaseOrderAcceptor SetCommitteePositionsCode(
        ParameterCode? committeePositionsCode)
    {
        this.CommitteePositionsCode = committeePositionsCode;

        return this;
    }

    public PPurchaseOrderAcceptor SetIsUnableToPerformDuties(bool isUnableToPerformDuties)
    {
        this.IsUnableToPerformDuties = isUnableToPerformDuties;

        return this;
    }

    public static PPurchaseOrderAcceptor Create(
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

    public static PPurchaseOrderAcceptor Create(
        AcceptorId id,
        AcceptorType type,
        SuUser user,
        int sequence,
        BusinessUnitId workBusinessUnitId)
    {
        var acceptor = new PPurchaseOrderAcceptor
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