namespace GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;

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

public record CamCertificateRequisitionAcceptorResponse(
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

public partial class CamCertificateRequisitionAcceptor : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public bool IsUnableToPerformDuties { get; private set; }

    public ParameterCode? CommitteePositionsCode { get; private set; }

    public virtual SuParameter? CommitteePosition { get; init; }

    public CamCertificateRequisitionId CertificateRequisitionId { get; init; }

    public virtual CamCertificateRequisition CertificateRequisition { get; init; }

    public record AcceptorInfoData(
        AcceptorType Type,
        UserId UserId,
        EmployeeCode EmployeeCode,
        string FullName,
        string FullPositionName,
        string BusinessUnitName,
        int Sequence
    );

    public static CamCertificateRequisitionAcceptor Create(
        CamCertificateRequisitionId certificateRequisitionId,
        AcceptorType type,
        SuUser user,
        int sequence,
        CamCertificateRequisitionStatus status)
    {
        var newAcceptor = new CamCertificateRequisitionAcceptor
        {
            Id = AcceptorId.New(),
            CertificateRequisitionId = certificateRequisitionId,
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

        newAcceptor.Status = GetInitialStatus(status);

        return newAcceptor;
    }

    public CamCertificateRequisitionAcceptor SetIsUnableToPerformDuties(bool isUnableToPerformDuties)
    {
        this.IsUnableToPerformDuties = isUnableToPerformDuties;

        return this;
    }

    public CamCertificateRequisitionAcceptor SetCommitteePositionsCode(ParameterCode? committeePositionsCode)
    {
        this.CommitteePositionsCode = committeePositionsCode;

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

    public bool IsCurrentApprover()
    {
        var pendingActiveAcceptors =
            this.CertificateRequisition.Acceptors
                .Where(w => w is { IsActive: true, Status: AcceptorStatus.Pending })
                .OrderBy(s => s.Sequence);

        return this.IsCurrentCommitteeApprover(pendingActiveAcceptors);
    }

    private bool IsCurrentCommitteeApprover(IEnumerable<CamCertificateRequisitionAcceptor> acceptors)
    {
        var camCertificateAcceptors =
            acceptors.ToArray();
        var hasNonChairmanAcceptors =
            camCertificateAcceptors.Any(a => !a.IsBoardChairman());

        return camCertificateAcceptors.Any(a =>
            a.UserId == this.UserId &&
            !a.IsUnableToPerformDuties &&
            (hasNonChairmanAcceptors ? !a.IsBoardChairman() : a.IsBoardChairman()));
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

    private static AcceptorStatus GetInitialStatus(CamCertificateRequisitionStatus status)
    {
        if (status == CamCertificateRequisitionStatus.WaitingForCommitteeApproval)
        {
            return AcceptorStatus.Pending;
        }

        return AcceptorStatus.Draft;
    }
}