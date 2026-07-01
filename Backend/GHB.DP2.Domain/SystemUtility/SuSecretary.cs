namespace GHB.DP2.Domain.SystemUtility;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct SecretaryOwnerId
{
    public static SecretaryOwnerId New() => From(Guid.CreateVersion7());
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct SecretaryId
{
    public static SecretaryId New() => From(Guid.CreateVersion7());
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct SecretaryAttachmentId
{
    public static SecretaryAttachmentId New() => From(Guid.CreateVersion7());
}

public partial class SuSecretaryOwner : AuditableEntity<SecretaryOwnerId>, IHasActivityInfo
{
    public override SecretaryOwnerId Id { get; init; }

    public bool IsPositionType { get; private set; }

    public UserId? SuUserId { get; init; }

    public BusinessUnitId? BusinessUnitId { get; private set; }

    public string? BusinessUnitName { get; private set; }

    public string? UserFullName { get; private set; }

    public string? EmployeeCode { get; private set; }

    public PositionId? PositionId { get; private set; }

    public string? FullPositionName { get; private set; }

    public virtual SuUser? SuUser { get; init; }

    public virtual IReadOnlyCollection<SuSecretary> Secretaries { get; private set; }

    public virtual IReadOnlyCollection<SuSecretaryAttachment> Attachments { get; private set; }

    public static SuSecretaryOwner Create(
        bool isPositionType,
        UserId? suUserId,
        BusinessUnitId? businessUnitId,
        string? businessUnitName,
        string? userFullName,
        string? employeeCode,
        PositionId? positionId,
        string? fullPositionName)
    {
        return new SuSecretaryOwner
        {
            Id = SecretaryOwnerId.New(),
            IsPositionType = isPositionType,
            SuUserId = suUserId,
            BusinessUnitId = businessUnitId,
            BusinessUnitName = businessUnitName,
            UserFullName = userFullName,
            EmployeeCode = employeeCode,
            PositionId = positionId,
            FullPositionName = fullPositionName,
        };
    }

    public Unit Update(string? userFullName, string? fullPositionName, string? businessUnitName)
    {
        this.UserFullName = userFullName;
        this.FullPositionName = fullPositionName;
        this.BusinessUnitName = businessUnitName;

        return unit;
    }

    public Unit AddSecretary(SuSecretary secretary)
    {
        var list = (this.Secretaries ?? Enumerable.Empty<SuSecretary>()).ToList();
        list.Add(secretary);
        this.Secretaries = list;

        return unit;
    }

    public Unit RemoveSecretary(SuSecretary secretary)
    {
        this.Secretaries = this.Secretaries.Where(s => s.Id != secretary.Id).ToList();

        return unit;
    }

    public Unit AddAttachment(SuSecretaryAttachment attachment)
    {
        var list = (this.Attachments ?? Enumerable.Empty<SuSecretaryAttachment>()).ToList();
        list.Add(attachment);
        this.Attachments = list;

        return unit;
    }

    public Unit RemoveAttachmentById(Guid attachmentId)
    {
        var remaining = this.Attachments
                            .Where(w => w.Id != SecretaryAttachmentId.From(attachmentId))
                            .OrderBy(o => o.Sequence)
                            .ToList();

        for (var i = 0; i < remaining.Count; i++)
        {
            remaining[i].Sequence = i + 1;
        }

        this.Attachments = remaining;

        return unit;
    }
}

public class SuSecretary : AuditableEntity<SecretaryId>
{
    public override SecretaryId Id { get; init; }

    public SecretaryOwnerId SecretaryOwnerId { get; init; }

    public UserId SuUserId { get; init; }

    public int Sequence { get; private set; }

    public string? UserFullName { get; private set; }

    public string? EmployeeCode { get; private set; }

    public PositionId? PositionId { get; private set; }

    public string? FullPositionName { get; private set; }

    public bool? Active { get; private set; }

    public DateOnly? EffectiveStartDate { get; private set; }

    public DateOnly? EffectiveEndDate { get; private set; }

    public virtual SuSecretaryOwner SecretaryOwner { get; init; }

    public virtual SuUser SuUser { get; init; }

    public static SuSecretary Create(
        SecretaryOwnerId secretaryOwnerId,
        UserId suUserId,
        int sequence,
        string? userFullName,
        string? employeeCode,
        PositionId? positionId,
        string? fullPositionName,
        bool? active,
        DateOnly? effectiveStartDate,
        DateOnly? effectiveEndDate)
    {
        return new SuSecretary
        {
            Id = SecretaryId.New(),
            SecretaryOwnerId = secretaryOwnerId,
            SuUserId = suUserId,
            Sequence = sequence,
            UserFullName = userFullName,
            EmployeeCode = employeeCode,
            PositionId = positionId,
            FullPositionName = fullPositionName,
            Active = active,
            EffectiveStartDate = effectiveStartDate,
            EffectiveEndDate = effectiveEndDate,
        };
    }

    public Unit Update(
        int sequence,
        bool? active,
        DateOnly? effectiveStartDate,
        DateOnly? effectiveEndDate)
    {
        this.Sequence = sequence;
        this.Active = active;
        this.EffectiveStartDate = effectiveStartDate;
        this.EffectiveEndDate = effectiveEndDate;

        return unit;
    }
}

public class SuSecretaryAttachment : AuditableEntity<SecretaryAttachmentId>
{
    public override SecretaryAttachmentId Id { get; init; }

    public SecretaryOwnerId SecretaryOwnerId { get; init; }

    public FileId FileId { get; init; }

    public string FileName { get; set; }

    public int Sequence { get; set; }

    public bool IsPublic { get; set; }

    public string? DocumentTypeCode { get; set; }

    public string? Remark { get; set; }

    public virtual SuSecretaryOwner SecretaryOwner { get; private set; }

    public static SuSecretaryAttachment Create(
        SecretaryOwnerId secretaryOwnerId,
        FileId fileId,
        string fileName,
        int sequence,
        string? documentTypeCode,
        string? remark,
        bool isPublic = true)
    {
        return new SuSecretaryAttachment
        {
            Id = SecretaryAttachmentId.New(),
            SecretaryOwnerId = secretaryOwnerId,
            FileId = fileId,
            FileName = fileName,
            Sequence = sequence,
            IsPublic = isPublic,
            DocumentTypeCode = documentTypeCode,
            Remark = remark,
        };
    }
}
