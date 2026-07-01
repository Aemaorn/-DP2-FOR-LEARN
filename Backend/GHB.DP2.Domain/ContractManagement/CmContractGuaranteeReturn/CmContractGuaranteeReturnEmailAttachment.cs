namespace GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CmContractGuaranteeReturnEmailAttachmentsId
{
    public static CmContractGuaranteeReturnEmailAttachmentsId New() => From(Guid.CreateVersion7());
}

public class CmContractGuaranteeReturnEmailAttachments : AuditableEntity<CmContractGuaranteeReturnEmailAttachmentsId>
{
    public override CmContractGuaranteeReturnEmailAttachmentsId Id { get; init; }

    public FileId FileId { get; private init; }

    public string FileName { get; private init; }

    public int Sequence { get; private set; }

    public static CmContractGuaranteeReturnEmailAttachments Create(
        FileId fileId,
        string fileName,
        int sequence)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        return new CmContractGuaranteeReturnEmailAttachments
        {
            Id = CmContractGuaranteeReturnEmailAttachmentsId.New(),
            Sequence = sequence,
            FileId = fileId,
            FileName = fileName,
        };
    }

    public CmContractGuaranteeReturnEmailAttachments SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public CmContractGuaranteeReturnEmailAttachments Clone()
    {
        return new CmContractGuaranteeReturnEmailAttachments
        {
            Id = CmContractGuaranteeReturnEmailAttachmentsId.New(),
            Sequence = this.Sequence,
            FileId = this.FileId,
            FileName = this.FileName,
        };
    }
}