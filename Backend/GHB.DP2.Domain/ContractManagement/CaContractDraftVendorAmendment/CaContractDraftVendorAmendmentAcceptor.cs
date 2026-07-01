namespace GHB.DP2.Domain.ContractManagement.CaContractDraftVendorAmendment;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;

public partial class CaContractDraftVendorAmendmentAcceptor : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public bool IsUnableToPerformDuties { get; private set; }

    public ParameterCode? CommitteePositionsCode { get; private set; }

    public virtual SuParameter? CommitteePosition { get; init; }

    public virtual CaContractDraftVendorAmendment CaContractDraftVendorAmendment { get; init; }

    public static CaContractDraftVendorAmendmentAcceptor Create(
        SuUser user,
        AcceptorType acceptorType,
        int sequence)
    {
        var newAcceptor = new CaContractDraftVendorAmendmentAcceptor
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

    public CaContractDraftVendorAmendmentAcceptor SetCommitteePositionsCode(
        ParameterCode? committeePositionsCode)
    {
        this.CommitteePositionsCode = committeePositionsCode;

        return this;
    }

    public CaContractDraftVendorAmendmentAcceptor SetIsUnableToPerformDuties(bool isUnableToPerformDuties)
    {
        this.IsUnableToPerformDuties = isUnableToPerformDuties;

        return this;
    }
}
