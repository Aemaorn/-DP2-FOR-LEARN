namespace GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;

using System.Text.Json;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Constants;
using GHB.DP2.Domain.SystemUtility;

public partial class CaContractDraftEditAcceptor : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public bool IsUnableToPerformDuties { get; private set; }

    public ParameterCode? CommitteePositionsCode { get; private set; }

    public virtual SuParameter? CommitteePosition { get; init; }

    public virtual CaContractDraftVendorEdit CaContractDraftVendorEdit { get; init; }

    public static CaContractDraftEditAcceptor Create(
        SuUser user,
        AcceptorType acceptorType,
        int sequence)
    {
        var newAcceptor = new CaContractDraftEditAcceptor
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

    public CaContractDraftEditAcceptor Update(
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

    public CaContractDraftEditAcceptor SetCommitteePositionsCode(
        ParameterCode? committeePositionsCode)
    {
        this.CommitteePositionsCode = committeePositionsCode;

        return this;
    }

    public CaContractDraftEditAcceptor SetIsUnableToPerformDuties(bool isUnableToPerformDuties)
    {
        this.IsUnableToPerformDuties = isUnableToPerformDuties;

        return this;
    }

    public bool IsCurrentApprover()
    {
        var pendingActiveAcceptors =
            this.CaContractDraftVendorEdit.Acceptors
                .Where(a =>
                    a.Type == this.Type &&
                    a is { IsActive: true, Status: AcceptorStatus.Pending, IsUnableToPerformDuties: false })
                .OrderBy(a => a.Sequence);

        if (this.CaContractDraftVendorEdit.Status == ContractDraftVendorEditStatus.WaitingCommitteeApproval)
        {
            var arr = pendingActiveAcceptors.ToArray();
            var hasNonChairman = arr.Any(a => !a.IsBoardChairman());

            return arr.Any(a =>
                a.UserId == this.UserId &&
                !a.IsUnableToPerformDuties &&
                (hasNonChairman ? !a.IsBoardChairman() : a.IsBoardChairman()));
        }

        var currentApprover = pendingActiveAcceptors.FirstOrDefault();

        return currentApprover != null && currentApprover.UserId == this.UserId;
    }

    public bool IsBoardChairman()
    {
        if (this.CommitteePosition is null)
        {
            return false;
        }

        var committeePosition =
            this.CommitteePosition.Values
                .GetValueOrDefault(PositionOnBoard.IsBoardChairmanKey);

        if (committeePosition?.Value is null)
        {
            return false;
        }

        var isBoardChairmanJson = (JsonElement)committeePosition.Value;

        return isBoardChairmanJson.ToBoolean();
    }
}
