namespace GHB.DP2.Domain.Procurement.Pw119;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct Pw119GLAccountId
{
    public static Pw119GLAccountId New() => From(Guid.CreateVersion7());
}

public class Pw119GLAccount : AuditableEntity<Pw119GLAccountId>
{
    public override Pw119GLAccountId Id { get; init; }

    public Pw119Id Pw119Id { get; set; }

    public int Sequence { get; private set; }

    public string SoId { get; private set; }

    public virtual ParameterCode BudgetTypeCode { get; private set; }

    public virtual ParameterCode GLAccountCode { get; private set; }

    public string? ProjectNumber { get; set; }

    public decimal Amount { get; set; }

    public virtual SuParameter GLAccount { get; init; }

    public virtual SuParameter BudgetType { get; init; }

    public virtual Pw119 Pw119 { get; init; }

    public static Pw119GLAccount Create(
        Pw119Id pw119Id)
    {
        var pw119gLAccount = new Pw119GLAccount
        {
            Id = Pw119GLAccountId.New(),
            Pw119Id = pw119Id,
        };

        return pw119gLAccount;
    }

    public Pw119GLAccount SetGLAccount(
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