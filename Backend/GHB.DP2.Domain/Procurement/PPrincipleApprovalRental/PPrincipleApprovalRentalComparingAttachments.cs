namespace GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;

public class PPrincipleApprovalRentalComparingAttachments : AuditableEntity<FileId>
{
    public override FileId Id { get; init; }

    public string FileName { get; set; }

    public bool IsPublic { get; set; }

    public int Sequence { get; set; }

    public virtual PPrincipleApprovalRental PPrincipleApprovalRental { get; init; }

    public static PPrincipleApprovalRentalComparingAttachments Create(
        FileId fileId,
        string fileName,
        int sequence,
        bool isPublic)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        return new PPrincipleApprovalRentalComparingAttachments
        {
            Sequence = sequence,
            Id = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }
}