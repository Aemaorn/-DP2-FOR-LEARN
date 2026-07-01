namespace GHB.DP2.Domain.Procurement.Pw184;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct Pw184GLAccountId
{
    public static Pw184GLAccountId New() => From(Guid.CreateVersion7());
}

public class Pw184GLAccount : AuditableEntity<Pw184GLAccountId>
{
    public override Pw184GLAccountId Id { get; init; }

    public Pw184Id Pw184Id { get; set; }

    public int Sequence { get; private set; }

    public string SoId { get; private set; }

    public virtual ParameterCode BudgetTypeCode { get; private set; }

    public virtual ParameterCode GLAccountCode { get; private set; }

    public string? ProjectNumber { get; set; }

    public decimal Amount { get; set; }

    public virtual SuParameter GLAccount { get; init; }

    public virtual SuParameter BudgetType { get; init; }

    public virtual Pw184 Pw184 { get; init; }

    public static Pw184GLAccount Create(Pw184Id pw184Id)
    {
        return new Pw184GLAccount
        {
            Id = Pw184GLAccountId.New(),
            Pw184Id = pw184Id,
        };
    }

    public Pw184GLAccount SetGLAccount(
        int sequence,
        string soId,
        ParameterCode budgetTypeCode,
        ParameterCode glAccountCode,
        string? projectNumber,
        decimal amount)
    {
        this.Sequence = sequence;
        this.SoId = soId;
        this.BudgetTypeCode = budgetTypeCode;
        this.GLAccountCode = glAccountCode;
        this.ProjectNumber = projectNumber;
        this.Amount = amount;
        return this;
    }
}
