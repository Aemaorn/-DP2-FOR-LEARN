namespace GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPurchaseOrderApprovalBudgetId
{
    public static PPurchaseOrderApprovalBudgetId New() => From(Guid.CreateVersion7());
}

public partial class PPurchaseOrderApprovalBudget : AuditableEntity<PPurchaseOrderApprovalBudgetId>
{
    public override PPurchaseOrderApprovalBudgetId Id { get; init; }

    public string Description { get; private set; }

    public decimal BudgetAmount { get; private set; }

    public int Sequence { get; private set; }

    public virtual PPurchaseOrderApproval PPurchaseOrderApproval { get; init; }

    public static PPurchaseOrderApprovalBudget Create(
        PPurchaseOrderApproval purchaseOrderApproval,
        int sequence,
        string description,
        decimal budgetAmount)
    {
        return new PPurchaseOrderApprovalBudget
        {
            Id = PPurchaseOrderApprovalBudgetId.New(),
            Sequence = sequence,
            Description = description,
            BudgetAmount = budgetAmount,
            PPurchaseOrderApproval = purchaseOrderApproval,
        };
    }

    public PPurchaseOrderApprovalBudget Update(
        string description,
        decimal budgetAmount)
    {
        this.Description = description;
        this.BudgetAmount = budgetAmount;

        return this;
    }
}
