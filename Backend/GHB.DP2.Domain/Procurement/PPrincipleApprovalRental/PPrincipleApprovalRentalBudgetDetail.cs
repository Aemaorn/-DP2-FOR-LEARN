namespace GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRentalBudgetDetailId
{
    public static PPrincipleApprovalRentalBudgetDetailId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalRentalBudgetDetail : AuditableEntity<PPrincipleApprovalRentalBudgetDetailId>
{
    public override PPrincipleApprovalRentalBudgetDetailId Id { get; init; }

    public int Sequence { get; set; }

    public string Department { get; set; }

    public string BudgetType { get; set; }

    public string? ProjectCode { get; set; }

    public string AccountNo { get; set; }

    public decimal Budget { get; set; }

    public virtual PPrincipleApprovalRentalBudget PrincipleApprovalRentalBudget { get; init; }

    public static PPrincipleApprovalRentalBudgetDetail Create(
        int sequence,
        string department,
        string budgetType,
        string? projectCode,
        string accountNo,
        decimal budget)
    {
        return new PPrincipleApprovalRentalBudgetDetail
        {
            Id = PPrincipleApprovalRentalBudgetDetailId.New(),
            Sequence = sequence,
            Department = department,
            BudgetType = budgetType,
            ProjectCode = projectCode,
            AccountNo = accountNo,
            Budget = budget,
        };
    }

    public PPrincipleApprovalRentalBudgetDetail Update(
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