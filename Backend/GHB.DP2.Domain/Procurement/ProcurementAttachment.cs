namespace GHB.DP2.Domain.Procurement;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ProcurementAttachmentId
{
    public static ProcurementAttachmentId New() => From(Guid.CreateVersion7());
}

public partial class ProcurementAttachment : AuditableEntity<ProcurementAttachmentId>, IHasSoftDelete
{
    public override ProcurementAttachmentId Id { get; init; }

    public ProcurementId ProcurementId { get; init; }

    public int Sequence { get; private set; }

    public ParameterCode TypeCode { get; private set; }

    public string? Remark { get; private set; }

    public virtual Procurement Procurement { get; init; }

    public virtual SuParameter SuParameter { get; init; }

    public virtual IReadOnlyCollection<ProcurementAttachmentInfo> ProcurementAttachmentInfos { get; private set; }

    public static ProcurementAttachment Create(
        ProcurementId procurementId,
        int sequence,
        ParameterCode typeCode,
        string? remark = null)
    {
        return new ProcurementAttachment
        {
            Id = ProcurementAttachmentId.New(),
            ProcurementId = procurementId,
            Sequence = sequence,
            TypeCode = typeCode,
            Remark = remark,
            ProcurementAttachmentInfos = new List<ProcurementAttachmentInfo>(),
        };
    }

    public ProcurementAttachment AddAttachmentInfos(
        ProcurementAttachmentInfo info)
    {
        if (info == null)
        {
            throw new ArgumentNullException(nameof(info), "Attachment info cannot be null.");
        }

        var attachmentInfo = this.ProcurementAttachmentInfos.ToHashSet();

        if (attachmentInfo.Any(a => a.Id == info.Id))
        {
            throw new InvalidOperationException("Attachment info already exists.");
        }

        attachmentInfo.Add(info);

        this.ProcurementAttachmentInfos = attachmentInfo;

        return this;
    }

    public ProcurementAttachment RemoveAttachmentInfos(
        ProcurementAttachmentInfo info)
    {
        if (info == null)
        {
            throw new ArgumentNullException(nameof(info), "Attachment info cannot be null.");
        }

        var attachmentInfo = this.ProcurementAttachmentInfos.ToHashSet();

        if (!attachmentInfo.Remove(info))
        {
            throw new InvalidOperationException("Attachment info does not exist.");
        }

        this.ProcurementAttachmentInfos = attachmentInfo;

        return this;
    }

    public ProcurementAttachment SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public ProcurementAttachment SetTypeCode(ParameterCode typeCode)
    {
        if (typeCode == this.TypeCode)
        {
            return this;
        }

        this.TypeCode = typeCode;

        return this;
    }

    public ProcurementAttachment SetRemark(string? remark)
    {
        this.Remark = remark;

        return this;
    }
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ProcurementAttachmentInfoId
{
    public static ProcurementAttachmentInfoId New() => From(Guid.CreateVersion7());
}

public partial class ProcurementAttachmentInfo : AuditableEntity<ProcurementAttachmentInfoId>, IHasSoftDelete
{
    public override ProcurementAttachmentInfoId Id { get; init; }

    public ProcurementAttachmentId ProcurementAttachmentId { get; private set; }

    public int Sequence { get; private set; }

    public FileId FileId { get; private set; }

    public string FileName { get; private set; }

    public bool IsPublic { get; private set; }

    public virtual ProcurementAttachment ProcurementAttachment { get; init; }

    public static ProcurementAttachmentInfo Create(
        ProcurementAttachmentId procurementAttachmentId,
        int sequence,
        FileId fileId,
        string fileName,
        bool isPublic)
    {
        return new ProcurementAttachmentInfo
        {
            Id = ProcurementAttachmentInfoId.New(),
            ProcurementAttachmentId = procurementAttachmentId,
            Sequence = sequence,
            FileId = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public ProcurementAttachmentInfo SetSequence(
        int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public ProcurementAttachmentInfo SetPublic(
        bool isPublic)
    {
        this.IsPublic = isPublic;

        return this;
    }
}