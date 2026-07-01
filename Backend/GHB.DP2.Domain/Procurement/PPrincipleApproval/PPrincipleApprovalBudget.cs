namespace GHB.DP2.Domain.Procurement.PPrincipleApproval;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalBudgetId
{
    public static PPrincipleApprovalBudgetId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalBudget : AuditableEntity<PPrincipleApprovalBudgetId>
{
    public override PPrincipleApprovalBudgetId Id { get; init; }

    public int Sequence { get; set; }

    public string Description { get; set; }

    public decimal BudgetAmount { get; set; }

    public virtual PPrincipleApproval PPrincipleApproval { get; init; }

    public virtual IReadOnlyCollection<PPrincipleApprovalBudgetDetail> PrincipleApprovalBudgetDetails { get; private set; }

    public static PPrincipleApprovalBudget Create(
        int sequence,
        string description,
        decimal budgetAmount)
    {
        return new PPrincipleApprovalBudget
        {
            Id = PPrincipleApprovalBudgetId.New(),
            Sequence = sequence,
            Description = description,
            BudgetAmount = budgetAmount,
        };
    }

    public PPrincipleApprovalBudget Update(
        int sequence,
        string description,
        decimal budgetAmount)
    {
        this.Sequence = sequence;
        this.Description = description;
        this.BudgetAmount = budgetAmount;

        return this;
    }

    public PPrincipleApprovalBudget AddBudgetDetail(PPrincipleApprovalBudgetDetail budgetDetail)
    {
        var acceptors = this.PrincipleApprovalBudgetDetails?.ToList() ?? new List<PPrincipleApprovalBudgetDetail>();
        acceptors.Add(budgetDetail);
        this.PrincipleApprovalBudgetDetails = acceptors;

        return this;
    }

    public PPrincipleApprovalBudget RemoveBudgetDetail(PPrincipleApprovalBudgetDetail budgetDetail)
    {
        var list = this.PrincipleApprovalBudgetDetails?.ToList() ?? new List<PPrincipleApprovalBudgetDetail>();
        list.Remove(budgetDetail);
        this.PrincipleApprovalBudgetDetails = list;

        return this;
    }
}