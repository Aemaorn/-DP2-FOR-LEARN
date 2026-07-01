namespace GHB.DP2.Domain.Procurement.PPettyCash;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PettyCashGLAccountId
{
    public static PettyCashGLAccountId New() => From(Guid.CreateVersion7());
}

public class PPettyCashGLAccount : AuditableEntity<PettyCashGLAccountId>
{
    public override PettyCashGLAccountId Id { get; init; }

    public PettyCashId PettyCashId { get; set; }

    public int Sequence { get; private set; }

    public string SoId { get; private set; }

    public virtual ParameterCode BudgetTypeCode { get; private set; }

    public virtual ParameterCode GLAccountCode { get; private set; }

    public string? ProjectNumber { get; set; }

    public decimal Amount { get; set; }

    public virtual SuParameter GLAccount { get; init; }

    public virtual SuParameter BudgetType { get; init; }

    public virtual PPettyCash PettyCash { get; init; }

    public static PPettyCashGLAccount Create(
        PettyCashId pettyCashId)
    {
        var PettyCashGLAccount = new PPettyCashGLAccount
        {
            Id = PettyCashGLAccountId.New(),
            PettyCashId = pettyCashId,
        };

        return PettyCashGLAccount;
    }

    public PPettyCashGLAccount SetGLAccount(
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