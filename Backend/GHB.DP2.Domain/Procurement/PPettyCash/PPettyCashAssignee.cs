namespace GHB.DP2.Domain.Procurement.PPettyCash;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPettyCashAssigneeId
{
    public static PPettyCashAssigneeId New() => From(Guid.CreateVersion7());
}

public class PPettyCashAssignee : AssigneeInfoEntity<PPettyCashAssigneeId>
{
    public override PPettyCashAssigneeId Id { get; init; }

    public PettyCashId PettyCashId { get; init; }

    public virtual PPettyCash PettyCash { get; init; }

    public static PPettyCashAssignee Create(
        AssigneeType type,
        SuUser user,
        int sequence)
    {
        if (user.Employee.View is null)
        {
            throw new ArgumentException("User does not have view assigned.");
        }

        var assignee = new PPettyCashAssignee
        {
            Id = PPettyCashAssigneeId.New(),
        };

        _ = assignee.SetType(type)
                    .SetUser(
                        user.Id,
                        user.EmployeeCode,
                        user.FullName,
                        user.Employee.View?.FullPositionName ?? string.Empty,
                        user.Employee.View?.BusinessUnitName ?? string.Empty)
                    .SetSequence(sequence)
                    .Draft();

        return assignee;
    }
}