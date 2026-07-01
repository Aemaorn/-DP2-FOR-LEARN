namespace GHB.DP2.Domain.Procurement.PpAppoint;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpAppointDocumentHistoryId
{
    public static PpAppointDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public class PpAppointDocumentHistory : DocumentHistory<PpAppointDocumentHistoryId>
{
    public override PpAppointDocumentHistoryId Id { get; init; }

    public AppointStatus StatusState { get; init; }

    public static PpAppointDocumentHistory Create(
        AppointStatus statusState,
        string version,
        FileId fileId,
        bool? isReplace = false)
    {
        return new PpAppointDocumentHistory
        {
            Id = PpAppointDocumentHistoryId.New(),
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
    }
}