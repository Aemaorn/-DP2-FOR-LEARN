namespace GHB.DP2.Domain.Procurement.ChangeCommittee;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CommitteeChangeAttachmentId
{
    public static CommitteeChangeAttachmentId New() => From(Guid.CreateVersion7());
}

/// <summary>
/// ไฟล์แนบการเปลี่ยนแปลงคณะกรรมการ
/// </summary>
public partial class CommitteeChangeAttachment : AuditableEntity<CommitteeChangeAttachmentId>, IHasSoftDelete
{
    public override CommitteeChangeAttachmentId Id { get; init; }

    public CommitteeChangeId CommitteeChangeId { get; init; }

    public int Sequence { get; private set; }

    public ParameterCode TypeCode { get; private set; }

    public string? Remark { get; private set; }

    public virtual CommitteeChanges CommitteeChanges { get; init; }

    public virtual SuParameter SuParameter { get; init; }

    public virtual IReadOnlyCollection<CommitteeChangeAttachmentInfo> CommitteeChangeAttachmentInfos { get; private set; }

    public static CommitteeChangeAttachment Create(
        CommitteeChangeId committeeChangeId,
        int sequence,
        ParameterCode typeCode,
        string? remark = null)
    {
        return new CommitteeChangeAttachment
        {
            Id = CommitteeChangeAttachmentId.New(),
            CommitteeChangeId = committeeChangeId,
            Sequence = sequence,
            TypeCode = typeCode,
            Remark = remark,
            CommitteeChangeAttachmentInfos = new List<CommitteeChangeAttachmentInfo>(),
        };
    }

    public CommitteeChangeAttachment AddAttachmentInfos(
        CommitteeChangeAttachmentInfo info)
    {
        if (info == null)
        {
            throw new ArgumentNullException(nameof(info), "Attachment info cannot be null.");
        }

        var attachmentInfo = this.CommitteeChangeAttachmentInfos.ToHashSet();

        if (attachmentInfo.Any(a => a.Id == info.Id))
        {
            throw new InvalidOperationException("Attachment info already exists.");
        }

        attachmentInfo.Add(info);

        this.CommitteeChangeAttachmentInfos = attachmentInfo;

        return this;
    }

    public CommitteeChangeAttachment RemoveAttachmentInfos(
        CommitteeChangeAttachmentInfo info)
    {
        if (info == null)
        {
            throw new ArgumentNullException(nameof(info), "Attachment info cannot be null.");
        }

        var attachmentInfo = this.CommitteeChangeAttachmentInfos.ToHashSet();

        if (!attachmentInfo.Remove(info))
        {
            throw new InvalidOperationException("Attachment info does not exist.");
        }

        this.CommitteeChangeAttachmentInfos = attachmentInfo;

        return this;
    }

    public CommitteeChangeAttachment SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public CommitteeChangeAttachment SetTypeCode(ParameterCode typeCode)
    {
        if (typeCode == this.TypeCode)
        {
            return this;
        }

        this.TypeCode = typeCode;

        return this;
    }

    public CommitteeChangeAttachment SetRemark(string? remark)
    {
        this.Remark = remark;

        return this;
    }
}