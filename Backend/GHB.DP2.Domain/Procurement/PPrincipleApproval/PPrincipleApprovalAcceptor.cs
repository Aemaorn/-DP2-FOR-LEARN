namespace GHB.DP2.Domain.Procurement.PPrincipleApproval;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;

public partial class PPrincipleApprovalAcceptor : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public virtual PPrincipleApproval PPrincipleApproval { get; init; }

    public record AcceptorInfoData(
        AcceptorType Type,
        UserId UserId,
        EmployeeCode EmployeeCode,
        string FullName,
        string FullPositionName,
        string BusinessUnitName,
        int Sequence
    );

    public static PPrincipleApprovalAcceptor Create(
        AcceptorType type,
        SuUser user,
        int sequence,
        PPrincipleApprovalStatus status)
    {
        var acceptor = new PPrincipleApprovalAcceptor
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

        acceptor.Status = GetInitialStatus(status, type);

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

    public Unit SetAcceptorStatus(AcceptorStatus status)
    {
        this.Status = status;

        return unit;
    }

    private static AcceptorStatus GetInitialStatus(PPrincipleApprovalStatus status, AcceptorType type)
    {
        if (status == PPrincipleApprovalStatus.WaitingUnitApproval && type == AcceptorType.DepartmentDirectorAgree)
        {
            return AcceptorStatus.Pending;
        }

        if (status == PPrincipleApprovalStatus.WaitingAcceptance && type == AcceptorType.Approver)
        {
            return AcceptorStatus.Pending;
        }

        return AcceptorStatus.Draft;
    }

    public bool IsCurrentApprover()
    {
        var pendingActiveAcceptors =
            this.GetPendingActiveAcceptorsWithSameType();

        return this.IsCurrentRegularApprover(pendingActiveAcceptors);
    }

    private IOrderedEnumerable<PPrincipleApprovalAcceptor> GetPendingActiveAcceptorsWithSameType()
    {
        return this.PPrincipleApproval.PrincipleApprovalAcceptors
                   .Where(a =>
                       a.Type == this.Type &&
                       a is { IsActive: true, Status: AcceptorStatus.Pending })
                   .OrderBy(a => a.Sequence);
    }

    private bool IsCurrentRegularApprover(IOrderedEnumerable<PPrincipleApprovalAcceptor> acceptors)
    {
        var currentApprover = acceptors.FirstOrDefault();

        return currentApprover != null && currentApprover.UserId == this.UserId;
    }
}