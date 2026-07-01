namespace GHB.DP2.Domain.Procurement.Pw119;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;

public partial class Pw119Acceptor : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public Pw119Id Pw119Id { get; init; }

    public virtual Pw119 Pw119 { get; init; }

    public record AcceptorInfoData(
        AcceptorType Type,
        UserId UserId,
        EmployeeCode EmployeeCode,
        string FullName,
        string FullPositionName,
        string BusinessUnitName,
        int Sequence
    );

    public static Pw119Acceptor Create(
        AcceptorType type,
        SuUser user,
        int sequence)
    {
        var acceptor = new Pw119Acceptor
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

    public static Pw119Acceptor Create(
        AcceptorId id,
        AcceptorType type,
        SuUser user,
        int sequence)
    {
        var acceptor = new Pw119Acceptor
        {
            Id = id,
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

    public Unit Update(
        AcceptorInfoData info,
        AcceptorStatus status)
    {
        this.SetType(info.Type)
            .SetUser(
                info.UserId,
                info.EmployeeCode,
                info.FullName,
                info.FullPositionName,
                info.BusinessUnitName)
            .SetSequence(info.Sequence)
            .SetActive()
            .SetStatus(status);

        return unit;
    }

    public static Pw119Acceptor CreateWithPending(
      AcceptorType type,
      SuUser user,
      int sequence)
    {
        var acceptor = new Pw119Acceptor
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
}