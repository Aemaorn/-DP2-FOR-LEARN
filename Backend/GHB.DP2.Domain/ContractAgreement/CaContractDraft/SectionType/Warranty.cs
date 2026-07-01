namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;

using GHB.DP2.Domain.SystemUtility;

public class Warranty
{
    public bool? HasWarranty { get; init; }

    public RentalDurationInfo? WarrantyPeriod { get; init; }

    public ParameterCode? WarrantyConditionCode { get; init; }

    public RentalDurationInfo? FixingDeadlinePeriod { get; init; }

    public virtual SuParameter? WarrantyCondition { get; init; }

    public int? WarrantyMonthlyAllowedDowntimeHours { get; set; }

    public decimal? WarrantyDowntimePercentPerMonth { get; set; }

    public decimal? WarrantyPenaltyPerHour { get; set; }

    public int? DowntimeResolutionHours { get; set; }

    public int? DowntimeResolutionDay { get; set; }

    public int? RepairCompletionHours { get; set; }

    public int? RepairCompletionDay { get; set; }

    public decimal? RepairDelayPenaltyPercentPerHour { get; set; }

    // สัญญาซื้อขายคอมพิวเตอร์
    public int? MaxMonthlyMalfunction { get; set; }

    public ParameterCode? MaxMonthlyMalfunctionTypeCode { get; set; }

    public decimal? MaxMonthlyMalfunctionRate { get; set; }

    public decimal? MaxMonthlyMalfunctionPenaltyPercentageRate { get; set; }

    public decimal? MaxMonthlyMalfunctionPenaltyPerHour { get; set; }

    public int? MaxMonthlyMalfunctionPenaltyDueDays { get; set; }

    public DateTimeOffset? WarrantyStartDate { get; set; }

    public DateTimeOffset? WarrantyEndDate { get; set; }

    public int? WarrantyMaintenanceCount { get; set; }

    public ParameterCode? WarrantyMaintenanceTypeCode { get; set; }

    public virtual SuParameter? MaxMonthlyMalfunctionTypeCodeNavigation { get; set; }

    public virtual SuParameter? WarrantyMaintenanceType { get; set; }

    public Warranty()
    {
        // Ef Core constructor
    }

    public Warranty(
        bool hasWarranty,
        ParameterCode? warrantyConditionCode,
        RentalDurationInfo? warrantyPeriod,
        RentalDurationInfo? fixingDeadlinePeriod,
        int? warrantyMonthlyAllowedDowntimeHours,
        decimal? warrantyDowntimePercentPerMonth,
        decimal? warrantyPenaltyPerHour,
        int? downtimeResolutionHours,
        int? downtimeResolutionDay,
        int? repairCompletionHours,
        int? repairCompletionDay,
        decimal? repairDelayPenaltyPercentPerHour,
        int? maxMonthlyMalfunction,
        ParameterCode? maxMonthlyMalfunctionTypeCode,
        decimal? maxMonthlyMalfunctionRate,
        decimal? maxMonthlyMalfunctionPenaltyPercentageRate,
        decimal? maxMonthlyMalfunctionPenaltyPerHour,
        int? maxMonthlyMalfunctionPenaltyDueDays,
        DateTimeOffset? warrantyStartDate,
        DateTimeOffset? warrantyEndDate,
        int? warrantyMaintenanceCount,
        ParameterCode? warrantyMaintenanceTypeCode)
    {
        this.HasWarranty = hasWarranty;
        this.WarrantyConditionCode = warrantyConditionCode;
        this.WarrantyPeriod = warrantyPeriod;
        this.FixingDeadlinePeriod = fixingDeadlinePeriod;
        this.WarrantyMonthlyAllowedDowntimeHours = warrantyMonthlyAllowedDowntimeHours;
        this.WarrantyDowntimePercentPerMonth = warrantyDowntimePercentPerMonth;
        this.WarrantyPenaltyPerHour = warrantyPenaltyPerHour;
        this.DowntimeResolutionHours = downtimeResolutionHours;
        this.DowntimeResolutionDay = downtimeResolutionDay;
        this.RepairCompletionHours = repairCompletionHours;
        this.RepairCompletionDay = repairCompletionDay;
        this.RepairDelayPenaltyPercentPerHour = repairDelayPenaltyPercentPerHour;
        this.MaxMonthlyMalfunction = maxMonthlyMalfunction;
        this.MaxMonthlyMalfunctionTypeCode = maxMonthlyMalfunctionTypeCode;
        this.MaxMonthlyMalfunctionRate = maxMonthlyMalfunctionRate;
        this.MaxMonthlyMalfunctionPenaltyPercentageRate = maxMonthlyMalfunctionPenaltyPercentageRate;
        this.MaxMonthlyMalfunctionPenaltyPerHour = maxMonthlyMalfunctionPenaltyPerHour;
        this.MaxMonthlyMalfunctionPenaltyDueDays = maxMonthlyMalfunctionPenaltyDueDays;
        this.WarrantyStartDate = warrantyStartDate;
        this.WarrantyEndDate = warrantyEndDate;
        this.WarrantyMaintenanceCount = warrantyMaintenanceCount;
        this.WarrantyMaintenanceTypeCode = warrantyMaintenanceTypeCode;
    }

    public static Warranty Default => new()
    {
        HasWarranty = null,
        WarrantyConditionCode = null,
        WarrantyPeriod = null,
        FixingDeadlinePeriod = null,
    };
}