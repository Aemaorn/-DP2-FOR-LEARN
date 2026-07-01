namespace GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CmContractGuaranteeReturnConditionId
{
    public static CmContractGuaranteeReturnConditionId New() => From(Guid.CreateVersion7());
}

public class CmContractGuaranteeReturnCondition : AuditableEntity<CmContractGuaranteeReturnConditionId>
{
    public override CmContractGuaranteeReturnConditionId Id { get; init; }

    public int Sequence { get; private set; }

    public string Description { get; private set; }

    public bool IsSatisfied { get; private set; }

    public virtual CmContractGuaranteeReturn CmContractGuaranteeReturn { get; init; }

    public static CmContractGuaranteeReturnCondition Create()
    {
        return new CmContractGuaranteeReturnCondition
        {
            Id = CmContractGuaranteeReturnConditionId.New(),
        };
    }

    public CmContractGuaranteeReturnCondition SetValue(
        int sequence,
        string description,
        bool isSatisfied)
    {
        this.Sequence = sequence;
        this.Description = description;
        this.IsSatisfied = isSatisfied;

        return this;
    }
}
