namespace GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CamContractAmendmentExtendChangeAssigneeId
{
    public static CamContractAmendmentExtendChangeAssigneeId New() => From(Guid.CreateVersion7());
}

public class CamContractAmendmentExtendChangeAssignee : AssigneeInfoEntity<CamContractAmendmentExtendChangeAssigneeId>
{
    public override CamContractAmendmentExtendChangeAssigneeId Id { get; init; }

    public virtual CamContractAmendmentExtendChange CamContractAmendmentExtendChange { get; init; }

    public static CamContractAmendmentExtendChangeAssignee Create(
        AssigneeGroup assigneeGroup,
        AssigneeType assigneeType,
        SuUser user,
        int sequence)
    {
        var assignee = new CamContractAmendmentExtendChangeAssignee
        {
            Id = CamContractAmendmentExtendChangeAssigneeId.New(),
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