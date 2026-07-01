namespace GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPurchaseOrderApprovalAssigneeId
{
    public static PPurchaseOrderApprovalAssigneeId New() => From(Guid.CreateVersion7());
}

public partial class PPurchaseOrderApprovalAssignee : AssigneeInfoEntity<PPurchaseOrderApprovalAssigneeId>, IHasSoftDelete
{
    public override PPurchaseOrderApprovalAssigneeId Id { get; init; }

    public virtual PPurchaseOrderApproval PPurchaseOrderApproval { get; init; }

    public static PPurchaseOrderApprovalAssignee Create(
        AssigneeGroup assigneeGroup,
        AssigneeType assigneeType,
        SuUser user,
        int sequence)
    {
        var assignee = new PPurchaseOrderApprovalAssignee
        {
            Id = PPurchaseOrderApprovalAssigneeId.New(),
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