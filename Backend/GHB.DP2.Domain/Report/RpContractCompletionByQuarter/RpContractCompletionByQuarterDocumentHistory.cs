namespace GHB.DP2.Domain.Report.RpContractCompletionByQuarter;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct RpContractCompletionByQuarterDocumentHistoryId
{
    public static RpContractCompletionByQuarterDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum RpContractCompletionByQuarterDocumentType
{
    Completion,
}

public class RpContractCompletionByQuarterDocumentHistory : DocumentHistory<RpContractCompletionByQuarterDocumentHistoryId>
{
    public override RpContractCompletionByQuarterDocumentHistoryId Id { get; init; }

    public RpContractCompletionByQuarterDocumentType DocumentType { get; init; }

    public RpContractCompletionByQuarterStatus StatusState { get; init; }

    public static RpContractCompletionByQuarterDocumentHistory Create(
        RpContractCompletionByQuarterDocumentType documentType,
        RpContractCompletionByQuarterStatus statusState,
        string version,
        FileId fileId,
        bool? isReplace = false)
    {
        return new RpContractCompletionByQuarterDocumentHistory
        {
            Id = RpContractCompletionByQuarterDocumentHistoryId.New(),
            DocumentType = documentType,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
    }
}