namespace GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CmDeliveryAcceptanceDocumentHistoryId
{
    public static CmDeliveryAcceptanceDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum CmDeliveryAcceptanceDocumentType
{
    DeliveryAcceptance,
}

public class CmDeliveryAcceptancePeriodDocumentHistory : DocumentHistory<CmDeliveryAcceptanceDocumentHistoryId>
{
    public override CmDeliveryAcceptanceDocumentHistoryId Id { get; init; }

    public CmDeliveryAcceptanceDocumentType DocumentType { get; init; }

    public CmDeliveryAcceptancePeriodStatus StatusState { get; init; }

    public static CmDeliveryAcceptancePeriodDocumentHistory Create(
        CmDeliveryAcceptanceDocumentType documentType,
        CmDeliveryAcceptancePeriodStatus statusState,
        string version,
        FileId fileId,
        bool? isReplace = false)
    {
        return new CmDeliveryAcceptancePeriodDocumentHistory
        {
            Id = CmDeliveryAcceptanceDocumentHistoryId.New(),
            DocumentType = documentType,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
    }
}