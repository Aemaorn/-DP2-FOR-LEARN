namespace GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CmContractGuaranteeReturnAssigneeId
{
    public static CmContractGuaranteeReturnAssigneeId New() => From(Guid.CreateVersion7());
}

public partial class CmContractGuaranteeReturnAssignee : AssigneeInfoEntity<CmContractGuaranteeReturnAssigneeId>, IHasSoftDelete
{
    public override CmContractGuaranteeReturnAssigneeId Id { get; init; }

    public virtual CmContractGuaranteeReturn CmContractGuaranteeReturn { get; init; }

    public static CmContractGuaranteeReturnAssignee Create(
        AssigneeGroup assigneeGroup,
        AssigneeType assigneeType,
        SuUser user,
        int sequence)
    {
        var assignee = new CmContractGuaranteeReturnAssignee
        {
            Id = CmContractGuaranteeReturnAssigneeId.New(),
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