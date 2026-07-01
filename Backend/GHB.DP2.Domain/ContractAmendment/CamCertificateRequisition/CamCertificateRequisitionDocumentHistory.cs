namespace GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CamCertificateRequisitionDocumentHistoryId
{
    public static CamCertificateRequisitionDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public class CamCertificateRequisitionDocumentHistory : DocumentHistory<CamCertificateRequisitionDocumentHistoryId>
{
    public override CamCertificateRequisitionDocumentHistoryId Id { get; init; }

    public CamCertificateRequisitionStatus StatusState { get; init; }

    public static CamCertificateRequisitionDocumentHistory Create(
        CamCertificateRequisitionStatus statusState,
        string version,
        FileId fileId,
        bool? isReplace = false)
    {
        return new CamCertificateRequisitionDocumentHistory
        {
            Id = CamCertificateRequisitionDocumentHistoryId.New(),
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
    }
}