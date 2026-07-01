namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;

public partial class CaContractDraftAcceptor : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public virtual CaContractDraftVendor ContractDraftVendor { get; init; }

    public static CaContractDraftAcceptor Create(
        SuUser user,
        AcceptorType acceptorType,
        int sequence)
    {
        var newAcceptor = new CaContractDraftAcceptor
        {
            Id = AcceptorId.New(),
        };

        if (user.Employee.View is null)
        {
            throw new ArgumentException("View is null");
        }

        newAcceptor
            .SetType(acceptorType)
            .SetUser(
                user.Id,
                user.EmployeeCode,
                user.Employee.View.FullName,
                user.Employee.View.FullPositionName,
                user.Employee.View.BusinessUnitName)
            .SetSequence(sequence)
            .SetActive();

        return newAcceptor;
    }

    public CaContractDraftAcceptor Update(
        SuUser user)
    {
        if (user.Employee.View is null)
        {
            throw new ArgumentException("View is null");
        }

        this.SetUser(
                user.Id,
                user.EmployeeCode,
                user.Employee.View.FullName,
                user.Employee.View.FullPositionName,
                user.Employee.View.BusinessUnitName);

        return this;
    }
}