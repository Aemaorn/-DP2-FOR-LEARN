namespace GHB.DP2.Domain.Procurement.PpAppoint;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;

public record AcceptorAppointInfoData(
    AcceptorType AcceptorType,
    UserId UserId,
    EmployeeCode EmployeeCode,
    string FullName,
    string FullPositionName,
    string BusinessUnitName,
    int Sequence
);

public record AcceptorAppointResponse(
    Guid? Id,
    AcceptorType AcceptorType,
    Guid UserId,
    string EmployeeCode,
    string FullName,
    string PositionName,
    string BusinessUnitName,
    int Sequence,
    Guid? DelegateeId,
    AcceptorStatus Status,
    DateTimeOffset? ActionAt,
    string? Remark,
    bool IsActive,
    bool? IsCurrent
);

public class PpAppointAcceptors : AcceptorInfoEntity
{
    public override AcceptorId Id { get; init; }

    public PpAppointId PpAppointId { get; init; }

    public virtual PpAppoint PpAppoint { get; init; }

    public static PpAppointAcceptors Create(
        PpAppointId ppAppointId,
        AcceptorAppointInfoData info,
        AppointStatus status)
    {
        var acceptor = new PpAppointAcceptors
        {
            Id = AcceptorId.New(),
            PpAppointId = ppAppointId,
        };

        acceptor.SetType(info.AcceptorType)
                .SetUser(
                    info.UserId,
                    info.EmployeeCode,
                    info.FullName,
                    info.FullPositionName,
                    info.BusinessUnitName)
                .SetSequence(info.Sequence)
                .SetActive();

        acceptor.Status = GetInitialStatus(status, info.AcceptorType);

        return acceptor;
    }

    public PpAppointAcceptors Clone(PpAppointId ppAppointId)
    {
        var acceptor = new PpAppointAcceptors
        {
            Id = AcceptorId.From(Guid.NewGuid()),
            PpAppointId = ppAppointId,
        };

        acceptor.SetType(this.Type)
                .SetUser(
                    this.UserId,
                    this.EmployeeCode,
                    this.FullName,
                    this.PositionName,
                    this.BusinessUnitName)
                .SetSequence(this.Sequence)
                .SetStatus(this.Status)
                .SetActionAt(this.ActionAt)
                .SetRemark(this.Remark)
                .SetActive()
                .Draft();

        return acceptor;
    }

    public Unit Update(
        AcceptorAppointInfoData info,
        AcceptorStatus status)
    {
        this.SetType(info.AcceptorType)
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

    private static AcceptorStatus GetInitialStatus(AppointStatus status, AcceptorType type)
    {
        if (status == AppointStatus.WaitingApproval && type == AcceptorType.Approver)
        {
            return AcceptorStatus.Pending;
        }

        return AcceptorStatus.Draft;
    }
}