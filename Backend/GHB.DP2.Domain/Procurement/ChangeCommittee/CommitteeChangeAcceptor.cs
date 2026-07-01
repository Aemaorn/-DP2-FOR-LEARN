namespace GHB.DP2.Domain.Procurement.ChangeCommittee;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Constants;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using System.Text.Json;

public class CommitteeChangeAcceptor : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public CommitteeChangeId CommitteeChangeId { get; init; }

    public bool IsUnableToPerformDuties { get; private set; }

    public ParameterCode? CommitteePositionsCode { get; private set; }

    public virtual SuParameter? CommitteePosition { get; init; }

    public virtual CommitteeChanges CommitteeChange { get; init; }

    public bool IsDeleted { get; private set; }

    public Unit Delete()
    {
        this.IsDeleted = true;

        return Unit.Default;
    }

    public static CommitteeChangeAcceptor Create(
        AcceptorType type,
        SuUser user,
        int sequence)
    {
        var acceptor = new CommitteeChangeAcceptor
        {
            Id = AcceptorId.New(),
        };

        if (user.Employee.View is null)
        {
            throw new ArgumentException("View is null");
        }

        _ = acceptor.SetType(type)
                    .SetUser(
                        user.Id,
                        user.EmployeeCode,
                        user.Employee.View.FullName,
                        user.Employee.View.FullPositionName,
                        user.Employee.View.BusinessUnitName)
                    .SetSequence(sequence)
                    .SetActive()
                    .Draft();

        return acceptor;
    }

    public CommitteeChangeAcceptor SetCommitteePositionsCode(
      ParameterCode? committeePositionsCode)
    {
        this.CommitteePositionsCode = committeePositionsCode;

        return this;
    }

    public CommitteeChangeAcceptor SetIsUnableToPerformDuties(bool isUnableToPerformDuties)
    {
        this.IsUnableToPerformDuties = isUnableToPerformDuties;

        return this;
    }

    public new CommitteeChangeAcceptor Clone()
    {
        var newAcceptor = new CommitteeChangeAcceptor
        {
            Id = AcceptorId.New(),
            CommitteeChangeId = this.CommitteeChangeId,
        };

        if (this.User.Employee.View is null)
        {
            throw new ArgumentException("View is null");
        }

        newAcceptor.SetType(this.Type)
                   .SetUser(
                       this.User.Id,
                       this.User.EmployeeCode,
                       this.User.Employee.View!.FullName,
                       this.User.Employee.View!.FullPositionName,
                       this.User.Employee.View!.BusinessUnitName)
                   .SetSequence(this.Sequence)
                   .SetActive()
                   .Draft();

        return newAcceptor;
    }

    public static CommitteeChangeAcceptor CreateWithPending(
        AcceptorType type,
        SuUser user,
        int sequence)
    {
        var acceptor = new CommitteeChangeAcceptor
        {
            Id = AcceptorId.New(),
        };

        if (user.Employee.View is null)
        {
            throw new ArgumentException("View is null");
        }

        _ = acceptor.SetType(type)
                    .SetUser(
                        user.Id,
                        user.EmployeeCode,
                        user.Employee.View.FullName,
                        user.Employee.View.FullPositionName,
                        user.Employee.View.BusinessUnitName)
                    .SetSequence(sequence)
                    .SetActive()
                    .Pending();

        return acceptor;
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

        if (committeePosition is null)
        {
            return false;
        }

        if (committeePosition.Value is null)
        {
            return false;
        }

        var isBoardChairmanJson = (JsonElement)committeePosition.Value;

        return isBoardChairmanJson.ToBoolean();
    }
}