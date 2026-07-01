namespace GHB.DP2.Domain.Procurement.PpPurchaseRequisition;

using System.Text.Json;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Constants;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

public enum GroupType
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
public partial struct PpPurchaseRequisitionCommitteeId
{
    public static PpPurchaseRequisitionCommitteeId New() => From(Guid.CreateVersion7());
}

public partial class PpPurchaseRequisitionCommittee : AuditableEntity<PpPurchaseRequisitionCommitteeId>
{
    public override PpPurchaseRequisitionCommitteeId Id { get; init; }

    public PpPurchaseRequisitionId PpPurchaseRequisitionId { get; init; }

    public GroupType GroupType { get; init; }

    public UserId SuUserId { get; init; }

    public string FullName { get; private set; }

    public string? FullPositionName { get; private set; }

    public ParameterCode CommitteePositionsCode { get; private set; }

    public string CommitteePositionsName { get; private set; }

    public int Sequence { get; private set; }

    public virtual PpPurchaseRequisition PpPurchaseRequisition { get; init; }

    public virtual SuUser User { get; init; }

    public virtual SuParameter CommitteePositions { get; init; }

    public static PpPurchaseRequisitionCommittee Create(
        PpPurchaseRequisitionId ppPurchaseRequisitionId,
        GroupType groupType,
        UserId suUserId,
        string fullName,
        string? fullPositionName,
        ParameterCode committeePositionsCode,
        string committeePositionsName,
        int sequence)
    {
        return new PpPurchaseRequisitionCommittee
        {
            Id = PpPurchaseRequisitionCommitteeId.New(),
            PpPurchaseRequisitionId = ppPurchaseRequisitionId,
            GroupType = groupType,
            SuUserId = suUserId,
            FullName = fullName,
            FullPositionName = fullPositionName,
            CommitteePositionsCode = committeePositionsCode,
            CommitteePositionsName = committeePositionsName,
            Sequence = sequence,
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

    public string GetSectionName()
    {
        var isCommittee =
            this.PpPurchaseRequisition.Committees
                .Where(c => c.GroupType == this.GroupType)
                .All(c => c.IsCommittee());

        return (this.GroupType, isCommittee) switch
        {
            (GroupType.ProcurementCommittee, false) => "ผู้จัดซื้อจัดจ้าง",
            (GroupType.ProcurementCommittee, true) => "คณะกรรมการจัดซื้อจัดจ้าง",
            (GroupType.InspectionCommittee, false) => "ผู้ตรวจรับพัสดุ",
            (GroupType.InspectionCommittee, true) => "คณะกรรมการตรวจรับพัสดุ",
            (GroupType.MaintenanceInspectionCommittee, true) => "คณะกรรมการตรวจรับพัสดุ (งานจ้างบริการบำรุงรักษา)",
            (GroupType.MaintenanceInspectionCommittee, false) => "ผู้ตรวจรับพัสดุ (งานจ้างบริการบำรุงรักษา)",
            (GroupType.ConstructionSupervisor, true) => "คณะกรรมการควบคุมงาน (เฉพาะงานก่อสร้าง)",
            (GroupType.ConstructionSupervisor, false) => "ผู้ควบคุมงาน (เฉพาะงานก่อสร้าง)",
            _ => throw new ArgumentOutOfRangeException(nameof(this.GroupType), this.GroupType, null),
        };
    }
}