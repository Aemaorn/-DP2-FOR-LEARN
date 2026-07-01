namespace GHB.DP2.Domain.Procurement.PJp005;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PJp005DocumentHistoryId
{
    public static PJp005DocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum PJp005DocumentType
{
    Approval,
    Command,
}

public class PJp005DocumentHistory : DocumentHistory<PJp005DocumentHistoryId>
{
    public override PJp005DocumentHistoryId Id { get; init; }

    public PJp005DocumentType DocumentType { get; init; }

    public PJp005Status StatusState { get; init; }

    public static PJp005DocumentHistory Create(
        PJp005DocumentType documentType,
        PJp005Status statusState,
        string version,
        FileId fileId,
        bool? isReplace = false)
    {
        return new PJp005DocumentHistory
        {
            Id = PJp005DocumentHistoryId.New(),
            DocumentType = documentType,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
    }
}