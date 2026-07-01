namespace GHB.DP2.Domain.Plan;

public class PlanViewGHBDP
{
    public Guid Id { get; init; }

    public string PlanNumber { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public int BudgetYear { get; init; }

    public decimal Budget { get; init; }

    public PlanType Type { get; init; }

    public string DepartmentName { get; init; } = string.Empty;

    public string SupplyMethod { get; init; } = string.Empty;

    public bool IsChange { get; init; }

    public bool IsCancel { get; init; }

    public PlanStatus Status { get; init; }
}