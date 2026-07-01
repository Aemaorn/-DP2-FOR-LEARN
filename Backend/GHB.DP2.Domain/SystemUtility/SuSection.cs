namespace GHB.DP2.Domain.SystemUtility;

using Vogen;

/// <summary>
/// ประเภทกระบวนการอนุมัติในระบบแผน (Section Process Types)
/// </summary>
public enum SectionProcessType
{
    /// <summary>
    /// แต่งตั้งคณะกรรมการก่อนการจัดซื้อจัดจ้าง
    /// </summary>
    AppointPreProcurement,

    /// <summary>
    /// แต่งตั้งคณะกรรมการก่อนการจัดซื้อจัดจ้างสำหรับที่ดินเชิงพาณิชย์
    /// </summary>
    AppointPreProcurementCommercialParcel,

    /// <summary>
    /// แต่งตั้งคณะกรรมการก่อนการจัดซื้อจัดจ้างสำหรับพัสดุคงคลัง
    /// </summary>
    AppointPreProcurementStock,

    /// <summary>
    /// แผนงาน
    /// </summary>
    Plan,

    /// <summary>
    /// ขอบเขตงาน (Terms of Reference)
    /// </summary>
    TOR,

    /// <summary>
    /// ขอบเขตงานสำหรับที่ดินเชิงพาณิชย์
    /// </summary>
    TORCommercialParcel,

    /// <summary>
    /// ขอบเขตงาน (Terms of Reference)
    /// </summary>
    TORHasMD,

    /// <summary>
    /// ขอบเขตงานสำหรับที่ดินเชิงพาณิชย์
    /// </summary>
    TORCommercialParcelHasMD,

    /// <summary>
    /// ขอบเขตงานสำหรับพัสดุคงคลัง
    /// </summary>
    TORStock,

    /// <summary>
    /// ราคากลาง
    /// </summary>
    MedianPrice,

    /// <summary>
    /// ราคากลาง กรณี 4 ฝ่ายตามตารางแนบท้าย
    /// </summary>
    MedianPriceCommercialParcel,

    /// <summary>
    /// ราคากลาง
    /// </summary>
    MedianPriceHasMD,

    /// <summary>
    /// ราคากลาง กรณี 4 ฝ่ายตามตารางแนบท้าย
    /// </summary>
    MedianPriceCommercialParcelHasMD,

    /// <summary>
    /// ราคากลาง Stock
    /// </summary>
    MedianPriceStock,

    /// <summary>
    /// จพ. 005
    /// </summary>
    ApprovePurchaseRequest,

    /// <summary>
    /// จพ. 005 กรณี 4 ฝ่ายตามตารางแนบท้าย
    /// </summary>
    ApprovePurchaseRequestCommercialParcel,

    /// <summary>
    /// จพ. 006
    /// </summary>
    PurchaseOrder,

    /// <summary>
    /// จัดการส่งมอบและตรวจรับงาน รายงวด
    /// </summary>
    DeliveryAcceptancePeriod,

    DeliveryAcceptancePeriodCommercialParcel,

    /// <summary>
    /// จัดการส่งมอบและตรวจรับงาน รายงวด กรณีมีค่าปรับ
    /// </summary>
    DeliveryAcceptancePeriodPenalty,

    /// <summary>
    /// อนุมัติใบสั่งซื้อ/จ้าง/เช่า และแจ้งทำสัญญา
    /// </summary>
    ApprovePurchaseOrder,

    ApprovePurchaseOrderCommercialParcel,

    /// <summary>
    /// หนังสือเชิญชวนทำสัญญา
    /// </summary>
    ContractInvitation,

    /// <summary>
    /// ร่างสัญญาและสัญญา
    /// </summary>
    ContractDraft,

    /// <summary>
    /// คืนหลักประกันสัญญา
    /// </summary>
    ContractGuaranteeReturn,

    PurchaseOrderCommercialParcel,
    ContractAcceptance,
    ContractAcceptanceCommercialParcel,
    ContractInvitationCommercialParcel,

    /// <summary>
    /// ขออนุมัติหลักการเช่า
    /// </summary>
    PrincipleRentalApproval,

    /// <summary>
    /// ขออนุมัติเช่า
    /// </summary>
    RentalApproval,

    /// <summary>
    /// เบิกจ่าย (สำหรับบัญชี)
    /// </summary>
    ExpenseDisbursement,

    /// <summary>
    /// บันทึกต่อท้ายสัญญา
    /// </summary>
    ContractAmendment,

    /// <summary>
    /// บอกเลิกสัญญส
    /// </summary>
    ContractTermination,
}

[ValueObject<string>(Conversions.EfCoreValueConverter)]
public partial struct SectionId;

public class SuSection
{
    public SectionId Id { get; private set; }

    public string RefBankOrder { get; private set; }

    public ParameterCode? SupplyMethodCode { get; private set; }

    public ParameterCode? SupplyMethodSpecialTypeCode { get; private set; }

    public decimal MaximumBudget { get; private set; }

    public string? Remark { get; private set; }

    public virtual SuParameter? SupplyMethod { get; private set; }

    public virtual SuParameter? SupplyMethodSpecialType { get; private set; }

    public virtual IReadOnlyCollection<SuSectionApprover> Approvers { get; private set; }

    public static SuSection Create(
        SectionId id,
        string refBankOrder,
        ParameterCode? supplyMethodCode,
        ParameterCode? supplyMethodSpecialTypeCode,
        decimal maximumBudget,
        string? remark,
        SuParameter? supplyMethod,
        SuParameter? supplyMethodSpecialType)
    {
        return new SuSection()
        {
            Id = id,
            RefBankOrder = refBankOrder,
            SupplyMethodCode = supplyMethodCode,
            SupplyMethodSpecialTypeCode = supplyMethodSpecialTypeCode,
            MaximumBudget = maximumBudget,
            Remark = remark,
            SupplyMethod = supplyMethod,
            SupplyMethodSpecialType = supplyMethodSpecialType,
        };
    }

    public SuSection Update(
        string refBankOrder,
        ParameterCode? supplyMethodCode,
        ParameterCode? supplyMethodSpecialTypeCode,
        decimal maximumBudget,
        string remark,
        SuParameter? supplyMethod,
        SuParameter? supplyMethodSpecialType)
    {
        this.RefBankOrder = refBankOrder;
        this.SupplyMethodCode = supplyMethodCode;
        this.SupplyMethodSpecialTypeCode = supplyMethodSpecialTypeCode;
        this.MaximumBudget = maximumBudget;
        this.Remark = remark;
        this.SupplyMethod = supplyMethod;
        this.SupplyMethodSpecialType = supplyMethodSpecialType;

        return this;
    }

    public SuSection UpdateApprovers(IReadOnlyCollection<SuSectionApprover> approvers)
    {
        this.Approvers = approvers;

        return this;
    }
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter)]
public partial struct SectionApproverId
{
    public static SectionApproverId New() => From(Guid.CreateVersion7());
}

public class SuSectionApprover
{
    public SectionApproverId Id { get; private set; }

    public string InRefCode { get; private set; }

    public string PositionName { get; private set; }

    public string ShortPosition { get; private set; }

    public decimal Budget { get; private set; }

    public SectionProcessType ProcessType { get; private set; }

    public string CommandText { get; private set; }

    public SectionId SuSectionId { get; private set; }

    public decimal? CommandBudget { get; private set; }

    public virtual SuSection Section { get; private set; }

    public static SuSectionApprover Create(
        SectionId suSectionId,
        string inRefCode,
        string positionName,
        string shortPosition,
        decimal budget,
        SectionProcessType processType,
        string commandText,
        decimal? commandBudget = null)
    {
        return new SuSectionApprover()
        {
            Id = SectionApproverId.New(),
            InRefCode = inRefCode,
            PositionName = positionName,
            ShortPosition = shortPosition,
            Budget = budget,
            ProcessType = processType,
            CommandText = commandText,
            SuSectionId = suSectionId,
            CommandBudget = commandBudget,
        };
    }

    public SuSectionApprover Update(
        string inRefCode,
        string positionName,
        string shortPosition,
        decimal budget,
        SectionProcessType processType,
        string commandText,
        decimal? commandBudget = null)
    {
        this.InRefCode = inRefCode;
        this.PositionName = positionName;
        this.ShortPosition = shortPosition;
        this.Budget = budget;
        this.ProcessType = processType;
        this.CommandText = commandText;
        this.CommandBudget = commandBudget;

        return this;
    }
}