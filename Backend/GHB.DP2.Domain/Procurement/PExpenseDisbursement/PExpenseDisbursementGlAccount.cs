namespace GHB.DP2.Domain.Procurement.PExpenseDisbursement;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PExpenseDisbursementGlAccountId
{
    public static PExpenseDisbursementGlAccountId New() => From(Guid.CreateVersion7());
}

public class PExpenseDisbursementGlAccount : AuditableEntity<PExpenseDisbursementGlAccountId>
{
    public override PExpenseDisbursementGlAccountId Id { get; init; }

    public int Sequence { get; private set; }

    public string SoId { get; private set; }

    public virtual ParameterCode BudgetTypeCode { get; private set; }

    public virtual ParameterCode GlAccountCode { get; private set; }

    public string? ProjectNumber { get; set; }

    public decimal Amount { get; set; }

    public virtual SuParameter GlAccount { get; init; }

    public virtual SuParameter BudgetType { get; init; }

    public virtual PExpenseDisbursement ExpenseDisbursement { get; init; }

    public static PExpenseDisbursementGlAccount Create()
    {
        var expenseDisbursement = new PExpenseDisbursementGlAccount
        {
            Id = PExpenseDisbursementGlAccountId.New(),
        };

        return expenseDisbursement;
    }

    public PExpenseDisbursementGlAccount SetValue(
        int sequence,
        string soId,
        ParameterCode budgetTypeCode,
        ParameterCode gLAccountCode,
        string? projectNumber,
        decimal amount)
    {
        this.Sequence = sequence;
        this.SoId = soId;
        this.BudgetTypeCode = budgetTypeCode;
        this.GlAccountCode = gLAccountCode;
        this.ProjectNumber = projectNumber;
        this.Amount = amount;

        return this;
    }
}