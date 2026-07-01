namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft;

using Codehard.FileService.Contracts.ValueObjects;

public class CaContractDraftVendorsAttachmentFile
{
    public FileId Id { get; init; }

    public string FileName { get; init; }

    public string FileType { get; init; }

    public int Sequence { get; init; }

    public static CaContractDraftVendorsAttachmentFile Create(
        Guid id,
        string fileName,
        string fileType,
        int sequence)
    {
        return new CaContractDraftVendorsAttachmentFile
        {
            Id = FileId.From(id),
            FileName = fileName,
            FileType = fileType,
            Sequence = sequence,
        };
    }
}