namespace GHB.DP2.Domain.Procurement.PPrincipleApproval;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalAssigneeId
{
    public static PPrincipleApprovalAssigneeId New() => From(Guid.CreateVersion7());
}

public partial class PPrincipleApprovalAssignee : AssigneeInfoEntity<PPrincipleApprovalAssigneeId>, IHasSoftDelete
{
    public override PPrincipleApprovalAssigneeId Id { get; init; }

    public virtual PPrincipleApproval PPrincipleApproval { get; init; }

    public static PPrincipleApprovalAssignee Create(
        AssigneeGroup assigneeGroup,
        AssigneeType assigneeType,
        SuUser user,
        int sequence)
    {
        var assignee = new PPrincipleApprovalAssignee
        {
            Id = PPrincipleApprovalAssigneeId.New(),
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
