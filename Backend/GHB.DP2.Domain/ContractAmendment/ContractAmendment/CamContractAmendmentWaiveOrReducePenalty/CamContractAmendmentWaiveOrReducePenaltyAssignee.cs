namespace GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct WaiveOrReducePenaltyAssigneeId
{
    public static WaiveOrReducePenaltyAssigneeId New() => From(Guid.CreateVersion7());
}

public class CamContractAmendmentWaiveOrReducePenaltyAssignee : AssigneeInfoEntity<WaiveOrReducePenaltyAssigneeId>
{
    public override WaiveOrReducePenaltyAssigneeId Id { get; init; }

    public virtual CamContractAmendmentWaiveOrReducePenalty CamContractAmendmentWaiveOrReducePenalty { get; init; }

    public static CamContractAmendmentWaiveOrReducePenaltyAssignee Create(
        AssigneeGroup assigneeGroup,
        AssigneeType assigneeType,
        SuUser user,
        int sequence)
    {
        var assignee = new CamContractAmendmentWaiveOrReducePenaltyAssignee
        {
            Id = WaiveOrReducePenaltyAssigneeId.New(),
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
                user.FullName,
                user.Employee.View.FullPositionName,
                user.Employee.View.BusinessUnitName)
            .SetSequence(sequence);

        return assignee;
    }
}