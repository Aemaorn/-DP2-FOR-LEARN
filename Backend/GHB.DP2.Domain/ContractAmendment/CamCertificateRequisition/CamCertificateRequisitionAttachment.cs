namespace GHB.DP2.Domain.ContractAmendment.CamCertificateRequisition;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CamCertificateRequisitionAttachmentId
{
    public static CamCertificateRequisitionAttachmentId New() => From(Guid.CreateVersion7());
}

public class CamCertificateRequisitionAttachment : AuditableEntity<CamCertificateRequisitionAttachmentId>
{
    public override CamCertificateRequisitionAttachmentId Id { get; init; }

    public ParameterCode DocumentTypeCode { get; private set; }

    public FileId FileId { get; init; }

    public string FileName { get; init; }

    public bool IsPublic { get; private set; }

    public int Sequence { get; private set; }

    public virtual SuParameter DocumentType { get; init; }

    public CamCertificateRequisitionAttachment ChangeDocumentType(ParameterCode documentTypeCode)
    {
        this.DocumentTypeCode = documentTypeCode;

        return this;
    }

    public CamCertificateRequisitionAttachment SetPublic(bool isPublic)
    {
        this.IsPublic = isPublic;

        return this;
    }

    public CamCertificateRequisitionAttachment SetSequence(int sequence)
    {
        this.Sequence = sequence;

        return this;
    }

    public static CamCertificateRequisitionAttachment Create(
        ParameterCode documentTypeCode,
        FileId fileId,
        string fileName)
    {
        return new CamCertificateRequisitionAttachment
        {
            Id = CamCertificateRequisitionAttachmentId.New(),
            DocumentTypeCode = documentTypeCode,
            FileId = fileId,
            FileName = fileName,
        };
    }
}
