namespace GHB.DP2.Domain.Procurement.ChangeCommittee;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CommitteeChangeAttachmentInfoId
{
    public static CommitteeChangeAttachmentInfoId New() => From(Guid.CreateVersion7());
}

/// <summary>
/// ข้อมูลไฟล์แนบการเปลี่ยนแปลงคณะกรรมการ
/// </summary>
public partial class CommitteeChangeAttachmentInfo : AuditableEntity<CommitteeChangeAttachmentInfoId>, IHasSoftDelete
{
    public override CommitteeChangeAttachmentInfoId Id { get; init; }

    public CommitteeChangeAttachmentId CommitteeChangeAttachmentId { get; private set; }

    public int Sequence { get; private set; }

    public FileId FileId { get; private set; }

    public string FileName { get; private set; }

    public bool IsPublic { get; private set; }

    public virtual CommitteeChangeAttachment CommitteeChangeAttachment { get; init; }

    public static CommitteeChangeAttachmentInfo Create(
        CommitteeChangeAttachmentId committeeChangeAttachmentId,
        int sequence,
        FileId fileId,
        string fileName,
        bool isPublic)
    {
        return new CommitteeChangeAttachmentInfo
        {
            Id = CommitteeChangeAttachmentInfoId.New(),
            CommitteeChangeAttachmentId = committeeChangeAttachmentId,
            Sequence = sequence,
            FileId = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public CommitteeChangeAttachmentInfo SetSequence(
        int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public CommitteeChangeAttachmentInfo SetPublic(
        bool isPublic)
    {
        this.IsPublic = isPublic;

        return this;
    }
}