namespace GHB.DP2.Domain.Procurement.PpPurchaseRequisition;

using GHB.DP2.Domain.Common;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpPurchaseRequisitionBudgetId
{
    public static PpPurchaseRequisitionBudgetId New() => From(Guid.CreateVersion7());
}

public partial class PpPurchaseRequisitionBudget : AuditableEntity<PpPurchaseRequisitionBudgetId>
{
    public override PpPurchaseRequisitionBudgetId Id { get; init; }

    public PpPurchaseRequisitionId PpPurchaseRequisitionId { get; init; }

    public string Description { get; private set; }

    public decimal BudgetAmount { get; private set; }

    public int Sequence { get; private set; }

    public virtual PpPurchaseRequisition PpPurchaseRequisition { get; init; }

    public virtual IReadOnlyCollection<PpPurchaseRequisitionBudgetDetail> PpPurchaseRequisitionBudgetDetails { get; private set; }

    public static PpPurchaseRequisitionBudget Create(
        PpPurchaseRequisitionId ppPurchaseRequisitionId,
        string description,
        decimal budgetAmount,
        int sequence)
    {
        return new PpPurchaseRequisitionBudget
        {
            Id = PpPurchaseRequisitionBudgetId.New(),
            PpPurchaseRequisitionId = ppPurchaseRequisitionId,
            Description = description,
            BudgetAmount = budgetAmount,
            PpPurchaseRequisitionBudgetDetails = new List<PpPurchaseRequisitionBudgetDetail>(),
            Sequence = sequence,
        };
    }

    public Unit Update(
        string description,
        decimal budgetAmount,
        int sequence)
    {
        this.Description = description;
        this.BudgetAmount = budgetAmount;
        this.Sequence = sequence;

        return Unit.Default;
    }

    public PpPurchaseRequisitionBudget AddBudgetDetail(PpPurchaseRequisitionBudgetDetail budgetDetail)
    {
        var ppBudgetDetail = this.PpPurchaseRequisitionBudgetDetails.ToHashSet();
        ppBudgetDetail.Add(budgetDetail);

        this.PpPurchaseRequisitionBudgetDetails = ppBudgetDetail;

        return this;
    }
}