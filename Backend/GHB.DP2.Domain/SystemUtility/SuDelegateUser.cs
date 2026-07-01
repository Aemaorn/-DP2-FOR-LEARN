namespace GHB.DP2.Domain.SystemUtility;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct DelegatorId
{
    public static DelegatorId New() => From(Guid.CreateVersion7());
}

public record CreateSuDelegatorRequest(
    UserId SuUserId,
    EmployeeCode EmployeeCode,
    string UserFullName,
    PositionId PositionId,
    string FullPositionName,
    DateTimeOffset DelegationStartDate,
    DateTimeOffset DelegationEndDate,
    string Annotation);

public partial class SuDelegator : AuditableEntity<DelegatorId>, IHasSoftDelete
{
    public override DelegatorId Id { get; init; }

    public UserId SuUserId { get; init; }

    public EmployeeCode EmployeeCode { get; init; }

    public string UserFullName { get; private set; }

    public PositionId PositionId { get; init; }

    public string FullPositionName { get; private set; }

    public DateTimeOffset DelegationStartDate { get; private set; }

    public DateTimeOffset DelegationEndDate { get; private set; }

    public string Annotation { get; private set; }

    public virtual SuUser SuUser { get; init; }

    public virtual RawPosition Position { get; init; }

    public virtual IReadOnlyCollection<SuDelegatee> Delegatees { get; private set; }

    public static SuDelegator Create(CreateSuDelegatorRequest req)
    {
        return new SuDelegator
        {
            Id = DelegatorId.New(),
            SuUserId = req.SuUserId,
            EmployeeCode = req.EmployeeCode,
            UserFullName = req.UserFullName,
            PositionId = req.PositionId,
            FullPositionName = req.FullPositionName,
            DelegationStartDate = req.DelegationStartDate,
            DelegationEndDate = req.DelegationEndDate,
            Annotation = req.Annotation,
            Delegatees = [],
        };
    }

    public Unit Update(
        DateTimeOffset delegationStartDate,
        DateTimeOffset delegationEndDate,
        string annotation)
    {
        this.DelegationStartDate = delegationStartDate;
        this.DelegationEndDate = delegationEndDate;
        this.Annotation = annotation;

        return unit;
    }

    public Unit AddDelegatee(SuDelegatee delegatee)
    {
        if (delegatee == null)
        {
            throw new ArgumentNullException(nameof(delegatee), "Delegatee cannot be null.");
        }

        if (this.Delegatees.Contains(delegatee))
        {
            throw new InvalidOperationException("This delegatee is already added.");
        }

        var delegatees = this.Delegatees.ToHashSet();

        delegatees.Add(delegatee);

        this.Delegatees = delegatees;

        return unit;
    }

    public Unit RemoveDelegatee(SuDelegatee delegatee)
    {
        var delegatees = this.Delegatees.ToHashSet();

        delegatees.RemoveWhere(w => w.Id == delegatee.Id);

        this.Delegatees = delegatees;

        return unit;
    }
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct DelegateeId
{
    public static DelegateeId New() => From(Guid.CreateVersion7());
}

public record CreateSuDelegateeRequest(
    DelegatorId DelegatorId,
    int Sequence,
    PositionId DelegatorPositionId,
    BusinessUnitId DelegatorBusinessUnitId,
    BusinessUnitId? ParentBusinessUnitId,
    string DelegatorPositionName,
    string Acting,
    UserId SuUserId,
    EmployeeCode EmployeeCode,
    string UserFullName,
    PositionId FullPositionId,
    string FullPositionName,
    BusinessUnitId? RawBusinessUnitId,
    BusinessUnitId? SubBusinessUnitId,
    bool Active);

public class SuDelegatee : AuditableEntity<DelegateeId>
{
    public override DelegateeId Id { get; init; }

    public DelegatorId DelegatorId { get; init; }

    public int Sequence { get; private set; }

    public PositionId DelegatorPositionId { get; init; }

    public BusinessUnitId DelegatorBusinessUnitId { get; init; }

    public BusinessUnitId? ParentBusinessUnitId { get; set; }

    public string DelegatorPositionName { get; private set; }

    public string Acting { get; private set; }

    public UserId SuUserId { get; init; }

    public EmployeeCode EmployeeCode { get; init; }

    public string UserFullName { get; private set; }

    public PositionId PositionId { get; init; }

    public string FullPositionName { get; private set; }

    public BusinessUnitId? BusinessUnitId { get; private set; }

    public BusinessUnitId? SubBusinessUnitId { get; private set; }

    public bool Active { get; private set; }

    public virtual SuDelegator SuDelegator { get; init; }

    public virtual SuUser SuUser { get; init; }

    public virtual RawPosition Position { get; init; }

    public virtual RawBusinessUnit RawBusinessUnit { get; init; }

    public virtual RawBusinessUnit ParentBusinessUnit { get; init; }

    public virtual RawBusinessUnit SubBusinessUnit { get; init; }

    public virtual IReadOnlyCollection<SuDelegateeHistories> DelegateeHistories { get; set; }

    public static SuDelegatee Create(CreateSuDelegateeRequest req)
    {
        return new SuDelegatee
        {
            Id = DelegateeId.New(),
            DelegatorId = req.DelegatorId,
            Sequence = req.Sequence,
            DelegatorPositionId = req.DelegatorPositionId,
            DelegatorBusinessUnitId = req.DelegatorBusinessUnitId,
            DelegatorPositionName = req.DelegatorPositionName,
            Acting = req.Acting,
            SuUserId = req.SuUserId,
            EmployeeCode = req.EmployeeCode,
            UserFullName = req.UserFullName,
            PositionId = req.FullPositionId,
            FullPositionName = req.FullPositionName,
            BusinessUnitId = req.RawBusinessUnitId,
            Active = req.Active,
            ParentBusinessUnitId = req.ParentBusinessUnitId,
            SubBusinessUnitId = req.SubBusinessUnitId,
        };
    }

    public Unit Update(int sequence, BusinessUnitId? parentBusinessUnitId = null, BusinessUnitId? rawBusinessUnitId = null, BusinessUnitId? subBusinessUnitId = null)
    {
        this.BusinessUnitId = rawBusinessUnitId;
        this.ParentBusinessUnitId = parentBusinessUnitId;
        this.SubBusinessUnitId = subBusinessUnitId;
        this.Sequence = sequence;

        return unit;
    }

    public Unit UpdateActive(bool active)
    {
        this.Active = active;

        return unit;
    }
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct DelegateeHistoriesId
{
    public static DelegateeHistoriesId New() => From(Guid.CreateVersion7());
}

public partial class SuDelegateeHistories : AuditableEntity<DelegateeHistoriesId>
{
    public override DelegateeHistoriesId Id { get; init; }

    public ActivityInfo ActivityInfo { get; init; }

    public DelegateeId SuDelegateeId { get; init; }

    public ProgramId ProgramId { get; init; }

    public string ActionStatus { get; init; }

    public string? Remark { get; init; }

    public virtual SuDelegatee SuDelegatee { get; init; }

    public virtual SuProgram SuProgram { get; init; }
}