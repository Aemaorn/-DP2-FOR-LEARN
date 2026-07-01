namespace GHB.DP2.Application.Extensions;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;

public static class DelegatorExtensions
{
    private static readonly HashSet<AcceptorType> DelegatableAcceptorTypes =
    [
        AcceptorType.DepartmentDirectorAgree,
        AcceptorType.Approver,
        AcceptorType.AcceptorSign,
        AcceptorType.Accounting,
        AcceptorType.AccountingApprover,
        AcceptorType.AccountingConfirmer,
    ];

    public static bool IsDelegatableType(AcceptorType type) =>
        DelegatableAcceptorTypes.Contains(type);

    /// <summary>
    /// Builds the position label for a delegated acceptor/assignee.
    /// Skips the "ปฏิบัติหน้าที่แทน" suffix when the delegatee holds the same
    /// position as the original owner (e.g. acting in the same role).
    /// </summary>
    private static string BuildDelegationPositionName(string? delegateePositionName, string? originalPositionName) =>
        string.Equals(delegateePositionName?.Trim(), originalPositionName?.Trim(), StringComparison.Ordinal)
            ? originalPositionName ?? string.Empty
            : $"{delegateePositionName} ปฏิบัติหน้าที่แทน {originalPositionName}";

    public static TAcceptor DelegatorToAcceptor<TAcceptor>(this TAcceptor acceptor)
        where TAcceptor : IHasAcceptor
    {
        if (acceptor.User is null)
        {
            return acceptor;
        }

        var acceptorPositionName = acceptor.User.Employee.View?.FullPositionName;

        if (acceptor.Delegatee is not null
            && acceptor.Status is AcceptorStatus.Approved or AcceptorStatus.Rejected)
        {
            var delegatee =
                (TAcceptor)acceptor
                    .SetUser(
                        acceptor.UserId,
                        acceptor.EmployeeCode,
                        acceptor.Delegatee.UserFullName,
                        BuildDelegationPositionName(acceptor.Delegatee.FullPositionName, acceptorPositionName),
                        acceptor.User.Employee.View?.BusinessUnitName ?? string.Empty);

            return delegatee;
        }

        var clonedAcceptor = acceptor.Clone();

        var sendToAcceptorBusinessUnit =
            acceptor.SendToAcceptor?.Employee?.PrimaryEmployeePosition?.BusinessUnit;

        var activeDelegatee =
            clonedAcceptor
                .User?.GetActiveDelegatee(sendToAcceptorBusinessUnit);

        if (activeDelegatee is null)
        {
            return acceptor;
        }

        var delegatorUser = activeDelegatee.SuUser;

        var delegatorAcceptor =
            clonedAcceptor
                .SetUser(
                    acceptor.UserId,
                    acceptor.EmployeeCode,
                    delegatorUser.Employee.View!.FullName,
                    BuildDelegationPositionName(delegatorUser.Employee.View.FullPositionName, acceptorPositionName),
                    acceptor.User?.Employee?.View?.BusinessUnitName ?? string.Empty)
                .SetDelegatee(
                    activeDelegatee.Id,
                    activeDelegatee);

        return (TAcceptor)delegatorAcceptor.Clone();
    }

    /// <summary>
    /// Returns UserIds to send notification to.
    /// For delegatable types: both original UserId and Delegatee's SuUserId.
    /// For non-delegatable types: only original UserId.
    /// </summary>
    public static IEnumerable<UserId> GetNotificationTargets<TAcceptor>(this TAcceptor acceptor)
        where TAcceptor : IHasAcceptor
    {
        yield return acceptor.UserId;

        if (!IsDelegatableType(acceptor.Type))
        {
            yield break;
        }

        var mapped = acceptor.DelegatorToAcceptor();

        if (mapped.Delegatee?.SuUserId is not null && mapped.Delegatee.SuUserId != acceptor.UserId)
        {
            yield return mapped.Delegatee.SuUserId;
        }
    }

    /// <summary>
    /// Returns UserIds to send notification to for assignees.
    /// Both original UserId and Delegatee's SuUserId if delegated.
    /// </summary>
    public static IEnumerable<UserId> GetAssigneeNotificationTargets<TAssignee>(this TAssignee assignee)
        where TAssignee : IHasAssignee
    {
        yield return assignee.UserId;

        var mapped = assignee.DelegatorToAssignee();

        if (mapped.Delegatee?.SuUserId is not null && mapped.Delegatee.SuUserId != assignee.UserId)
        {
            yield return mapped.Delegatee.SuUserId;
        }
    }

    public static TAssignee DelegatorToAssignee<TAssignee>(this TAssignee assignee)
        where TAssignee : IHasAssignee
    {
        if (assignee.User is null)
        {
            return assignee;
        }

        var assigneePositionName = assignee.User.Employee.View?.FullPositionName;

        if (assignee.Delegatee is not null
            && assignee.Status is AssigneeStatus.Assigned or AssigneeStatus.Rejected)
        {
            var delegatee =
                (TAssignee)assignee
                    .SetUser(
                        assignee.UserId,
                        assignee.EmployeeCode,
                        assignee.Delegatee.UserFullName,
                        BuildDelegationPositionName(assignee.Delegatee.FullPositionName, assigneePositionName),
                        assignee.User.Employee.View?.BusinessUnitName ?? string.Empty);

            return delegatee;
        }

        var clonedAssignee = assignee.Clone();

        var sendToAcceptorBusinessUnit =
            assignee.SendToAcceptor?.Employee?.PrimaryEmployeePosition?.BusinessUnit;

        var activeDelegatee =
            clonedAssignee
                .User?.GetActiveDelegatee(sendToAcceptorBusinessUnit);

        if (activeDelegatee is null)
        {
            return assignee;
        }

        var delegatorUser = activeDelegatee.SuUser;

        var delegatorAssignee =
            clonedAssignee
                .SetUser(
                    assignee.UserId,
                    assignee.EmployeeCode,
                    delegatorUser.Employee.View!.FullName,
                    BuildDelegationPositionName(delegatorUser.Employee.View.FullPositionName, assigneePositionName),
                    assignee.User?.Employee.View?.BusinessUnitName ?? string.Empty)
                .SetDelegatee(
                    (Domain.SystemUtility.DelegateeId?)activeDelegatee.Id.Value,
                    activeDelegatee);

        return (TAssignee)delegatorAssignee.Clone();
    }
}