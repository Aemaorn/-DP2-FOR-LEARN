namespace GHB.DP2.Domain.Procurement.PpPurchaseRequisition;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;

public record PpPurchaseRequisitionAcceptorInfoData(
    AcceptorType Type,
    UserId UserId,
    EmployeeCode EmployeeCode,
    string FullName,
    string FullPositionName,
    string BusinessUnitName,
    int Sequence
);

public record PpPurchaseRequisitionAcceptorResponse(
    Guid Id,
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
    bool? IsCurrent,
    string? DepartmentCode,
    Guid? DelegateeUserId
);

public partial class PpPurchaseRequisitionAcceptors : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public PpPurchaseRequisitionId PpPurchaseRequisitionId { get; init; }

    public virtual PpPurchaseRequisition PpPurchaseRequisition { get; init; }

    public bool ArePreviousAcceptorsApproved(IEnumerable<PpPurchaseRequisitionAcceptors> acceptors)
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

    public static PpPurchaseRequisitionAcceptors Create(
        PpPurchaseRequisitionId ppPurchaseRequisitionId,
        PpPurchaseRequisitionAcceptorInfoData info,
        PurchaseRequisitionStatus status)
    {
        var acceptor = new PpPurchaseRequisitionAcceptors
        {
            Id = AcceptorId.New(),
            PpPurchaseRequisitionId = ppPurchaseRequisitionId,
        };

        acceptor.SetType(AcceptorType.Approver)
                .SetUser(
                    info.UserId,
                    info.EmployeeCode,
                    info.FullName,
                    info.FullPositionName,
                    info.BusinessUnitName)
                .SetSequence(info.Sequence)
                .SetActive();

        acceptor.Status = GetInitialStatus(status, AcceptorType.Approver);

        return acceptor;
    }

    public Unit Update(
        PpPurchaseRequisitionAcceptorInfoData info,
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

    private static AcceptorStatus GetInitialStatus(PurchaseRequisitionStatus status, AcceptorType type)
    {
        var waitingStatus = status == PurchaseRequisitionStatus.WaitingApproval || status == PurchaseRequisitionStatus.WaitingAssign;

        if (waitingStatus && type == AcceptorType.Approver)
        {
            return AcceptorStatus.Pending;
        }

        return AcceptorStatus.Draft;
    }
}