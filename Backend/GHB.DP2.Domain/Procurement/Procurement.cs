namespace GHB.DP2.Domain.Procurement;

using System.Globalization;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

public enum ProcurementType
{
    /// <summary>
    /// Procurement (จัดซื้อจัดจ้าง)
    /// </summary>
    Procurement,

    /// <summary>
    /// Rent (จ้างเช่า)
    /// </summary>
    Rent,
}

public enum ProcurementStep
{
    /// <summary>
    /// Pre-Procurement (ร่างแผนจัดซื้อจัดจ้าง)
    /// </summary>
    PreProcurement,

    /// <summary>
    /// Procurement (จัดซื้อจัดจ้าง)
    /// </summary>
    Procurement,

    /// <summary>
    /// Contract Agreement (ทำสัญญา)
    /// </summary>
    ContractAgreement,

    /// <summary>
    /// Contract Management (บริหารสัญญา)
    /// </summary>
    ContractManagement,
}

public enum ProcessType
{
    /// <summary>
    /// Appoint (แต่งตั้ง)
    /// </summary>
    Appoint,

    /// <summary>
    /// Tor Draft (ร่างเอกสาร TOR)
    /// </summary>
    TorDraft,

    /// <summary>
    /// PrincipleApproval (ขออนุมัติหลักการ)
    /// </summary>
    PrincipleApproval,

    /// <summary>
    /// PrincipleApprovalRental (ขออนุมัติเช่า)
    /// </summary>
    PrincipleApprovalRental,

    /// <summary>
    /// Median Price (ราคากลาง)
    /// </summary>
    MedianPrice,

    /// <summary>
    /// PurchaseRequisition ( จพ.04 )
    /// </summary>
    PurchaseRequisition,

    /// <summary>
    /// Jp005 ( จพ.05 )
    /// </summary>
    Jp005,

    /// <summary>
    /// Invite (หนังสือเชิญชวน)
    /// </summary>
    Invite,

    /// <summary>
    /// PurchaseOrder (จพ.06) | (ขออนุมัติเช่า)
    /// </summary>
    PurchaseOrder,

    /// <summary>
    /// PurchaseOrderApproval (อนุมัติใบสั่งซื้อ/จ้าง/เข่า และแจ้งทำสัญญา) | (อนุมัติใบสั่งเช่า และแจ้งทำสัญญา)
    /// </summary>
    PurchaseOrderApproval,

    /// <summary>
    /// ContractInvitation (หนังสือเชิญชวนทำสัญญา)
    /// </summary>
    ContractInvitation,

    /// <summary>
    /// ContractDraft (ร่างสัญญาและสัญญา)
    /// </summary>
    ContractDraft,

    W119,
    P79Clause2,
    PettyCash,
    PettyCashReimbursement,
}

public enum ProcurementStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    /// <summary>
    /// กำลังดำเนินการ
    /// </summary>
    InProgress,

    /// <summary>
    /// ดำเนินการแล้วเสร็จ
    /// </summary>
    Completed,

    /// <summary>
    /// ยกเลิกรายการ
    /// </summary>
    Cancelled,
}

[ValueObject<string>(Conversions.EfCoreValueConverter)]
public partial struct ProcurementNumber
{
    public static ProcurementNumber New(ProcurementType procurementType)
    {
        // Generate a new plan number in the format "PYY00001"
        // where YY is the last two digits of the year and 00001 is the initial number
        var yearString = DateTimeOffset.UtcNow.ToString("yy", new CultureInfo("th-TH")); // Get last two digits of year and ensure it's two digits
        var prefix = procurementType is ProcurementType.Procurement ? RunningPrefixConstant.Procurement : RunningPrefixConstant.Rental;

        var planNumber = $"{prefix}{yearString}00001";

        return From(planNumber);
    }

    public ProcurementNumber Next(ProcurementType procurementType)
    {
        var prefix = procurementType is ProcurementType.Procurement ? RunningPrefixConstant.Procurement : RunningPrefixConstant.Rental;

        if (string.IsNullOrWhiteSpace(this.Value))
        {
            throw new InvalidOperationException("ProcurementNumber cannot be null or empty.");
        }

        // Assuming the ProcurementNumber is in the format "PYYXXXXX"
        if (this.Value.Length < 4 || !this.Value.StartsWith(prefix))
        {
            throw new FormatException("Invalid ProcurementNumber format.");
        }

        int prefixLength = this.Value.StartsWith("PP") ? 2 : 1;

        if (this.Value.Length < prefixLength + 7) // prefix + YY + 5 digits
        {
            throw new FormatException("Invalid ProcurementNumber format.");
        }

        string yearPart = this.Value.Substring(prefixLength, 2);
        string numberPart = this.Value.Substring(prefixLength + 2);

        if (!int.TryParse(yearPart, out var year) || !int.TryParse(numberPart, out var number))
        {
            throw new FormatException("Invalid ProcurementNumber format.");
        }

        // Increment the number part
        number++;

        // Format the new ProcurementNumber
        var newPlanNumber = $"{prefix}{year}{number:D5}";

        return From(newPlanNumber);
    }
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ProcurementId
{
    public static ProcurementId New() => From(Guid.CreateVersion7());
}

public partial class Procurement : AuditableEntity<ProcurementId>, IHasSoftDelete, IHasActivityInfo
{
    public override ProcurementId Id { get; init; }

    public PlanId? PlanId { get; private set; }

    public ProcurementType Type { get; private set; }

    public ProcurementStep Step { get; private set; }

    public ProcessType ProcessType { get; private set; }

    public ProcurementStatus Status { get; private set; }

    public BusinessUnitId DepartmentId { get; private set; }

    public ParameterCode SupplyMethodCode { get; private set; }

    public ParameterCode? SupplyMethodTypeCode { get; private set; }

    public ParameterCode? SupplyMethodSpecialTypeCode { get; private set; }

    public string Name { get; private set; }

    public decimal? Budget { get; private set; }

    public int? BudgetYear { get; private set; }

    public DateTimeOffset? ExpectingProcurementAt { get; private set; }

    public bool IsCancelled { get; private set; }

    public string? RemarkClosed { get; private set; }

    public ProcurementStatus? LastStatusBeforeClosed { get; private set; }

    public bool IsStock { get; private set; }

    public bool IsCommercialMaterial { get; private set; }

    public bool HasMd { get; private set; }

    public ProcurementNumber? ProcurementNumber { get; private set; }

    public virtual Plan Plan { get; init; }

    public virtual RawBusinessUnit Department { get; init; }

    public virtual SuParameter SupplyMethod { get; init; }

    public virtual SuParameter? SupplyMethodType { get; init; }

    public virtual SuParameter? SupplyMethodSpecialType { get; init; }

    public virtual IReadOnlyCollection<PpAppoint.PpAppoint> Appoints { get; init; }

    public virtual IReadOnlyCollection<PpTorDraft.PpTorDraft> TorDrafts { get; init; }

    public virtual IReadOnlyCollection<PpMedianPrice.PpMedianPrice> MedianPrices { get; init; }

    public virtual IReadOnlyCollection<PPurchaseOrder.PPurchaseOrder> PurchaseOrder { get; init; }

    public virtual IReadOnlyCollection<PPurchaseOrderApproval.PPurchaseOrderApproval> PurchaseOrderApprovals { get; init; }

    public virtual IReadOnlyCollection<CaContractInvitation> ContractInvitations { get; init; }

    public virtual IReadOnlyCollection<PInvite.PInvite> Invites { get; init; }

    public virtual IReadOnlyCollection<PpPurchaseRequisition.PpPurchaseRequisition> PurchaseRequisitions { get; init; }

    public virtual IReadOnlyCollection<ProcurementAttachment> Attachments { get; private set; }

    public virtual IReadOnlyCollection<PJp005.PJp005> Jp005 { get; init; }

    public virtual IReadOnlyCollection<PPrincipleApproval.PPrincipleApproval> PrincipleApprovals { get; init; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRental.PPrincipleApprovalRental> PrincipleApprovalRentals { get; init; }

    public virtual IReadOnlyCollection<CaContractDraft> ContractDrafts { get; init; }

    public bool IsSixtyAndMoreThanOneHundredThousand => this.SupplyMethodCode == ParameterCode.From("SMethod002") && this.Budget > 100000;

    public Procurement AddAttachment(ProcurementAttachment file)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file), "Attachment cannot be null.");
        }

        var attachment = this.Attachments.ToHashSet();

        if (attachment.Any(a => a.Id == file.Id))
        {
            throw new InvalidOperationException("Attachment already exists.");
        }

        attachment.Add(file);

        this.Attachments = attachment;

        return this;
    }

    public Procurement RemoveAttachment(ProcurementAttachment info)
    {
        if (info == null)
        {
            throw new ArgumentNullException(nameof(info), "Attachment info cannot be null.");
        }

        var attachmentInfo = this.Attachments.ToHashSet();

        if (!attachmentInfo.Remove(info))
        {
            throw new InvalidOperationException("Attachment info does not exist.");
        }

        this.Attachments = attachmentInfo;

        return this;
    }

    public Procurement SetClosed(string? remarkClosed = default)
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Closed,
                $"ปิดงานรายการจัดซื้อจัดจ้างสำเร็จ",
                nameof(ProcurementStatus.Cancelled),
                remarkClosed));

        this.LastStatusBeforeClosed = this.Status;
        this.Status = ProcurementStatus.Cancelled;
        this.RemarkClosed = remarkClosed;

        return this;
    }

    public Procurement SetCancelClosed()
    {
        if (this.LastStatusBeforeClosed is null)
        {
            throw new InvalidOperationException("ไม่พบสถานะก่อนปิดงาน");
        }

        var previousStatus = this.LastStatusBeforeClosed.Value;

        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.CancelClosed,
                $"ยกเลิกปิดงานรายการจัดซื้อจัดจ้างสำเร็จ",
                previousStatus.ToString()));

        this.Status = previousStatus;
        this.LastStatusBeforeClosed = null;
        this.RemarkClosed = null;

        return this;
    }

    public Procurement SetCancelledProcurement()
    {
        this.IsCancelled = true;

        return this;
    }

    public Procurement SetActiveProcurement()
    {
        this.IsCancelled = false;

        return this;
    }

    public Procurement SetProcurementInfo(
        ProcurementType procurementType,
        ProcurementStep procurementStep,
        BusinessUnitId departmentId,
        string name,
        decimal budget,
        int budgetYear,
        DateTimeOffset? expectingProcurementAt)
    {
        this.Type = procurementType;
        this.Step = procurementStep;
        this.DepartmentId = departmentId;
        this.Name = name;
        this.Budget = budget;
        this.BudgetYear = budgetYear;
        this.ExpectingProcurementAt = expectingProcurementAt;

        return this;
    }

    public Procurement SetProcurementStep(
        ProcurementType procurementType,
        ProcurementStep procurementStep)
    {
        this.Type = procurementType;
        this.Step = procurementStep;

        return this;
    }

    public Procurement SetSupplyMethod(
        ParameterCode supplyMethodCode,
        ParameterCode? supplyMethodTypeCode,
        ParameterCode? supplyMethodSpecialTypeCode)
    {
        this.SupplyMethodCode = supplyMethodCode;
        this.SupplyMethodTypeCode = supplyMethodTypeCode;
        this.SupplyMethodSpecialTypeCode = supplyMethodSpecialTypeCode;

        return this;
    }

    public Procurement SetMaterialType(
        bool isStock = false,
        bool isCommercialMaterial = false)
    {
        this.IsStock = isStock;
        this.IsCommercialMaterial = isCommercialMaterial;

        return this;
    }

    public Procurement SetHasMd(bool hasMd)
    {
        this.HasMd = hasMd;

        return this;
    }

    public Procurement SetBudget(decimal budget)
    {
        this.Budget = budget;

        return this;
    }

    public Procurement SetDepartmentId(BusinessUnitId departmentId)
    {
        this.DepartmentId = departmentId;

        return this;
    }

    public Procurement SetName(string name)
    {
        this.Name = name;

        return this;
    }

    public Procurement SetStatus(ProcurementStatus status)
    {
        this.Status = status;

        return this;
    }

    public Procurement SetProcessType(ProcessType processType)
    {
        this.ProcessType = processType;

        return this;
    }

    public Procurement SetProcurementNumber(ProcurementNumber procurementNumber)
    {
        this.ProcurementNumber = procurementNumber;

        return this;
    }

    public static Procurement Create(
        PlanId? planId,
        bool hasMd = false)
    {
        var newData = new Procurement
        {
            Id = ProcurementId.New(),
            Status = ProcurementStatus.Draft,
            PlanId = planId,
            HasMd = hasMd,
        };

        newData.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            $"สร้างข้อมูลใหม่ {nameof(Procurement)}",
            newData.Status.ToString()));

        return newData;
    }
}