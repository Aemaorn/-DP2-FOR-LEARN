namespace GHB.DP2.Domain.Procurement.PJp005;

using GHB.DP2.Domain.Common;
using System.Text.Json;
using GHB.DP2.Domain.Procurement.Constants;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

public enum PJp005CommitteeGroupType
{
    /// <summary>
    /// คณะกรรมการจัดซื้อจัดจ้าง
    /// </summary>
    ProcurementCommittee,

    /// <summary>
    /// ผู้ตรวจรับพัสดุ-คณะกรรมการตรวจรับพัสดุ
    /// </summary>
    InspectionCommittee,

    /// <summary>
    /// คณะกรรมการตรวจรับพัสดุ (งานจ้างบริการบำรุงรักษา)
    /// </summary>
    MaintenanceInspectionCommittee,

    /// <summary>
    /// ผู้ควบคุมงาน (เฉพาะงานก่อสร้าง)
    /// </summary>
    ConstructionSupervisor,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PJp005CommitteeId
{
    public static PJp005CommitteeId New() => From(Guid.CreateVersion7());
}

public partial class PJp005Committee : AuditableEntity<PJp005CommitteeId>
{
    public override PJp005CommitteeId Id { get; init; }

    public PJp005Id PJp005Id { get; init; }

    public PJp005CommitteeGroupType GroupType { get; init; }

    public UserId SuUserId { get; init; }

    public string FullName { get; init; }

    public string FullPositionName { get; init; }

    public ParameterCode CommitteePositionsCode { get; private set; }

    public string CommitteePositionsName { get; private set; }

    public int Sequence { get; private set; }

    public virtual PJp005 PJp005 { get; init; }

    public virtual SuUser User { get; init; }

    public virtual SuParameter CommitteePositions { get; init; }

    public static PJp005Committee CreateProcurementCommittee(
        PJp005Id pJp005Id,
        UserId suUserId,
        string fullName,
        string fullPositionName,
        ParameterCode committeePositionsCode,
        string committeePositionsName,
        int sequence)
    {
        return new PJp005Committee
        {
            Id = PJp005CommitteeId.New(),
            PJp005Id = pJp005Id,
            SuUserId = suUserId,
            FullName = fullName,
            FullPositionName = fullPositionName,
            CommitteePositionsCode = committeePositionsCode,
            CommitteePositionsName = committeePositionsName,
            Sequence = sequence,
            GroupType = PJp005CommitteeGroupType.ProcurementCommittee,
        };
    }

    public static PJp005Committee CreateInspectionCommittee(
        PJp005Id pJp005Id,
        UserId suUserId,
        string fullName,
        string fullPositionName,
        ParameterCode committeePositionsCode,
        string committeePositionsName,
        int sequence)
    {
        return new PJp005Committee
        {
            Id = PJp005CommitteeId.New(),
            PJp005Id = pJp005Id,
            SuUserId = suUserId,
            FullName = fullName,
            FullPositionName = fullPositionName,
            CommitteePositionsCode = committeePositionsCode,
            CommitteePositionsName = committeePositionsName,
            Sequence = sequence,
            GroupType = PJp005CommitteeGroupType.InspectionCommittee,
        };
    }

    public static PJp005Committee CreateMaintenanceInspectionCommittee(
        PJp005Id pJp005Id,
        UserId suUserId,
        string fullName,
        string fullPositionName,
        ParameterCode committeePositionsCode,
        string committeePositionsName,
        int sequence)
    {
        return new PJp005Committee
        {
            Id = PJp005CommitteeId.New(),
            PJp005Id = pJp005Id,
            SuUserId = suUserId,
            FullName = fullName,
            FullPositionName = fullPositionName,
            CommitteePositionsCode = committeePositionsCode,
            CommitteePositionsName = committeePositionsName,
            Sequence = sequence,
            GroupType = PJp005CommitteeGroupType.MaintenanceInspectionCommittee,
        };
    }

    public static PJp005Committee CreateConstructionSupervisor(
        PJp005Id pJp005Id,
        UserId suUserId,
        string fullName,
        string fullPositionName,
        ParameterCode committeePositionsCode,
        string committeePositionsName,
        int sequence)
    {
        return new PJp005Committee
        {
            Id = PJp005CommitteeId.New(),
            PJp005Id = pJp005Id,
            SuUserId = suUserId,
            FullName = fullName,
            FullPositionName = fullPositionName,
            CommitteePositionsCode = committeePositionsCode,
            CommitteePositionsName = committeePositionsName,
            Sequence = sequence,
            GroupType = PJp005CommitteeGroupType.ConstructionSupervisor,
        };
    }

    public Unit Update(
        ParameterCode committeePositionsCode,
        string committeePositionsName,
        int sequence)
    {
        this.CommitteePositionsCode = committeePositionsCode;
        this.CommitteePositionsName = committeePositionsName;
        this.Sequence = sequence;

        return unit;
    }

    public bool IsCommittee()
    {
        var committeePosition =
            this.CommitteePositions.Values
                .GetValueOrDefault(PositionOnBoard.IsCommittee);

        if (committeePosition is null)
        {
            return false;
        }

        if (committeePosition.Value is null)
        {
            return false;
        }

        var isCommittee = (JsonElement)committeePosition.Value;

        return isCommittee.GetBoolean();
    }

    public bool IsBoardChairman()
    {
        var boardChairmanPosition =
            this.CommitteePositions.Values
                .GetValueOrDefault(PositionOnBoard.IsBoardChairmanKey);

        if (boardChairmanPosition is null)
        {
            return false;
        }

        if (boardChairmanPosition.Value is null)
        {
            return false;
        }

        var isBoardChairman = (JsonElement)boardChairmanPosition.Value;

        return isBoardChairman.GetBoolean();
    }
}