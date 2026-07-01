namespace GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRentalBudgetId
{
    public static PPrincipleApprovalRentalBudgetId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalRentalBudget : AuditableEntity<PPrincipleApprovalRentalBudgetId>
{
    public override PPrincipleApprovalRentalBudgetId Id { get; init; }

    public int Sequence { get; set; }

    public string Description { get; set; }

    public decimal BudgetAmount { get; set; }

    public virtual PPrincipleApprovalRental PPrincipleApprovalRental { get; init; }

    public virtual IReadOnlyCollection<PPrincipleApprovalRentalBudgetDetail> PrincipleApprovalRentalBudgetDetails { get; private set; }

    public static PPrincipleApprovalRentalBudget Create(
        int sequence,
        string description,
        decimal budgetAmount)
    {
        return new PPrincipleApprovalRentalBudget
        {
            Id = PPrincipleApprovalRentalBudgetId.New(),
            Sequence = sequence,
            Description = description,
            BudgetAmount = budgetAmount,
        };
    }

    public PPrincipleApprovalRentalBudget Update(
        int sequence,
        string description,
        decimal budgetAmount)
    {
        this.Sequence = sequence;
        this.Description = description;
        this.BudgetAmount = budgetAmount;

        return this;
    }

    public PPrincipleApprovalRentalBudget AddBudgetDetail(PPrincipleApprovalRentalBudgetDetail budgetDetail)
    {
        var acceptors = this.PrincipleApprovalRentalBudgetDetails?.ToList() ?? new List<PPrincipleApprovalRentalBudgetDetail>();
        acceptors.Add(budgetDetail);
        this.PrincipleApprovalRentalBudgetDetails = acceptors;

        return this;
    }

    public PPrincipleApprovalRentalBudget RemoveBudgetDetail(PPrincipleApprovalRentalBudgetDetail budgetDetail)
    {
        var list = this.PrincipleApprovalRentalBudgetDetails?.ToList() ?? new List<PPrincipleApprovalRentalBudgetDetail>();
        list.Remove(budgetDetail);
        this.PrincipleApprovalRentalBudgetDetails = list;

        return this;
    }
}