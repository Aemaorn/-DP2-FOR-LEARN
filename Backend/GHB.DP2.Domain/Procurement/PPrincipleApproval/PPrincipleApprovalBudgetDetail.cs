namespace GHB.DP2.Domain.Procurement.PPrincipleApproval;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalBudgetDetailId
{
    public static PPrincipleApprovalBudgetDetailId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalBudgetDetail : AuditableEntity<PPrincipleApprovalBudgetDetailId>
{
    public override PPrincipleApprovalBudgetDetailId Id { get; init; }

    public PPrincipleApprovalBudgetId PrincipleApprovalBudgetId { get; init; }

    public int Sequence { get; set; }

    public string Department { get; set; }

    public string BudgetType { get; set; }

    public string? ProjectCode { get; set; }

    public string AccountNo { get; set; }

    public decimal Budget { get; set; }

    public virtual PPrincipleApprovalBudget PrincipleApprovalBudget { get; init; }

    public static PPrincipleApprovalBudgetDetail Create(
        int sequence,
        string department,
        string budgetType,
        string? projectCode,
        string accountNo,
        decimal budget)
    {
        return new PPrincipleApprovalBudgetDetail
        {
            Id = PPrincipleApprovalBudgetDetailId.New(),
            Sequence = sequence,
            Department = department,
            BudgetType = budgetType,
            ProjectCode = projectCode,
            AccountNo = accountNo,
            Budget = budget,
        };
    }

    public PPrincipleApprovalBudgetDetail Update(
        int sequence,
        string department,
        string budgetType,
        string? projectCode,
        string accountNo,
        decimal budget)
    {
        this.Sequence = sequence;
        this.Department = department;
        this.BudgetType = budgetType;
        this.ProjectCode = projectCode;
        this.AccountNo = accountNo;
        this.Budget = budget;

        return this;
    }
}