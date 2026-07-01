namespace GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;

using Codehard.FileService.Contracts.ValueObjects;

public class CaContractDraftEditVendorsAttachmentFile
{
    public FileId Id { get; init; }

    public string FileName { get; init; }

    public string FileType { get; init; }

    public int Sequence { get; init; }

    public static CaContractDraftEditVendorsAttachmentFile Create(
        Guid id,
        string fileName,
        string fileType,
        int sequence)
    {
        return new CaContractDraftEditVendorsAttachmentFile
        {
            Id = FileId.From(id),
            FileName = fileName,
            FileType = fileType,
            Sequence = sequence,
        };
    }
}
