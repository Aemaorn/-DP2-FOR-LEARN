namespace GHB.DP2.Domain.ContractManagement.CmContractTermination;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CmContractTerminationAssigneeId
{
    public static CmContractTerminationAssigneeId New() => From(Guid.CreateVersion7());
}

public partial class CmContractTerminationAssignee : AssigneeInfoEntity<CmContractTerminationAssigneeId>, IHasSoftDelete
{
    public override CmContractTerminationAssigneeId Id { get; init; }

    public virtual CmContractTermination CmContractTermination { get; init; }

    public static CmContractTerminationAssignee Create(
        AssigneeGroup assigneeGroup,
        AssigneeType assigneeType,
        SuUser user,
        int sequence)
    {
        var assignee = new CmContractTerminationAssignee
        {
            Id = CmContractTerminationAssigneeId.New(),
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