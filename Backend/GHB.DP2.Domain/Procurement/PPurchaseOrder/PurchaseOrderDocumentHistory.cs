namespace GHB.DP2.Domain.Procurement.PPurchaseOrder;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PurchaseOrderDocumentHistoryId
{
    public static PurchaseOrderDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum PurchaseOrderDocumentType
{
    Jp006,
    Winner,
}

public class PurchaseOrderDocumentHistory : DocumentHistory<PurchaseOrderDocumentHistoryId>
{
    public override PurchaseOrderDocumentHistoryId Id { get; init; }

    public PurchaseOrderDocumentType DocumentType { get; init; }

    public PurchaseOrderStatus StatusState { get; init; }

    public static PurchaseOrderDocumentHistory Create(
        PurchaseOrderDocumentType documentType,
        PurchaseOrderStatus statusState,
        string version,
        FileId fileId,
        bool? isReplace = false)
    {
        return new PurchaseOrderDocumentHistory
        {
            Id = PurchaseOrderDocumentHistoryId.New(),
            DocumentType = documentType,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
    }
}