namespace GHB.DP2.Domain.Procurement.PpMedianPrice;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpMedianPriceDocumentHistoryId
{
    public static PpMedianPriceDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public class PpMedianPriceDocumentHistory : DocumentHistory<PpMedianPriceDocumentHistoryId>
{
    public override PpMedianPriceDocumentHistoryId Id { get; init; }

    public MedianPriceStatus StatusState { get; init; }

    public static PpMedianPriceDocumentHistory Create(
        MedianPriceStatus statusState,
        string version,
        FileId fileId,
        bool? isReplace = false)
    {
        return new PpMedianPriceDocumentHistory
        {
            Id = PpMedianPriceDocumentHistoryId.New(),
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
    }
}