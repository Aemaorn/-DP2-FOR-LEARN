namespace GHB.DP2.Domain.Procurement.P79Clause2;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct P79Clause2GLAccountId
{
    public static P79Clause2GLAccountId New() => From(Guid.CreateVersion7());
}

public class P79Clause2GLAccount : AuditableEntity<P79Clause2GLAccountId>
{
    public override P79Clause2GLAccountId Id { get; init; }

    public P79Clause2Id P79Clause2Id { get; set; }

    public int Sequence { get; private set; }

    public string SoId { get; private set; }

    public virtual ParameterCode BudgetTypeCode { get; private set; }

    public virtual ParameterCode GLAccountCode { get; private set; }

    public string? ProjectNumber { get; set; }

    public decimal Amount { get; set; }

    public virtual SuParameter GLAccount { get; init; }

    public virtual SuParameter BudgetType { get; init; }

    public virtual P79Clause2 P79Clause2 { get; init; }

    public static P79Clause2GLAccount Create(
        P79Clause2Id p79Clause2Id)
    {
        var P79Clause2gLAccount = new P79Clause2GLAccount
        {
            Id = P79Clause2GLAccountId.New(),
            P79Clause2Id = p79Clause2Id,
        };

        return P79Clause2gLAccount;
    }

    public P79Clause2GLAccount SetGLAccount(
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
        this.GLAccountCode = gLAccountCode;
        this.ProjectNumber = projectNumber;
        this.Amount = amount;

        return this;
    }
}