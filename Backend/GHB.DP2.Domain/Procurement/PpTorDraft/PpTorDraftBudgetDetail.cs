namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpTorDraftBudgetDetailId
{
    public static PpTorDraftBudgetDetailId New() => From(Guid.CreateVersion7());
}

public class PpTorDraftBudgetDetail : AuditableEntity<PpTorDraftBudgetDetailId>
{
    public override PpTorDraftBudgetDetailId Id { get; init; }

    public PpTorDraftBudgetId PpTorDraftBudgetId { get; init; }

    public int? Sequence { get; set; }

    public string? Department { get; set; }

    public string? BudgetType { get; set; }

    public string? ProjectCode { get; set; }

    public string? AccountNo { get; set; }

    public decimal? Budget { get; set; }

    public virtual PpTorDraftBudget PpTorDraftBudget { get; init; }
}