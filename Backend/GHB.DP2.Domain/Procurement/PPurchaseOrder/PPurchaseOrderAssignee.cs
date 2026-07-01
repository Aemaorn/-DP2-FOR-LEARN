namespace GHB.DP2.Domain.Procurement.PPurchaseOrder;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPurchaseOrderAssigneeId
{
    public static PPurchaseOrderAssigneeId New() => From(Guid.CreateVersion7());
}

public partial class PPurchaseOrderAssignee : AssigneeInfoEntity<PPurchaseOrderAssigneeId>, IHasSoftDelete
{
    public override PPurchaseOrderAssigneeId Id { get; init; }

    public virtual PPurchaseOrder PPurchaseOrder { get; init; }

    public static PPurchaseOrderAssignee Create(
        AssigneeGroup assigneeGroup,
        AssigneeType assigneeType,
        SuUser user,
        int sequence)
    {
        return Create(PPurchaseOrderAssigneeId.New(), assigneeGroup, assigneeType, user, sequence);
    }

    public static PPurchaseOrderAssignee Create(
        PPurchaseOrderAssigneeId id,
        AssigneeGroup assigneeGroup,
        AssigneeType assigneeType,
        SuUser user,
        int sequence)
    {
        var assignee = new PPurchaseOrderAssignee
        {
            Id = id,
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
            .SetSequence(sequence)
            .Draft();

        return assignee;
    }
}