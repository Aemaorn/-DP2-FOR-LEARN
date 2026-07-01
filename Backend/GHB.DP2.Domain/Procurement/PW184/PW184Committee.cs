namespace GHB.DP2.Domain.Procurement.Pw184;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

public enum Pw184CommitteeGroupType
{
    /// <summary>
    /// ผู้ขอซื้อขอจ้าง
    /// </summary>
    ProcurementCommittee,

    /// <summary>
    /// ผู้ตรวจรับพัสดุ
    /// </summary>
    InspectionCommittee,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct Pw184CommitteeId
{
    public static Pw184CommitteeId New() => From(Guid.CreateVersion7());
}

public class Pw184Committee : AuditableEntity<Pw184CommitteeId>
{
    public override Pw184CommitteeId Id { get; init; }

    public Pw184Id Pw184Id { get; init; }

    public Pw184CommitteeGroupType GroupType { get; private set; }

    public UserId SuUserId { get; init; }

    public string FullName { get; private set; }

    public string FullPositionName { get; private set; }

    public ParameterCode CommitteePositionsCode { get; private set; }

    public string CommitteePositionsName { get; private set; }

    public int Sequence { get; private set; }

    public virtual Pw184 Pw184 { get; init; }

    public virtual SuUser User { get; init; }

    public virtual SuParameter CommitteePositions { get; init; }

    public static Pw184Committee Create(
        Pw184Id pw184Id,
        Pw184CommitteeGroupType groupType,
        UserId suUserId,
        string fullName,
        string fullPositionName,
        ParameterCode committeePositionsCode,
        string committeePositionsName,
        int sequence)
    {
        return new Pw184Committee
        {
            Id = Pw184CommitteeId.New(),
            Pw184Id = pw184Id,
            GroupType = groupType,
            SuUserId = suUserId,
            FullName = fullName,
            FullPositionName = fullPositionName,
            CommitteePositionsCode = committeePositionsCode,
            CommitteePositionsName = committeePositionsName,
            Sequence = sequence,
        };
    }

    public Pw184Committee Update(
        string fullName,
        string fullPositionName,
        ParameterCode committeePositionsCode,
        string committeePositionsName,
        Pw184CommitteeGroupType groupType,
        int sequence)
    {
        this.FullName = fullName;
        this.FullPositionName = fullPositionName;
        this.CommitteePositionsCode = committeePositionsCode;
        this.CommitteePositionsName = committeePositionsName;
        this.GroupType = groupType;
        this.Sequence = sequence;
        return this;
    }
}
