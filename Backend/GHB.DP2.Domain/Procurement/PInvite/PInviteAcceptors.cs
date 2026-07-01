namespace GHB.DP2.Domain.Procurement.PInvite;

using System.Text.Json;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Constants;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;

public partial class PInviteAcceptors : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public bool IsUnableToPerformDuties { get; private set; }

    public ParameterCode? CommitteePositionsCode { get; private set; }

    public virtual SuParameter? CommitteePosition { get; init; }

    public virtual PInvite Invite { get; init; }

    public record AcceptorInfoData(
        AcceptorType Type,
        UserId UserId,
        EmployeeCode EmployeeCode,
        string FullName,
        string FullPositionName,
        string BusinessUnitName,
        int Sequence
    );

    public static PInviteAcceptors Create(
        AcceptorInfoData info,
        PInviteStatus status)
    {
        var acceptor = new PInviteAcceptors
        {
            Id = AcceptorId.New(),
        };

        acceptor.SetType(info.Type)
                .SetUser(
                    info.UserId,
                    info.EmployeeCode,
                    info.FullName,
                    info.FullPositionName,
                    info.BusinessUnitName)
                .SetSequence(info.Sequence)
                .SetActive();

        acceptor.Status = GetInitialStatus(status);

        return acceptor;
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

    public PInviteAcceptors SetCommitteePositionsCode(
        ParameterCode? committeePositionsCode)
    {
        this.CommitteePositionsCode = committeePositionsCode;

        return this;
    }

    public PInviteAcceptors SetIsUnableToPerformDuties(bool isUnableToPerformDuties)
    {
        this.IsUnableToPerformDuties = isUnableToPerformDuties;

        return this;
    }

    public bool IsCurrentApprover(AcceptorType type)
    {
        var pendingActiveAcceptors =
            this.GetPendingActiveAcceptorsWithSameType();

        return this.IsCurrentCommitteeApprover(pendingActiveAcceptors, type);
    }

    private IOrderedEnumerable<PInviteAcceptors> GetPendingActiveAcceptorsWithSameType()
    {
        return this.Invite.Acceptors
                   .Where(a =>
                       a.Type == this.Type &&
                       a is { IsActive: true, Status: AcceptorStatus.Pending, IsUnableToPerformDuties: false })
                   .OrderBy(a => a.Sequence);
    }

    private bool IsCurrentCommitteeApprover(IEnumerable<PInviteAcceptors> acceptors, AcceptorType type)
    {
        if (type == AcceptorType.Approver)
        {
            var inviteAcceptor = acceptors.FirstOrDefault();
            return inviteAcceptor != null && inviteAcceptor.UserId == this.UserId;
        }

        var inviteAcceptors =
            acceptors.ToArray();
        var hasNonChairmanAcceptors =
            inviteAcceptors.Any(a => !a.IsBoardChairman());

        var ssd = inviteAcceptors.Any(a =>
            a.UserId == this.UserId &&
            !a.IsUnableToPerformDuties &&
            (hasNonChairmanAcceptors ? !a.IsBoardChairman() : a.IsBoardChairman()));

        return inviteAcceptors.Any(a =>
            a.UserId == this.UserId &&
            !a.IsUnableToPerformDuties &&
            (hasNonChairmanAcceptors ? !a.IsBoardChairman() : a.IsBoardChairman()));
    }

    private static AcceptorStatus GetInitialStatus(PInviteStatus status)
    {
        return status == PInviteStatus.WaitingApproval ? AcceptorStatus.Pending : AcceptorStatus.Draft;
    }
}