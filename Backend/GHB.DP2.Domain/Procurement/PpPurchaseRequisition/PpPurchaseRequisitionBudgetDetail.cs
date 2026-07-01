namespace GHB.DP2.Domain.Procurement.PpPurchaseRequisition;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpPurchaseRequisitionBudgetDetailId
{
    public static PpPurchaseRequisitionBudgetDetailId New() => From(Guid.CreateVersion7());
}

public partial class PpPurchaseRequisitionBudgetDetail : AuditableEntity<PpPurchaseRequisitionBudgetDetailId>
{
    public override PpPurchaseRequisitionBudgetDetailId Id { get; init; }

    public PpPurchaseRequisitionBudgetId PpPurchaseRequisitionBudgetId { get; init; }

    public int Sequence { get; private set; }

    public string Department { get; private set; }

    public ParameterCode BudgetTypeCode { get; private set; }

    public string? ProjectCode { get; private set; }

    public ParameterCode AccountNoCode { get; private set; }

    public decimal Budget { get; private set; }

    public virtual PpPurchaseRequisitionBudget PpPurchaseRequisitionBudget { get; init; }

    public virtual SuParameter BudgetType { get; init; }

    public virtual SuParameter AccountNo { get; init; }

    public static PpPurchaseRequisitionBudgetDetail Create(
        PpPurchaseRequisitionBudgetId ppPurchaseRequisitionBudgetId,
        int sequence,
        string department,
        ParameterCode budgetTypeCode,
        string? projectCode,
        ParameterCode accountNoCode,
        decimal budget)
    {
        return new PpPurchaseRequisitionBudgetDetail
        {
            Id = PpPurchaseRequisitionBudgetDetailId.New(),
            PpPurchaseRequisitionBudgetId = ppPurchaseRequisitionBudgetId,
            Sequence = sequence,
            Department = department,
            BudgetTypeCode = budgetTypeCode,
            ProjectCode = projectCode,
            AccountNoCode = accountNoCode,
            Budget = budget,
        };
    }

    public Unit Update(
        int sequence,
        string department,
        ParameterCode budgetTypeCode,
        string? projectCode,
        ParameterCode accountNoCode,
        decimal budget)
    {
        this.Sequence = sequence;
        this.Department = department;
        this.BudgetTypeCode = budgetTypeCode;
        this.ProjectCode = projectCode;
        this.AccountNoCode = accountNoCode;
        this.Budget = budget;

        return Unit.Default;
    }
}