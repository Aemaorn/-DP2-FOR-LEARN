namespace GHB.DP2.Domain.Procurement.PpPurchaseRequisition;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PurchaseRequisitionDocumentHistoryId
{
    public static PurchaseRequisitionDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public class PurchaseRequisitionDocumentHistory : DocumentHistory<PurchaseRequisitionDocumentHistoryId>
{
    public override PurchaseRequisitionDocumentHistoryId Id { get; init; }

    public PurchaseRequisitionStatus StatusState { get; init; }

    public static PurchaseRequisitionDocumentHistory Create(
        PurchaseRequisitionStatus statusState,
        string version,
        FileId fileId,
        bool? isReplace = false)
    {
        return new PurchaseRequisitionDocumentHistory
        {
            Id = PurchaseRequisitionDocumentHistoryId.New(),
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
    }
}