namespace GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CmDeliveryAcceptancePeriodBudgetId
{
    public static CmDeliveryAcceptancePeriodBudgetId New() => From(Guid.CreateVersion7());
}

public partial class CmDeliveryAcceptancePeriodBudget : AuditableEntity<CmDeliveryAcceptancePeriodBudgetId>
{
    public override CmDeliveryAcceptancePeriodBudgetId Id { get; init; }

    public CmDeliveryAcceptancePeriodId CmDeliveryAcceptancePeriodId { get; init; }

    public int Sequence { get; private set; }

    public string Department { get; private set; }

    public ParameterCode BudgetTypeCode { get; private set; }

    public string? ProjectCode { get; private set; }

    public ParameterCode AccountNoCode { get; private set; }

    public decimal Budget { get; private set; }

    public virtual CmDeliveryAcceptancePeriod Period { get; init; }

    public virtual SuParameter BudgetType { get; init; }

    public virtual SuParameter AccountNo { get; init; }

    public static CmDeliveryAcceptancePeriodBudget Create(
        CmDeliveryAcceptancePeriodId cmDeliveryAcceptancePeriodId,
        int sequence,
        string department,
        ParameterCode budgetTypeCode,
        string? projectCode,
        ParameterCode accountNoCode,
        decimal budget)
    {
        return new CmDeliveryAcceptancePeriodBudget
        {
            Id = CmDeliveryAcceptancePeriodBudgetId.New(),
            CmDeliveryAcceptancePeriodId = cmDeliveryAcceptancePeriodId,
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