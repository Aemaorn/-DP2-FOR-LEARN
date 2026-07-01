namespace GHB.DP2.Domain.Procurement.ChangeCommittee;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CommitteeChangeAssigneeId
{
    public static CommitteeChangeAssigneeId New() => From(Guid.CreateVersion7());
}

public partial class CommitteeChangeAssignee : AssigneeInfoEntity<CommitteeChangeAssigneeId>, IHasSoftDelete
{
    public override CommitteeChangeAssigneeId Id { get; init; }

    public CommitteeChangeId CommitteeChangeId { get; init; }

    public virtual CommitteeChanges CommitteeChanges { get; init; }

    public static CommitteeChangeAssignee Create(
        CommitteeChangeId committeeChangeId,
        AssigneeGroup assigneeGroup,
        AssigneeType assigneeType,
        SuUser user,
        int sequence)
    {
        var assignee = new CommitteeChangeAssignee
        {
            Id = CommitteeChangeAssigneeId.New(),
            CommitteeChangeId = committeeChangeId,
        };

        if (user.Employee.View is null)
        {
            throw new ArgumentException("User must have an associated employee.");
        }

        _ = assignee
            .SetGroup(assigneeGroup)
            .SetType(assigneeType)
            .SetUser(
                user.Id,
                user.EmployeeCode,
                user.Employee.View.FullName,
                user.Employee.View.FullPositionName,
                user.Employee.View.BusinessUnitName)
            .SetSequence(sequence);

        return assignee;
    }
}
