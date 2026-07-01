namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpPpTorTemplateComputerId
{
    public static PpPpTorTemplateComputerId New() => From(Guid.CreateVersion7());
}

public class PpTorTemplateComputer : AuditableEntity<PpPpTorTemplateComputerId>
{
    public override PpPpTorTemplateComputerId Id { get; init; }

    public PpTorDraftId PpTorDraftId { get; set; }

    public string? EvidenceDescription { get; set; }

    public string? EvidenceNumber { get; set; }

    public string? DocumentDescription { get; set; }

    public string? CriteriaConsiderDescription { get; set; }

    public string? ManuelDescription { get; set; }

    public TorEvidence? Evidence { get; set; }

    public TorPreventiveMaintenance? PreventiveMaintenance { get; set; }

    public TorCorrectiveMaintenance? CorrectiveMaintenance { get; set; }

    public TorTraining? Training { get; set; }

    public virtual PpTorDraft PpTorDraft { get; init; }
}

public class TorEvidence
{
    public int? PresentTotal { get; set; }

    public int? PresentCount { get; set; }

    public ParameterCode? PresentCountUnit { get; set; }

    public ParameterCode? PresentCountType { get; set; }

    public virtual SuParameter? SuPresentCountUnit { get; init; }

    public virtual SuParameter? SuPresentCountType { get; init; }
}

public class TorPreventiveMaintenance
{
    public string? PmProductName { get; set; }

    public int? PmCount { get; set; }

    public ParameterCode? PmUnit { get; set; }

    public decimal? PmFinePct { get; set; }

    public decimal? PmFineAmount { get; set; }

    public string? Condition { get; set; }

    public int? DisruptedCount { get; set; }

    public ParameterCode? DisruptedCountUnit { get; set; }

    public decimal? DisruptedPercent { get; set; }

    public decimal? DisruptedFinePercent { get; set; }

    public decimal? DisruptedFineAmount { get; set; }

    public ParameterCode? PmFinePctUnit { get; set; }

    public virtual SuParameter? PmUnitType { get; init; }

    public virtual SuParameter? DisruptedCountUnitType { get; init; }

    public virtual SuParameter? PmFinePctUnitType { get; init; }
}

public class TorCorrectiveMaintenance
{
    public string? CmProductName { get; set; }

    public DateTimeOffset? StartDate { get; set; }

    public DateTimeOffset? EndDate { get; set; }

    public int? CmCount { get; set; }

    public ParameterCode? CmUnit { get; set; }

    public int? CmCompleteCount { get; set; }

    public ParameterCode? CmCompleteUnit { get; set; }

    public decimal? CmFinePercent { get; set; }

    public decimal? CmDisruptedFinePercent { get; set; }

    public ParameterCode? DayStart { get; set; }

    public ParameterCode? DayEnd { get; set; }

    public string? StartTime { get; set; }

    public string? EndTime { get; set; }

    public ParameterCode? CmFinePercentUnit { get; set; }

    public virtual SuParameter? CmUnitType { get; init; }

    public virtual SuParameter? CmCompleteUnitType { get; init; }

    public virtual SuParameter? DayStartType { get; init; }

    public virtual SuParameter? DayEndType { get; init; }

    public virtual SuParameter? CmFinePercentUnitType { get; init; }
}

public class TorTraining
{
    public int? TrainingCount { get; set; }

    public ParameterCode? TrainingCountUnit { get; set; }

    public ParameterCode? TrainingUnitId { get; set; }

    public virtual SuParameter? TrainingCountUnitType { get; init; }

    public virtual SuParameter? TrainingUnitType { get; init; }
}
