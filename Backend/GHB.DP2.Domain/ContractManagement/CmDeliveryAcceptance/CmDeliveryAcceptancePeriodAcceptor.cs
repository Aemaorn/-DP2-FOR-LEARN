namespace GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;

using System.Text.Json;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Constants;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;

public record AcceptorInfoData(
    AcceptorType Type,
    UserId UserId,
    EmployeeCode EmployeeCode,
    string FullName,
    string FullPositionName,
    string BusinessUnitName,
    int Sequence
);

public record CmDeliveryAcceptanceAcceptorResponse(
    Guid Id,
    AcceptorType Type,
    Guid UserId,
    string EmployeeCode,
    string FullName,
    string PositionName,
    string BusinessUnitName,
    int Sequence,
    Guid? DelegateId,
    AcceptorStatus Status,
    DateTimeOffset? ActionAt,
    string? Remark,
    bool IsActive,
    bool? IsCurrent
);

public partial class CmDeliveryAcceptancePeriodAcceptor : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public bool IsUnableToPerformDuties { get; private set; }

    public ParameterCode? CommitteePositionsCode { get; private set; }

    public virtual SuParameter? CommitteePosition { get; private set; }

    public CmDeliveryAcceptancePeriodId DeliveryAcceptancePeriodId { get; init; }

    public virtual CmDeliveryAcceptancePeriod DeliveryAcceptancePeriod { get; init; }

    public record AcceptorInfoData(
        AcceptorType Type,
        UserId UserId,
        EmployeeCode EmployeeCode,
        string FullName,
        string FullPositionName,
        string BusinessUnitName,
        int Sequence
    );

    public static CmDeliveryAcceptancePeriodAcceptor Create(
        CmDeliveryAcceptancePeriodId deliveryAcceptancePeriodId,
        AcceptorType type,
        SuUser user,
        int sequence,
        CmDeliveryAcceptancePeriodStatus status)
    {
        var newAcceptor = new CmDeliveryAcceptancePeriodAcceptor
        {
            Id = AcceptorId.New(),
            DeliveryAcceptancePeriodId = deliveryAcceptancePeriodId,
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

        newAcceptor.Status =
            GetInitialStatus(
                status,
                AcceptorType.Approver);

        return newAcceptor;
    }

    public CmDeliveryAcceptancePeriodAcceptor SetIsUnableToPerformDuties(bool isUnableToPerformDuties)
    {
        this.IsUnableToPerformDuties = isUnableToPerformDuties;

        return this;
    }

    public CmDeliveryAcceptancePeriodAcceptor SetCommitteePositionsCode(
        ParameterCode? committeePositionsCode)
    {
        this.CommitteePositionsCode = committeePositionsCode;

        return this;
    }

    public CmDeliveryAcceptancePeriodAcceptor SetCommitteePositions(SuParameter? committeePosition)
    {
        this.CommitteePosition = committeePosition;

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

    private static AcceptorStatus GetInitialStatus(
        CmDeliveryAcceptancePeriodStatus status,
        AcceptorType type)
    {
        if (status == CmDeliveryAcceptancePeriodStatus.WaitingAcceptance &&
            type == AcceptorType.Approver)
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

    private IOrderedEnumerable<CmDeliveryAcceptancePeriodAcceptor> GetPendingActiveAcceptors()
    {
        return this.DeliveryAcceptancePeriod.Acceptors
                   .Where(a =>
                       a.Type == this.Type &&
                       a is { IsActive: true, Status: AcceptorStatus.Pending })
                   .OrderBy(a => a.Sequence);
    }

    private bool IsCurrentApprover(IOrderedEnumerable<CmDeliveryAcceptancePeriodAcceptor> acceptors)
    {
        var currentApprover = acceptors.FirstOrDefault();

        return currentApprover != null && currentApprover.UserId == this.UserId;
    }

    public bool ArePreviousAcceptorsApproved(IEnumerable<CmDeliveryAcceptancePeriodAcceptor> acceptors)
    {
        var previousAcceptors =
            acceptors.Where(a =>
                         a.Type == this.Type &&
                         a.Sequence < this.Sequence &&
                         a.IsActive)
                     .ToList();

        return previousAcceptors.Count == 0 ||
               previousAcceptors.All(a => a.Status == AcceptorStatus.Approved);
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

    public bool IsCommittee()
    {
        var committeePosition =
            this.CommitteePosition?.Values
                .GetValueOrDefault(PositionOnBoard.IsCommittee);

        if (committeePosition is null)
        {
            return true;
        }

        if (committeePosition.Value is null)
        {
            return true;
        }

        var isCommittee = (JsonElement)committeePosition.Value;

        return isCommittee.GetBoolean();
    }
}