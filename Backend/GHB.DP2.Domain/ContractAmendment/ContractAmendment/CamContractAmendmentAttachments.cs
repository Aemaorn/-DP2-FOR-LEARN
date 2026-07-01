namespace GHB.DP2.Domain.ContractAmendment.ContractAmendment;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractAmendmentAttachmentId
{
    public static ContractAmendmentAttachmentId New() => From(Guid.NewGuid());
}

public class CamContractAmendmentAttachment : AuditableEntity<ContractAmendmentAttachmentId>
{
    public override ContractAmendmentAttachmentId Id { get; init; }

    public ParameterCode DocumentTypeCode { get; private set; }

    public FileId FileId { get; init; }

    public string FileName { get; init; }

    public bool IsPublic { get; private set; }

    public int Sequence { get; private set; }

    public virtual SuParameter DocumentType { get; init; }

    public CamContractAmendmentAttachment ChangeDocumentType(ParameterCode documentTypeCode)
    {
        this.DocumentTypeCode = documentTypeCode;

        return this;
    }

    public CamContractAmendmentAttachment SetPublic(bool isPublic)
    {
        this.IsPublic = isPublic;

        return this;
    }

    public CamContractAmendmentAttachment SetSequence(int sequence)
    {
        this.Sequence = sequence;

        return this;
    }

    public static CamContractAmendmentAttachment Create(
        ParameterCode documentTypeCode,
        FileId fileId,
        string fileName)
    {
        return new CamContractAmendmentAttachment
        {
            Id = ContractAmendmentAttachmentId.New(),
            DocumentTypeCode = documentTypeCode,
            FileId = fileId,
            FileName = fileName,
        };
    }
}