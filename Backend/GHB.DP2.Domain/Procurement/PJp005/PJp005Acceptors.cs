namespace GHB.DP2.Domain.Procurement.PJp005;

using GHB.DP2.Domain.Common;
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

public record PJp005AcceptorResponse(
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

public partial class PJp005Acceptors : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public PJp005Id PJp005Id { get; init; }

    public virtual PJp005 PJp005 { get; init; }

    public record AcceptorInfoData(
        AcceptorType Type,
        UserId UserId,
        EmployeeCode EmployeeCode,
        string FullName,
        string FullPositionName,
        string BusinessUnitName,
        int Sequence
    );

    public static PJp005Acceptors Create(
        PJp005Id pJp005Id,
        AcceptorType type,
        SuUser user,
        int sequence,
        PJp005Status status,
        BusinessUnitId workBusinessUnitId)
    {
        var newAcceptor = new PJp005Acceptors
        {
            Id = AcceptorId.New(),
            PJp005Id = pJp005Id,
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
                user.Employee.ConvertPositionName(workBusinessUnitId),
                user.Employee.View.BusinessUnitName)
            .SetSequence(sequence)
            .SetActive();

        newAcceptor.Status =
            GetInitialStatus(
                status,
                AcceptorType.Approver);

        return newAcceptor;
    }

    public Unit Update(
        AcceptorInfoData info,
        AcceptorStatus status,
        BusinessUnitId workBusinessUnitId)
    {
        var fullPositionName =
            !this.User.IsNull()
                ? this.User.Employee.ConvertPositionName(workBusinessUnitId)
                : info.FullPositionName;

        this.SetType(info.Type)
            .SetUser(
                info.UserId,
                info.EmployeeCode,
                info.FullName,
                fullPositionName,
                info.BusinessUnitName)
            .SetSequence(info.Sequence)
            .SetStatus(status)
            .SetActive();

        return unit;
    }

    private static AcceptorStatus GetInitialStatus(
        PJp005Status status,
        AcceptorType type)
    {
        if (status == PJp005Status.WaitingApproval &&
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

    private IOrderedEnumerable<PJp005Acceptors> GetPendingActiveAcceptors()
    {
        return this.PJp005.Acceptors
                   .Where(a =>
                       a.Type == this.Type &&
                       a is { IsActive: true, Status: AcceptorStatus.Pending })
                   .OrderBy(a => a.Sequence);
    }

    private bool IsCurrentApprover(IOrderedEnumerable<PJp005Acceptors> acceptors)
    {
        var currentApprover = acceptors.FirstOrDefault();

        return currentApprover != null && currentApprover.UserId == this.UserId;
    }

    public bool ArePreviousAcceptorsApproved(IEnumerable<PJp005Acceptors> acceptors)
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
}