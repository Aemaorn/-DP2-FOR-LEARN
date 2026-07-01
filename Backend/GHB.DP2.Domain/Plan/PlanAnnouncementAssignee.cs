namespace GHB.DP2.Domain.Plan;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PlanAnnouncementAssigneeId
{
    public static PlanAnnouncementAssigneeId New() => From(Guid.CreateVersion7());
}

public class PlanAnnouncementAssignee : AssigneeInfoEntity<PlanAnnouncementAssigneeId>
{
    public override PlanAnnouncementAssigneeId Id { get; init; }

    public PlanAnnouncementId PlanAnnouncementId { get; init; }

    public virtual PlanAnnouncement PlanAnnouncement { get; init; }

    public static PlanAnnouncementAssignee Create(
        AssigneeType type,
        SuUser user,
        int sequence)
    {
        if (user.Employee.View is null)
        {
            throw new ArgumentException("User does not have view assigned.");
        }

        var assignee = new PlanAnnouncementAssignee
        {
            Id = PlanAnnouncementAssigneeId.New(),
        };

        _ = assignee.SetType(type)
                    .SetUser(
                        user.Id,
                        user.EmployeeCode,
                        user.Employee.View.FullName,
                        user.Employee.View?.FullPositionName ?? string.Empty,
                        user.Employee.View?.BusinessUnitName ?? string.Empty)
                    .SetSequence(sequence)
                    .Draft();

        return assignee;
    }
}