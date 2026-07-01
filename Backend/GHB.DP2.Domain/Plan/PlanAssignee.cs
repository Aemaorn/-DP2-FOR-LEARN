namespace GHB.DP2.Domain.Plan;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PlanAssigneeId
{
    public static PlanAssigneeId New() => From(Guid.CreateVersion7());
}

public class PlanAssignee : AssigneeInfoEntity<PlanAssigneeId>
{
    public override PlanAssigneeId Id { get; init; }

    public PlanId PlanId { get; init; }

    public virtual Plan Plan { get; init; }

    public static PlanAssignee Create(
        AssigneeType type,
        SuUser user,
        int sequence)
    {
        var assignee = new PlanAssignee
        {
            Id = PlanAssigneeId.New(),
        };

        if (user.Employee.View is null)
        {
            throw new ArgumentException("View is null");
        }

        _ = assignee.SetType(type)
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

    public PlanAssignee Clone()
    {
        var newData = new PlanAssignee
        {
            Id = PlanAssigneeId.New(),
            PlanId = this.PlanId,
        };

        if (this.User.Employee.View is null)
        {
            throw new ArgumentException("View is null");
        }

        _ = newData
            .SetType(this.Type)
            .SetUser(
                this.User.Id,
                this.User.EmployeeCode,
                this.User.Employee.View!.FullName,
                this.User.Employee.View!.FullPositionName,
                this.User.Employee.View!.BusinessUnitName)
            .SetSequence(this.Sequence)
            .Draft();

        return newData;
    }
}