namespace GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CmContractGuaranteeReturnRequiredDocumentId
{
    public static CmContractGuaranteeReturnRequiredDocumentId New() => From(Guid.CreateVersion7());
}

public class CmContractGuaranteeReturnRequiredDocument : AuditableEntity<CmContractGuaranteeReturnRequiredDocumentId>
{
    public override CmContractGuaranteeReturnRequiredDocumentId Id { get; init; }

    public int Sequence { get; private set; }

    public string DocumentName { get; private set; }

    public bool IsSubmitted { get; private set; }

    public virtual CmContractGuaranteeReturn CmContractGuaranteeReturn { get; init; }

    public static CmContractGuaranteeReturnRequiredDocument Create()
    {
        return new CmContractGuaranteeReturnRequiredDocument
        {
            Id = CmContractGuaranteeReturnRequiredDocumentId.New(),
        };
    }

    public CmContractGuaranteeReturnRequiredDocument SetValue(
        int sequence,
        string documentName,
        bool isSubmitted)
    {
        this.Sequence = sequence;
        this.DocumentName = documentName;
        this.IsSubmitted = isSubmitted;

        return this;
    }
}
