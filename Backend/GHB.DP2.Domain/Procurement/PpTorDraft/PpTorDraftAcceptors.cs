namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using System.Text.Json;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Constants;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;

public partial class PpTorDraftAcceptors : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public bool IsUnableToPerformDuties { get; private set; }

    public ParameterCode? CommitteePositionsCode { get; private set; }

    public virtual SuParameter? CommitteePosition { get; init; }

    public virtual PpTorDraft PpTorDraft { get; init; }

    public record AcceptorInfoData(
        AcceptorType Type,
        UserId UserId,
        EmployeeCode EmployeeCode,
        string FullName,
        string FullPositionName,
        string BusinessUnitName,
        int Sequence
    );

    public static PpTorDraftAcceptors Create(
        AcceptorInfoData info,
        TorDraftStatus status)
    {
        var acceptor = new PpTorDraftAcceptors
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

        acceptor.Status = GetInitialStatus(status, info.Type);

        return acceptor;
    }

    public new PpTorDraftAcceptors Clone()
    {
        var newAcceptor = new PpTorDraftAcceptors
        {
            Id = AcceptorId.New(),
        };

        newAcceptor.SetType(this.Type)
                   .SetUser(
                       this.UserId,
                       this.EmployeeCode,
                       this.FullName,
                       this.PositionName,
                       this.BusinessUnitName)
                   .SetSequence(this.Sequence)
                   .SetActive()
                   .Draft();

        newAcceptor.SetCommitteePositionsCode(this.CommitteePositionsCode);
        newAcceptor.SetIsUnableToPerformDuties(this.IsUnableToPerformDuties);

        return newAcceptor;
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

    public PpTorDraftAcceptors SetCommitteePositionsCode(
        ParameterCode? committeePositionsCode)
    {
        this.CommitteePositionsCode = committeePositionsCode;

        return this;
    }

    public PpTorDraftAcceptors SetIsUnableToPerformDuties(bool isUnableToPerformDuties)
    {
        this.IsUnableToPerformDuties = isUnableToPerformDuties;

        return this;
    }

    public Unit Update(
        AcceptorInfoData info)
    {
        this.SetType(info.Type)
            .SetUser(
                info.UserId,
                info.EmployeeCode,
                info.FullName,
                info.FullPositionName,
                info.BusinessUnitName)
            .SetSequence(info.Sequence)
            .SetActive();

        return unit;
    }

    public Unit SetAcceptorStatus(AcceptorStatus status)
    {
        this.Status = status;

        return unit;
    }

    private static AcceptorStatus GetInitialStatus(TorDraftStatus status, AcceptorType type)
    {
        if (status == TorDraftStatus.WaitingCommitteeApproval && type == AcceptorType.TorDraftCommittee)
        {
            return AcceptorStatus.Pending;
        }

        if (status == TorDraftStatus.WaitingApproval && type == AcceptorType.Approver)
        {
            return AcceptorStatus.Pending;
        }

        return AcceptorStatus.Draft;
    }

    public bool IsCurrentApprover()
    {
        var pendingActiveAcceptors =
            this.GetPendingActiveAcceptorsWithSameType();

        if (this.PpTorDraft.Status == TorDraftStatus.WaitingCommitteeApproval)
        {
            return this.IsCurrentCommitteeApprover(pendingActiveAcceptors);
        }

        return this.IsCurrentRegularApprover(pendingActiveAcceptors);
    }

    private IOrderedEnumerable<PpTorDraftAcceptors> GetPendingActiveAcceptorsWithSameType()
    {
        return this.PpTorDraft.PpTorDraftAcceptors
                   .Where(a =>
                       a.Type == this.Type &&
                       a is { IsActive: true, Status: AcceptorStatus.Pending, IsUnableToPerformDuties: false })
                   .OrderBy(a => a.Sequence);
    }

    private bool IsCurrentCommitteeApprover(IEnumerable<PpTorDraftAcceptors> acceptors)
    {
        var torAcceptors =
            acceptors.ToArray();
        var hasNonChairmanAcceptors =
            torAcceptors.Any(a => !a.IsBoardChairman());

        return torAcceptors.Any(a =>
            a.UserId == this.UserId &&
            !a.IsUnableToPerformDuties &&
            (hasNonChairmanAcceptors ? !a.IsBoardChairman() : a.IsBoardChairman()));
    }

    private bool IsCurrentRegularApprover(IOrderedEnumerable<PpTorDraftAcceptors> acceptors)
    {
        var currentApprover = acceptors.FirstOrDefault();

        return currentApprover != null && currentApprover.UserId == this.UserId;
    }
}