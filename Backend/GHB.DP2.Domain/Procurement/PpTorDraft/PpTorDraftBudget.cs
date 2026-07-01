namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpTorDraftBudgetId
{
    public static PpTorDraftBudgetId New() => From(Guid.CreateVersion7());
}

public class PpTorDraftBudget : AuditableEntity<PpTorDraftBudgetId>
{
    public override PpTorDraftBudgetId Id { get; init; }

    public int? Sequence { get; set; }

    public string? Description { get; set; }

    public decimal? BudgetAmount { get; set; }

    public virtual PpTorDraft PpTorDraft { get; init; }

    public virtual ICollection<PpTorDraftBudgetDetail>? PpTorDraftBudgetDetails { get; init; }
}