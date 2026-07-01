namespace GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CmDeliveryAcceptanceId
{
    public static CmDeliveryAcceptanceId New() => From(Guid.CreateVersion7());
}

public enum CmDeliveryAcceptanceStatus
{
    /// <summary>
    /// อยู่ระหว่างดำเนินการ
    /// </summary>
    InProgress,

    /// <summary>
    /// ดำเนินการแล้วเสร็จ
    /// </summary>
    Completed,
}

public enum SourceType
{
    Plan,

    ContractDraftVendor,

    Procurement,

    ContractDraftVendorEdit,

    /// <summary>
    /// สร้างใหม่โดยไม่อ้างอิงเอกสารต้นทางในระบบ
    /// </summary>
    Manual,
}

/// <summary>
/// ส่งมอบและตรวจรับงาน
/// </summary>
public partial class CmDeliveryAcceptance : AuditableEntity<CmDeliveryAcceptanceId>, IHasSoftDelete
{
    public override CmDeliveryAcceptanceId Id { get; init; }

    public CmDeliveryAcceptanceStatus Status { get; private set; }

    public string? ContractType { get; private set; }

    public SourceType SourceType { get; init; }

    public Guid? RefId { get; init; }

    /// <summary>เลขที่เอกสาร (auto-gen สำหรับ Manual mode)</summary>
    public string? Number { get; private set; }

    /// <summary>ฝ่าย/ภาคเขต</summary>
    public BusinessUnitId? DepartmentId { get; private set; }

    /// <summary>วิธีจัดหา</summary>
    public ParameterCode? SupplyMethodCode { get; private set; }

    public ParameterCode? SupplyMethodTypeCode { get; private set; }

    public ParameterCode? SupplyMethodSpecialTypeCode { get; private set; }

    /// <summary>โครงการ</summary>
    public string? Name { get; private set; }

    /// <summary>งบประมาณ</summary>
    public decimal? Budget { get; private set; }

    public bool? IsCommercialMaterial { get; private set; }

    public virtual RawBusinessUnit? Department { get; init; }

    public virtual SuParameter? SupplyMethod { get; init; }

    public virtual SuParameter? SupplyMethodType { get; init; }

    public virtual SuParameter? SupplyMethodSpecialType { get; init; }

    public virtual IReadOnlyCollection<CmDeliveryAcceptancePeriod> Periods { get; private set; }

    public static CmDeliveryAcceptance Create(
        string? contractType,
        SourceType sourceType,
        Guid? refId)
    {
        return new CmDeliveryAcceptance
        {
            Id = CmDeliveryAcceptanceId.New(),
            Status = CmDeliveryAcceptanceStatus.InProgress,
            SourceType = sourceType,
            RefId = refId,
            ContractType = contractType,
            Periods = [],
        };
    }

    public static CmDeliveryAcceptance CreateManual(
        BusinessUnitId? departmentId,
        ParameterCode? supplyMethodCode,
        ParameterCode? supplyMethodTypeCode,
        ParameterCode? supplyMethodSpecialTypeCode,
        string? name,
        decimal? budget,
        bool? isCommercialMaterial = null)
    {
        return new CmDeliveryAcceptance
        {
            Id = CmDeliveryAcceptanceId.New(),
            Status = CmDeliveryAcceptanceStatus.InProgress,
            SourceType = SourceType.Manual,
            RefId = null,
            DepartmentId = departmentId,
            SupplyMethodCode = supplyMethodCode,
            SupplyMethodTypeCode = supplyMethodTypeCode,
            SupplyMethodSpecialTypeCode = supplyMethodSpecialTypeCode,
            Name = name,
            Budget = budget,
            IsCommercialMaterial = isCommercialMaterial,
            Periods = [],
        };
    }

    public CmDeliveryAcceptance SetNumber(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new ArgumentException("Number cannot be null or empty.", nameof(number));
        }

        this.Number = number;

        return this;
    }

    public CmDeliveryAcceptance UpdateManual(
        BusinessUnitId? departmentId,
        ParameterCode? supplyMethodCode,
        ParameterCode? supplyMethodTypeCode,
        ParameterCode? supplyMethodSpecialTypeCode,
        string? name,
        decimal? budget,
        bool? isCommercialMaterial = null)
    {
        if (this.SourceType != SourceType.Manual)
        {
            throw new InvalidOperationException("UpdateManual can be called only when SourceType is Manual.");
        }

        this.DepartmentId = departmentId;
        this.SupplyMethodCode = supplyMethodCode;
        this.SupplyMethodTypeCode = supplyMethodTypeCode;
        this.SupplyMethodSpecialTypeCode = supplyMethodSpecialTypeCode;
        this.Name = name;
        this.Budget = budget;
        this.IsCommercialMaterial = isCommercialMaterial;

        return this;
    }

    public CmDeliveryAcceptance AddPeriod(CmDeliveryAcceptancePeriod period)
    {
        var periods = (this.Periods ?? []).ToHashSet();

        periods.Add(period);

        this.Periods = periods;

        return this;
    }

    public CmDeliveryAcceptance RemovePeriod(CmDeliveryAcceptancePeriod period)
    {
        var periods = (this.Periods ?? []).ToHashSet();

        periods.Remove(period);

        this.Periods = periods;

        return this;
    }

    public CmDeliveryAcceptance EvaluateStatusToApprove()
    {
        var isAllPeriodsCompleted =
            this.Periods
                .All(p => p.Status == CmDeliveryAcceptancePeriodStatus.Approved);

        if (!isAllPeriodsCompleted)
        {
            return this;
        }

        this.Status = CmDeliveryAcceptanceStatus.Completed;

        return this;
    }

    public CmDeliveryAcceptance ApproveDeliveryAcceptance()
    {
        var isAllPeriodsPaid =
            this.Periods
                .All(p => p.AccountStatus == CmDeliveryAcceptancePeriodAccountStatus.Paid);

        if (!isAllPeriodsPaid)
        {
            throw new InvalidOperationException(
                "ไม่สามารถอนุมัติได้ เนื่องจากยังมีงวดที่ยังไม่ได้ชำระเงิน");
        }

        this.Status = CmDeliveryAcceptanceStatus.Completed;

        return this;
    }
}