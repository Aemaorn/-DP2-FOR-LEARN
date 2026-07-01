namespace GHB.DP2.Domain.ContractManagement.CmContractTermination;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CmContractTerminationDocumentHistoryId
{
    public static CmContractTerminationDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum CmContractTerminationDocumentType
{
    ContractTermination,
}

public class CmContractTerminationDocumentHistory : DocumentHistory<CmContractTerminationDocumentHistoryId>
{
    public override CmContractTerminationDocumentHistoryId Id { get; init; }

    public CmContractTerminationDocumentType DocumentType { get; init; }

    public CmContractTerminationStatus StatusState { get; init; }

    public virtual CmContractTermination CmContractTermination { get; init; }

    public static CmContractTerminationDocumentHistory Create(
        CmContractTerminationDocumentType documentType,
        CmContractTerminationStatus statusState,
        string version,
        FileId fileId,
        bool isReplaced = false)
    {
        return new CmContractTerminationDocumentHistory
        {
            Id = CmContractTerminationDocumentHistoryId.New(),
            DocumentType = documentType,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplaced,
        };
    }
}